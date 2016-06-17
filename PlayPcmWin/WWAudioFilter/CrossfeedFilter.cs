using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace WWAudioFilter {
    class CrossfeedFilter : FilterBase {
        public string FilterFilePath { get; set; }

        private const string FILE_HEADER = "CFD1";

        private double[][] mCoeffs;
        private long mNumSamples;
        private int mNumChannels;
        private int mChannelId;
        private int mCoeffNumChannels;
        private int mCoeffSampleRate;

        private PcmDataLib.LargeArray<double>[] mPcmAllChannels;

        private enum Channels {
            LeftSpeakerToLeftEar,
            LeftSpeakerToRightEar,
            RightSpeakerToLeftEar,
            RightSpeakerToRightEar,
            NUM
        }

        public CrossfeedFilter(string path)
                : base(FilterType.Crossfeed) {
            FilterFilePath = path;

            // ファイルを読み込んでフィルターサイズを調べる
            ReadCrossfeedConfigurationFromFile(FilterFilePath);
        }

        public override bool WaitUntilAllChannelDataAvailable() {
            return true;
        }

        public override void SetChannelPcm(int ch, PcmDataLib.LargeArray<double> inPcm) {
            mPcmAllChannels[ch] = inPcm;
        }

        public override FilterBase CreateCopy() {
            return new CrossfeedFilter(FilterFilePath);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterCrossfeedDesc, FilterFilePath);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", WWUtil.EscapeString(FilterFilePath));
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            return new CrossfeedFilter(tokens[1]);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            if (inputFormat.NumChannels != mCoeffNumChannels) {
                MessageBox.Show("Crossfeed NumChannels Mismatch!");
                return null;
            }
            if (inputFormat.SampleRate != mCoeffSampleRate) {
                MessageBox.Show("Crossfeed SampleRate Mismatch! (among crossfeed coefficient file and input pcm file)");
                return null;
            }
            mNumSamples  = inputFormat.NumSamples;
            mNumChannels = inputFormat.NumChannels;
            mChannelId   = inputFormat.ChannelId;

            mPcmAllChannels = new PcmDataLib.LargeArray<double>[mNumChannels];
            return inputFormat;
        }

        public override long NumOfSamplesNeeded() {
            return mNumSamples;
        }

        private static PcmDataLib.LargeArray<WWComplex>
        Mul(PcmDataLib.LargeArray<WWComplex> a, PcmDataLib.LargeArray<WWComplex> b) {
            if (a.LongLength != b.LongLength) {
                return null;
            }

            var result = new PcmDataLib.LargeArray<WWComplex>(a.LongLength);
            for (long i = 0; i < a.LongLength; ++i) {
                var t = new WWComplex(a.At(i));
                result.Set(i, t.Mul(b.At(i)));
            }

            return result;
        }

        private static PcmDataLib.LargeArray<double>
        Add(PcmDataLib.LargeArray<double> a, PcmDataLib.LargeArray<double> b) {
            if (a.LongLength != b.LongLength) {
                return null;
            }

            var result = new PcmDataLib.LargeArray<double>(a.LongLength);
            for (long i = 0; i < a.LongLength; ++i) {
                result.Set(i, a.At(i) + b.At(i));
            }

            return result;
        }

        private PcmDataLib.LargeArray<double> FFTFir(PcmDataLib.LargeArray<double> inPcm,
                double[] coef, long fftLength) {
            var fft = new WWRadix2FftLargeArray(fftLength);
            var inTime = new PcmDataLib.LargeArray<WWComplex>(fftLength);

            for (long i = 0; i < mNumSamples; ++i) {
                inTime.Set(i, new WWComplex(inPcm.At(i), 0));
            }

            var inFreq = fft.ForwardFft(inTime);
            inTime = null;

            var coefTime = new PcmDataLib.LargeArray<WWComplex>(fftLength);
            for (long i = 0; i < mCoeffs[mChannelId * 2].Length; ++i) {
                coefTime.Set(i, new WWComplex(coef[i], 0));
            }

            var coefFreq = fft.ForwardFft(coefTime);
            coefTime = null;

            var mulFreq = Mul(inFreq, coefFreq);
            inFreq = null;
            coefFreq = null;

            var mulTime = fft.InverseFft(mulFreq);
            mulFreq = null;

            var result = new PcmDataLib.LargeArray<double>(inPcm.LongLength);
            for (int i = 0; i < inPcm.LongLength; ++i) {
                result.Set(i, mulTime.At(i).real);
            }
            mulTime = null;

            return result;
        }

        public override PcmDataLib.LargeArray<double> FilterDo(PcmDataLib.LargeArray<double> inPcm) {
            // この計算で求めるのは、mChannelId==0のとき左耳, mChannelId==1のとき右耳の音。mChannelIdは耳のチャンネル番号。
            // 入力データとしてmPcmAllChannelsが使用できる。mPcmAllChannels[0]==左スピーカーの音、mPcmAllChannels[1]==右スピーカーの音。

            long fftLength = (mNumSamples < mCoeffs[0].Length) ? mCoeffs[0].Length : mNumSamples;
            fftLength = WWUtil.NextPowerOf2(fftLength);

            // 左スピーカーの音=mPcmAllChannels[0]
            // 左スピーカーと耳chの相互作用のCoeff==mCoeffs[ch]
            var leftSpeaker = FFTFir(mPcmAllChannels[0], mCoeffs[mChannelId+0], fftLength);

            // 右スピーカーの音=mPcmAllChannels[1]
            // 右スピーカーと耳chの相互作用のCoeff==mCoeffs[ch+2]
            var rightSpeaker = FFTFir(mPcmAllChannels[1], mCoeffs[mChannelId+2], fftLength);

            var mixed = Add(leftSpeaker, rightSpeaker);
            return mixed;
        }

        private void ReadCrossfeedConfigurationFromFile(string path) {
            using (StreamReader sr = new StreamReader(path)) {
                string header = sr.ReadLine();
                if (0 != FILE_HEADER.CompareTo(header)) {
                    throw new FileFormatException("FileHeader mismatch");
                }

                mCoeffNumChannels = 2;

                if (!Int32.TryParse(sr.ReadLine(), out mCoeffSampleRate) || mCoeffSampleRate <= 0) {
                    throw new FileFormatException("Could not read Sample Rate");
                }

                int count;
                if (!Int32.TryParse(sr.ReadLine(), out count) || count <= 0) {
                    throw new FileFormatException("Could not read coefficient count");
                }

                mCoeffs = new double[(int)Channels.NUM][];
                for (int i = 0; i < (int)Channels.NUM; ++i) {
                    mCoeffs[i] = new double[count];
                }

                for (int i = 0; i < count; ++i) {
                    string line = sr.ReadLine();
                    var tokens = line.Split(',');
                    if (tokens.Length != 4) {
                        throw new FileFormatException("Could not read 4 coefficients on line " + (i + 3));
                    }
                    for (int ch = 0; ch < (int)Channels.NUM; ++ch) {
                        double v;
                        if (!Double.TryParse(tokens[ch], out v)) {
                            throw new FileFormatException("Could not read coefficient value line " + (i+3));
                        }
                        mCoeffs[ch][i] = v;
                    }
                }
            }
        }
    }
}
