using System;

namespace WWMath {
    public class MaximumLengthSequence {
        /// <summary>
        /// Maximum Length Sequenceを生成する。
        /// 値は1か0、(2^order)-1要素の配列が出る。
        /// order==4のとき、(2^4)-1 = 15個のsequenceが出る
        /// 
        /// https://en.wikipedia.org/wiki/Maximum_length_sequence
        /// </summary>
        public static byte[] Create(int order) {
            System.Diagnostics.Debug.Assert(4 <= order);
            System.Diagnostics.Debug.Assert(order <= 30);

            int count = (int)(Math.Pow(2, order) - 1);
            var b = new byte[count];
            var a = new byte[2][];
            a[0] = new byte[order];
            a[1] = new byte[order];

            a[0][0] = 1;

            int cur = 0;
            int next = 1;
            for (int pos = 0; pos < count; ++pos) {
                a[next][order - 1] = (byte)((a[cur][0] + a[cur][1]) % 2);
                for (int i = order - 2; 0 <= i; --i) {
                    a[next][i] = a[cur][i + 1];
                }

                b[pos] = a[next][0];

                cur = cur == 0 ? 1 : 0;
                next = next == 0 ? 1 : 0;
            }

            return b;
        }
    }
}
