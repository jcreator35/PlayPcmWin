// 日本語。

using System;
using System.Collections.Generic;

namespace WWMath {
    /// <summary>
    /// Time Dependent Fourier Analysis
    /// Short-time Fourier Transform
    /// </summary>
    public class WWTimeDependentForwardFourierTransform {
        /// <summary>
        /// Hannのほうが性能が良い。
        /// </summary>
        public enum WindowType {
            Bartlett,
            Hann,
        };

        private int        mProcessBlockSize;
        private WindowType mWindowType;
        private double[]   mWindow;
        private int mOutputCounter;
        private WWRadix2Fft mFFT;
        private List<double[]> mInputList = new List<double[]>();

        private double mGain = 1.0;

        /// <summary>
        /// Short-time Fourier Transform for frequency domain DSP.
        /// time domain data → freq domain data.
        /// </summary>
        /// <param name="processBlockSize">FFT size</param>
        public WWTimeDependentForwardFourierTransform(int processBlockSize, WindowType windowType) {
            if (!Functions.IsPowerOfTwo(processBlockSize) || processBlockSize < 4) {
                throw new ArgumentException("processBlockSize should be power of two and 4 or larger int");
            }

            mProcessBlockSize = processBlockSize;
            mOutputCounter = 0;

            PrepareWindow(processBlockSize + 1, windowType);

            mFFT = new WWRadix2Fft(processBlockSize);
        }

        /// <summary>
        /// FFTのゲイン。デフォルト=1.0
        /// </summary>
        public void SetGain(double gain) {
            mGain = gain;
        }

        private void PrepareWindow(int windowSize, WindowType windowType) {
            mWindowType = windowType;

            switch (windowType) {
            case WindowType.Bartlett:
                mWindow = WWWindowFunc.BartlettWindow(mProcessBlockSize + 1);
                break;
            case WindowType.Hann:
                mWindow = WWWindowFunc.HannWindow(mProcessBlockSize + 1);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        public int WantSamples {
            get { return mProcessBlockSize / 2; }
        }

        /// <summary>
        /// returns FFT size
        /// </summary>
        public int ProcessSize {
            get { return mProcessBlockSize; }
        }

        private int InputSampleCount() {
            int n = 0;
            foreach (var item in mInputList) {
                n += item.Length;
            }

            return n;
        }

        /// <summary>
        /// サンプル置き場からnサンプル取り出す。
        /// </summary>
        private double [] GetInputSamples(int n) {
            System.Diagnostics.Debug.Assert(n <= InputSampleCount());

            var r = new double[n];

            int want = n;
            int pos = 0;
            while (0 < want) {
                var x = mInputList[0];
                mInputList.RemoveAt(0);

                if (x.Length <= want) {
                    Array.Copy(x, 0, r, pos, x.Length);

                    pos += x.Length;
                    want -= x.Length;
                } else {
                    // xが欲しいサンプル数よりも多い場合。
                    // 余ったxをmInputListに戻す。
                    Array.Copy(x, 0, r, pos, want);

                    var remain = new double[x.Length - want];
                    Array.Copy(x, want, remain, 0, remain.Length);

                    mInputList.Insert(0, remain);

                    pos += want;
                    want = 0;
                }
            }

            return r;
        }

        public WWComplex[] Process(double[] x) {
            var outBuff = new List<WWComplex[]>();

            {
                var X = Process1(x);
                if (0 < X.Length) {
                    outBuff.Add(X);
                } else {
                    return new WWComplex[0];
                }
            }

            while (ProcessSize <= InputSampleCount()) {
                var X = ProcessNotFirst();
                if (0 < X.Length) {
                    outBuff.Add(X);
                }
            }

            return WWUtil.ListUtils<WWComplex>.ArrayListToArray(outBuff);
        }

        /// <summary>
        /// 入力サンプルを与える。
        /// 必要量が揃ったら出力が出てくる。
        /// </summary>
        private WWComplex[] Process1(double[] x) {
            mInputList.Add(x);
            int n = InputSampleCount();

            if (mOutputCounter == 0) {
                if (n < ProcessSize/2) {
                    // 入力サンプル数が不足しているのでまだ処理を開始できない。
                    return new WWComplex[0];
                }

                // 十分な入力データが集まった。
                return ProcessFirst();
            } else {
                if (n < ProcessSize) {
                    return new WWComplex[0];
                }

                // 十分な入力データが集まった。
                return ProcessNotFirst();
            }
        }

        private WWComplex[] ProcessFirst() {
            // 入力サンプル置き場から後半サンプルを取り出す。
            var x = new double[ProcessSize];
            var lastHalf = GetInputSamples(ProcessSize/2);

            // 前半はclamp。
            double firstValue = lastHalf[0];
            for (int i = 0; i < ProcessSize / 2; ++i) {
                x[i] = firstValue;
            }

            Array.Copy(lastHalf, 0, x, ProcessSize / 2, ProcessSize / 2);

            var X = Process2(x);

            // 後半サンプルを入力サンプル置き場に戻す。
            mInputList.Insert(0, lastHalf);

            ++mOutputCounter;

            return X;
        }

        /// <summary>
        /// すべての入力サンプルを足し終わった後、1回呼ぶ。
        /// 滞留しているデータをすべて処理して出力に出す。
        /// 入力サンプル数よりも多いサンプルが出てくる。
        /// </summary>
        public WWComplex[] Drain() {
            int count = InputSampleCount();

            // 最後のサンプル値をHOLDする。
            var lastSequence = mInputList[mInputList.Count - 1];
            double lastSample = lastSequence[lastSequence.Length - 1];

            // 不足するサンプル数
            int remain = ProcessSize + ProcessSize/2 - count;

            var fillData = new double[remain];
            for (int i = 0; i < remain; ++i) {
                fillData[i] = lastSample;
            }

            mInputList.Add(fillData);

            var outBuff = new List<WWComplex[]>();
            while (ProcessSize <= InputSampleCount()) {
                var X = ProcessNotFirst();
                if (X.Length == 0) {
                    break;
                }
                outBuff.Add(X);
            }

            return WWUtil.ListUtils<WWComplex>.ArrayListToArray(outBuff);
        }

        private WWComplex[] ProcessNotFirst() {
            // 入力サンプル置き場から取り出す。
            var x = GetInputSamples(ProcessSize);
            var lastHalf = new double[ProcessSize / 2];
            Array.Copy(x, ProcessSize / 2, lastHalf, 0, lastHalf.Length);

            var X = Process2(x);

            // 後半サンプルを入力サンプル置き場に戻す。
            mInputList.Insert(0, lastHalf);

            ++mOutputCounter;

            return X;
        }

        private WWComplex[] Process2(double[] x) {
            System.Diagnostics.Debug.Assert(x.Length == ProcessSize);

            // 入力x に窓関数wを掛ける → xw。
            var xw = Functions.Mul(x, 0, mWindow, 0, ProcessSize);

            // DFTする。
            var X = mFFT.ForwardFft(WWComplex.FromRealArray(xw), mGain);

            return X;
        }

    }
}
