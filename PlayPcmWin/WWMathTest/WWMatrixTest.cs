// 日本語。

using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    [TestClass()]
    public class WWMatrixTest {


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


        [TestMethod()]
        public void MatrixLU() {
            int N = 2;
            var A = new Matrix(N, N, new double[]
                  { 4, 3,
                    6, 3 });

            Matrix L;
            Matrix U;
            var result = Matrix.LUdecompose(A, out L, out U);
            Assert.IsTrue(result == Matrix.ResultEnum.Success);

            // Lの右上は0が入っている。
            Assert.IsTrue(IsApproxSame(L.At(0, 1), 0));

            // Uの左下は0が入っている。
            Assert.IsTrue(IsApproxSame(U.At(1, 0), 0));

            // L * U = A
            Matrix Arecovered = Matrix.Mul(L, U);
            Assert.IsTrue(Matrix.IsSame(A, Arecovered));
        }
    }
}
