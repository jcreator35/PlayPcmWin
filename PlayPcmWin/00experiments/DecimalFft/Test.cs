using System;

namespace DecimalFft {
    class Test {

        public static void TestAll() {
            TestLog10();
            TestCos();
            TestSin();
            TestSqrt();
        }

        public static void TestLog10() {
            for (int i=1; i < 10; ++i) {
                Console.WriteLine("log10({0})={1}", i, WWDecimalMath.Log10(i));
            }

            Console.WriteLine("log10({0})={1}", 10M, WWDecimalMath.Log10(10M));
            Console.WriteLine("log10({0})={1}", 1.0M, WWDecimalMath.Log10(1.0M));
            Console.WriteLine("log10({0})={1}", 1.0e-1M, WWDecimalMath.Log10(1.0e-1M));
            Console.WriteLine("log10({0})={1}", 1.0e-2M, WWDecimalMath.Log10(1.0e-2M));
            Console.WriteLine("log10({0})={1}", 1.0e-3M, WWDecimalMath.Log10(1.0e-3M));
            Console.WriteLine("log10({0})={1}", 1.0e-4M, WWDecimalMath.Log10(1.0e-4M));
            Console.WriteLine("log10({0})={1}", 1.0e-5M, WWDecimalMath.Log10(1.0e-5M));
            Console.WriteLine("log10({0})={1}", 1.0e-6M, WWDecimalMath.Log10(1.0e-6M));
            Console.WriteLine("log10({0})={1}", 1.0e-7M, WWDecimalMath.Log10(1.0e-7M));
            Console.WriteLine("log10({0})={1}", 1.0e-8M, WWDecimalMath.Log10(1.0e-8M));
            Console.WriteLine("log10({0})={1}", 1.0e-9M, WWDecimalMath.Log10(1.0e-9M));
            Console.WriteLine("log10({0})={1}", 1.0e-10M, WWDecimalMath.Log10(1.0e-10M));
            Console.WriteLine("log10({0})={1}", 1.0e-11M, WWDecimalMath.Log10(1.0e-11M));
            Console.WriteLine("log10({0})={1}", 1.0e-12M, WWDecimalMath.Log10(1.0e-12M));
            Console.WriteLine("log10({0})={1}", 1.0e-13M, WWDecimalMath.Log10(1.0e-13M));
            Console.WriteLine("log10({0})={1}", 1.0e-14M, WWDecimalMath.Log10(1.0e-14M));
            Console.WriteLine("log10({0})={1}", 1.0e-15M, WWDecimalMath.Log10(1.0e-15M));
            Console.WriteLine("log10({0})={1}", 1.0e-16M, WWDecimalMath.Log10(1.0e-16M));
            Console.WriteLine("log10({0})={1}", 1.0e-17M, WWDecimalMath.Log10(1.0e-17M));
            Console.WriteLine("log10({0})={1}", 1.0e-18M, WWDecimalMath.Log10(1.0e-18M));
            Console.WriteLine("log10({0})={1}", 1.0e-19M, WWDecimalMath.Log10(1.0e-19M));
            Console.WriteLine("log10({0})={1}", 1.0e-20M, WWDecimalMath.Log10(1.0e-20M));
            Console.WriteLine("log10({0})={1}", 1.0e-21M, WWDecimalMath.Log10(1.0e-21M));
            Console.WriteLine("log10({0})={1}", 1.0e-22M, WWDecimalMath.Log10(1.0e-22M));
            Console.WriteLine("log10({0})={1}", 1.0e-23M, WWDecimalMath.Log10(1.0e-23M));
            Console.WriteLine("log10({0})={1}", 1.0e-24M, WWDecimalMath.Log10(1.0e-24M));
            Console.WriteLine("log10({0})={1}", 1.0e-25M, WWDecimalMath.Log10(1.0e-25M));
            Console.WriteLine("log10({0})={1}", 1.0e-26M, WWDecimalMath.Log10(1.0e-26M));
            Console.WriteLine("log10({0})={1}", 1.0e-27M, WWDecimalMath.Log10(1.0e-27M));
        }

        public static void TestCos() {
            for (int i=-360; i <= 720; i += 30) {
                Console.WriteLine("cos({0})={1}", i, WWDecimalMath.Cos(WWDecimalMath.M_2PI * i / 180));
            }
        }

        public static void TestSin() {
            for (int i=-360; i <= 720; i += 30) {
                Console.WriteLine("sin({0})={1}", i, WWDecimalMath.Sin(WWDecimalMath.M_2PI * i/180));
            }
        }

        public static void TestSqrt() {
            for (int i=0; i <= 10; ++i) {
                Console.WriteLine("sqrt({0})={1}", i, WWDecimalMath.Sqrt(i));
            }
        }
    }
}
