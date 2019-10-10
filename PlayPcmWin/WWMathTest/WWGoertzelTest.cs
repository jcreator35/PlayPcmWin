using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWGoertzelTest and is intended
    ///to contain all WWGoertzelTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWGoertzelTest {


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


        private bool CompareDft(double[] x) {
            int N = 4;

            var Xexpected = WWDftCpu.Dft1d(WWComplex.FromRealArray(x), 1.0);
            var Xactual = new WWComplex[N];

            for (int m = 0; m < N; ++m) {
                var g = new WWGoertzel(m, N);
                for (int i = 0; i < N; ++i) {
                    Xactual[m] = g.Filter(x[i]);
                }
            }

            return WWComplex.AverageDistance(Xexpected, Xactual) < 1e-8;
        }

        [TestMethod()]
        public void GoertzelFilterTestImpulse() {
            var x = new double[] { 1, 0, 0, 0 };

            Assert.IsTrue(CompareDft(x));
        }

        [TestMethod()]
        public void GoertzelFilterTestDC() {
            var x = new double[] { 1, 1, 1, 1 };

            Assert.IsTrue(CompareDft(x));
        }

        [TestMethod()]
        public void GoertzelFilterTestSin() {
            var x = new double[] { 0, 1, 0, -1 };

            Assert.IsTrue(CompareDft(x));
        }

        [TestMethod()]
        public void GoertzelFilterTestCos() {
            var x = new double[] { 1, 0, -1, 0 };

            Assert.IsTrue(CompareDft(x));
        }
    }
}
