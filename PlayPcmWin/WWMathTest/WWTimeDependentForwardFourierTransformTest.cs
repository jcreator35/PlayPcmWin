using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWTimeDependentForwardFourierTransformTest and is intended
    ///to contain all WWTimeDependentForwardFourierTransformTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWTimeDependentForwardFourierTransformTest {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        private void Test(WWTimeDependentForwardFourierTransform t, WWTimeDependentInverseFourierTransform f, double [] x, int fragmentSize) {
            int iPos = 0;
            int oPos = 0;

            // Processのテスト。
            while (iPos < x.Length) {
                int size = fragmentSize;
                if (x.Length - iPos < size) {
                    size = x.Length - iPos;
                }

                var xF = new double[size];
                Array.Copy(x, iPos, xF, 0, size);
                iPos += size;

                var X = t.Process(xF);
                if (0 < X.Length) {
                    var xR = f.Process(X);
                    if (0 <= xR.Length) {
                        for (int j = 0; j < xR.Length; ++j) {
                            Assert.IsTrue(Math.Abs(xR[j] - x[oPos]) < 1e-8);
                            ++oPos;
                        }
                    }
                }
            }

            {
                // Drainのテスト。
                var X = t.Drain();
                var xR = f.Process(X);
                for (int j = 0; j < xR.Length; ++j) {
                    if (x.Length <= oPos) {
                        break;
                    }
                    Assert.IsTrue(Math.Abs(xR[j] - x[oPos]) < 1e-8);
                    ++oPos;
                }
            }
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestDC() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);
            var x = new double[20];
            for (int i = 0; i < x.Length; ++i) {
                x[i] = 1;
            }

            Test(t, f, x, 1);
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestDClong() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);
            var x = new double[20];
            for (int i = 0; i < x.Length; ++i) {
                x[i] = 1;
            }

            Test(t, f, x, x.Length);
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestHannDC() {
            var t = new WWTimeDependentForwardFourierTransform(8, WWTimeDependentForwardFourierTransform.WindowType.Hann);
            var f = new WWTimeDependentInverseFourierTransform(8);
            var x = new double[19];
            for (int i = 0; i < x.Length; ++i) {
                x[i] = 1;
            }

            f.SetNumSamples(x.Length);

            Test(t, f, x, 1);
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestImpulse() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);

            var x = new double[20];
            x[0] = 1;

            Test(t, f, x, 1);
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestSine() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);

            var x = new double[20];

            for (int i = 0; i < x.Length; ++i) {
                x[0] = Math.Sin(i * 2.0 * Math.PI / x.Length);
            }

            Test(t, f, x, 1);
        }

    }
}
