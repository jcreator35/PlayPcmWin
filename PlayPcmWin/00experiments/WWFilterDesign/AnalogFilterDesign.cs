using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWAudioFilter {
    public class AnalogFilterDesign {

        public int Order() {
            return mOrder;
        }

        public double Beta() {
            return mBeta;
        }

        private int mOrder;
        private double mBeta;
        private double mNumeratorConstant;

        public double NumeratorConstant() {
            return mNumeratorConstant;
        }

        public WWComplex PoleNth(int nth) {
            return mPoleList[nth];
        }

        private List<WWComplex> mPoleList = new List<WWComplex>();

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
        /// バターワースローパスフィルターの設計。
        /// </summary>
        /// <param name="g0">0Hzのゲイン (dB)</param>
        /// <param name="gc">カットオフ周波数のゲイン (dB)</param>
        /// <param name="gs">ストップバンドのゲイン (dB)</param>
        /// <param name="fc">カットオフ周波数(Hz)</param>
        /// <param name="fs">ストップバンドの下限周波数(Hz)</param>
        /// <returns></returns>
        public bool DesignButterworthLowpass(double g0, double gc, double gs, double fc, double fs, ButterworthDesign.BetaType betaType) {

            // Hz → rad/s
            double ωc = fc * 2.0 * Math.PI;
            double ωs = fs * 2.0 * Math.PI;

            // dB → linear
            double h0 = Math.Pow(10, g0 / 20);
            double hc = Math.Pow(10, gc / 20);
            double hs = Math.Pow(10, gs / 20);

            var bwd = new ButterworthDesign(h0, hc, hs, ωc, ωs, betaType);
            mOrder = bwd.Order();
            mBeta = bwd.Beta();
            mNumeratorConstant = bwd.TransferFunctionConstant();
            Console.WriteLine("order={0}, β={1}", mOrder, mBeta);

            // 伝達関数のポールの位置。
            mPoleList.Clear();
            for (int i = 0; i < bwd.Order(); ++i) {
                mPoleList.Add(bwd.PoleNth(i));
            }

            TransferFunction = (WWComplex s) => {
                WWComplex denominator = new WWComplex(1, 0);
                for (int i = 0; i < bwd.Order(); ++i) {
                    var a = bwd.PoleNth(i);
                    denominator.Mul(WWComplex.Sub(WWComplex.Div(s, ωc), a));
                }
                return WWComplex.Div(new WWComplex(mNumeratorConstant, 0), denominator);
            };

            PoleZeroPlotTransferFunction = (WWComplex s) => {
                WWComplex denominator = new WWComplex(1, 0);
                for (int i = 0; i < bwd.Order(); ++i) {
                    var a = bwd.PoleNth(i);
                    denominator.Mul(WWComplex.Sub(s, a));
                }
                return WWComplex.Div(new WWComplex(mNumeratorConstant, 0), denominator);
            };

            {
                // 伝達関数を部分分数展開する。
                var numeratorC = new List<WWComplex>();
                numeratorC.Add(new WWComplex(mNumeratorConstant, 0));

                var H_Roots = new List<WWComplex>();
                var stepResponseTFRoots = new List<WWComplex>();
                for (int i = 0; i < bwd.Order(); ++i) {
                    var p = bwd.PoleNth(i);
                    H_Roots.Add(p);
                    stepResponseTFRoots.Add(p);
                }
                stepResponseTFRoots.Add(new WWComplex(0, 0));
                var H_PFD = WWPolynomial.PartialFractionDecomposition(numeratorC, H_Roots);
                var stepResponseTFPFD = WWPolynomial.PartialFractionDecomposition(numeratorC, stepResponseTFRoots);

                Console.Write("Transfer function (After Partial Fraction Decomposition): H(s) = ");
                for (int i = 0; i < H_PFD.Count(); ++i) {
                    Console.Write(H_PFD[i].ToString("s/ωc"));
                    if (i != H_PFD.Count - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");

                Console.Write("Unit Step Function (After Partial Fraction Decomposition): Yγ(s) = ");
                for (int i = 0; i < stepResponseTFPFD.Count(); ++i) {
                    Console.Write(stepResponseTFPFD[i].ToString("s/ωc"));
                    if (i != stepResponseTFPFD.Count - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");

                ImpulseResponseFunction = (double t) => {
                    if (t <= 0) {
                        return 0;
                    }

                    // 逆ラプラス変換してインパルス応答関数を得る。
                    WWComplex result = new WWComplex(0, 0);

                    foreach (var item in H_PFD) {
                        // numerator * exp(denominator * t)
                        result.Add(WWComplex.Mul(item.NumeratorCoeff(0),
                            new WWComplex(Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Cos(-t * item.DenominatorCoeff(0).imaginary),
                                          Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Sin(-t * item.DenominatorCoeff(0).imaginary))));
                    }

                    return result.real;
                };

                Console.Write("Impulse Response (frequency normalized): h(t) = ");
                for (int i = 0; i < H_PFD.Count; ++i) {
                    var item = H_PFD[i];
                    Console.Write(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.NumeratorCoeff(0), item.DenominatorCoeff(0)));
                    if (i != H_PFD.Count - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");

                UnitStepResponseFunction = (double t) => {
                    if (t <= 0) {
                        return 0;
                    }

                    // 逆ラプラス変換してインパルス応答関数を得る。
                    WWComplex result = new WWComplex(0, 0);

                    foreach (var item in stepResponseTFPFD) {
                        if (item.DenominatorCoeff(0).EqualValue(new WWComplex(0, 0))) {
                            // b/s → b*u(t)
                            result.Add(item.NumeratorCoeff(0));
                        } else {
                            // b/(s-a) → b * exp(a * t)
                            result.Add(WWComplex.Mul(item.NumeratorCoeff(0),
                                new WWComplex(Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Cos(-t * item.DenominatorCoeff(0).imaginary),
                                              Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Sin(-t * item.DenominatorCoeff(0).imaginary))));
                        }
                    }

                    return result.real;
                };

                Console.Write("Step Response (frequency normalized): y(t) = ");
                for (int i = 0; i < stepResponseTFPFD.Count; ++i) {
                    var item = stepResponseTFPFD[i];
                    if (item.DenominatorCoeff(0).EqualValue(new WWComplex(0, 0))) {
                        // b/s → b*u(t)
                        Console.Write(string.Format("({0}) * u(t)", item.NumeratorCoeff(0)));
                    } else {
                        // b/(s-a) → b * exp(a * t)
                        Console.Write(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.NumeratorCoeff(0), item.DenominatorCoeff(0)));
                    }
                    if (i != stepResponseTFPFD.Count() - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");

                TimeDomainFunctionTimeScale = 1.0 / bwd.CutoffFrequency();

                // 共役複素数のペアを組み合わせて伝達関数の係数を全て実数にする。
                // s平面のjω軸から遠い項から並べる。
                mRealPolynomialList.Clear();
                if ((H_PFD.Count() & 1) == 1) {
                    // 奇数。
                    int center = H_PFD.Count() / 2;
                    mRealPolynomialList.Add(H_PFD[center].CreateCopy());
                    for (int i = 0; i < H_PFD.Count() / 2; ++i) {
                        mRealPolynomialList.Add(WWPolynomial.Mul(
                            H_PFD[center - i - 1], H_PFD[center + i + 1]));
                    }
                } else {
                    // 偶数。
                    int center = H_PFD.Count() / 2;
                    for (int i = 0; i < H_PFD.Count() / 2; ++i) {
                        mRealPolynomialList.Add(WWPolynomial.Mul(
                            H_PFD[center - i - 1], H_PFD[center + i]));
                    }
                }

                Console.Write("Transfer function (real coefficients): H(s) = ");
                for (int i = 0; i < mRealPolynomialList.Count(); ++i) {
                    // 周波数スケーリングする。
                    mRealPolynomialList[i].FrequencyScaling(ωc);

                    Console.Write(mRealPolynomialList[i].ToString("s"));
                    if (i != mRealPolynomialList.Count() - 1) {
                        Console.Write(" + ");
                    }
                }
                Console.WriteLine("");

                return true;
            }
        }
    }
}
