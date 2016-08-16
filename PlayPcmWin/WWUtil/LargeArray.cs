using System;
using System.Reflection.Emit;
using System.Diagnostics.Contracts;

namespace WWUtil {

    /// <summary>
    /// 大きいサイズの配列。
    /// </summary>
    /// <typeparam name="T">byte, [u]short, [u]int, float, doubleが適する</typeparam>
    public class LargeArray<T> {
        /// <summary>
        /// TにWWComplex型を入れた時に配列が2GBを超えないようにする。
        /// あまりギリギリのサイズにしないようにした。
        /// WWComplexのLargeArrayが範囲を超えなければ良しとする。
        /// </summary>
        public const int ARRAY_FRAGMENT_LENGTH_NUM = 64 * 1024 * 1024;

        private readonly long mCount;
        private T[][] mArrayArray;

        public LargeArray<T> Clone() {
            var cloned = new LargeArray<T>(mCount);
            cloned.CopyFrom(this, 0, 0, mCount);
            return cloned;
        }

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

            int arrayNum = (int)((count + ARRAY_FRAGMENT_LENGTH_NUM - 1) / ARRAY_FRAGMENT_LENGTH_NUM);
            mArrayArray = new T[arrayNum][];

            int idx = 0;
            for (long remain=count; 0 < remain; ) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentCount) {
                    fragmentCount = (int)remain;
                }

                var a = new T[fragmentCount];

                mArrayArray[idx++] = a;
                remain -= fragmentCount;
            }
        }

        /// <summary>
        /// 配列から作成する。fromを参照することがある。
        /// </summary>
        public LargeArray(T[] from) {
            if (from == null) {
                throw new ArgumentNullException("from");
            }

            // mCountはINT_MAXよりも少ないはずである。
            mCount = from.Length;

            if (mCount == 0) {
                mArrayArray=new T[1][];
                mArrayArray[0] = new T[0];
                return;
            }
            
            int arrayNum = (int)((mCount + ARRAY_FRAGMENT_LENGTH_NUM - 1) / ARRAY_FRAGMENT_LENGTH_NUM);
            mArrayArray = new T[arrayNum][];

            if (arrayNum == 1) {
                mArrayArray[0] = from;
                return;
            }

            int idx = 0;
            int fromPos = 0;
            for (int remain = from.Length; 0 < remain; ) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentCount) {
                    fragmentCount = remain;
                }

                var fragment = new T[fragmentCount];
                Array.Copy(from, fromPos, fragment, 0, fragmentCount);
                mArrayArray[idx++] = fragment;

                fromPos += fragmentCount;
                remain  -= fragmentCount;
            }
        }

        /// <summary>
        /// 中身が出てくる。
        /// </summary>
        /// <param name="nth"></param>
        /// <returns></returns>
        public T[] ArrayNth(int nth) {
            return mArrayArray[nth];
        }

        /// <summary>
        /// 中に持っている配列の数。
        /// </summary>
        public int ArrayNum() {
            return mArrayArray.Length;
        }

        /// <summary>
        /// インスタンスを複製するが、新しいインスタンスの要素数はnewCount個にする。
        /// つまり内容を最大newCount個コピーする。自分自身は変わらない。
        /// </summary>
        /// <param name="newCount">returnで戻る新しいインスタンスの要素数</param>
        /// <returns>要素数がnewCount個になったインスタンス。</returns>
        [Pure]
        public LargeArray<T> Resize(long newCount) {
            var result = new LargeArray<T>(newCount);
            long copyCount = newCount;
            if (mCount < copyCount) {
                copyCount = mCount;
            }
            CopyTo(0, ref result, 0, copyCount);
            return result;
        }

        /// <summary>
        /// 要素数を戻す。バイト数ではありません。
        /// </summary>
        public long LongLength { get { return mCount; } }

        [Pure]
        public T At(long pos) {
            if (pos < 0 || mCount <= pos) {
                throw new ArgumentOutOfRangeException("pos");
            }

            if (pos < ARRAY_FRAGMENT_LENGTH_NUM) {
                // 高速化。このif文はなくても動作する。
                return mArrayArray[0][pos];
            }

            int arrayIdx  = (int)(pos / ARRAY_FRAGMENT_LENGTH_NUM);
            int arrayOffs = (int)(pos % ARRAY_FRAGMENT_LENGTH_NUM);

            // System.Diagnostics.Debug.Assert(arrayIdx < mArrayArray.Length && arrayOffs < mArrayArray[arrayIdx].Length);

            return mArrayArray[arrayIdx][arrayOffs];
        }

        public void Set(long pos, T val) {
            if (pos < 0 || mCount <= pos) {
                throw new ArgumentOutOfRangeException("pos");
            }

            if (pos < ARRAY_FRAGMENT_LENGTH_NUM) {
                // 高速化。このif文はなくても動作する。
                mArrayArray[0][pos] = val;
                return;
            }

            int arrayIdx  = (int)(pos / ARRAY_FRAGMENT_LENGTH_NUM);
            int arrayOffs = (int)(pos % ARRAY_FRAGMENT_LENGTH_NUM);

            mArrayArray[arrayIdx][arrayOffs] = val;
        }

        public int CopyFrom(T[] from, int fromPos, long toPos, int count) {
            if (from == null) {
                throw new ArgumentNullException("from");
            }
            if (fromPos < 0 || from.Length < fromPos + count) {
                throw new ArgumentOutOfRangeException("fromPos");
            }
            if (toPos < 0 || mCount < toPos + count) {
                throw new ArgumentOutOfRangeException("toPos");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            for (int remain = count; 0 < remain; ) {
                int arrayIdx = (int)(toPos / ARRAY_FRAGMENT_LENGTH_NUM);
                int arrayOffs = (int)(toPos % ARRAY_FRAGMENT_LENGTH_NUM);

                int fragmentCount = ARRAY_FRAGMENT_LENGTH_NUM - arrayOffs;
                if (remain < fragmentCount) {
                    fragmentCount = remain;
                }

                Array.Copy(from, fromPos, mArrayArray[arrayIdx], arrayOffs, fragmentCount);

                fromPos += fragmentCount;
                toPos += fragmentCount;
                remain -= fragmentCount;
            }

            return count;
        }

        /// <summary>
        /// toにコピー。
        /// </summary>
        /// <param name="fromPos">コピー元(この配列)先頭要素番号。</param>
        /// <param name="to">コピー先配列。</param>
        /// <param name="toPos">コピー先(to[])先頭要素番号。</param>
        /// <param name="count">コピーする要素数。</param>
        /// <returns>コピーした要素数。</returns>
        [Pure]
        public int CopyTo(long fromPos, ref T[] to, int toPos, int count) {
            if (to == null) {
                throw new ArgumentNullException("to");
            }
            if (fromPos < 0 || mCount < fromPos + count) {
                throw new ArgumentOutOfRangeException("fromPos");
            }
            if (toPos < 0 || to.Length < toPos + count) {
                throw new ArgumentOutOfRangeException("toPos");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            for (int remain = count; 0 < remain; ) {
                int arrayIdx  = (int)(fromPos / ARRAY_FRAGMENT_LENGTH_NUM);
                int arrayOffs = (int)(fromPos % ARRAY_FRAGMENT_LENGTH_NUM);

                int fragmentCount = ARRAY_FRAGMENT_LENGTH_NUM - arrayOffs;
                if (remain < fragmentCount) {
                    fragmentCount = remain;
                }

                Array.Copy(mArrayArray[arrayIdx], arrayOffs, to, toPos, fragmentCount);

                fromPos += fragmentCount;
                toPos   += fragmentCount;
                remain  -= fragmentCount;
            }

            return count;
        }

        public long CopyFrom(LargeArray<T> from, long fromPos, long toPos, long count) {
            if (from == null) {
                throw new ArgumentNullException("from");
            }
            if (fromPos < 0 || from.LongLength < fromPos + count) {
                throw new ArgumentOutOfRangeException("fromPos");
            }
            if (toPos < 0 || mCount < toPos + count) {
                throw new ArgumentOutOfRangeException("toPos");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            for (long remain = count; 0 < remain; ) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentCount) {
                    fragmentCount = (int)(remain);
                }

                var fragment = new T[fragmentCount];
                from.CopyTo(fromPos, ref fragment, 0, fragmentCount);
                CopyFrom(fragment, 0, toPos, fragmentCount);

                fromPos += fragmentCount;
                toPos   += fragmentCount;
                remain  -= fragmentCount;
            }

            return count;
        }

        [Pure]
        public long CopyTo(long fromPos, ref LargeArray<T> to, long toPos, long count) {
            if (to == null) {
                throw new ArgumentNullException("to");
            }
            if (fromPos < 0 || mCount < fromPos + count) {
                throw new ArgumentOutOfRangeException("fromPos");
            }
            if (toPos < 0 || to.LongLength < toPos + count) {
                throw new ArgumentOutOfRangeException("toPos");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            for (long remain = count; 0 < remain; ) {
                int fragmentCount = ARRAY_FRAGMENT_LENGTH_NUM;
                if (remain < fragmentCount) {
                    fragmentCount = (int)remain;
                }

                var fragment = new T[fragmentCount];
                CopyTo(fromPos, ref fragment, 0, fragmentCount);
                to.CopyFrom(fragment, 0, toPos, fragmentCount);

                fromPos += fragmentCount;
                toPos += fragmentCount;
                remain -= fragmentCount;
            }

            return count;
        }

        /// <summary>
        /// ただの配列に変換する。mCountが小さいとき可能。
        /// </summary>
        [Pure]
        public T[] ToArray() {
            if (Int32.MaxValue < mCount) {
                throw new ArgumentOutOfRangeException();
            }

            var result = new T[mCount];
            CopyTo(0, ref result, 0, (int)mCount);
            return result;
        }
    }
}
