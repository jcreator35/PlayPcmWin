// 日本語。
using System;

namespace WWMath {
    public class WWNormalize {

        /// <summary>
        /// 平均を0、分散を1にして戻す。
        /// </summary>
        /// <param name="v">入力データ。vの内容を変更しない。</param>
        /// <param name="vOut">平均を0、分散を1になるよう加工されたデータ。</param>
        /// <param name="ε">分散が小さいと判断する閾値。</param>
        /// <returns>true:分散を1にすることに成功。false: varianceがほとんど0なので1に拡大できなかった。</returns>
        public bool Normalize(float[] v, out float[] vOut, float ε = 1.0e-5f) {
            System.Diagnostics.Debug.Assert(0.0f < ε);

            // 平均値meanを求める。
            // 分散varianceを求める。
            float mean;
            float variance;
            WWStatistics.MeanAndVariance(v, out mean, out variance);

            vOut = new float[v.Length];

            if (variance < ε) {
                // できるだけ拡大する。
                float denom = (float)Math.Sqrt(variance + ε);

                for (int i = 0; i < v.Length; ++i) {
                    double x = (v[i] - mean) / denom;
                    vOut[i] = (float)x;
                }
                return false;

            } else {
                // 平均を0にし、分散を1にする。
                float denom = (float)Math.Sqrt(variance);

                for (int i = 0; i < v.Length; ++i) {
                    double x = (v[i] - mean) / denom;
                    vOut[i] = (float)x;
                }
                return true;
            }
        }
    }
}
