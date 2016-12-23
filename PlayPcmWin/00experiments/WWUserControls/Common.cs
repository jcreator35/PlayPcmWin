using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWUserControls {
    public class Common {
        public delegate WWComplex TransferFunctionDelegate(WWComplex s);
        public delegate double TimeDomainResponseFunctionDelegate(double t);

        public static string UnitNumberString(double n) {
            if (n == 0.0) {
                return "0";
            }

            double absn = Math.Abs(n);

            if (absn < 0.001 * 0.001 * 0.001) {
                return string.Format("{0:0}p", n * 1000.0 * 1000.0 * 1000.0 * 1000.0);
            }
            if (absn < 0.001 * 0.001) {
                return string.Format("{0:0}n", n * 1000.0 * 1000.0 * 1000.0);
            }
            if (absn < 0.001) {
                return string.Format("{0:0}μ", n * 1000.0 * 1000.0);
            }
            if (absn < 1) {
                return string.Format("{0:0}m", n * 1000.0);
            }

            if (absn < 1000) {
                return string.Format("{0}", n);
            }
            if (absn < 1000 * 10) {
                return string.Format("{0:0}k", n / 1000);
            }
            if (absn < 1000 * 100) {
                if (absn / 100 == (int)(absn / 100)) {
                    // 10.00kとか20.00kは10k,20kと書く
                    return string.Format("{0:0}k", n / 1000);
                }
                return string.Format("{0:0.0}k", n / 1000);
            }
            if (absn < 1000 * 1000) {
                return string.Format("{0:0}k", n / 1000);
            }
            if (absn < 1000 * 1000 * 10) {
                if (absn / 10000 == (int)(absn / 10000)) {
                    // 1.00Mとか2.00Mは1M,2Mと書く
                    return string.Format("{0:0}M", n / 1000 / 1000);
                }
                return string.Format("{0:0.00}M", n / 1000 / 1000);
            }
            if (absn < 1000 * 1000 * 100) {
                if (absn / 100000 == (int)(absn / 100000)) {
                    // 10.0Mとか20.0Mは10M,20Mと書く
                    return string.Format("{0:0}M", n / 1000 / 1000);
                }
                return string.Format("{0:0.0}M", n / 1000 / 1000);
            }
            if (absn < 1000 * 1000 * 1000) {
                return string.Format("{0:0}M", n / 1000 / 1000);
            }

            return string.Format("{0.00}G", n / 1000 / 1000 / 1000);
        }


    }
}
