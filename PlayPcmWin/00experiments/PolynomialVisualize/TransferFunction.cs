using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWAudioFilter;

namespace PolynomialVisualize {
    class TransferFunction {
        public static WWComplex EvalH(double[] numerators, double[] denominators, WWComplex z) {
            
            var zRecip = new WWComplex(z).Reciprocal();

            var zRecip2 = new WWComplex(zRecip).Mul(zRecip);
            var zRecip3 = new WWComplex(zRecip2).Mul(zRecip);
            var zRecip4 = new WWComplex(zRecip3).Mul(zRecip);
            var zRecip5 = new WWComplex(zRecip4).Mul(zRecip);
            var zRecip6 = new WWComplex(zRecip5).Mul(zRecip);
            var zRecip7 = new WWComplex(zRecip6).Mul(zRecip);
            var zRecip8 = new WWComplex(zRecip7).Mul(zRecip);

            var hDenom0 = new WWComplex(denominators[0], 0.0f);
            var hDenom1 = new WWComplex(denominators[1], 0.0f).Mul(zRecip);
            var hDenom2 = new WWComplex(denominators[2], 0.0f).Mul(zRecip2);
            var hDenom3 = new WWComplex(denominators[3], 0.0f).Mul(zRecip3);
            var hDenom4 = new WWComplex(denominators[4], 0.0f).Mul(zRecip4);
            var hDenom5 = new WWComplex(denominators[5], 0.0f).Mul(zRecip5);
            var hDenom6 = new WWComplex(denominators[6], 0.0f).Mul(zRecip6);
            var hDenom7 = new WWComplex(denominators[7], 0.0f).Mul(zRecip7);
            var hDenom8 = new WWComplex(denominators[8], 0.0f).Mul(zRecip8);
            var hDenom = new WWComplex(hDenom0).Add(hDenom1).Add(hDenom2).Add(hDenom3).Add(hDenom4).Add(hDenom5).Add(hDenom6).Add(hDenom7).Add(hDenom8).Reciprocal();

#if false
            const int N = 200;
            var zRecipArray = new WWComplex[N+1];
            zRecipArray[0] = new WWComplex(z).Reciprocal();
            for (int i = 1; i <N+1; ++i) {
                zRecipArray[i] = new WWComplex(zRecipArray[i - 1]).Mul(zRecipArray[0]);
            }

            var hNumer = zRecipArray[N];
#else
            var hNumer0 = new WWComplex(numerators[0], 0.0f);
            var hNumer1 = new WWComplex(numerators[1], 0.0f).Mul(zRecip);
            var hNumer2 = new WWComplex(numerators[2], 0.0f).Mul(zRecip2);
            var hNumer3 = new WWComplex(numerators[3], 0.0f).Mul(zRecip3);
            var hNumer4 = new WWComplex(numerators[4], 0.0f).Mul(zRecip4);
            var hNumer5 = new WWComplex(numerators[5], 0.0f).Mul(zRecip5);
            var hNumer6 = new WWComplex(numerators[6], 0.0f).Mul(zRecip6);
            var hNumer7 = new WWComplex(numerators[7], 0.0f).Mul(zRecip7);
            var hNumer8 = new WWComplex(numerators[8], 0.0f).Mul(zRecip8);
            var hNumer = new WWComplex(hNumer0).Add(hNumer1).Add(hNumer2).Add(hNumer3).Add(hNumer4).Add(hNumer5).Add(hNumer6).Add(hNumer7).Add(hNumer8);
#endif
            var h = new WWComplex(hNumer).Mul(hDenom);

            // 孤立特異点や極で起きる異常を適当に除去する
            if (double.IsNaN(h.Magnitude())) {
                return new WWComplex(0.0f, 0.0f);
            }
            return h;
        }


    }
}
