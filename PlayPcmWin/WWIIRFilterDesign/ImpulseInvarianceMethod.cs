using WWMath;
using System;
using System.Collections.Generic;

namespace WWIIRFilterDesign {
    public class ImpulseInvarianceMethod {

        // y[1] : z^{-2}の項
        // y[1] : z^{-1}の項
        // y[0] : 定数項
        private HighOrderRationalPolynomial mH_z;
        public HighOrderRationalPolynomial HzCombined() {
            return mH_z;
        }

        private List<FirstOrderRationalPolynomial> mHzList = new List<FirstOrderRationalPolynomial>();

        public int HzCount() {
            return mHzList.Count;
        }

        public FirstOrderRationalPolynomial Hz(int nth) {
            return mHzList[nth];
        }

        private double mSamplingFrequency;
        public double SamplingFrequency() {
            return mSamplingFrequency;
        }

        public WWMath.Functions.TransferFunctionDelegate TransferFunction;


        /// <summary>
        /// Design of Discrete-time IIR filters from continuous-time filter using impulse invariance method
        /// A. V. Oppenheim, R. W. Schafer, Discrete-Time Signal Processing, 3rd Ed, Prentice Hall, 2009
        /// pp. 526 - 529
        /// </summary>
        public ImpulseInvarianceMethod(List<FirstOrderRationalPolynomial> H_s,
                double ωc, double sampleFreq) {
            mSamplingFrequency = sampleFreq;
            /*
             * H_sはノーマライズされているので、戻す。
             * 
             *     b          b * ωc
             * ────────── = ────────────
             *  s/ωc - a     s - a * ωc
             */

            double td = 1.0 / sampleFreq;

            foreach (var pS in H_s) {
                WWComplex sktd;
                if (pS.DenomOrder() == 0) {
                    System.Diagnostics.Debug.Assert(pS.D(0).EqualValue(WWComplex.Unity()));
                    // ? 
                    // a * u[t] → exp^(a)
                    sktd = WWComplex.Minus(WWComplex.Mul(WWComplex.Unity(), ωc * td));
                } else {
                    sktd = WWComplex.Minus(WWComplex.Mul(pS.D(0), ωc * td));
                }

                // e^{sktd} = e^{real(sktd)} * e^{imag{sktd}}
                //          = e^{real(sktd)} * ( cos(imag{sktd}) + i*sin(imag{sktd})
                var expsktd = new WWComplex(
                    Math.Exp(sktd.real) * Math.Cos(sktd.imaginary),
                    Math.Exp(sktd.real) * Math.Sin(sktd.imaginary));

                // y[1] : z^{-1}の項
                // y[0] : 定数項
                var pZ = new FirstOrderRationalPolynomial(
                    WWComplex.Zero(), WWComplex.Mul(pS.N(0), ωc * td),
                    WWComplex.Minus(expsktd), WWComplex.Unity());

                mHzList.Add(pZ);
            }

            mH_z = new HighOrderRationalPolynomial(mHzList[0]);
            for (int i = 1; i < mHzList.Count; ++i) {
                mH_z = WWPolynomial.Add(mH_z, mHzList[i]);
            }

            //Console.WriteLine(mH_z.ToString("z", WWUtil.SymbolOrder.Inverted));

            TransferFunction = (WWComplex z) => { return TransferFunctionValue(z); };
        }

        private WWComplex TransferFunctionValue(WWComplex z) {
#if true
            // mHzListは1次有理多項式の和の形になっている。
            var zRecip = WWComplex.Reciprocal(z);
            var result = WWComplex.Zero();
            foreach (var H in mHzList) {
                var numer = WWComplex.Add(H.N(0), WWComplex.Mul(H.N(1), zRecip));
                var denom = WWComplex.Add(H.D(0), WWComplex.Mul(H.D(1), zRecip));
                result = WWComplex.Add(result, WWComplex.Div(numer, denom));
            }
            return result;
#else
            // mH_zは1個に合体した有理多項式で計算。

            var zN = WWComplex.Unity();
            var numer = WWComplex.Zero();
            for (int i = 0; i < mH_z.NumerOrder()+1; ++i) {
                numer = WWComplex.Add(numer, WWComplex.Mul(mH_z.N(i), zN));
                zN = WWComplex.Div(zN, z);
            }

            zN = WWComplex.Unity();
            var denom = WWComplex.Zero();
            for (int i = 0; i < mH_z.DenomOrder() + 1; ++i) {
                denom = WWComplex.Add(denom, WWComplex.Mul(mH_z.D(i), zN));
                zN = WWComplex.Div(zN, z);
            }
            return WWComplex.Div(numer, denom);
#endif
        }

    }
}
