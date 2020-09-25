// 日本語。
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WWMath;

namespace WWMathTest {
    [TestClass]
    public class WWNormalizeTest {
        [TestMethod]
        public void NormalzeTest1() {
            var v = new float[]{ 2, 2, 2, 2};

            var n = new WWNormalize();
            float [] vOut;
            bool r = n.Normalize(v, out vOut);

            Assert.AreEqual(r, false);
            Assert.AreEqual(vOut.Length, v.Length);
            for (int i = 0; i < v.Length; ++i) {
                Assert.IsTrue(Math.Abs(vOut[i]) < 1.0e-7);
            }
        }

        [TestMethod]
        public void NormalzeTest2() {
            var v = new float[] { 1, 2, 1, 2 };
            var vAnswer = new float[] { -1, 1, -1, 1 };

            var n = new WWNormalize();
            float[] vOut;
            bool r = n.Normalize(v, out vOut);

            Assert.AreEqual(r, true);
            Assert.AreEqual(vOut.Length, v.Length);
            for (int i = 0; i < v.Length; ++i) {
                Assert.IsTrue(Math.Abs(vOut[i] - vAnswer[i]) < 1.0e-7);
            }
        }
    }
}
