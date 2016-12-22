using System;

namespace WWMath {
    public class Functions {
        /// <summary>
        /// area cos hyp
        /// 双曲線関数coshの逆関数 y=arcosh(x)の正の解。x1つに対してyが正負計2つ対応するがそのうち正のyの値を戻す。
        /// </summary>
        /// <param name="x">xの定義域は 1≤x </param>
        public static double ArCosHypPositive(double x) {
            return Math.Log(x+Math.Sqrt(x*x-1));
        }

        /// <summary>
        /// area cos hyp
        /// 双曲線関数coshの逆関数 y=arcosh(x)の負の解。x1つに対してyが正負計2つ対応するがそのうち負のyの値を戻す。
        /// </summary>
        /// <param name="x">xの定義域は 1≤x </param>
        public static double ArCosHypNegative(double x) {
            return -Math.Log(x + Math.Sqrt(x * x - 1));
        }

        /// <summary>
        /// area sin hyp
        /// 双曲線関数sinhの逆関数 y=arsinh(x)。xとyは1対1の対応。
        /// </summary>
        /// <returns></returns>
        public static double ArcSinHyp(double x) {
            return Math.Log(x+Math.Sqrt(x*x+1));
        }
    }
}
