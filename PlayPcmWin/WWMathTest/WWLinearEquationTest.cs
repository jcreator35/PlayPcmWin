// 日本語。

using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    [TestClass()]
    public class WWLinearEquationTest {


        private TestContext testContextInstance;

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

        private bool IsApproxSame(double a, double b) {
            double distance = Math.Abs(a - b);
            return distance < 1.0e-7;
        }

        private bool IsApproxSame(double[] a, double[] b) {
            if (a.Length != b.Length) {
                return false;
            }
            for (int i = 0; i < a.Length; ++i) {
                if (!IsApproxSame(a[i], b[i])) {
                    return false;
                }
            }
            return true;
        }

        [TestMethod()]
        public void SolveTestK3_1() {
            int N = 3;
            var K = new WWMatrix(N, N, new double[] {
                 2, -1,  0,
                -1,  2, -1,
                 0, -1,  2});
            var f = new double[] { 4, 0, 0 };
            double[] u = WWLinearEquation.SolveKu_eq_f(K, f);

            Assert.IsTrue(IsApproxSame(u, new double[] {3,2,1}));
        }

        [TestMethod()]
        public void SolveTestK4_1() {
            int N = 4;
            var K = new WWMatrix(N, N, new double[] {
                 2, -1,  0,  0,
                -1,  2, -1,  0,
                 0, -1,  2, -1,
                 0,  0, -1,  2});
            var f = new double[] { 1, 0, 0, 0 };
            double[] u = WWLinearEquation.SolveKu_eq_f(K, f);

            Assert.IsTrue(IsApproxSame(u, new double[] { 4.0 / 5, 3.0 / 5, 2.0 / 5, 1.0 / 5 }));
        }

        [TestMethod()]
        public void SolveTestK4_2() {
            int N = 4;
            var K = new WWMatrix(N, N, new double[] {
                 2, -1,  0,  0,
                -1,  2, -1,  0,
                 0, -1,  2, -1,
                 0,  0, -1,  2});
            var f = new double[] { 0, 1, 0, 0 };
            double[] u = WWLinearEquation.SolveKu_eq_f(K, f);

            Assert.IsTrue(IsApproxSame(u, new double[] { 3.0/5, 6.0/5, 4.0/5, 2.0/5 }));
        }

        [TestMethod()]
        public void SolveTestK4_3() {
            int N = 4;
            var K = new WWMatrix(N, N, new double[] {
                 2, -1,  0,  0,
                -1,  2, -1,  0,
                 0, -1,  2, -1,
                 0,  0, -1,  2});
            var f = new double[] { 0, 0, 1, 0 };
            double[] u = WWLinearEquation.SolveKu_eq_f(K, f);

            Assert.IsTrue(IsApproxSame(u, new double[] { 2.0 / 5, 4.0 / 5, 6.0 / 5, 3.0 / 5 }));
        }

        [TestMethod()]
        public void SolveTestK4_4() {
            int N = 4;
            var K = new WWMatrix(N, N, new double[] {
                 2, -1,  0,  0,
                -1,  2, -1,  0,
                 0, -1,  2, -1,
                 0,  0, -1,  2});
            var f = new double[] { 0, 0, 0, 1 };
            double[] u = WWLinearEquation.SolveKu_eq_f(K, f);

            Assert.IsTrue(IsApproxSame(u, new double[] { 1.0 / 5, 2.0 / 5, 3.0 / 5, 4.0 / 5 }));
        }
    }
}
