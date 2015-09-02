using System;

namespace DecimalFft {
    // Radix2 fft using decimal data type
    class WWDecimalFft {
        private int mNumPoints;
        private int mNumStage;
        private WWDecimalComplex[] mWn;
        private uint [] mBitReversalTable;

        public WWDecimalFft(int numPoints) {
            if (!IsPowerOfTwo(numPoints) || numPoints < 2) {
                throw new ArgumentException("numPoints must be power of two integer and larger than 2");
            }
            mNumPoints = numPoints;

            mWn = new WWDecimalComplex[mNumPoints];
            for (int i=0; i < mNumPoints; ++i) {
                decimal angle = -2.0M * WWDecimalMath.M_PI * i / mNumPoints;
                mWn[i] = new WWDecimalComplex(WWDecimalMath.Cos(angle), WWDecimalMath.Sin(angle));
            }

            // mNumStage == log_2(mNumPoints)
            int t = mNumPoints;
            for (int i=0; 0 < t; ++i) {
                t >>= 1;
                mNumStage = i;
            }

            mBitReversalTable = new uint[mNumPoints];
            for (uint i=0; i < mNumPoints; ++i) {
                mBitReversalTable[i] = BitReversal(mNumStage, i);
            }
        }

        private static int Pow2(int x) {
            return 1 << x;
        }

        private static bool IsPowerOfTwo(int x) {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public static int NextPowerOf2(int v) {
            if (v <= 0 || 0x3fffffff < v) {
                return 0;
            }
            
            int result = 1;
            while (result < v) {
                result *= 2;
            }
            return result;
        }

        private static uint BitReversal(int numOfBits, uint v) {
            uint r = v;
            int s = numOfBits - 1;

            for (v >>= 1; v!=0; v >>= 1) {
                r <<= 1;
                r |= v & 1;
                s--;
            }

            r <<= s;

            uint mask = ~(0xffffffffU << numOfBits);
            r &= mask;

            return r;
        }

        /*
        private static void Butterfly(WWDecimalComplex vFrom0, WWDecimalComplex vFrom1, WWDecimalComplex wn, WWDecimalComplex[] vTo, int toPos) {
            vTo[toPos].CopyFrom(vFrom0);
            var t = new WWDecimalComplex(vFrom1);
            t.Mul(wn);
            vTo[toPos].Mul(t);

            vTo[toPos + 1].CopyFrom(vFrom0);
            t.Mul(-1);
            vTo[toPos + 1].Mul(t);
        }
        */

        public WWDecimalComplex[] ForwardFft(WWDecimalComplex[] aFrom, decimal scale = 1M) {
            if (aFrom == null || aFrom.Length != mNumPoints) {
                throw new ArgumentOutOfRangeException("aFrom");
            }
            var aTo = new WWDecimalComplex[aFrom.Length];

            var aTmp0 = new WWDecimalComplex[mNumPoints];
            for (int i=0; i < aTmp0.Length; ++i) {
                aTmp0[i] = new WWDecimalComplex(aFrom[mBitReversalTable[i]]);
            }
            var aTmp1 = new WWDecimalComplex[mNumPoints];
            for (int i=0; i < aTmp1.Length; ++i) {
                aTmp1[i] = new WWDecimalComplex();
            }

            var aTmps = new WWDecimalComplex[2][];
            aTmps[0] = aTmp0;
            aTmps[1] = aTmp1;

            for (int i=0; i < mNumStage - 1; ++i) {
                FftStageN(i, aTmps[((i & 1) == 1) ? 1 : 0], aTmps[((i & 1) == 0) ? 1 : 0]);
            }
            FftStageN(mNumStage - 1, aTmps[(((mNumStage - 1) & 1) == 1) ? 1 : 0], aTo);

            if (scale != 1M) {
                for (int i=0; i < aTo.Length; ++i) {
                    aTo[i].Mul(scale);
                }
            }
            return aTo;
        }

        public WWDecimalComplex[] InverseFft(WWDecimalComplex[] aFrom, decimal? compensation = null) {
            for (int i=0; i < aFrom.LongLength; ++i) {
                aFrom[i].imaginary *= -1.0M;
            }

            var aTo = ForwardFft(aFrom);

            decimal c = 1.0M / mNumPoints;
            if (compensation != null) {
                c = (decimal)compensation;
            }

            for (int i=0; i < aTo.LongLength; ++i) {
                aTo[i].real      *= c;
                aTo[i].imaginary *= -1.0M * c;
            }

            return aTo;
        }

        private void FftStageN(int stageNr, WWDecimalComplex[] x, WWDecimalComplex[] y) {
            /*
             * stage0: 2つの入力データにバタフライ演算 (length=8の時) 4回 (nRepeat=4, nSubRepeat=2)
             * y[0] = x[0] + w_n^(0*4) * x[1]
             * y[1] = x[0] + w_n^(1*4) * x[1]
             *
             * y[2] = x[2] + w_n^(0*4) * x[3]
             * y[3] = x[2] + w_n^(1*4) * x[3]
             *
             * y[4] = x[4] + w_n^(0*4) * x[5]
             * y[5] = x[4] + w_n^(1*4) * x[5]
             *
             * y[6] = x[6] + w_n^(0*4) * x[7]
             * y[7] = x[6] + w_n^(1*4) * x[7]
             */

            /*
             * stage1: 4つの入力データにバタフライ演算 (length=8の時) 2回 (nRepeat=2, nSubRepeat=4)
             * y[0] = x[0] + w_n^(0*2) * x[2]
             * y[1] = x[1] + w_n^(1*2) * x[3]
             * y[2] = x[0] + w_n^(2*2) * x[2]
             * y[3] = x[1] + w_n^(3*2) * x[3]
             *
             * y[4] = x[4] + w_n^(0*2) * x[6]
             * y[5] = x[5] + w_n^(1*2) * x[7]
             * y[6] = x[4] + w_n^(2*2) * x[6]
             * y[7] = x[5] + w_n^(3*2) * x[7]
             */

            /*
             * stage2: 8つの入力データにバタフライ演算 (length=8の時) 1回 (nRepeat=1, nSubRepeat=8)
             * y[0] = x[0] + w_n^(0*1) * x[4]
             * y[1] = x[1] + w_n^(1*1) * x[5]
             * y[2] = x[2] + w_n^(2*1) * x[6]
             * y[3] = x[3] + w_n^(3*1) * x[7]
             * y[4] = x[0] + w_n^(4*1) * x[4]
             * y[5] = x[1] + w_n^(5*1) * x[5]
             * y[6] = x[2] + w_n^(6*1) * x[6]
             * y[7] = x[3] + w_n^(7*1) * x[7]
             */

            /*
             * stageN:
             */

            int nRepeat    = Pow2(mNumStage - stageNr - 1);
            int nSubRepeat = mNumPoints / nRepeat;
            var t = new WWDecimalComplex();

            for (int i=0; i<nRepeat; ++i) {
                int offsBase = i * nSubRepeat;

                bool allZero = true;
                for (int j=0; j < nSubRepeat/2; ++j) {
                    int offs = offsBase + (j % (nSubRepeat/2));
                    if (!WWDecimalMath.IsExtremelySmall(x[offs].Magnitude())) {
                        allZero = false;
                        break;
                    }
                    if (!WWDecimalMath.IsExtremelySmall(x[offs + nSubRepeat / 2].Magnitude())) {
                        allZero = false;
                        break;
                    }
                }

                if (allZero) {
                    for (int j=0; j < nSubRepeat / 2; ++j) {
                        y[j + offsBase].Set(0, 0);
                    }
                } else {
                    for (int j=0; j < nSubRepeat; ++j) {
                        int offs = offsBase + (j % (nSubRepeat / 2));
                        y[j + offsBase].CopyFrom(x[offs]);

                        t.CopyFrom(mWn[j * nRepeat]);
                        t.Mul(x[offs + nSubRepeat / 2]);

                        y[j + offsBase].Add(t);
                    }
                }
            }
        }
    }
}
