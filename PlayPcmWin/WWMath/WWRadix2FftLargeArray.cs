﻿using System;
using WWUtil;

namespace WWMath {
    public class WWRadix2FftLargeArray {
        private long mNumPoints;
        private int mNumStage;
        private LargeArray<WWComplex> mWn;
        private LargeArray<ulong> mBitReversalTable;

        public WWRadix2FftLargeArray(long numPoints) {
            if (!Functions.IsPowerOfTwo(numPoints) || numPoints < 2) {
                throw new ArgumentException("numPoints must be power of two integer and larger than 2");
            }
            mNumPoints = numPoints;

            mWn = new LargeArray<WWComplex>(mNumPoints);
            for (long i=0; i < mNumPoints; ++i) {
                double angle = -2.0 * Math.PI * i / mNumPoints;
                mWn.Set(i, new WWComplex(Math.Cos(angle), Math.Sin(angle)));
            }

            // mNumStage == log_2(mNumPoints)
            long t = mNumPoints;
            for (int i=0; 0 < t; ++i) {
                t >>= 1;
                mNumStage = i;
            }

            mBitReversalTable = new LargeArray<ulong>(mNumPoints);
            for (long i=0; i < mNumPoints; ++i) {
                mBitReversalTable.Set(i, BitReversal(mNumStage, (ulong)i));
            }
        }

        private static long Pow2(int x) {
            return 1L << x;
        }

        private static ulong BitReversal(int numOfBits, ulong v) {
            ulong r = v;
            int s = numOfBits - 1;

            for (v >>= 1; v!=0; v >>= 1) {
                r <<= 1;
                r |= v & 1;
                s--;
            }

            r <<= s;

            ulong mask = ~(0xffffffffffffffffUL << numOfBits);
            r &= mask;

            return r;
        }

        public LargeArray<WWComplex> ForwardFft(LargeArray<WWComplex> aFrom) {
            if (aFrom == null || aFrom.LongLength != mNumPoints) {
                throw new ArgumentOutOfRangeException("aFrom");
            }
            var aTo = new LargeArray<WWComplex>(aFrom.LongLength);

            var aTmp0 = new LargeArray<WWComplex>(mNumPoints);
            for (int i=0; i < aTmp0.LongLength; ++i) {
                aTmp0.Set(i, aFrom.At((long)mBitReversalTable.At(i)));
            }
            var aTmp1 = new LargeArray<WWComplex>(mNumPoints);
            for (int i=0; i < aTmp1.LongLength; ++i) {
                aTmp1.Set(i, WWComplex.Zero());
            }

            var aTmps = new LargeArray<WWComplex>[2];
            aTmps[0] = aTmp0;
            aTmps[1] = aTmp1;

            for (int i=0; i < mNumStage - 1; ++i) {
                FftStageN(i, aTmps[((i & 1) == 1) ? 1 : 0], aTmps[((i & 1) == 0) ? 1 : 0]);
            }
            FftStageN(mNumStage - 1, aTmps[(((mNumStage - 1) & 1) == 1) ? 1 : 0], aTo);

            return aTo;
        }

        public LargeArray<WWComplex> InverseFft(LargeArray<WWComplex> aFrom,
                double? compensation = null) {
            for (int i=0; i < aFrom.LongLength; ++i) {
                var t = new WWComplex(aFrom.At(i).real, -aFrom.At(i).imaginary);
                aFrom.Set(i, t);
            }

            var aTo = ForwardFft(aFrom);

            double c = 1.0 / mNumPoints;
            if (compensation != null) {
                c = (double)compensation;
            }

            for (int i=0; i < aTo.LongLength; ++i) {
                var t = new WWComplex(aTo.At(i).real * c, -aTo.At(i).imaginary * c);
                aTo.Set(i, t);
            }

            return aTo;
        }

        private void FftStageN(int stageNr, LargeArray<WWComplex> x, LargeArray<WWComplex> y) {
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

            long nRepeat    = Pow2(mNumStage - stageNr - 1);
            long nSubRepeat = mNumPoints / nRepeat;

            for (long i=0; i<nRepeat; ++i) {
                long offsBase = i * nSubRepeat;

                bool allZero = true;
                for (long j=0; j < nSubRepeat/2; ++j) {
                    long offs = offsBase + (j % (nSubRepeat/2));
                    if (Double.Epsilon < x.At(offs).Magnitude()) {
                        allZero = false;
                        break;
                    }
                    if (Double.Epsilon < x.At(offs + nSubRepeat / 2).Magnitude()) {
                        allZero = false;
                        break;
                    }
                }

                if (allZero) {
                    for (long j=0; j < nSubRepeat; ++j) {
                        y.Set(j + offsBase, WWComplex.Zero());
                    }
                } else {
                    for (long j=0; j < nSubRepeat; ++j) {
                        long offs = offsBase + (j % (nSubRepeat / 2));
                        var t = x.At(offs);

                        var t2 = WWComplex.Mul(mWn.At(j * nRepeat), x.At(offs + nSubRepeat / 2));
                        var t3 = WWComplex.Add(t, t2);

                        y.Set(j + offsBase, t3);
                    }
                }
            }
        }
    }
}
