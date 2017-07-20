using System;

namespace WWMath {
    public class MaximumLengthSequence {
        private int mOrder;
        private byte[] mSequence;

        /// <summary>
        /// Maximum Length Sequenceを生成する。
        /// 値は1か0、(2^order)-1要素の配列が出る。
        /// order==4のとき、(2^4)-1 = 15個のsequenceが出る
        /// 
        /// https://en.wikipedia.org/wiki/Maximum_length_sequence
        /// </summary>
        public MaximumLengthSequence(int order) {
            mOrder = order;
            switch (order) {
            case 16:
                mSequence = Create16();
                break;
            default:
                throw new ArgumentException("order");
            }
        }

        public byte[] Sequence() {
            return mSequence;
        }

        /// <summary>
        /// 
        /// https://en.wikipedia.org/wiki/Linear-feedback_shift_register
        /// </summary>
        private byte[] Create16() {
            int order = 16;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            ushort start_state = 1;
            ushort lfsr = start_state;
            ushort bit;
            uint period = 0;

            do {
                bit = (ushort)(((lfsr >> 0) ^ (lfsr >> 2) ^ (lfsr >> 3) ^ (lfsr >> 5)) & 1);
                lfsr = (ushort)((lfsr >> 1) | (bit << 15));
                ++period;
            } while (lfsr != start_state);


            return b;
        }
    }
}
