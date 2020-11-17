// 日本語。

using WWUtil;
using System;

namespace FIRFilterConsole {
    public class FIRConvolution {
        private float mMaxMagnitude;
        public float MaxMagnitude {
            get {
                return mMaxMagnitude;
            }
        }

        /// <summary>
        /// dataにcoeffsを畳み込む。
        /// </summary>
        public LargeArray<float> Convolution(LargeArray<float> data, float[] coeffs) {
            mMaxMagnitude = 0;

            var r = new LargeArray<float>(data.LongLength);

            // rIdx: 出力サンプル列rの書き込みidx。1づつ増える。
            for (long rIdx = 0; rIdx < data.LongLength; ++rIdx) {
                // v: 畳み込み結果。
                float v = 0;

                // dIdx: dataの読み出し要素番号。
                long dIdx = rIdx - coeffs.Length / 2;

                // cIdx: coeffsの読み出し要素番号。1づつ減る。
                for (int cIdx = coeffs.Length - 1; 0 <= cIdx; --cIdx) {
                    // dataの範囲外を読まないようにする。
                    if (0 <= dIdx && dIdx < data.LongLength) {
                        v += data.At(dIdx++) * coeffs[cIdx];
                    }
                }

                if (mMaxMagnitude < Math.Abs(v)) {
                    mMaxMagnitude = v;
                }

                r.Set(rIdx, v);
            }

            return r;
        }
    }
}
