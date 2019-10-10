using WWMath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WWMathTest
{
    
    
    /// <summary>
    ///This is a test class for WWWindowFuncTest and is intended
    ///to contain all WWWindowFuncTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WWWindowFuncTest {


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



        [TestMethod()]
        public void BartlettWindow3Test() {
            var expected = new double[] { 0, 1, 0 };
            var actual = WWWindowFunc.BartlettWindow(3);

            var distance = Functions.AverageDistance(expected, actual);

            Assert.IsTrue(distance < 1e-8);
        }

        [TestMethod()]
        public void BartlettWindow5Test() {
            var expected = new double[] { 0, 0.5, 1, 0.5, 0 };
            var actual = WWWindowFunc.BartlettWindow(5);

            var distance = Functions.AverageDistance(expected, actual);

            Assert.IsTrue(distance < 1e-8);
        }
    }
}
