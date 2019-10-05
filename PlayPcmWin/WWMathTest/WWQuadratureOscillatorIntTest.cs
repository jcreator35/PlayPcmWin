// 日本語。

using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWQuadratureOscillatorIntTest and is intended
    ///to contain all WWQuadratureOscillatorIntTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWQuadratureOscillatorIntTest {


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


        /// <summary>
        ///A test for Next
        ///</summary>
        [TestMethod()]
        public void QOscIntNextTest() {
            int ft = 11025;
            int fs = 44100;
            var target = new WWQuadratureOscillatorInt(ft, fs);
            WWComplex[] expected = new WWComplex[] {
                new WWComplex(1, 0),
                new WWComplex(0, 1),
                new WWComplex(-1, 0),
                new WWComplex(0, -1),
            };

            int differentCount = 0;
            for (int i = 0; i < 10000; ++i) {
                var actual = target.Next();
                Console.WriteLine("{0} {1}", i, actual);
                if (1e-8 < WWComplex.Distance(expected[i % 4], actual)) {
                    ++differentCount;
                }
            }

            Assert.AreEqual(0, differentCount);
        }
    }
}
