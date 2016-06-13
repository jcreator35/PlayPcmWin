using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    class LargeArray<T> {
        public LargeArray(long count) {
            mCount = count;

            int arrayNum = (int)((count + ARRAY_LENGTH_MAX-1) / ARRAY_LENGTH_MAX);
            mArrayArray = new T[arrayNum][];

            int idx = 0;
            long remains = count;
            do {
                long size = remains;
                if (ARRAY_LENGTH_MAX < remains) {
                    size = ARRAY_LENGTH_MAX;
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

        public T At(long pos) {
            if (pos < 0 || mCount <= pos) {
                throw new ArgumentOutOfRangeException("pos");
            }

            int arrayIdx = (int)(pos / ARRAY_LENGTH_MAX);
            int arrayOffs = (int)(pos % ARRAY_LENGTH_MAX);

            return mArrayArray[arrayIdx][arrayOffs];
        }

        public void Set(long pos, T val) {
            if (pos < 0 || mCount <= pos) {
                throw new ArgumentOutOfRangeException("pos");
            }

            int arrayIdx = (int)(pos / ARRAY_LENGTH_MAX);
            int arrayOffs = (int)(pos % ARRAY_LENGTH_MAX);

            mArrayArray[arrayIdx][arrayOffs] = val;
        }

        private const int ARRAY_LENGTH_MAX = 1024 * 1024;
        private long mCount;
        private T[][] mArrayArray;
    }
}
