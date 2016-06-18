using System;
using System.Reflection.Emit;

namespace PcmDataLib {

    /// <summary>
    /// 基本データ型の大きいサイズの配列。
    /// </summary>
    /// <typeparam name="T">byte, [u]short, [u]int, float, doubleが適する</typeparam>
    public class LargeArray<T> {
        /// <summary>
        /// TにWWComplex型を入れた時に配列が2GBを超えないようにする。
        /// あまりギリギリのサイズにしないようにした。
        /// </summary>
        public const int ARRAY_FRAGMENT_LENGTH_MAX = 1024 * 1024;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="count">要素数。バイト数ではありません。</param>
        public LargeArray(long count) {
            mCount = count;
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count == 0) {
                mArrayArray = new T[1][];
                mArrayArray[0] = new T[0];
                return;
            }

            int arrayNum = (int)((count + ARRAY_FRAGMENT_LENGTH_MAX - 1) / ARRAY_FRAGMENT_LENGTH_MAX);
            mArrayArray = new T[arrayNum][];

            int idx = 0;
            long remains = count;
            do {
                long size = remains;
                if (ARRAY_FRAGMENT_LENGTH_MAX < remains) {
                    size = ARRAY_FRAGMENT_LENGTH_MAX;
                }

                var a = new T[size];
                if (a == null) {
                    throw new OutOfMemoryException();
                }

                mArrayArray[idx] = a;
                ++idx;
                remains -= size;

            } while (0 < remains);
        }

        /// <summary>
        /// 配列から作成する。fromを参照することがある。
        /// </summary>
        public LargeArray(T[] from) {

            // mCountはINT_MAXよりも少ないはずである。
            mCount = from.Length;

            if (mCount == 0) {
                mArrayArray=new T[1][];
                mArrayArray[0] = new T[0];
                return;
            }
            
            int arrayNum = (int)((mCount + ARRAY_FRAGMENT_LENGTH_MAX - 1) / ARRAY_FRAGMENT_LENGTH_MAX);
            mArrayArray = new T[arrayNum][];

            if (arrayNum == 1) {
                mArrayArray[0] = from;
                return;
            }

            int arrayIdx = 0;
            for (int offs = 0; offs < mCount; offs += ARRAY_FRAGMENT_LENGTH_MAX) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_MAX;
                if (mCount < offs+fragmentCount) {
                    fragmentCount = (int)mCount - offs;
                }

                var fragment = new T[fragmentCount];
                Array.Copy(from, offs, fragment, 0, fragmentCount);
                mArrayArray[arrayIdx] = fragment;
                ++arrayIdx;
            }
        }

        /// <summary>
        /// 要素数を戻す。バイト数ではありません。
        /// </summary>
        public long LongLength { get { return mCount; } }

        public T At(long pos) {
            if (pos < 0 || mCount <= pos) {
                throw new ArgumentOutOfRangeException("pos");
            }

            int arrayIdx = (int)(pos / ARRAY_FRAGMENT_LENGTH_MAX);
            int arrayOffs = (int)(pos % ARRAY_FRAGMENT_LENGTH_MAX);

            // System.Diagnostics.Debug.Assert(arrayIdx < mArrayArray.Length && arrayOffs < mArrayArray[arrayIdx].Length);

            return mArrayArray[arrayIdx][arrayOffs];
        }

        public void Set(long pos, T val) {
            if (pos < 0 || mCount <= pos) {
                throw new ArgumentOutOfRangeException("pos");
            }

            int arrayIdx = (int)(pos / ARRAY_FRAGMENT_LENGTH_MAX);
            int arrayOffs = (int)(pos % ARRAY_FRAGMENT_LENGTH_MAX);

            mArrayArray[arrayIdx][arrayOffs] = val;
        }

        /// <summary>
        /// toにコピー。
        /// </summary>
        /// <param name="fromOffsCount">コピー元(この配列)先頭要素番号。</param>
        /// <param name="to">コピー先配列。</param>
        /// <param name="toOffsCount">コピー先(to[])先頭要素番号。</param>
        /// <param name="copyCount">コピーする要素数。</param>
        /// <returns>コピーした要素数。</returns>
        public int CopyTo(long fromOffsCount, T[] to, int toOffsCount, int copyCount) {
            if (mCount < fromOffsCount + copyCount) {
                copyCount = (int)(mCount - fromOffsCount);
            }
            if (copyCount < 0) {
                copyCount = 0;
            }

            for (int i = 0; i < copyCount; ++i) {
                to[toOffsCount + i] = At(fromOffsCount + i);
            }

            return copyCount;
        }

        public long CopyTo(long fromOffsCount, LargeArray<T> to, long toOffsCount, long totalCopyCount) {
            if (mCount < fromOffsCount + totalCopyCount) {
                totalCopyCount = mCount - fromOffsCount;
            }
            if (totalCopyCount < 0) {
                totalCopyCount = 0;
            }

            long fromPos = fromOffsCount;
            long toPos = toOffsCount;

            for (long i = 0; i < totalCopyCount; i += ARRAY_FRAGMENT_LENGTH_MAX) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_MAX;
                if (mCount < fromPos + fragmentCount) {
                    fragmentCount = (int)(mCount - fromPos);
                }

                var fragment = new T[fragmentCount];
                CopyTo(fromPos, fragment, 0, fragmentCount);
                to.CopyFrom(fragment, 0, toPos, fragmentCount);

                fromPos += fragmentCount;
                toPos += fragmentCount;
            }

            return totalCopyCount;
        }

        /// <summary>
        /// fromからコピー。
        /// </summary>
        /// <param name="from">コピー元配列。</param>
        /// <param name="fromOffsCout">コピー元(from[])先頭要素番号。</param>
        /// <param name="toOffsCount">コピー先(この配列)先頭要素番号。</param>
        /// <param name="copyCount">コピーする要素数。</param>
        /// <returns>コピーした要素数。</returns>
        public int CopyFrom(T[] from, int fromOffsCount, long toOffsCount, int copyCount) {
            if (mCount < toOffsCount + copyCount) {
                copyCount = (int)(mCount - toOffsCount);
            }
            if (copyCount < 0) {
                copyCount = 0;
            }

            for (int i = 0; i < copyCount; ++i) {
                Set(toOffsCount + i, from[fromOffsCount + i]);
            }

            return copyCount;
        }

        public long CopyFrom(LargeArray<T> from, long fromOffsCount, long toOffsCount, long totalCopyCount) {
            if (mCount < toOffsCount + totalCopyCount) {
                totalCopyCount = mCount - toOffsCount;
            }
            if (totalCopyCount < 0) {
                totalCopyCount = 0;
            }

            long fromPos = fromOffsCount;
            long toPos = toOffsCount;

            for (long i = 0; i < totalCopyCount; i += ARRAY_FRAGMENT_LENGTH_MAX) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_MAX;
                if (mCount < toPos + fragmentCount) {
                    fragmentCount = (int)(mCount - toPos);
                }

                var fragment = new T[fragmentCount];
                from.CopyTo(fromPos, fragment, 0, fragmentCount);
                CopyFrom(fragment, 0, toPos, fragmentCount);

                fromPos += fragmentCount;
                toPos   += fragmentCount;
            }

            return totalCopyCount;
        }

        /// <summary>
        /// ただの配列に変換する。mCountが小さいとき可能。
        /// </summary>
        public T[] ToArray() {
            if (Int32.MaxValue < mCount) {
                throw new ArgumentOutOfRangeException();
            }

            var result = new T[mCount];
            CopyTo(0, result, 0, (int)mCount);
            return result;
        }

        private long mCount;
        private T[][] mArrayArray;
    }
}
