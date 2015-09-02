using System;

namespace DecimalFft {
    class WWDecimalMath {
        // precision of decimal type is max 29 digits
        // Following constants are generated using bc, something like this:
        // $ bc -l
        // scale=32; 4 * a(1)     # pi      with 32 digits
        // sqrt(2)                # sqrt(2) with 32 digits
        // l(10)                  # ln(10)
        public const decimal M_2PI = 6.28318530717958647692528676655896M;
        public const decimal M_3PI_2 = 4.71238898038468985769396507491922M;
        public const decimal M_PI   = 3.1415926535897932384626433832795M;
        public const decimal M_PI_2 = 1.57079632679489661923132169163974M;
        public const decimal M_PI_3 = 1.04719755119659774615421446109315M;
        public const decimal M_PI_4 = 0.78539816339744830961566084581987M;
        public const decimal M_PI_6 = 0.52359877559829887307710723054657M;

        public const decimal M_SQRT2 = 1.41421356237309504880168872420969M;
        public const decimal M_1_SQRT2 = 0.70710678118654752440084436210485M;
        public const decimal M_SQRT3_2 = 0.86602540378443864676372317075293M;
        public const decimal M_LN10 = 2.30258509299404568401799145468436M;
        public const decimal M_LN2 = 0.69314718055994530941723212145817M;
        public const decimal M_1_LN10 = 0.43429448190325182765112891891660M;

        public static bool IsExtremelySmall(decimal v) {
            // theoretical precision is 28.8 digits
            return -1.0e-28M < v && v < 1.0e-28M;
        }

        public static bool IsRelativelySmall(decimal x, decimal yRef) {
            if (IsExtremelySmall(yRef)) {
                return IsExtremelySmall(x);
            }
            return IsExtremelySmall(x / yRef);
        }

        public static bool IsAlmostTheSame(decimal x, decimal y) {
            if (IsExtremelySmall(x)) {
                return IsExtremelySmall(y);
            }

            decimal diff = 1M - x / y;

            return -1.0e-25M < diff && diff < 1.0e-25M;
        }

        private static decimal Cos0to45(decimal rad) {
            if (IsExtremelySmall(rad)) {
                return decimal.One;
            }
            System.Diagnostics.Debug.Assert(-M_PI_4 <= rad && rad <= M_PI_4 + 1.0e-27M);

            decimal result = 1M;
            decimal elem = 1M;

            // Maclaurin series of cosine. cos(x)=Σ_{a=0}^∞{(x^(2a))/(2a!)}
            for (int i=0; i < 12; ++i) {
                elem *= (-rad / (i * 2 + 1)) * (rad / (i * 2 + 2));
                result += elem;

                // test passed on 45 degree
                // Console.WriteLine("i={0} v={1} diff={2}", i, result, result - M_1_SQRT2);
            }

            return result;
        }

        private static decimal Sin0to45(decimal rad) {
            if (IsExtremelySmall(rad)) {
                return rad;
            }
            System.Diagnostics.Debug.Assert(-M_PI_4 <= rad && rad <= M_PI_4 + 1.0e-27M);


            decimal result = rad;
            decimal elem = rad;

            // Maclaurin series of sine
            for (int i=0; i < 12; ++i) {
                elem *= (-rad / (i * 2 + 2)) * (rad / (i * 2 + 3));
                result += elem;

                // test passed on 45 degree
                // Console.WriteLine("i={0} v={1} diff={2}", i, result, result - M_1_SQRT2);
            }

            return result;
        }

        public static decimal Cos(decimal rad) {
            while (M_2PI < rad) {
                rad -= M_2PI;
            }
            while (rad < 0) {
                rad += M_2PI;
            }

            int deg45 = (int)(rad / M_PI_4);
            switch (deg45) {
            case 0: // 0°≦rad<45°
                return Cos0to45(rad);
            case 1: // 45°≦rad<90°
                return Sin0to45(M_PI_2 - rad);
            case 2: // 90°≦rad<135°
                return -Sin0to45(rad - M_PI_2);
            case 3: // 135°≦rad<180°
                return -Cos0to45(M_PI - rad);
            case 4: // 180°≦rad<225°
                return -Cos0to45(rad - M_PI);
            case 5: // 225°≦rad<270°
                return -Sin0to45(M_3PI_2 - rad);
            case 6: // 270°≦rad<315°
                return Sin0to45(rad - M_3PI_2);
            case 7: // 315°≦rad<360°
                return Cos0to45(M_2PI - rad);
            case 8: // 360° 誤差によりここに来る。
                return 1M;
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0M;
            }
        }

        public static decimal Sin(decimal rad) {
            while (M_2PI < rad) {
                rad -= M_2PI;
            }
            while (rad < 0) {
                rad += M_2PI;
            }

            int deg45 = (int)(rad / M_PI_4);
            switch (deg45) {
            case 0: // 0°≦rad<45°
                return Sin0to45(rad);
            case 1: // 45°≦rad<90°
                return Cos0to45(M_PI_2 - rad);
            case 2: // 90°≦rad<135°
                return Cos0to45(rad - M_PI_2);
            case 3: // 135°≦rad<180°
                return Sin0to45(M_PI - rad);
            case 4: // 180°≦rad<225°
                return -Sin0to45(rad - M_PI);
            case 5: // 225°≦rad<270°
                return -Cos0to45(M_3PI_2 - rad);
            case 6: // 270°≦rad<315°
                return -Cos0to45(rad - M_3PI_2);
            case 7: // 315°≦rad<360°
                return -Sin0to45(M_2PI - rad);
            case 8: // 360° 僅かな丸め誤差によりここに来る。
                return 0;
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0M;
            }
        }

        public static decimal Sqrt(decimal v) {
            if (v < 0M) {
                throw new ArgumentOutOfRangeException("v");
            }

            // Algorithm is Newton's method
            // https://en.wikipedia.org/wiki/Newton's_method#Square_root_of_a_number

            // x^2 = v
            // f(x) = x^2 - v
            // f'(x) = 2x

            // x0 = 10
            // x1 = x0 - f(x0)/f'(x0)
            // x2 = x1 - f(x1)/f'(x1)

            // f(x)/f'(x) = (x^2-v)/(2x) = (1/2)*(x -v/x)

            decimal x = 10M;
            for (int i=0; i < 128; ++i) {
                decimal diff = (x - v/x) / 2;
                if (WWDecimalMath.IsRelativelySmall(diff, x)) {
                    break;
                }
                x -= diff;

                // Console.WriteLine("    {0} {1}", i, x);
            }

            return x;
        }

        public static decimal Exp(decimal x) {
            decimal y = 0M;

            // algorithm is simple Taylor series

            decimal diff = 1M;
            for (int i=1; i < 1000 * 1000; ++i) {
                y += diff;

                diff *= x / i;
                if (WWDecimalMath.IsRelativelySmall(diff, y)) {
                    break;
                }
            }

            return y;
        }

#if false
        // this algorithm is very inefficient
        public static decimal Ln(decimal v) {
            if (v <= 0M) {
                throw new ArgumentOutOfRangeException("v");
            }

            // algorithm is based on area hyperbolic tangent
            // https://en.wikipedia.org/wiki/Logarithm

            decimal x = 0;

            decimal t = (v-1M)/(v+1M);
            decimal tMul = t * t;

            for (int i=0; i < 1000 * 1000 * 1000; ++i) {
                decimal diff = 2M / (i * 2 + 1M) * t;
                if (WWDecimalMath.IsExtremelySmall(diff)) {
                    break;
                }
                x += diff;
                t *= tMul;

                //Console.WriteLine("    {0} {1}", i, x);
            }

            return x;
        }
#endif

#if true
        // this method is slow
        public static decimal Ln(decimal x) {
            if (x <= 0M) {
                throw new ArgumentOutOfRangeException("x");
            }

            // algorithm is Newton's method
            decimal expy = 1;
            decimal y = 2M * (x - expy) / (x + expy);

            for (int i=0; i < 1000 * 1000; ++i) {
                expy = Exp(y);
                decimal diff = 2M * (x - expy) / (x + expy);
                if (WWDecimalMath.IsRelativelySmall(diff, y)) {
                    break;
                }

                y += diff;
            }

            return y;
        }
#endif

#if false
        private static long Pow2(int x) {
            return 1L << x;
        }
        
        private static decimal AGM(decimal x, decimal y) {
            decimal a = (x + y)/2;
            decimal g = Sqrt(x*y);

            while (!IsAlmostTheSame(a,g)) {
                decimal nextA = (a+g)/2;
                decimal nextG = Sqrt(a*g);
                a = nextA;
                g = nextG;
            }

            return a;
        }

        // it seems not so accurate
        public static decimal Ln(decimal x) {
            // Algorithm is `Another alternative for extremely high precision calculation' of
            // https://en.wikipedia.org/wiki/Natural_logarithm

            // p = 96 (bit precision)
            // s = x * 2^m > 2^(p/2)
            int m = 7;
            decimal s;
            do {
                ++m;
                s = x * Pow2(m);
            } while (s < Pow2(48));

            return M_PI_2 / AGM(1, 4M/s) - m * M_LN2;
        }
#endif

        public static decimal Log10(decimal x) {
            if (IsExtremelySmall(x)) {
                return Decimal.MinValue;
            }

            int pow = 0;
            while (10M <= x) {
                x *= 0.1M;
                ++pow;
            }
            while (x < 1M) {
                x *= 10M;
                --pow;
            }

            return pow + Ln(x) * M_1_LN10;
        }
    }
}
