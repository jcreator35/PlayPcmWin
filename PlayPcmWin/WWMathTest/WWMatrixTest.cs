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
        public void MatrixJoinH() {
            int N = 2;
            var A = new WWMatrix(N, N, new double[]
                  { 1, 2,
                    3, 4 });
            var B = new WWMatrix(N, N, new double[]
                  { 5, 6,
                    7, 8 });
            var AB = WWMatrix.JoinH(A, B);
            var ABref = new WWMatrix(N,N*2, new double[]
                { 1,2,5,6,
                    3,4,7,8
                });
            Assert.IsTrue(WWMatrix.IsSame(AB, ABref));
        }

        [TestMethod()]
        public void MatrixJoinV() {
            int N = 2;
            var A = new WWMatrix(N, N, new double[]
                  { 1, 2,
                    3, 4 });
            var B = new WWMatrix(N, N, new double[]
                  { 5, 6,
                    7, 8 });
            var AB = WWMatrix.JoinV(A, B);
            var ABref = new WWMatrix(N*2, N, new double[]
                  { 1,2,
                    3,4,
                    5,6,
                    7,8
                  });
            Assert.IsTrue(WWMatrix.IsSame(AB, ABref));
        }

        [TestMethod()]
        public void MatrixLU() {
            int N = 2;
            var A = new WWMatrix(N, N, new double[]
                  { 4, 3,
                    6, 3 });

            WWMatrix L;
            WWMatrix U;
            var result = WWMatrix.LUdecompose(A, out L, out U);
            Assert.IsTrue(result == WWMatrix.ResultEnum.Success);

            // Lは下三角行列。
            var Ltype = L.DetermineMatType();
            Assert.IsTrue(0 != (Ltype & (ulong)WWMatrix.MatType.LowerTriangular));

            // Uは上三角行列。
            var Utype = U.DetermineMatType();
            Assert.IsTrue(0 != (Utype & (ulong)WWMatrix.MatType.UpperTriangular));

            // L * U = A
            WWMatrix Arecovered = WWMatrix.Mul(L, U);
            Assert.IsTrue(WWMatrix.IsSame(A, Arecovered));
        }

        [TestMethod()]
        public void MatrixLPU_2x2() {
            int N = 2;

            var r = new Random();

            for (int i = 0; i < 1000; ++i) {
                var A = new WWMatrix(N, N, new double[]
                  { r.Next(10), r.Next(10),
                    r.Next(10), r.Next(10)});

                WWMatrix P;
                WWMatrix L;
                WWMatrix U;
                var result = WWMatrix.LUdecompose2(A, out L, out P, out U);
                if (result == WWMatrix.ResultEnum.FailedToChoosePivot) {
                    continue;
                }
                Assert.IsTrue(result == WWMatrix.ResultEnum.Success);

                // Lは下三角行列。
                var Ltype = L.DetermineMatType();
                Assert.IsTrue(0 != (Ltype & (ulong)WWMatrix.MatType.LowerTriangular));

                // Uは上三角行列。
                var Utype = U.DetermineMatType();
                Assert.IsTrue(0 != (Utype & (ulong)WWMatrix.MatType.UpperTriangular));

                A.Print("A");
                L.Print("L");
                U.Print("U");
                P.Print("P");

                // P * A = L * U
                var LU = WWMatrix.Mul(L, U);
                var PA = WWMatrix.Mul(P, A);
                Assert.IsTrue(WWMatrix.IsSame(PA, LU));
            }
        }

        [TestMethod()]
        public void MatrixLPU_3x3a() {
            int N = 3;

            var A = new WWMatrix(N, N, new double[]
                { 6,4,1,
                  3,3,2,
                  7,7,3});

            WWMatrix P;
            WWMatrix L;
            WWMatrix U;
            var result = WWMatrix.LUdecompose2(A, out L, out P, out U);
            Assert.IsTrue(result == WWMatrix.ResultEnum.Success);

            // Lは下三角行列。
            var Ltype = L.DetermineMatType();
            Assert.IsTrue(0 != (Ltype & (ulong)WWMatrix.MatType.LowerTriangular));

            // Uは上三角行列。
            var Utype = U.DetermineMatType();
            Assert.IsTrue(0 != (Utype & (ulong)WWMatrix.MatType.UpperTriangular));

            A.Print("A");
            L.Print("L");
            U.Print("U");
            P.Print("P");

            // P * A = L * U
            var LU = WWMatrix.Mul(L, U);
            var PA = WWMatrix.Mul(P, A);
            Assert.IsTrue(WWMatrix.IsSame(PA, LU));
        }

        [TestMethod()]
        public void MatrixLPU_3x3() {
            int N = 3;

            var r = new Random();

            for (int i = 0; i < 1000; ++i) {
                var A = new WWMatrix(N, N, new double[]
                  { r.Next(10), r.Next(10), r.Next(10),
                    r.Next(10), r.Next(10), r.Next(10),
                    r.Next(10), r.Next(10), r.Next(10)});

                WWMatrix P;
                WWMatrix L;
                WWMatrix U;
                var result = WWMatrix.LUdecompose2(A, out L, out P, out U);
                if (result == WWMatrix.ResultEnum.FailedToChoosePivot) {
                    continue;
                }
                Assert.IsTrue(result == WWMatrix.ResultEnum.Success);

                // Lは下三角行列。
                var Ltype = L.DetermineMatType();
                Assert.IsTrue(0 != (Ltype & (ulong)WWMatrix.MatType.LowerTriangular));

                // Uは上三角行列。
                var Utype = U.DetermineMatType();
                Assert.IsTrue(0 != (Utype & (ulong)WWMatrix.MatType.UpperTriangular));

                A.Print("A");
                L.Print("L");
                U.Print("U");
                P.Print("P");

                // P * A = L * U
                var LU = WWMatrix.Mul(L, U);
                var PA = WWMatrix.Mul(P, A);
                Assert.IsTrue(WWMatrix.IsSame(PA, LU));
            }
        }

        [TestMethod()]
        public void MatrixLPU_4x4() {
            int N = 4;

            var r = new Random();

            for (int i = 0; i < 1000; ++i) {
                var A = new WWMatrix(N, N, new double[]
                  { r.Next(10), r.Next(10), r.Next(10), r.Next(10),
                    r.Next(10), r.Next(10), r.Next(10), r.Next(10),
                    r.Next(10), r.Next(10), r.Next(10), r.Next(10),
                    r.Next(10), r.Next(10), r.Next(10), r.Next(10)});

                WWMatrix P;
                WWMatrix L;
                WWMatrix U;
                var result = WWMatrix.LUdecompose2(A, out L, out P, out U);
                if (result == WWMatrix.ResultEnum.FailedToChoosePivot) {
                    continue;
                }
                Assert.IsTrue(result == WWMatrix.ResultEnum.Success);

                // Lは下三角行列。
                var Ltype = L.DetermineMatType();
                Assert.IsTrue(0 != (Ltype & (ulong)WWMatrix.MatType.LowerTriangular));

                // Uは上三角行列。
                var Utype = U.DetermineMatType();
                Assert.IsTrue(0 != (Utype & (ulong)WWMatrix.MatType.UpperTriangular));

                A.Print("A");
                L.Print("L");
                U.Print("U");
                P.Print("P");

                // P * A = L * U
                var LU = WWMatrix.Mul(L, U);
                var PA = WWMatrix.Mul(P, A);
                Assert.IsTrue(WWMatrix.IsSame(PA, LU));
            }
        }
    }
}
