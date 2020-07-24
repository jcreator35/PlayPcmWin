using PcmDataLib;
using System;
using System.Collections.Generic;
using System.Windows.Markup;
using WWDirectCompute12;
using WWFlacRWCS;
using WWUtil;

namespace WWArbitraryResampler {
    class Converter {
        WWResampleGpu mResampleGpu = new WWResampleGpu();

        public int Init() {
            return mResampleGpu.Init();
        }

        public int UpdateAdapterList() {
            return mResampleGpu.UpdateAdapterList();
        }

        public void Term() {
            mResampleGpu.Term();
        }

        public List<WWResampleGpu.AdapterDesc> AdapterList { get { return mResampleGpu.AdapterList; } }

        public enum EventCallbackTypes {
            Started,
            InitGpuAdapterFailed,
            ReadCompleted,
            ReadFailed,
            PrepareDataCompleted,
            ConvProgress,
            ConvertFailed,
            WriteStarted,
            WriteCompleted,
            WriteFailed,
        }

        public class EventCallbackArgs {
            public EventCallbackTypes mCBT;
            public int mProgressPercentage = 0;
            public int mHr = 0;
            public EventCallbackArgs(EventCallbackTypes t, int progressPercentage, int hr) {
                mCBT = t;
                mProgressPercentage = progressPercentage;
                mHr = hr;
            }
        };

        public delegate void EventCallback(EventCallbackArgs a);

        private EventCallback mCB = null;

        public void UpdateEventCallback(EventCallback cb) {
            mCB = cb;
        }


        public class ConvertArgs {
            public int mGpuIdx;
            public string mInPath;
            public string mOutPath;
            public double mScale = 1.0;
            public ConvertArgs(int gpuIdx, string inPath, string outPath, double scale) {
                mGpuIdx = gpuIdx;
                mInPath = inPath;
                mOutPath = outPath;
                mScale = scale;
            }
        };

        private const int PROGRESS_STARTED = 5;
        private const int PROGRESS_READ_END = 10;
        private const int PROGRESS_PREPARE_END = 20;
        private const int PROGRESS_CONV_START = PROGRESS_PREPARE_END;
        private const int PROGRESS_CONV_END = 90;

        private const int SINC_CONVOLUTION_N = 2047;

        /// @brief Dispatch()が一度に何サンプル出力するか。(convolutionNとは関係ない)
        private const int GPU_WORK_COUNT = 4096;

        private int Convert1(ConvertArgs ca) {
            int hr = 0;

            { 
                var cba = new EventCallbackArgs(EventCallbackTypes.Started, PROGRESS_STARTED, hr);
                mCB(cba);
            }

            hr = mResampleGpu.ChooseAdapter(ca.mGpuIdx);
            if (hr < 0) {
                var cba = new EventCallbackArgs(EventCallbackTypes.InitGpuAdapterFailed, 0, hr);
                mCB(cba);
                return hr;
            }

            var flacR = new FlacRW();
            
            hr = flacR.DecodeAll(ca.mInPath);
            if (hr < 0) {
                var cba = new EventCallbackArgs(EventCallbackTypes.ReadFailed, 0, hr);
                mCB(cba);
                return hr;
            } else {
                var cba = new EventCallbackArgs(EventCallbackTypes.ReadCompleted, PROGRESS_READ_END, hr);
                mCB(cba);
            }

            Metadata metaR;
            flacR.GetDecodedMetadata(out metaR);

            var inPcmOfCh = new List<float[]>();

            for (int ch=0; ch<metaR.channels; ++ch) {
                var pcm = new LargeArray<byte>(metaR.BytesPerSample * metaR.totalSamples);

                int fragmentSamples = 1024 * 1024;
                for (long posSamples=0; posSamples < metaR.totalSamples; posSamples += fragmentSamples) {
                    int copySamples = fragmentSamples;
                    if ((metaR.totalSamples - posSamples) < copySamples) {
                        copySamples = (int)(metaR.totalSamples - posSamples);
                    }
                    var b = new byte[metaR.BytesPerSample * copySamples];
                    flacR.GetPcmOfChannel(ch, posSamples, ref b, copySamples);
                    pcm.CopyFrom(b, 0,
                        metaR.BytesPerSample * posSamples, metaR.BytesPerSample * copySamples);
                }

                var pcmData = new PcmData();
                pcmData.SetFormat(1, metaR.bitsPerSample, metaR.bitsPerSample,
                    metaR.sampleRate, PcmData.ValueRepresentationType.SInt,
                    metaR.totalSamples);
                pcmData.SetSampleLargeArray(pcm);

                var fb = new float[metaR.totalSamples];
                for (long i=0; i <metaR.totalSamples; ++i) {
                    fb[i] = pcmData.GetSampleValueInFloat(0, i);
                }
                pcmData = null;
                pcm = null;

                inPcmOfCh.Add(fb);
            }

            {
                var cba = new EventCallbackArgs(EventCallbackTypes.PrepareDataCompleted, PROGRESS_PREPARE_END, hr);
                mCB(cba);
            }


            System.Diagnostics.Debug.Assert(0.5 <= ca.mScale & ca.mScale <= 2.0);
            int sampleRateTo = (int)(ca.mScale * metaR.sampleRate);
            int sampleTotalTo = (int)(ca.mScale * metaR.totalSamples);

            // metaW: 出力フォーマット。
            var metaW = new Metadata(metaR);
            if (ca.mScale < 1.0) {
                // 曲の長さを縮めると、エイリアシング雑音が出るのでローパスフィルターが必要になる。
                // 出力サンプルレートを倍にしてローパスフィルターを省略。
                sampleRateTo = (int)(2.0*ca.mScale * metaR.sampleRate);
                sampleTotalTo = (int)(2.0*ca.mScale * metaR.totalSamples);

                metaW.sampleRate *= 2;
            }

            metaW.bitsPerSample = 24;
            metaW.totalSamples = sampleTotalTo;

            // ローパスフィルターが不要になる条件。
            System.Diagnostics.Debug.Assert(metaR.sampleRate <= metaW.sampleRate);

            var outPcmOfCh = new List<float[]>();

            for (int ch = 0; ch < metaR.channels; ++ch) {
                var inPcm = inPcmOfCh[ch];
                mResampleGpu.Setup(SINC_CONVOLUTION_N, inPcm,
                    (int)metaR.totalSamples, metaR.sampleRate, sampleRateTo, sampleTotalTo);
                for (int i = 0; i < sampleTotalTo; i += GPU_WORK_COUNT) {
                    // 出力サンプル数countの調整。
                    int count = GPU_WORK_COUNT;
                    if (sampleTotalTo < i + count) {
                        count = sampleTotalTo - i;
                    }

                    hr = mResampleGpu.Dispatch(i, count);
                    if (hr < 0) {
                        var cba = new EventCallbackArgs(EventCallbackTypes.ConvertFailed, 0, hr);
                        mCB(cba);
                        return hr;
                    } else {
                        float progress0to1 = ((ch+1.0f) / metaR.channels)
                            * ((float)i / sampleTotalTo);
                        int percent = (int)(PROGRESS_CONV_START + 
                            progress0to1 * (PROGRESS_CONV_END - PROGRESS_CONV_START));
                        var cba = new EventCallbackArgs(EventCallbackTypes.ConvProgress, percent, hr);
                        mCB(cba);
                    }
                }

                var outPcm = new float[sampleTotalTo];
                mResampleGpu.ResultGetFromGpuMemory(outPcm);
                outPcmOfCh.Add(outPcm);

                mResampleGpu.Unsetup();
            }

            {
                var cba = new EventCallbackArgs(EventCallbackTypes.WriteStarted, PROGRESS_CONV_END, hr);
                mCB(cba);
            }

            var flacW = new FlacRW();

            hr = flacW.EncodeInit(metaW);
            if (hr < 0) {
                var cba = new EventCallbackArgs(EventCallbackTypes.WriteFailed, 0, hr);
                mCB(cba);
                return hr;
            }

            if (0 < metaR.pictureBytes) {
                // 画像。
                byte[] metaPicture = null;
                flacR.GetDecodedPicture(out metaPicture, metaR.pictureBytes);
                hr = flacW.EncodeSetPicture(metaPicture);
                if (hr < 0) {
                    var cba = new EventCallbackArgs(EventCallbackTypes.WriteFailed, 0, hr);
                    mCB(cba);
                    return hr;
                }
            }


            for (int ch=0; ch<metaW.channels; ++ch) {
                // 24bitのbyteAry作成。
                var floatAry = outPcmOfCh[ch];
                var byteAry = new LargeArray<byte>(3 * metaW.totalSamples); //< 24bitなので1サンプルあたり3バイト。
                for (long i=0; i<metaW.totalSamples; ++i) {
                    var b = PcmDataUtil.ConvertTo24bitLE(floatAry[i]);
                    byteAry.CopyFrom(b, 0, i*3, 3);
                }

                flacW.EncodeAddPcm(ch, byteAry);
            }

            hr = flacW.EncodeRun(ca.mOutPath);
            if (hr < 0) {
                var cba = new EventCallbackArgs(EventCallbackTypes.WriteFailed, 0, hr);
                mCB(cba);
                return hr;
            }

            flacW.EncodeEnd();
            flacR.DecodeEnd();

            return hr;
        }

        public int Convert(ConvertArgs cp, EventCallback cb) {
            Console.WriteLine("Converting {0} {1} {2}x", cp.mInPath, cp.mOutPath, cp.mScale);

            mCB = cb;

            int hr = Convert1(cp);

            mCB = null;
            return hr;
        }

        private int ReadFlac(string path) {
            return 0;
        }

        private int Setup() {
            return 0;
        }

        private int Run(long offs, int count) {
            return 0;
        }

        private int WriteFlac(string path) {
            return 0;
        }


    }
}
