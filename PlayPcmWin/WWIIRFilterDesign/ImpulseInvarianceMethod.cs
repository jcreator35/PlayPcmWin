using WWMath;
using System;
using System.Collections.Generic;

namespace WWIIRFilterDesign {
    public class ImpulseInvarianceMethod {
        // d[1] : z^{-2}の項
        // d[1] : z^{-1}の項
        // d[0] : 定数項
        private HighOrderRationalPolynomial mH_z;

        public WWMath.Functions.TransferFunctionDelegate TransferFunction;

        public HighOrderRationalPolynomial Hz() {
            return mH_z;
        }

        public ImpulseInvarianceMethod(List<FirstOrderRationalPolynomial> H_s, double ωc, double sampleFreq) {
            /*
             * H_sはノーマライズされているので、戻す。
             * 
             *     b          b * ωc
             * ────────── = ────────────
             *  s/ωc - a     s - a * ωc
             */

            double td = 1.0 / sampleFreq;

            var pList = new List<FirstOrderRationalPolynomial>();

            foreach (var pS in H_s) {
                if (pS.DenomOrder() == 0) {
                    throw new NotSupportedException();
                }

                var sktd = WWComplex.Minus(WWComplex.Mul(pS.D(0), ωc * td));

                // e^{sktd} = e^{real(sktd)} * e^{imag{sktd}}
                //          = e^{real(sktd)} * ( cos(imag{sktd}) + i*sin(imag{sktd})
                var expsktd = new WWComplex(
                    Math.Exp(sktd.real) * Math.Cos(sktd.imaginary),
                    Math.Exp(sktd.real) * Math.Sin(sktd.imaginary));

                // d[1] : z^{-1}の項
                // d[0] : 定数項
                var pZ = new FirstOrderRationalPolynomial(
                    WWComplex.Zero(), WWComplex.Mul(pS.N(0), ωc * td),
                    WWComplex.Minus(expsktd), WWComplex.Unity());

                pList.Add(pZ);
            }

            mH_z = new HighOrderRationalPolynomial(pList[0]);
            for (int i = 1; i < pList.Count; ++i) {
                mH_z = WWPolynomial.Add(mH_z, pList[i]);
            }

            //Console.WriteLine(mH_z.ToString("z", WWUtil.SymbolOrder.Inverted));

            TransferFunction = (WWComplex z) => { return TransferFunctionValue(z); };
        }

        private WWComplex TransferFunctionValue(WWComplex z) {
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
        }

    }
}
