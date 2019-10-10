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

        [TestMethod()]
        public void TimeDependentFourierTransformTestDC() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);

            for (int i = 0; i < 20; ++i) {
                var x = new double[1];
                x[0] = 1;

                var X = t.Process(x);
                if (0 < X.Length) {
                    var xR = f.Process(X);
                    if (0 <= xR.Length) {
                        for (int j = 0; j < xR.Length; ++j) {
                            Assert.IsTrue(Math.Abs(xR[j] - 1) < 1e-8);
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestHannDC() {
            var t = new WWTimeDependentForwardFourierTransform(8, WWTimeDependentForwardFourierTransform.WindowType.Hann);
            var f = new WWTimeDependentInverseFourierTransform(8);

            for (int i = 0; i < 20; ++i) {
                var x = new double[1];
                x[0] = 1;

                var X = t.Process(x);
                if (0 < X.Length) {
                    var xR = f.Process(X);
                    if (0 <= xR.Length) {
                        for (int j = 0; j < xR.Length; ++j) {
                            Assert.IsTrue(Math.Abs(xR[j] - 1) < 1e-8);
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestImpulse() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);

            var x = new double[20];
            x[0] = 1;

            int outIdx = 0;

            for (int i = 0; i < x.Length; ++i) {
                var X = t.Process(new double[1]{x[i]});
                if (0 < X.Length) {
                    var xR = f.Process(X);
                    if (0 <= xR.Length) {
                        for (int j = 0; j < xR.Length; ++j) {
                            Assert.IsTrue(Math.Abs(xR[j] - x[outIdx]) < 1e-8);
                            ++outIdx;
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void TimeDependentFourierTransformTestSine() {
            var t = new WWTimeDependentForwardFourierTransform(4, WWTimeDependentForwardFourierTransform.WindowType.Bartlett);
            var f = new WWTimeDependentInverseFourierTransform(4);

            var x = new double[20];

            for (int i = 0; i < x.Length; ++i) {
                x[0] = Math.Sin(i * 2.0 * Math.PI / x.Length);
            }

            int outIdx = 0;

            for (int i = 0; i < x.Length; ++i) {
                var X = t.Process(new double[1] { x[i] });
                if (0 < X.Length) {
                    var xR = f.Process(X);
                    if (0 <= xR.Length) {
                        for (int j = 0; j < xR.Length; ++j) {
                            Assert.IsTrue(Math.Abs(xR[j] - x[outIdx]) < 1e-8);
                            ++outIdx;
                        }
                    }
                }
            }

        }
    }
}
