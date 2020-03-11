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

            // Lは下三角行列。
            var Ltype = L.DetermineMatType();
            Assert.IsTrue(0 != (Ltype & (ulong)Matrix.MatType.LowerTriangular));

            // Uは上三角行列。
            var Utype = U.DetermineMatType();
            Assert.IsTrue(0 != (Utype & (ulong)Matrix.MatType.UpperTriangular));

            // L * U = A
            Matrix Arecovered = Matrix.Mul(L, U);
            Assert.IsTrue(Matrix.IsSame(A, Arecovered));
        }

        [TestMethod()]
        public void MatrixLPU() {
            int N = 3;
            var A = new Matrix(N, N, new double[]
                  { 10, -7, 0,
                    -3,  2, 6,
                     5, -1, 5});

            Matrix P;
            Matrix L;
            Matrix U;
            var result = Matrix.LUdecompose2(A, out L, out P, out U);
            Assert.IsTrue(result == Matrix.ResultEnum.Success);

            // Lは下三角行列。
            var Ltype = L.DetermineMatType();
            Assert.IsTrue(0 != (Ltype & (ulong)Matrix.MatType.LowerTriangular));

            // Uは上三角行列。
            var Utype = U.DetermineMatType();
            Assert.IsTrue(0 != (Utype & (ulong)Matrix.MatType.UpperTriangular));

            A.Print("A");
            L.Print("L");
            U.Print("U");
            P.Print("P");

            // P * A = L * U
            var LU = Matrix.Mul(L, U);
            var PA = Matrix.Mul(P, A);
            Assert.IsTrue(Matrix.IsSame(PA, LU));
        }
    }
}
