// 日本語。

using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWSlidingDFTTest and is intended
    ///to contain all WWSlidingDFTTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWSlidingDFTTest {


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


        public bool TestSDFT(double[] x) {
            WWComplex[] Xexpected = null;
            WWDftCpu.Dft1d(WWComplex.FromRealArray(x), out Xexpected);

            var sdft = new WWSlidingDFT(x.Length);

            WWComplex[] Xactual = null;
            for (int i = 0; i < x.Length; ++i) {
                Xactual = sdft.Filter(x[i]);
            }

            return WWComplex.AverageDistance(Xexpected, Xactual) < 1e-8;
        }

        public bool TestSDFT_LastN(double[] x, int N) {
            System.Diagnostics.Debug.Assert(N <= x.Length);

            var xLastN = new double[N];
            Array.Copy(x, x.Length - N, xLastN, 0, N);

            WWComplex[] Xexpected = null;
            WWDftCpu.Dft1d(WWComplex.FromRealArray(xLastN), out Xexpected);

            var sdft = new WWSlidingDFT(N);

            WWComplex[] Xactual = null;
            for (int i = 0; i < x.Length; ++i) {
                Xactual = sdft.Filter(x[i]);
            }

            return WWComplex.AverageDistance(Xexpected, Xactual) < 1e-8;
        }

        [TestMethod()]
        public void WWSlidingDFTFilterTest1() {
            var x = new double[] { 1, 0, 0, 0 };
            Assert.IsTrue(TestSDFT(x));
        }

        [TestMethod()]
        public void WWSlidingDFTFilterTest2() {
            var x = new double[] { 1, 1, 1, 1 };
            Assert.IsTrue(TestSDFT(x));
        }

        [TestMethod()]
        public void WWSlidingDFTFilterTest3() {
            var x = new double[] { 0, 1, 0, -1 };
            Assert.IsTrue(TestSDFT(x));
        }

        [TestMethod()]
        public void WWSlidingDFTFilterTest4() {
            var x = new double[] { 0, 1, 0, -1, 0, 0, 0, 0 };
            Assert.IsTrue(TestSDFT_LastN(x,4));
        }

        [TestMethod()]
        public void WWSlidingDFTFilterTest5() {
            var x = new double[] { 0, 1, 0, -1, 1, 0, 0, 0 };
            Assert.IsTrue(TestSDFT_LastN(x, 4));
        }
    }
}
