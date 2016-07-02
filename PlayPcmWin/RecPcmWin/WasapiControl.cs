using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wasapi;
using System.Globalization;

namespace RecPcmWin {
    class WasapiControl {
        private WasapiCS mWasapi = new WasapiCS();
        List<WasapiCS.DeviceAttributes> mDeviceAttributeList = new List<WasapiCS.DeviceAttributes>();
        private byte[] mCapturedPcmData;
        private int mNextWritePos = 0;
        private Wasapi.WasapiCS.CaptureCallback mCsCaptureCallback;
        private WasapiCS.SampleFormatType mSampleFormat;
        private int mSampleRate;
        private int mNumChannels;

        private bool mRecord = false;

        public delegate void ControlCaptureCallback(byte[] data);
        private ControlCaptureCallback mControlCaptureCallback = null;

        struct InspectFormat {
            public int sampleRate;
            public WasapiCS.SampleFormatType sampleFormat;
            public InspectFormat(int sr, WasapiCS.SampleFormatType sf) {
                sampleRate = sr;
                sampleFormat = sf;
            }
        };

        public readonly int[] mSampleRateList = {
                44100,48000,88200,96000,176400,192000};
        public readonly WasapiCS.SampleFormatType[] mSampleFormatList = {
                WasapiCS.SampleFormatType.Sint16,
                WasapiCS.SampleFormatType.Sint24,
                WasapiCS.SampleFormatType.Sint32V24,
                WasapiCS.SampleFormatType.Sint32};

        public readonly int[] mChannelCountList = {
            2,4,6,8,10,12,16,18,24,26,32,48,64
        };

        public void SetCaptureCallback(ControlCaptureCallback cb) {
            mControlCaptureCallback = cb;
        }

        public bool AllocateCaptureMemory(int bytes) {
            try {
                mCapturedPcmData = null;
                mCapturedPcmData = new byte[bytes];
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return false;
            }

            mNextWritePos = 0;
            return true;
        }

        public void ReleaseCaptureMemory() {
            mCapturedPcmData = null;
            mNextWritePos = 0;
        }

        /// mCapturedPcmData is resized: you must call ReleaseCaptureMemory() and AllocateCaptureMemory() after this call
        public byte[] GetCapturedData() {
            Array.Resize(ref mCapturedPcmData, mNextWritePos);
            return mCapturedPcmData;
        }

        private void CsCaptureCallback(byte[] pcmData) {
            if (pcmData == null || pcmData.Length == 0) {
                return;
            }

            if (mRecord) {
                if (mCapturedPcmData.Length <= mNextWritePos + pcmData.Length) {
                    return;
                }

                Array.Copy(pcmData, 0, mCapturedPcmData, mNextWritePos, pcmData.Length);
                mNextWritePos += pcmData.Length;
            }

            if (mControlCaptureCallback != null) {
                mControlCaptureCallback(pcmData);
            }
        }

        public int GetPosFrame() {
            return mNextWritePos / WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / mNumChannels * 8;
        }

        public int GetNumFrames() {
            return mCapturedPcmData.Length / WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / mNumChannels * 8;
        }

        public bool IsRunning() {
            return mCapturedPcmData != null;
        }

        public int Init() {
            int hr = mWasapi.Init();
            if (hr < 0) {
                return hr;
            }
            
            mCsCaptureCallback = new WasapiCS.CaptureCallback(CsCaptureCallback);
            mWasapi.RegisterCaptureCallback(mCsCaptureCallback);

            return hr;
        }

        public void Term() {
            mWasapi.Stop();
            mWasapi.Unsetup();
            mWasapi.Term();
            ReleaseCaptureMemory();
            mWasapi = null;
        }

        public int EnumerateRecDeviceNames(List<WasapiCS.DeviceAttributes> deviceList) {
            mDeviceAttributeList.Clear();
            deviceList.Clear();

            int hr = mWasapi.EnumerateDevices(WasapiCS.DeviceType.Rec);
            if (hr < 0) {
                return hr;
            }

            int nDevices = mWasapi.GetDeviceCount();
            for (int i = 0; i < nDevices; ++i) {
                var att = mWasapi.GetDeviceAttributes(i);
                mDeviceAttributeList.Add(att);
                deviceList.Add(att);
            }

            return hr;
        }

        public int Setup(int deviceIdx, WasapiCS.DataFeedMode dfm, int wasapiBufferSize,
                int sampleRate, WasapiCS.SampleFormatType sampleFormatType,
                int numChannels, int dwChannelMask) {
            int hr = mWasapi.Setup(deviceIdx, WasapiCS.DeviceType.Rec,
                WasapiCS.StreamType.PCM, sampleRate, sampleFormatType, numChannels, dwChannelMask,
                WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive, dfm, wasapiBufferSize, 0, 10000);
            mSampleFormat = sampleFormatType;
            mSampleRate = sampleRate;
            mNumChannels = numChannels;
            return hr;
        }

        public void Unsetup() {
            mWasapi.Stop();
            mWasapi.Unsetup();
        }

        public void Stop() {
            mWasapi.Stop();
        }

        public bool Run(int millisec) {
            return mWasapi.Run(millisec);
        }

        public string InspectDevice(int deviceIdx, int dwChannelMask) {
            if (deviceIdx < 0 || mDeviceAttributeList.Count <= deviceIdx) {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            string dn = mDeviceAttributeList[deviceIdx].Name;
            string did = mDeviceAttributeList[deviceIdx].DeviceIdString;

            sb.AppendFormat(CultureInfo.InvariantCulture, "wasapi.InspectDevice({0})\r\n"
                +"  DeviceIdString={1}",
                    dn, did);
            sb.Append("\r\n  Available formats:");
            foreach (int ch in mChannelCountList) {
                foreach (int sr in mSampleRateList) {
                    foreach (var fmt in mSampleFormatList) {
                        int hr = mWasapi.InspectDevice(deviceIdx, WasapiCS.DeviceType.Rec, sr, fmt, ch, dwChannelMask);
                        if (0 <= hr) {
                            sb.AppendFormat("\r\n    {0}Hz {2}ch {1,-16}", sr, fmt, ch);
                        }
                    }
                }
            }

            sb.Append("\r\nwasapi.InspectDevice completed.\r\n");

            return sb.ToString();
        }

        public int StartRecording() {
            int hr = mWasapi.StartRecording();
            return hr;
        }

        public void StorePcm(bool b) {
            mRecord = b;
        }

        public long GetCaptureGlitchCount() {
            return mWasapi.GetCaptureGlitchCount();
        }
    }
}
