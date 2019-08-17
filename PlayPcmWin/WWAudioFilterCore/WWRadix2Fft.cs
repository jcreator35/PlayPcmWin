using System;
using WWMath;

namespace WWAudioFilterCore {
    public class WWRadix2Fft {
        private int mNumPoints;
        private int mNumStage;
        private WWComplex[] mWn;
        private uint [] mBitReversalTable;

        public WWRadix2Fft(int numPoints) {
            if (!Functions.IsPowerOfTwo(numPoints) || numPoints < 2) {
                throw new ArgumentException("numPoints must be power of two integer and larger than 2");
            }
            mNumPoints = numPoints;

            mWn = new WWComplex[mNumPoints];
            for (int i=0; i < mNumPoints; ++i) {
                double angle = -2.0 * Math.PI * i / mNumPoints;
                mWn[i] = new WWComplex(Math.Cos(angle), Math.Sin(angle));
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

        public WWComplex[] ForwardFft(WWComplex[] aFrom) {
            if (aFrom == null || aFrom.Length != mNumPoints) {
                throw new ArgumentOutOfRangeException("aFrom");
            }
            var aTo = new WWComplex[aFrom.Length];

            var aTmp0 = new WWComplex[mNumPoints];
            for (int i=0; i < aTmp0.Length; ++i) {
                aTmp0[i] = aFrom[mBitReversalTable[i]];
            }
            var aTmp1 = new WWComplex[mNumPoints];
            for (int i=0; i < aTmp1.Length; ++i) {
                aTmp1[i] = WWComplex.Zero();
            }

            var aTmps = new WWComplex[2][];
            aTmps[0] = aTmp0;
            aTmps[1] = aTmp1;

            for (int i=0; i < mNumStage - 1; ++i) {
                FftStageN(i, aTmps[((i & 1) == 1) ? 1 : 0], aTmps[((i & 1) == 0) ? 1 : 0]);
            }
            FftStageN(mNumStage - 1, aTmps[(((mNumStage - 1) & 1) == 1) ? 1 : 0], aTo);

            return aTo;
        }

        public WWComplex[] InverseFft(WWComplex[] aFrom, double? compensation = null) {
            for (int i=0; i < aFrom.LongLength; ++i) {
                aFrom[i] = new WWComplex(aFrom[i].real, aFrom[i].imaginary * -1.0);
            }

            var aTo = ForwardFft(aFrom);

            double c = 1.0 / mNumPoints;
            if (compensation != null) {
                c = (double)compensation;
            }

            for (int i=0; i < aTo.LongLength; ++i) {
                aTo[i] = new WWComplex(aTo[i].real * c, aTo[i].imaginary * (-1.0 * c));
            }

            return aTo;
        }

        private void FftStageN(int stageNr, WWComplex[] x, WWComplex[] y) {
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
            var t = WWComplex.Zero();

            for (int i=0; i<nRepeat; ++i) {
                int offsBase = i * nSubRepeat;

                bool allZero = true;
                for (int j=0; j < nSubRepeat/2; ++j) {
                    int offs = offsBase + (j % (nSubRepeat/2));
                    if (Double.Epsilon < x[offs].Magnitude()) {
                        allZero = false;
                        break;
                    }
                    if (Double.Epsilon < x[offs + nSubRepeat / 2].Magnitude()) {
                        allZero = false;
                        break;
                    }
                }

                if (allZero) {
                    for (int j=0; j < nSubRepeat / 2; ++j) {
                        y[j + offsBase] = WWComplex.Zero();
                    }
                } else {
                    for (int j=0; j < nSubRepeat; ++j) {
                        int offs = offsBase + (j % (nSubRepeat / 2));
                        y[j + offsBase] = x[offs];

                        t = mWn[j * nRepeat];
                        t = WWComplex.Mul(t, x[offs + nSubRepeat / 2]);

                        y[j + offsBase] = WWComplex.Add(y[j + offsBase], t);
                    }
                }
            }
        }
    }
}
