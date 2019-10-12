// 日本語。

using System;
using System.Collections.Generic;

namespace WWMath {
    /// <summary>
    /// Time Dependent Fourier Synthesis
    /// Short-time Fourier Transform
    /// </summary>
    public class WWTimeDependentInverseFourierTransform {
        private int        mProcessBlockSize;
        private int mProcessCounter;
        private long mProcessedSamples;
        private WWRadix2Fft mFFT;

        private List<WWComplex[]> mInputList = new List<WWComplex[]>();

        private double[] mOverlapBuff;

        private long mNumSamples = -1;

        public WWTimeDependentInverseFourierTransform(int processBlockSize) {
            if (!Functions.IsPowerOfTwo(processBlockSize) || processBlockSize < 4) {
                throw new ArgumentException("processBlockSize should be power of two and 4 or larger int");
            }

            mProcessBlockSize = processBlockSize;
            mProcessCounter = 0;
            mProcessedSamples = 0;

            mFFT = new WWRadix2Fft(processBlockSize);

            mOverlapBuff = new double[WantSamples];
        }

        public void SetNumSamples(long numSamples) {
            mNumSamples = numSamples;
        }

        public int WantSamples {
            get { return mProcessBlockSize; }
        }

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

        private WWComplex [] GetNextInput(int n) {
            if (InputSampleCount() < n) {
                return new WWComplex[0];
            }

            var r = new WWComplex[n];

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

                    var remain = new WWComplex[x.Length - want];
                    Array.Copy(x, want, remain, 0, remain.Length);

                    mInputList.Insert(0, remain);

                    pos += want;
                    want = 0;
                }
            }

            return r;
        }

        public double[] Process(WWComplex[] X) {
            System.Diagnostics.Debug.Assert(0 < X.Length);
            mInputList.Add(X);

            var outBuff = new List<double[]>();

            while (true) {
                var XF = GetNextInput(WantSamples);
                if (XF.Length == 0) {
                    break;
                }
                var x = Process1(XF);
                if (0 < x.Length) {
                    outBuff.Add(x);
                }
            }

            return WWUtil.ListUtils<double>.ArrayListToArray(outBuff);
        }

        /// <summary>
        /// 周波数ドメインの値を入力。
        /// 時間ドメインの値を出力。
        /// </summary>
        /// <param name="X">周波数ドメインの値。サンプル数＝processBlockSize</param>
        /// <returns>時間ドメインの値。</returns>
        private double[] Process1(WWComplex[] X) {
            System.Diagnostics.Debug.Assert(X.Length == WantSamples);

            var x = WWComplex.ToRealArray(mFFT.InverseFft(X));
            if (0 == mProcessCounter) {
                // 1回目。
                // 前半のデータは埋め草なので破棄。後半のみ意味があるデータ。
                var r = new double[ProcessSize / 2];
                Array.Copy(x, ProcessSize / 2, r, 0, ProcessSize / 2);

                mOverlapBuff = r;

                ++mProcessCounter;

                return new double[0];
            }

            {
                // 2回目以降。
                var r = new double[ProcessSize / 2];
                for (int i = 0; i < r.Length; ++i) {
                    r[i] = x[i] + mOverlapBuff[i];
                }

                // 次回計算用にオーバーラップ部分を保存。
                for (int i = 0; i < r.Length; ++i) {
                    mOverlapBuff[i] = x[ProcessSize/2 + i];
                }

                if (0 <= mNumSamples && mNumSamples < mProcessedSamples + r.Length) {
                    // 必要サンプル数を超えて出力する必要はない。
                    var tmp = new double[mNumSamples - mProcessedSamples];
                    Array.Copy(r, 0, tmp, 0, tmp.Length);
                    r = tmp;
                }

                ++mProcessCounter;
                mProcessedSamples += r.Length;
                return r;
            }
        }
    }
}
