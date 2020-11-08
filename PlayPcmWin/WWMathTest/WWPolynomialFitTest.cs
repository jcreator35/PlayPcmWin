// 日本語。
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WWMath;

namespace WWMathTest {
    [TestClass]
    public class WWPolynomialFitTest {
        [TestMethod]
        public void PolynomialFitTest1() {
            var x = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var y_dB = new double[] {
                0, -0.37,-1.29,-2.43,-3.54,
                -4.53,-5.38,-6.09,-6.69,-7.19,
                -7.6,-7.95,-8.24,-8.49,-8.71,
                -8.89,-9.04,-9.18,-9.3,-9.4,
                -9.49,
            };

#if true
            var r = WWPolynomialFit.Calc(y_dB, x, 6);

            /*
             Result:
             
             y_dB = -0.0000029212346025337816 x^6
                 +0.00020291497408909238 x^5
                 -0.0054888099286801205 x^4
                 +0.071110615465301924 x^3
                 -0.40078216359333169 x^2
                 -0.11354738870338571 x
                 +0.028327961846712043
            */
#else
            var y = new double[y_dB.Length];
            for (int i = 0; i < y.Length; ++i) {
                y[i] = Math.Pow(10, y_dB[i] / 20.0);
            }

            var r = WWPolynomialFit.Calc(y, x, 6);

            /*
             Result:
             
             y = -0.00000033828719542729184 x^6
                 +0.000022513672620503619 x^5
                 -0.00057304970809784746 x^4
                 +0.0067677257436163374 x^3
                 -0.032232583173533474 x^2
                 -0.031365202488051289 x
                 +1.0053057661304232
            */
#endif
            Assert.AreEqual(r.c.Length, 7);
        }
    }
}
