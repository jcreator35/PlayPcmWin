// 日本語。

namespace WWMath {
    public class WWStatistics {

        /// <summary>
        /// 平均値を求める。
        /// </summary>
        static public float Mean(float[] v) {
            double mean;
            {
                double acc = 0;
                for (int i = 0; i < v.Length; ++i) {
                    acc += v[i];
                }
                mean = acc / v.Length;
            }
            return (float)mean;
        }

        /// <summary>
        /// 平均値と分散を求める。
        /// </summary>
        static public void MeanAndVariance(float[] v, out float meanOut, out float varianceOut) {
            // 平均値meanを求める。
            double mean;
            {
                double acc = 0;
                for (int i = 0; i < v.Length; ++i) {
                    acc += v[i];
                }
                mean = acc / v.Length;
            }

            // 分散varianceを求める。
            double variance;
            {
                double accSqD = 0;
                for (int i = 0; i < v.Length; ++i) {
                    double d = v[i] - mean;
                    accSqD += d * d;
                }
                variance = accSqD / v.Length;
            }

            meanOut = (float)mean;
            varianceOut = (float)variance;
        }

        /// <summary>
        /// 分散を求める。
        /// 平均値も欲しい場合MeanAndVariance()を使用して下さい。
        /// </summary>
        static public float Variance(float[] v) {
            // 平均値meanを求める。
            double mean;
            {
                double acc = 0;
                for (int i = 0; i < v.Length; ++i) {
                    acc += v[i];
                }
                mean = acc / v.Length;
            }

            // 分散varianceを求める。
            double variance;
            {
                double accSqD = 0;
                for (int i = 0; i < v.Length; ++i) {
                    double d = v[i] - mean;
                    accSqD += d * d;
                }
                variance = accSqD / v.Length;
            }

            return (float)variance;
        }
    }
}
