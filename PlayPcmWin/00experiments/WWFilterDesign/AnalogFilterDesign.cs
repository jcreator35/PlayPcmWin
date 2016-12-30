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
            InverseChebyshev,
            Cauer
        };

        private static WWComplex InverseLaplaceTransformOne(FirstOrderRationalPolynomial p, double t) {
            if (p.D(1).EqualValue(WWComplex.Zero())
                    && p.D(0).EqualValue(WWComplex.Unity())) {
                if (!p.N(1).EqualValue(WWComplex.Zero())) {
                    throw new NotImplementedException();
                }
                // 1 → δ(t)
                if (t == 0) {
                    return p.N(0);
                }
                return WWComplex.Zero();
            }

            if (p.D(0).EqualValue(WWComplex.Zero())) {
                // b/s → b*u(t)
                return p.N(0);
            }

            // b/(s-a) → b * exp(a * t)
            return WWComplex.Mul(p.N(0),
                new WWComplex(Math.Exp(-t * p.D(0).real) * Math.Cos(-t * p.D(0).imaginary),
                                Math.Exp(-t * p.D(0).real) * Math.Sin(-t * p.D(0).imaginary)));
        }

        /// <summary>
        /// 伝達関数Hを逆ラプラス変換して時刻tのインパルス応答 h(t)を戻す。
        /// </summary>
        private double InverseLaplaceTransformValue(List<FirstOrderRationalPolynomial> Hf, List<FirstOrderRationalPolynomial> Hi, double t) {
            if (t <= 0) {
                return 0;
            }

            // 共役複素数のペアを作って足す。
            WWComplex rvf = new WWComplex(0, 0);
            if ((Hf.Count & 1) == 1) {
                rvf.Add(InverseLaplaceTransformOne(Hf[Hf.Count/2], t));
            }
            if (2 <= Hf.Count) {
                for (int i = 0; i < Hf.Count / 2; ++i) {
                    var p0 = Hf[i];
                    var p1 = Hf[Hf.Count - i - 1];

                    var v0 = InverseLaplaceTransformOne(p0, t);
                    var v1 = InverseLaplaceTransformOne(p1, t);
                    var v = WWComplex.Add(v0, v1);

                    //Console.WriteLine("{0} {1}", i, v);
                    rvf.Add(v);
                }
            }

            WWComplex rvi = new WWComplex(0, 0);
            foreach (var p in Hi) {
                var v = InverseLaplaceTransformOne(p, t);
                rvi.Add(v);
            }

            return WWComplex.Add(rvf, rvi).real;
        }

        /// <summary>
        /// 伝達関数の地点sの値を計算する。
        /// </summary>
        private WWComplex TransferFunctionValue(WWComplex s) {
            WWComplex numer = new WWComplex(mNumeratorConstant, 0);
            for (int i = 0; i < mZeroList.Count; ++i) {
                var b = mZeroList[i];
                numer.Mul(WWComplex.Sub(s, b));
            }

            WWComplex denom = new WWComplex(1, 0);
            for (int i = 0; i < mPoleList.Count; ++i) {
                var a = mPoleList[i];
                denom.Mul(WWComplex.Sub(s, a));
            }
            return WWComplex.Div(numer, denom);
        }

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
            case FilterType.Cauer:
                filter = new CauerDesign(h0, hc, hs, ωc, ωs, betaType);
                break;
            default:
                throw new NotImplementedException();
            }
            mOrder = filter.Order();
            mNumeratorConstant = filter.TransferFunctionConstant();
            //Console.WriteLine("orderPlus1={0}, β={1}", mN, mBeta);

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
                return TransferFunctionValue(WWComplex.Div(s, ωc));
            };

            PoleZeroPlotTransferFunction = (WWComplex s) => {
                return TransferFunctionValue(s);
            };

            {
                // 伝達関数を部分分数展開する。

                var H_Roots = new List<WWComplex>();
                var stepResponseTFRoots = new List<WWComplex>();
                for (int i = 0; i < filter.NumOfPoles(); ++i) {
                    var p = filter.PoleNth(i);
                    H_Roots.Add(p);
                    stepResponseTFRoots.Add(p);
                }
                stepResponseTFRoots.Add(new WWComplex(0, 0));

                var numerCoeffs = new List<WWComplex>();
                if (filter.NumOfZeroes() == 0) {
                    numerCoeffs.Add(new WWComplex(mNumeratorConstant, 0));
                } else {
                    numerCoeffs = WWPolynomial.RootListToCoeffList(mZeroList, new WWComplex(mNumeratorConstant, 0));
                }

                var H_s = WWPolynomial.Reduction(numerCoeffs, H_Roots);

                var H_fraction = WWPolynomial.PartialFractionDecomposition(H_s.numerCoeffList, H_Roots);
                var H_integer = FirstOrderRationalPolynomial.CreateFromCoeffList(H_s.coeffList);

                mH_PFD = FirstOrderRationalPolynomial.Add(H_fraction, H_integer);

                var Hstep_fraction = WWPolynomial.PartialFractionDecomposition(H_s.numerCoeffList, stepResponseTFRoots);

                ImpulseResponseFunction = (double t) => {
                    return InverseLaplaceTransformValue(H_fraction, H_integer, t);
                };

                UnitStepResponseFunction = (double t) => {
                    return InverseLaplaceTransformValue(Hstep_fraction, H_integer, t);
                };

                TimeDomainFunctionTimeScale = 1.0 / filter.CutoffFrequencyHz();

                // 共役複素数のペアを組み合わせて伝達関数の係数を全て実数にする。
                // s平面のjω軸から遠い項から並べる。
                mRealPolynomialList.Clear();
                if ((H_fraction.Count() & 1) == 1) {
                    // 奇数。
                    int center = H_fraction.Count() / 2;
                    mRealPolynomialList.Add(H_fraction[center].CreateCopy());
                    for (int i = 0; i < H_fraction.Count() / 2; ++i) {
                        mRealPolynomialList.Add(WWPolynomial.Mul(
                            H_fraction[center - i - 1], H_fraction[center + i + 1]));
                    }
                } else {
                    // 偶数。
                    int center = H_fraction.Count() / 2;
                    for (int i = 0; i < H_fraction.Count() / 2; ++i) {
                        mRealPolynomialList.Add(WWPolynomial.Mul(
                            H_fraction[center - i - 1], H_fraction[center + i]));
                    }
                }

                {   // integral polynomialは全て実数係数の多項式。単に足す。
                    System.Diagnostics.Debug.Assert(H_integer.Count <= 1);
                    foreach (var p in H_integer) {
                        mRealPolynomialList.Add(p);
                    }
                }

                return true;
            }
        }
    }
}
