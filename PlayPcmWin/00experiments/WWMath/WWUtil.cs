using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class WWUtil {
        /// <summary>
        /// 1乗以上の項の係数項の表示。
        /// </summary>
        public static string FirstCoeffToString(WWComplex c, string variableString) {
            if (c.EqualValue(new WWComplex(1, 0))) {
                return variableString; // 係数を表示せず、変数のみ表示する。

            }
            
            if (c.EqualValue(new WWComplex(-1, 0))) {
                return string.Format("-{0}", variableString);
            }

            if (c.imaginary == 0) {
                return string.Format("{0}{1}", c.real ,variableString);
            }

            if (c.EqualValue(new WWComplex(0, 1))) {
                return string.Format("i{0}", variableString);
            }

            if (c.EqualValue(new WWComplex(0, -1))) {
                return string.Format("-i{0}", variableString);
            }

            if (c.real == 0) {
                return string.Format("{0}i{1}", c.imaginary, variableString);
            }

            return string.Format("({0}){1}", c, variableString);
        }

        public static string ContinuedCoeffToString(WWComplex c, string variableString) {
            if (c.EqualValue(new WWComplex(1, 0))) {
                return string.Format(" +{0}", variableString);
            }

            if (c.EqualValue(new WWComplex(-1, 0))) {
                return string.Format(" -{0}", variableString);
            }

            if (c.imaginary == 0) {
                if (c.real < 0) {
                    return string.Format(" {0}{1}", c.real, variableString);
                } else {
                    return string.Format(" +{0}{1}", c.real, variableString);
                }
            }

            if (c.EqualValue(new WWComplex(0, 1))) {
                return string.Format(" +i{0}", variableString);
            }

            if (c.EqualValue(new WWComplex(0, -1))) {
                return string.Format(" -i{0}", variableString);
            }

            if (c.real == 0) {
                if (c.imaginary < 0) {
                    return string.Format(" {0}i{1}", c.imaginary, variableString);
                } else {
                    return string.Format(" +{0}i{1}", c.imaginary, variableString);
                }
            }

            return string.Format(" +({0}){1}", c, variableString);
        }

        /// <summary>
        /// 1乗以上の項が存在するときに、そのあとにつなげて書くときの定数項の値表示。
        /// </summary>
        public static string ZeroOrderCoeffToString(WWComplex c) {
            if (c.Magnitude() == 0) {
                return "";
            }

            if (c.imaginary == 0) {
                if (c.real < 0) {
                    return string.Format(" {0}", c.real);
                } else {
                    return string.Format(" +{0}", c.real);
                }
            }

            if (c.real == 0) {
                if (c.imaginary == 1) {
                    return " i";
                }

                if (c.imaginary == -1) {
                    return " -i";
                }

                if (c.imaginary < 0) {
                    return string.Format(" {0}i", c.imaginary);
                } else {
                    return string.Format(" +{0}i", c.imaginary);
                }
            }

            if (c.real < 0) {
                return c.ToString();
            } else {
                return string.Format(" +{0}", c.ToString());
            }
        }

        /// <summary>
        /// output string represents "c1x + c0"
        /// </summary>
        public static string PolynomialToString(WWComplex c1, WWComplex c0, string variableSymbol) {
            if (c1.Magnitude() == 0) {
                return string.Format("{0}", c0);
            }

            return string.Format("{0}{1}", FirstCoeffToString(c1, variableSymbol), ZeroOrderCoeffToString(c0));
        }

        /// <summary>
        /// output string represents "c2x^2 + c1x + c0"
        /// </summary>
        public static string PolynomialToString(WWComplex c2, WWComplex c1, WWComplex c0, string variableSymbol) {
            if (c2.Magnitude() == 0) {
                // 1乗以下の項のみ。
                return PolynomialToString(c1, c0, variableSymbol);
            }

            if (c1.Magnitude() == 0 && c0.Magnitude() == 0) {
                // 2乗の項のみ。
                return FirstCoeffToString(c2, string.Format("{0}^2", variableSymbol));
            }

            if (c0.Magnitude() == 0) {
                // 2乗の項と1乗の項。
                return string.Format("{0}{1}",
                    FirstCoeffToString(c2, string.Format("{0}^2", variableSymbol)),
                    ContinuedCoeffToString(c1, variableSymbol));
            }

            if (c1.Magnitude() == 0) {
                // 2乗の項と定数項。
                return string.Format("{0}{1}",
                    FirstCoeffToString(c2, string.Format("{0}^2", variableSymbol)),
                    ZeroOrderCoeffToString(c0));
            }

            // 2乗＋1乗＋定数
            return string.Format("{0}{1}{2}",
                FirstCoeffToString(c2, string.Format("{0}^2", variableSymbol)),
                ContinuedCoeffToString(c1, variableSymbol),
                ZeroOrderCoeffToString(c0));
        }

    }
}
