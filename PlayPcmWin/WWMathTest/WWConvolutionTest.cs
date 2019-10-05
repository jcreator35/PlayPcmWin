// 日本語。

using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWConvolutionTest and is intended
    ///to contain all WWConvolutionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWConvolutionTest {


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


        bool Compare(WWComplex[] a, WWComplex[] b) {
            if (a == null || b == null) {
                return false;
            }

            if (a.Length != b.Length) {
                return false;
            }

            var d = WWComplex.AverageDistance(a, b);
            if (1.0e-8 < d) {
                return false;
            }

            return true;
        }

        [TestMethod()]
        public void ConvolutionBruteForceTest() {
            var target = new WWConvolution();
            var h = new WWComplex [] {new WWComplex(2,0), WWComplex.Unity()};
            var x = new WWComplex[] { WWComplex.Unity(), WWComplex.Unity() };
            var actual = target.ConvolutionBruteForce(h, x);
            var expected = new WWComplex[] { new WWComplex(2,0), new WWComplex(3, 0), WWComplex.Unity() };
            Assert.IsTrue(Compare(actual, expected));
        }

        [TestMethod()]
        public void ConvolutionBruteForceTest_p690() {
            var target = new WWConvolution();
            var h = WWComplex.FromRealArray(new double[] { 1, 1, 1, 1, 1, 2, 2, 2 });
            var x = WWComplex.FromRealArray(new double[] { 1, 1, 1, 1 });
            var actual = target.ConvolutionBruteForce(h, x);
            var expected = WWComplex.FromRealArray(new double[] { 1, 2, 3, 4, 4, 5, 6, 7, 6, 4, 2 });
            Assert.IsTrue(Compare(actual, expected));
        }

        [TestMethod()]
        public void ConvolutionBruteForceTest_p690inv() {
            var target = new WWConvolution();
            var h = WWComplex.FromRealArray(new double[] { 1, 1, 1, 1, 1, 2, 2, 2 });
            var x = WWComplex.FromRealArray(new double[] { 1, 1, 1, 1 });
            var actual2 = target.ConvolutionBruteForce(x, h);
            var expected = WWComplex.FromRealArray(new double[] { 1, 2, 3, 4, 4, 5, 6, 7, 6, 4, 2 });
            Assert.IsTrue(Compare(actual2, expected));
        }

        [TestMethod()]
        public void ConvolutionFftTest() {
            var target = new WWConvolution();
            var h = new WWComplex[] { new WWComplex(2, 0), WWComplex.Unity() };
            var x = new WWComplex[] { WWComplex.Unity(), WWComplex.Unity() };
            var actual = target.ConvolutionFft(h, x);
            var expected = new WWComplex[] { new WWComplex(2, 0), new WWComplex(3, 0), WWComplex.Unity() };
            Assert.IsTrue(Compare(actual, expected));
        }

        [TestMethod()]
        public void ConvolutionContinuousFftTest() {
            var target = new WWConvolution();
            var h = new WWComplex[256];
            for (int i=0; i<h.Length; ++i) {
                h[i] = new WWComplex(h.Length - i,0);
            }

            var x = new WWComplex[65536];
            for (int i = 0; i < x.Length; ++i) {
                x[i] = WWComplex.Unity();
            }

            var time1 = DateTime.Now.Ticks;

            var expected = target.ConvolutionFft(h, x);

            var time2 = DateTime.Now.Ticks;
            
            var actual = target.ConvolutionContinuousFft(h, x, h.Length);

            var time3 = DateTime.Now.Ticks;

            double elapsed1 = (time2 - time1) * 0.0001 * 0.001;
            double elapsed2 = (time3 - time2) * 0.0001 * 0.001;

            Assert.IsTrue(Compare(actual, expected));
        }
    }
}
