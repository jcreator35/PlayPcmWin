﻿// 日本語。
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WWMath;

namespace WWMathTest {
    [TestClass]
    public class WWPolynomialFitTest {
        [TestMethod]
        public void PolynomialFitTest1() {
            // x: frequency (kHz)
            var x = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            // y_dB: filter gain (dB)
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
             6th degree polynomial fit Result:
             
             y_dB = -0.0000029212346025337816 x^6
                 +0.00020291497408909238 x^5
                 -0.0054888099286801205 x^4
                 +0.071110615465301924 x^3
                 -0.40078216359333169 x^2
                 -0.11354738870338571 x
                 +0.028327961846712043
            */
#endif
#if false
            var r = WWPolynomialFit.Calc(y_dB, x, 8);

            /*
            8th degree polynomial fit result:
            -0.0013195438852613571	
            0.11547946279072106	
            -0.59831464200680629	
            0.13546539561883844	
            -0.015821907177958838	
            0.0011043012083932031	
            -0.0000462875847426081	
            0.0000010754447419795869	
            -0.000000010649146400525852	
            */
#endif
#if false
            var r = WWPolynomialFit.Calc(y_dB, x, 10);

            /*
            10th degree polynomial fit result:
             
            -0.00055308348316090181	
            0.0753739383629172	
            -0.53241087349638294	
            0.094950006549977192	
            -0.0031143640301222503	
            -0.0012030862221463828	
            0.00021049469586661266	
            -0.000016703661726794029	
            0.00000073620724364291522	
            -0.000000017421988336701071	
            0.00000000017311729273344438	
            */
#endif
#if false
            // linear y version (fitting performance becomes worse)
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
