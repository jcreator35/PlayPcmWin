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
            var y = new double[] {
                0, -0.37,-1.29,-2.43,-3.54,
                -4.53,-5.38,-6.09,-6.69,-7.19,
                -7.6,-7.95,-8.24,-8.49,-8.71,
                -8.89,-9.04,-9.18,-9.3,-9.4,
                -9.49,
            };

            var r = WWPolynomialFit.Calc(y, x, 6);

            Assert.AreEqual(r.c.Length, 7);

            /*
             Result:
             
             y = -0.0000029212346025337816 x^6
                 +0.00020291497408909238 x^5
                 -0.0054888099286801205 x^4
                 +0.071110615465301924 x^3
                 -0.40078216359333169 x^2
                 -0.11354738870338571 x
                 +0.028327961846712043
            */
        }
    }
}
