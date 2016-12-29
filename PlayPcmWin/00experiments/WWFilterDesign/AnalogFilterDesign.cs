using System;
using System.Collections.Generic;
using System.Linq;
using WWMath;

namespace WWAudioFilter {
    public class AnalogFilterDesign {

        public int Order() {
            return mOrder;
        }

        private int mOrder;
        private double mNumeratorConstant;

        public double NumeratorConstant() {
            return mNumeratorConstant;
        }

        public int NumOfPoles() {
            return mPoleList.Count;
        }

        public int NumOfZeroes() {
            return mZeroList.Count;
        }

        public WWComplex PoleNth(int nth) {
            return mPoleList[nth];
        }

        public WWComplex ZeroNth(int nth) {
            return mZeroList[nth];
        }

        private List<WWComplex> mPoleList = new List<WWComplex>();
        private List<WWComplex> mZeroList = new List<WWComplex>();

        public WWUserControls.Common.TransferFunctionDelegate TransferFunction;
        public WWUserControls.Common.TransferFunctionDelegate PoleZeroPlotTransferFunction;
        public WWUserControls.Common.TimeDomainResponseFunctionDelegate ImpulseResponseFunction;
        public WWUserControls.Common.TimeDomainResponseFunctionDelegate UnitStepResponseFunction;

        public double TimeDomainFunctionTimeScale { get; set; }

        public int RealPolynomialCount() {
            return mRealPolynomialList.Count();
        }

        public RationalPolynomial RealPolynomialNth(int n) {
            return mRealPolynomialList[n];
        }

        private List<RationalPolynomial> mRealPolynomialList = new List<RationalPolynomial>();

        /// <summary>
        /// partial fraction decomposition-ed transfer function
        /// </summary>
        private List<FirstOrderRationalPolynomial> mH_PFD = new List<FirstOrderRationalPolynomial>();

        /// <summary>
        /// polynomial count of partial fraction decomposition-ed transfer function
        /// </summary>
        public int HPfdCount() {
            return mH_PFD.Count();
        }

        /// <summary>
        /// partial fraction decomposition-ed transfer function
        /// </summary>
        public FirstOrderRationalPolynomial HPfdNth(int n) {
            return mH_PFD[n];
        }

        public enum FilterType {
            Butterworth,
            Chebyshev,
            Pascal,
            InverseChebyshev
        };

        /// <summary>
        /// ローパスフィルターの設計。
        /// </summary>
        /// <param name="mG0">0Hzのゲイン (dB)</param>
        /// <param name="mGc">カットオフ周波数のゲイン (dB)</param>
        /// <param name="mGs">ストップバンドのゲイン (dB)</param>
        /// <param name="mFc">カットオフ周波数(Hz)</param>
        /// <param name="mFs">ストップバンドの下限周波数(Hz)</param>
        /// <returns></returns>
        public bool DesignLowpass(double g0, double gc, double gs, double fc, double fs, FilterType ft, ApproximationBase.BetaType betaType) {

            // Hz → rad/s
            double ωc = fc * 2.0 * Math.PI;
            double ωs = fs * 2.0 * Math.PI;

            // dB → linear
            double h0 = Math.Pow(10, g0 / 20);
            double hc = Math.Pow(10, gc / 20);
            double hs = Math.Pow(10, gs / 20);

            ApproximationBase filter;
            switch (ft) {
            case FilterType.Butterworth:
                filter = new ButterworthDesign(h0, hc, hs, ωc, ωs, betaType);
                break;
            case FilterType.Chebyshev:
                filter = new ChebyshevDesign(h0, hc, hs, ωc, ωs, betaType);
                break;
            case FilterType.Pascal:
                filter = new PascalDesign(h0, hc, hs, ωc, ωs, betaType);
                break;
            case FilterType.InverseChebyshev:
                filter = new InverseChebyshevDesign(h0, hc, hs, ωc, ωs, betaType);
                break;
            default:
                throw new NotImplementedException();
            }
            mOrder = filter.Order();
            mNumeratorConstant = filter.TransferFunctionConstant();
            //Console.WriteLine("order={0}, β={1}", mN, mBeta);

            // 伝達関数のポールの位置。
            mPoleList.Clear();
            for (int i = 0; i < filter.NumOfPoles(); ++i) {
                mPoleList.Add(filter.PoleNth(i));
            }

            // 伝達関数の零の位置。
            mZeroList.Clear();
            for (int i = 0; i < filter.NumOfZeroes(); ++i) {
                mZeroList.Add(filter.ZeroNth(i));
            }

            TransferFunction = (WWComplex s) => {
                WWComplex numerator = new WWComplex(mNumeratorConstant, 0);
                for (int i = 0; i < filter.NumOfZeroes(); ++i) {
                    var b = filter.ZeroNth(i);
                    numerator.Mul(WWComplex.Sub(WWComplex.Div(s, ωc), b));
                }

                WWComplex denominator = new WWComplex(1, 0);
                for (int i = 0; i < filter.NumOfPoles(); ++i) {
                    var a = filter.PoleNth(i);
                    denominator.Mul(WWComplex.Sub(WWComplex.Div(s, ωc), a));
                }
                return WWComplex.Div(numerator, denominator);
            };

            PoleZeroPlotTransferFunction = (WWComplex s) => {
                WWComplex numerator = new WWComplex(mNumeratorConstant, 0);
                for (int i = 0; i < filter.NumOfZeroes(); ++i) {
                    var b = filter.ZeroNth(i);
                    numerator.Mul(WWComplex.Sub(s, b));
                }

                WWComplex denominator = new WWComplex(1, 0);
                for (int i = 0; i < filter.NumOfPoles(); ++i) {
                    var a = filter.PoleNth(i);
                    denominator.Mul(WWComplex.Sub(s, a));
                }
                return WWComplex.Div(numerator, denominator);
            };

            {
                // 伝達関数を部分分数展開する。

                var numeratorCoeffs = new List<WWComplex>();
                if (filter.NumOfZeroes() == 0) {
                    numeratorCoeffs.Add(new WWComplex(mNumeratorConstant, 0));
                } else {
                    /*
                    // 多項式の根のリストをexpandして多項式の係数のリストを作る。
                    var rootList = new List<WWComplex>();

                    for (int i = 0; i < filter.NumOfZeroes(); ++i) {
                        var b = filter.ZeroNth(i);
                        rootList.Add(b);
                    }

                    numeratorCoeffs = WWPolynomial.RootListToCoefficientList(rootList, mNumeratorConstant);
                    */
                }

                var H_Roots = new List<WWComplex>();
                var stepResponseTFRoots = new List<WWComplex>();
                for (int i = 0; i < filter.NumOfPoles(); ++i) {
                    var p = filter.PoleNth(i);
                    H_Roots.Add(p);
                    stepResponseTFRoots.Add(p);
                }
                stepResponseTFRoots.Add(new WWComplex(0, 0));

                mH_PFD = WWPolynomial.PartialFractionDecomposition(numeratorCoeffs, H_Roots);
                var stepResponseTFPFD = WWPolynomial.PartialFractionDecomposition(numeratorCoeffs, stepResponseTFRoots);

                /*
                Console.Write("Transfer function (After Partial Fraction Decomposition): H(s) = ");
                for (int i = 0; i < mH_PFD.Count(); ++i) {
                    Console.Write(mH_PFD[i].ToString("s/ωc"));
                    if (i != mH_PFD.Count - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");
                */

                /*
                Console.Write("Unit Step Function (After Partial Fraction Decomposition): Yγ(s) = ");
                for (int i = 0; i < stepResponseTFPFD.Count(); ++i) {
                    Console.Write(stepResponseTFPFD[i].ToString("s/ωc"));
                    if (i != stepResponseTFPFD.Count - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");
                */

                ImpulseResponseFunction = (double t) => {
                    if (t <= 0) {
                        return 0;
                    }

                    // 逆ラプラス変換してインパルス応答関数を得る。
                    WWComplex result = new WWComplex(0, 0);

                    foreach (var item in mH_PFD) {
                        // numerator * exp(denominator * t)
                        result.Add(WWComplex.Mul(item.N(0),
                            new WWComplex(Math.Exp(-t * item.D(0).real) * Math.Cos(-t * item.D(0).imaginary),
                                          Math.Exp(-t * item.D(0).real) * Math.Sin(-t * item.D(0).imaginary))));
                    }

                    return result.real;
                };

                /*
                Console.Write("Impulse Response (frequency normalized): h(t) = ");
                for (int i = 0; i < mH_PFD.Count; ++i) {
                    var item = mH_PFD[i];
                    Console.Write(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.N(0), item.D(0)));
                    if (i != mH_PFD.Count - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");
                */

                UnitStepResponseFunction = (double t) => {
                    if (t <= 0) {
                        return 0;
                    }

                    // 逆ラプラス変換してインパルス応答関数を得る。
                    WWComplex result = new WWComplex(0, 0);

                    foreach (var item in stepResponseTFPFD) {
                        if (item.D(0).EqualValue(new WWComplex(0, 0))) {
                            // b/s → b*u(t)
                            result.Add(item.N(0));
                        } else {
                            // b/(s-a) → b * exp(a * t)
                            result.Add(WWComplex.Mul(item.N(0),
                                new WWComplex(Math.Exp(-t * item.D(0).real) * Math.Cos(-t * item.D(0).imaginary),
                                              Math.Exp(-t * item.D(0).real) * Math.Sin(-t * item.D(0).imaginary))));
                        }
                    }

                    return result.real;
                };

                /*
                Console.Write("Step Response (frequency normalized): y(t) = ");
                for (int i = 0; i < stepResponseTFPFD.Count; ++i) {
                    var item = stepResponseTFPFD[i];
                    if (item.D(0).EqualValue(new WWComplex(0, 0))) {
                        // b/s → b*u(t)
                        Console.Write(string.Format("({0}) * u(t)", item.N(0)));
                    } else {
                        // b/(s-a) → b * exp(a * t)
                        Console.Write(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.N(0), item.D(0)));
                    }
                    if (i != stepResponseTFPFD.Count() - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");
                */

                TimeDomainFunctionTimeScale = 1.0 / filter.CutoffFrequencyHz();

                // 共役複素数のペアを組み合わせて伝達関数の係数を全て実数にする。
                // s平面のjω軸から遠い項から並べる。
                mRealPolynomialList.Clear();
                if ((mH_PFD.Count() & 1) == 1) {
                    // 奇数。
                    int center = mH_PFD.Count() / 2;
                    mRealPolynomialList.Add(mH_PFD[center].CreateCopy());
                    for (int i = 0; i < mH_PFD.Count() / 2; ++i) {
                        mRealPolynomialList.Add(WWPolynomial.Mul(
                            mH_PFD[center - i - 1], mH_PFD[center + i + 1]));
                    }
                } else {
                    // 偶数。
                    int center = mH_PFD.Count() / 2;
                    for (int i = 0; i < mH_PFD.Count() / 2; ++i) {
                        mRealPolynomialList.Add(WWPolynomial.Mul(
                            mH_PFD[center - i - 1], mH_PFD[center + i]));
                    }
                }

                /*
                Console.Write("Transfer function (real coefficients): H(s) = ");
                for (int i = 0; i < mRealPolynomialList.Count(); ++i) {
                    Console.Write(mRealPolynomialList[i].ToString("s"));
                    if (i != mRealPolynomialList.Count() - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");
                */

                return true;
            }
        }
    }
}
