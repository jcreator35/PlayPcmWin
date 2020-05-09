using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    /// <summary>
    ///RealPolynomial unit test
    ///</summary>
    [TestClass()]
    public class WWRealPolynomialTest {
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


        private bool IsSimilar(RealPolynomial a, RealPolynomial b) {
            if (a.Degree != b.Degree) {
                return false;
            }

            for (int i = 0; i < a.Degree; ++i) {
                double aC = a.Coeffs()[i];
                double bC = b.Coeffs()[i];

                if (1e-8 <= Math.Abs(aC - bC)) {
                    return false;
                }
            }

            return true;
        }

        [TestMethod()]
        public void IIntegralTest2() {
            // p(x) = 2
            // ∫(2)dx == [2x] +C

            double C = 1.0;
            var p = new RealPolynomial(new double[] { 2 });
            var pI = new RealPolynomial(new double[] { C, 2 });

            var r = p.Integral(C);
            Assert.IsTrue(IsSimilar(r, pI));
        }

        [TestMethod()]
        public void IIntegralTestx() {
            // p(x) = x
            // ∫xdx == [x^2/2] +C

            double C = 1.0;
            var p = new RealPolynomial(new double[] { 0, 1 });
            var pI = new RealPolynomial(new double[] { C, 0, 1.0/2.0 });

            var r = p.Integral(C);
            Assert.IsTrue(IsSimilar(r, pI));
        }

        [TestMethod()]
        public void IIntegralTestx2() {
            // p(x) = x^2
            // ∫x^2dx == [x^3/3] +C

            double C = 1.0;
            var p = new RealPolynomial(new double[] { 0, 0, 1 });
            var pI = new RealPolynomial(new double[] { C, 0, 0, 1.0 / 3.0 });

            var r = p.Integral(C);
            Assert.IsTrue(IsSimilar(r, pI));
        }

        private bool IsSimilar(double a, double b) {
            return Math.Abs(a - b) < 1e-8;
        }

        [TestMethod()]
        public void DIntegralTest2_3to4() {
            // p(x) = 2
            // ∫_3^4(2dx) == [2x]_{x=3}^4 = (8-6) = 2

            var p = new RealPolynomial(new double[] { 2 });
            double r = p.Integral(3, 4);

            Assert.IsTrue(IsSimilar(r, 2.0));
        }

        [TestMethod()]
        public void DIntegralTest2_4to3() {
            // p(x) = 2
            // ∫_4^3(2dx) == [2x]_{x=4}^3 = (6-8) = -2

            var p = new RealPolynomial(new double[] { 2 });
            double r = p.Integral(4, 3);

            Assert.IsTrue(IsSimilar(r, -2.0));
        }

        [TestMethod()]
        public void DIntegralTestx_1to2() {
            // p(x) = x
            // ∫_1^2(xdx) == [x^2/2]_{x=1}^2 = (4-1)/2 = 3/2

            var p = new RealPolynomial(new double[] { 0, 1 });
            double r = p.Integral(1, 2);

            Assert.IsTrue(IsSimilar(r, 3.0 / 2.0));
        }

        [TestMethod()]
        public void DIntegralTestx_0to1() {
            // p(x) = x
            // ∫_0^1(xdx) == [x^2/2]_{x=0}^1 = 1/2

            var p = new RealPolynomial(new double[] { 0, 1 });
            double r = p.Integral(0, 1);
            Assert.IsTrue(IsSimilar(r, 1.0 / 2.0));
        }

        [TestMethod()]
        public void DIntegralTestx2_0to1() {
            // p(x) = x^2
            // ∫_0^1(x^2dx) == [x^3/3]_{x=0}^1 = 1/3

            var p = new RealPolynomial(new double[] { 0, 0, 1 });
            double r = p.Integral(0, 1);
            Assert.IsTrue(IsSimilar(r, 1.0 / 3.0));
        }

        [TestMethod()]
        public void DIntegralTestx2_1to2() {
            // p(x) = x^2
            // ∫_1^2(x^2dx) == [x^3/3]_{x=1}^2 = (8-1)/3 = 7/3

            var p = new RealPolynomial(new double[] { 0, 0, 1 });
            double r = p.Integral(1, 2);
            Assert.IsTrue(IsSimilar(r, 7.0 / 3.0));
        }

        [TestMethod()]
        public void DIntegralTestx3px2_2to5() {
            // p(x) = x^3 + x^2
            // ∫_2^5(x^3+x^2)dx == [x^4/4+x^3/3]_{x=2}^5 = 765/4

            var p = new RealPolynomial(new double[] { 0, 0, 1, 1 });
            double r = p.Integral(2, 5);
            Assert.IsTrue(IsSimilar(r, 765.0 / 4.0));
        }

    }
}
