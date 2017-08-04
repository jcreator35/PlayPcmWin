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
        /// https://en.wikipedia.org/wiki/Linear-feedback_shift_register
        /// </summary>
        public MaximumLengthSequence(int order) {
            mOrder = order;
            switch (order) {
            case 3:
                mSequence = Create3();
                break;
            case 4:
                mSequence = Create4();
                break;
            case 5:
                mSequence = Create5();
                break;
            case 6:
                mSequence = Create6();
                break;
            case 7:
                mSequence = Create7();
                break;
            case 8:
                mSequence = Create8();
                break;
            case 16:
                mSequence = Create16();
                break;
            case 17:
                mSequence = Create17();
                break;
            case 18:
                mSequence = Create18();
                break;
            case 19:
                mSequence = Create19();
                break;
            case 20:
                mSequence = Create20();
                break;
            default:
                throw new ArgumentException("order");
            }
        }

        public byte[] Sequence() {
            return mSequence;
        }

        /// <summary>
        /// order=3
        /// </summary>
        private byte[] Create3() {
            int order = 3;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 7;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==3, nPoly=2, 3,2
                bit = ((lfsr >> 0) ^ (lfsr >> 2)) & 1;
                lfsr = (lfsr >> 1) | (bit << 2);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=4
        /// trinomial。
        /// x^4 + x + 1
        /// </summary>
        private byte[] Create4() {
            int order = 4;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 15;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==4, nPoly=2, 4,1
                bit = ((lfsr >> 0) ^ (lfsr >> 1)) & 1;
                lfsr = (lfsr >> 1) | (bit << 3);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=5
        /// trinomial。
        /// x^5 + x^2 + 1
        /// </summary>
        private byte[] Create5() {
            int order = 5;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 31;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==5, nPoly=2, 5,2
                bit = ((lfsr >> 0) ^ (lfsr >> 2)) & 1;
                lfsr = (lfsr >> 1) | (bit << 4);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=6
        /// trinomial。
        /// x^6 + x + 1
        /// </summary>
        private byte[] Create6() {
            int order = 6;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 63;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==6, nPoly=2, 6,1
                bit = ((lfsr >> 0) ^ (lfsr >> 1)) & 1;
                lfsr = (lfsr >> 1) | (bit << 5);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }
        
        /// <summary>
        /// order=7
        /// trinomial。
        /// x^7 + x^6 + 1
        /// </summary>
        private byte[] Create7() {
            int order = 7;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 127;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==7, nPoly=2, 7,6
                bit = ((lfsr >> 0) ^ (lfsr >> 6)) & 1;
                lfsr = (lfsr >> 1) | (bit << 6);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=8
        /// trinomialではなく比較的計算量が多い。
        /// x^8 + x^4 + x^3 + x^2 + 1
        /// </summary>
        private byte[] Create8() {
            int order = 8;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 255;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==8, nPoly=4, 8,4,3,2
                bit = ((lfsr >> 4) ^ (lfsr >> 3) ^ (lfsr >> 2) ^ (lfsr >> 0)) & 1;
                lfsr = (lfsr >> 1) | (bit << 7);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=16
        /// x^16 + x^5 + x^3 + x^2 + 1
        /// </summary>
        private byte[] Create16() {
            int order = 16;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 65535;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==16, nPoly=4, 16,5,3,2
                bit = ((lfsr >> 0) ^ (lfsr >> 2) ^ (lfsr >> 3) ^ (lfsr >> 5)) & 1;
                lfsr = (lfsr >> 1) | (bit << 15);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=17
        /// trinomialで効率的。
        /// x^17 + x^3 + 1
        /// </summary>
        private byte[] Create17() {
            int order = 17;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 131071;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // 0x10004 nPoly=2,17 3
                bit = ((lfsr >> 0) ^ (lfsr >> 3)) & 1;
                lfsr = (lfsr >> 1) | (bit << 16);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=18
        /// trinomialで効率的。
        /// x^18 + x^7 + 1
        /// </summary>
        private byte[] Create18() {
            int order = 18;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 262143;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // 0x20040 nPoly=2,18 7
                bit = ((lfsr >> 0) ^ (lfsr >> 7)) & 1;
                lfsr = (lfsr >> 1) | (bit << 17);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=19
        /// x^19 + x^5 + x^2 + x + 1
        /// </summary>
        private byte[] Create19() {
            int order = 19;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 524287;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // 0x40013 nPoly=4,19 5 2 1
                bit = ((lfsr >> 0) ^ (lfsr >> 1) ^ (lfsr >> 2) ^ (lfsr >> 5)) & 1;
                lfsr = (lfsr >> 1) | (bit << 18);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=20
        /// trinomialで効率的。
        /// x^20 + x^3 + 1
        /// </summary>
        private byte[] Create20() {
            int order = 20;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 1048575;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // 0x80004 nPoly=2,20 3
                bit = ((lfsr >> 0) ^ (lfsr >> 3)) & 1;
                lfsr = (lfsr >> 1) | (bit << 19);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }
    }
}
