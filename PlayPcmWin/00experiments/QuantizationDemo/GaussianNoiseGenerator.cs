using System;
using System.Security.Cryptography;

namespace WWAudioFilter {
    class GaussianNoiseGenerator {
        private RNGCryptoServiceProvider mRng = new RNGCryptoServiceProvider();

        /// <summary>
        /// returns white gaussian noise σ^2 = 1
        /// </summary>
        private float NextFloatBoxMuller() {
            const double dDiv = 1.0 / ((double)UInt32.MaxValue + 1.0);
            byte[] b4 = new byte[4];

            mRng.GetNonZeroBytes(b4);
            uint   v = BitConverter.ToUInt32(b4, 0);
            double d1 = ((double)v) * dDiv;

            mRng.GetBytes(b4);
            v = BitConverter.ToUInt32(b4, 0);
            double d2 = ((double)v) * dDiv;

            double rD = Math.Sqrt(-2.0 * Math.Log(d1)) * Math.Cos(2.0 * Math.PI * d2);

            return (float)(rD);
        }

        /// <summary>
        /// returns white gaussian noise σ^2 = 1 in the range of [-1 1)
        /// </summary>
        private float NextFloatBoxMullerM1P1() {
            double rD;

            do {
                rD = NextFloatBoxMuller();
            } while ((float)rD < -1.0f || 1.0f <= (float)rD);

            return (float)(rD);
        }

        public float NextFloat() {
            return NextFloatBoxMuller();
        }
    }
}
