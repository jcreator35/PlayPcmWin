using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WWMath;

namespace WWMathTest {
    [TestClass]
    public class WWSegmentPointDistanceTest {
        private bool IsApproxSame(double a, double b) {
            double distance = Math.Abs(a - b);
            return distance < 1.0e-7;
        }

        [TestMethod()]
        public void SegmentPointDistanceTestX1() {
            var edgeP1 = new WWVectorD2(0, 0);
            var edgeP2 = new WWVectorD2(1, 0);
            var point = new WWVectorD2(0.5, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestX2() {
            var edgeP1 = new WWVectorD2(1, 0);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(0.5, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestX3() {
            var edgeP1 = new WWVectorD2(1, 0);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(1, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestX4() {
            var edgeP1 = new WWVectorD2(1, 0);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(0, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestX5() {
            var edgeP1 = new WWVectorD2(1, 0);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(2, 0);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestX6() {
            var edgeP1 = new WWVectorD2(1, 0);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(-1, 0);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestY1() {
            var edgeP1 = new WWVectorD2(0, 0);
            var edgeP2 = new WWVectorD2(0, 1);
            var point = new WWVectorD2(1, 0.5);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestY2() {
            var edgeP1 = new WWVectorD2(0, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(1, 0.5);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestY3() {
            var edgeP1 = new WWVectorD2(0, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(1, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestY4() {
            var edgeP1 = new WWVectorD2(0, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(1, 0);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestY5() {
            var edgeP1 = new WWVectorD2(0, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(0, 2);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestY6() {
            var edgeP1 = new WWVectorD2(0, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(0, -1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestXY1() {
            var edgeP1 = new WWVectorD2(0, 0);
            var edgeP2 = new WWVectorD2(1, 1);
            var point = new WWVectorD2(0, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1.0/Math.Sqrt(2)));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestXY2() {
            var edgeP1 = new WWVectorD2(1, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(0, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1.0/Math.Sqrt(2)));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestXY3() {
            var edgeP1 = new WWVectorD2(0, 0);
            var edgeP2 = new WWVectorD2(1, 1);
            var point = new WWVectorD2(1, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 0));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestXY4() {
            var edgeP1 = new WWVectorD2(1, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(0, 0);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 0));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestXY5() {
            var edgeP1 = new WWVectorD2(0, 0);
            var edgeP2 = new WWVectorD2(1, 1);
            var point = new WWVectorD2(2, 1);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1.0));
        }

        [TestMethod()]
        public void SegmentPointDistanceTestXY6() {
            var edgeP1 = new WWVectorD2(1, 1);
            var edgeP2 = new WWVectorD2(0, 0);
            var point = new WWVectorD2(-1, 0);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, 1));
        }

        [TestMethod()]
        public void SegmentPointDistanceTest1() {
            var edgeP1 = new WWVectorD2(1, 1);
            var edgeP2 = new WWVectorD2(0, 1);
            var point = new WWVectorD2(2, 2);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, Math.Sqrt(2)));
        }

        [TestMethod()]
        public void SegmentPointDistanceTest2() {
            var edgeP1 = new WWVectorD2(1, 0);
            var edgeP2 = new WWVectorD2(1, 1);
            var point = new WWVectorD2(2, 2);

            double d = WWSegmentPointDistance.SegmentPointDistance(edgeP1, edgeP2, point, 1e-8);
            Assert.IsTrue(IsApproxSame(d, Math.Sqrt(2)));
        }
    }
}
