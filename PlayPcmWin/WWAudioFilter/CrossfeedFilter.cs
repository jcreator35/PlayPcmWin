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

        private double[][] mPcmAllChannels;

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

        public override void SetChannelPcm(int ch, double[] inPcm) {
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

            mPcmAllChannels = new double[mNumChannels][];
            return inputFormat;
        }

        public override long NumOfSamplesNeeded() {
            return mNumSamples;
        }

        private WWComplex[] Mul(WWComplex[] a, WWComplex[] b) {
            if (a.Length != b.Length) {
                return null;
            }

            var result = new WWComplex[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                result[i] = a[i].Mul(b[i]);
            }

            return result;
        }

        private double[] Add(double[] a, double[] b) {
            if (a.Length != b.Length) {
                return null;
            }

            var result = new double[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                result[i] = a[i] + b[i];
            }

            return result;
        }

        private double[] FFTFir(double[] inPcm, double[] coef, int fftLength) {
            var inTime = new WWComplex[fftLength];

            for (int i = 0; i < mNumSamples; ++i) {
                inTime[i].real = inPcm[i];
            }

            var inFreq = new WWComplex[fftLength];
            {
                var fft = new WWRadix2Fft(fftLength);
                fft.ForwardFft(inTime, inFreq);
            }
            inTime = null;

            var coefTime = new WWComplex[fftLength];
            for (int i = 0; i < mCoeffs[mChannelId * 2].Length; ++i) {
                coefTime[i].real = coef[i];
            }

            var coefFreq = new WWComplex[fftLength];
            {
                var fft = new WWRadix2Fft(fftLength);
                fft.ForwardFft(coefTime, coefFreq);
            }
            coefTime = null;

            var mulFreq = Mul(inFreq, coefFreq);
            inFreq = null;
            coefFreq = null;

            var mulTime = new WWComplex[fftLength];
            {
                var fft = new WWRadix2Fft(fftLength);
                fft.InverseFft(mulFreq, mulTime);
            }
            mulFreq = null;

            var result = new double[inPcm.Length];
            for (int i = 0; i < inPcm.Length; ++i) {
                result[i] = mulTime[i].real;
            }
            mulTime = null;

            return result;
        }

        public override double[] FilterDo(double[] inPcm) {
            // この計算で求めるのは、mChannelId==0のとき左耳, mChannelId==1のとき右耳の音。mChannelIdは耳のチャンネル番号。
            // 入力データとしてmPcmAllChannelsが使用できる。mPcmAllChannels[0]==左スピーカーの音、mPcmAllChannels[1]==右スピーカーの音。

            int fftLength = ((int)mNumSamples < mCoeffs[0].Length) ? mCoeffs[0].Length : (int)mNumSamples;
            fftLength = WWRadix2Fft.NextPowerOf2(fftLength);

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
