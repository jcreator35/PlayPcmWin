using System;

namespace WWDirectComputeCS {
    public class WWHilbert {
        public enum HilbertFilterType {
            Bandlimited,
            HighPass
        };

        public static double[] HilbertFirCoeff(HilbertFilterType filterType, int filterLength) {
            System.Diagnostics.Debug.Assert(0 < filterLength && ((filterLength & 1) == 1));

            double [] rv = new double[filterLength];

            switch (filterType) {
            case HilbertFilterType.Bandlimited:
                for (int i=0; i < filterLength; ++i) {
                    int m = i - filterLength / 2;
                    if ((m & 1) == 0) {
                        rv[i] = 0;
                    } else {
                        rv[i] = 2.0 / m / Math.PI;
                    }
                }
                break;
            case HilbertFilterType.HighPass:
                for (int i=0; i < filterLength; ++i) {
                    int m = (i - filterLength / 2)*2+1;
                    rv[i] = 2.0 / m / Math.PI;
                }
                break;
            }

            return rv;
        }
    }
}
