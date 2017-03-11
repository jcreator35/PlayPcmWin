using WWMath;
using System;

namespace WWIIRFilterDesign {
    /// <summary>
    /// Building block of IIR filter. used by IIRFilter class
    /// </summary>
    class IIRFilterBlockReal128 {
        private ComplexRationalPolynomial mH;
        private decimal[] mA;
        private decimal[] mB;
        private decimal[] mV;
        private int mMaxOrder;
        
        public override string ToString() {
            return mH.ToString();
        }

        public IIRFilterBlockReal128(ComplexRationalPolynomial p) {
            mH = p;
            mMaxOrder = p.NumerOrder();
            if (mMaxOrder < p.DenomOrder()) {
                mMaxOrder = p.DenomOrder();
            }

            mV = new decimal[mMaxOrder + 1];

            mB = new decimal[mMaxOrder + 1];
            for (int i = 0; i < mB.Length; ++i) {
                mB[i] = (decimal)p.N(i).real;
            }

            mA = new decimal[mMaxOrder + 1];
            mA[0] = 1;
            for (int i = 1; i < mA.Length; ++i) {
                mA[i] = (decimal)(-p.D(i).real);
            }
        }

        public double Filter(double xDouble) {
            decimal x = (decimal)xDouble;
            decimal y = 0;

            switch (mMaxOrder) {
            case 2:
                // Transposed Direct form 2 structure
                // Discrete-time signal processing 3rd edition pp.427 figure 6.26 and equation 6.44a-d

                // equation 6.44a and 6.44b
                y = mB[0] * x + mV[1];

                // equation 6.44c
                mV[1] = mA[1] * y + mB[1] * x + mV[2];
                mV[2] = mA[2] * y + mB[2] * x;
                break;
            case 1:
                // equation 6.44a and 6.44b
                y = mB[0] * x + mV[1];

                // equation 6.44c
                mV[1] = mA[1] * y + mB[1] * x;
                break;
            default:
                throw new NotImplementedException();
            }

            return (double)y;

        }
    }
}
