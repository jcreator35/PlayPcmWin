using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWRadix2FftTest and is intended
    ///to contain all WWRadix2FftTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWRadix2FftTest {


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

        private bool CompareFFT(double[] x) {
            int N = x.Length;
            var xC = WWComplex.FromRealArray(x);

            var Xexpected = WWDftCpu.Dft1d(xC);

            var fft = new WWRadix2Fft(N);
            var Xactual = fft.ForwardFft(xC, 1.0/N);

            return WWComplex.AverageDistance(Xactual, Xexpected) < 1e-8;
        }

        private bool CompareFFTroundTrip(double[] x) {
            int N = x.Length;
            var xC = WWComplex.FromRealArray(x);

            var fft = new WWRadix2Fft(N);
            var X = fft.ForwardFft(xC);
            var xR = fft.InverseFft(X);

            return WWComplex.AverageDistance(xC, xR) < 1e-8;
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest1() {
            var x = new double[] { 1, 0, 0, 0 };
            Assert.IsTrue(CompareFFT(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest1r() {
            var x = new double[] { 1, 0, 0, 0 };
            Assert.IsTrue(CompareFFTroundTrip(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest2() {
            var x = new double[] { 1, 1, 1, 1 };
            Assert.IsTrue(CompareFFT(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest2r() {
            var x = new double[] { 1, 1, 1, 1 };
            Assert.IsTrue(CompareFFTroundTrip(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest3() {
            var x = new double[] { 0, 1, 0, -1 };
            Assert.IsTrue(CompareFFT(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest3r() {
            var x = new double[] { 0, 1, 0, -1 };
            Assert.IsTrue(CompareFFTroundTrip(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest4() {
            var x = new double[] { 1, 0, -1, 0 };
            Assert.IsTrue(CompareFFT(x));
        }

        [TestMethod()]
        public void WWRadix2FFT_ForwardFftTest4r() {
            var x = new double[] { 1, 0, -1, 0 };
            Assert.IsTrue(CompareFFTroundTrip(x));
        }
    }
}
