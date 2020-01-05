using System;
using System.Collections.Generic;

namespace WWUtil {
    public class ListUtils<T> {
        public static T[] ArrayListToArray(List<T[]> al) {
            int count = 0;
            foreach (var item in al) {
                count += item.Length;
            }

            var r = new T[count];
            int pos = 0;
            foreach (var item in al) {
                Array.Copy(item, 0, r, pos, item.Length);
                pos += item.Length;
            }

            return r;
        }

        /// <summary>
        /// fromのstartPosからcount個のデータを取り出し配列として戻す。
        /// もしもcount個無いときは、戻るデータの個数は減る。
        /// 一つもないときは要素数0個の配列が戻る。
        /// </summary>
        public static T[] GetArrayFragment(List<T[]> from, long startPos, int count) {
            if (startPos < 0) {
                throw new ArgumentOutOfRangeException("startPos");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            var r = new T[count];

            long accPos = 0;
            int toPos = 0;
            foreach (var item in from) {
                // 例1:
                // startPos = 10
                // bytes = 10
                // itemLen=8のとき、
                // 1個目のitemは accPos=0, itemLen=8  → スキップ。
                // 2個目のitemは accPos=8, itemLen=8  → fromPos=2, toPos = 0, copyCount=6
                // 3個目のitemは accPos=16, itemLen=8 → fromPos=0, toPos = 6, copyCount=4

                if (accPos + item.Length <= startPos) {
                    // まだ最初の取得位置よりも前。
                    accPos += item.Length;
                    continue;
                }

                // ちょうど取得データが有る場所に来た。
                long fromPos = startPos - accPos;
                int copyCount = item.Length;
                if (startPos + count < fromPos + item.Length) {
                    copyCount = (int)((startPos + count) - fromPos);
                }
                if (0 < copyCount) {
                    Array.Copy(item, fromPos, r, toPos, copyCount);
                    toPos += copyCount;
                } else {
                    // 最後のコピーデータよりも後の位置に来た。
                    break;
                }
            }

            if (toPos < count) {
                // toPos個しか取得できなかったので戻り配列rのサイズを変更する。
                var tmp = new T[toPos];
                Array.Copy(r, 0, tmp, 0, toPos);
                r = tmp;
            }

            return r;
        }

        public static LargeArray<T> GetLargeArrayFragment(List<T[]> from, long startPos, long count) {
            if (startPos < 0) {
                throw new ArgumentOutOfRangeException("startPos");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            var r = new LargeArray<T>(count);

            long accPos = 0;
            long toPos = 0;
            foreach (var item in from) {
                // 例1:
                // startPos = 10
                // bytes = 10
                // itemLen=8のとき、
                // 1個目のitemは accPos=0, itemLen=8  → スキップ。
                // 2個目のitemは accPos=8, itemLen=8  → fromPos=2, toPos = 0, copyCount=6
                // 3個目のitemは accPos=16, itemLen=8 → fromPos=0, toPos = 6, copyCount=4

                if (accPos + item.Length <= startPos) {
                    // まだ最初の取得位置よりも前。
                    accPos += item.Length;
                    continue;
                }

                // ちょうど取得データが有る場所に来た。
                int fromPos = (int)(startPos - accPos);
                int copyCount = item.Length;
                if (startPos + count < fromPos + item.Length) {
                    copyCount = (int)((startPos + count) - fromPos);
                }
                if (0 < copyCount) {
                    r.CopyFrom(item, fromPos, toPos, copyCount);
                    toPos += copyCount;
                } else {
                    // 最後のコピーデータよりも後の位置に来た。
                    break;
                }
            }

            if (toPos < count) {
                // toPos個しか取得できなかったので戻り配列rのサイズを変更する。
                var tmp = new LargeArray<T>(toPos);
                tmp.CopyFrom(r, 0, 0, toPos);
                r = tmp;
            }

            return r;
        }
    }
}
