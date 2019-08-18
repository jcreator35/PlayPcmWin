using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class WWMathUtil {
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
                // 実数。
                return string.Format("{0}{1}", c.real ,variableString);
            }

            if (c.EqualValue(new WWComplex(0, 1))) {
                return string.Format("{0}{1}", WWComplex.imaginaryUnit, variableString);
            }

            if (c.EqualValue(new WWComplex(0, -1))) {
                return string.Format("-{0}{1}", WWComplex.imaginaryUnit, variableString);
            }

            if (c.real == 0) {
                // 純虚数。
                return string.Format("{0}{1}{2}", c.imaginary, WWComplex.imaginaryUnit, variableString);
            }

            // 純虚数ではない虚数。
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
                return string.Format(" +{0}{1}", WWComplex.imaginaryUnit, variableString);
            }

            if (c.EqualValue(new WWComplex(0, -1))) {
                return string.Format(" -{0}{1}", WWComplex.imaginaryUnit, variableString);
            }

            if (c.real == 0) {
                if (c.imaginary < 0) {
                    return string.Format(" {0}{1}{2}", c.imaginary, WWComplex.imaginaryUnit, variableString);
                } else {
                    return string.Format(" +{0}{1}{2}", c.imaginary, WWComplex.imaginaryUnit, variableString);
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
                    return string.Format(" {0}", WWComplex.imaginaryUnit);
                }

                if (c.imaginary == -1) {
                    return string.Format(" -{0}", WWComplex.imaginaryUnit);
                }

                if (c.imaginary < 0) {
                    return string.Format(" {0}{1}", c.imaginary, WWComplex.imaginaryUnit);
                } else {
                    return string.Format(" +{0}{1}", c.imaginary, WWComplex.imaginaryUnit);
                }
            }

            if (c.real < 0) {
                return c.ToString();
            } else {
                return string.Format(" +{0}", c);
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

        public enum SymbolOrder {
            NonInverted,
            Inverted
        };

        public static string PolynomialToString(WWComplex[] pCoeffs, string variableSymbol, SymbolOrder invert) {
            var sb = new StringBuilder();
            bool bFirst = true;

            if (invert == SymbolOrder.Inverted) {
                for (int i = 0; i < pCoeffs.Length; ++i) {
                    var p = pCoeffs[i];
                    if (p.AlmostZero()) {
                        continue;
                    }

                    if (!bFirst) {
                        sb.Append(" + ");
                    } else {
                        bFirst = false;
                    }

                    if (i == 0) {
                        sb.AppendFormat("({0})", p);
                    } else {
                        sb.AppendFormat("({0})*{1}^({2})", p, variableSymbol, -i);
                    }
                }
            } else {
                for (int i = pCoeffs.Length - 1; 0 <= i; --i) {
                    var p = pCoeffs[i];
                    if (p.AlmostZero()) {
                        continue;
                    }

                    if (!bFirst) {
                        sb.Append(" + ");
                    } else {
                        bFirst = false;
                    }

                    if (i == 0) {
                        sb.AppendFormat("({0})", p);
                    } else {
                        sb.AppendFormat("({0})*{1}^{2}", p, variableSymbol, i);
                    }
                }
            }

            if (bFirst) {
                sb.Append("0");
            }

            return sb.ToString();
        }

    }
}
