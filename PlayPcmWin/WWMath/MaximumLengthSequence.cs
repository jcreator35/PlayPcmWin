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
        /// https://users.ece.cmu.edu/~koopman/lfsr/index.html
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
            case 9:
                mSequence = Create9();
                break;
            case 10:
                mSequence = Create10();
                break;
            case 11:
                mSequence = Create11();
                break;
            case 12:
                mSequence = Create12();
                break;
            case 13:
                mSequence = Create13();
                break;
            case 14:
                mSequence = Create14();
                break;
            case 15:
                mSequence = Create15();
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
            case 21:
                mSequence = Create21();
                break;
            case 22:
                mSequence = Create22();
                break;
            case 23:
                mSequence = Create23();
                break;
            case 24:
                mSequence = Create24();
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
        /// order=9
        /// trinomial。
        /// x^9 + x^5 + 1
        /// </summary>
        private byte[] Create9() {
            int order = 9;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 511;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==9, nPoly=2, 9,5
                bit = ((lfsr >> 0) ^ (lfsr >> 5)) & 1;
                lfsr = (lfsr >> 1) | (bit << 8);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=10
        /// trinomial。
        /// x^10 + x^7 + 1
        /// </summary>
        private byte[] Create10() {
            int order = 10;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 1023;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==10, nPoly=2, 10,7
                bit = ((lfsr >> 0) ^ (lfsr >> 7)) & 1;
                lfsr = (lfsr >> 1) | (bit << 9);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=11
        /// trinomial。
        /// x^11 + x^9 + 1
        /// </summary>
        private byte[] Create11() {
            int order = 11;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 2047;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==11, nPoly=2, 11,9
                bit = ((lfsr >> 0) ^ (lfsr >> 9)) & 1;
                lfsr = (lfsr >> 1) | (bit << 10);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=12
        /// 遅い。
        /// x^12 + x^11  + x^10  + x^4 + 1
        /// </summary>
        private byte[] Create12() {
            int order = 12;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 4095;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==12, nPoly=4, 12,11,10,4
                bit = ((lfsr >> 0) ^ (lfsr >> 4) ^ (lfsr >> 10) ^ (lfsr >> 11)) & 1;
                lfsr = (lfsr >> 1) | (bit << 11);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=13
        /// 遅い。
        /// x^13 + x^12  + x^11  + x^8 + 1
        /// </summary>
        private byte[] Create13() {
            int order = 13;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 8191;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==13, nPoly=4, 13,12,11,8
                bit = ((lfsr >> 0) ^ (lfsr >> 8) ^ (lfsr >> 11) ^ (lfsr >> 12)) & 1;
                lfsr = (lfsr >> 1) | (bit << 12);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=14
        /// 遅い。
        /// x^14 + x^13  + x^12  + x2 + 1
        /// </summary>
        private byte[] Create14() {
            int order = 14;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 16383;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // order==14, nPoly=4, 14,13,12,2
                bit = ((lfsr >> 0) ^ (lfsr >> 2) ^ (lfsr >> 12) ^ (lfsr >> 13)) & 1;
                lfsr = (lfsr >> 1) | (bit << 13);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=15
        /// trinomialで効率的。
        /// x^15 + x^14 + 1
        /// </summary>
        private byte[] Create15() {
            int order = 15;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 32767;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // nPoly=2,15 14
                bit = ((lfsr >> 0) ^ (lfsr >> 14)) & 1;
                lfsr = (lfsr >> 1) | (bit << 14);

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

        /// <summary>
        /// order=21
        /// trinomialで効率的。
        /// x^21 + x^2 + 1
        /// </summary>
        private byte[] Create21() {
            int order = 21;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 2097151;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // nPoly=2,21 2
                bit = ((lfsr >> 0) ^ (lfsr >> 2)) & 1;
                lfsr = (lfsr >> 1) | (bit << 20);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=22
        /// trinomialで効率的。
        /// x^22 + x^1 + 1
        /// </summary>
        private byte[] Create22() {
            int order = 22;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 4194303;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // nPoly=2,22 1
                bit = ((lfsr >> 0) ^ (lfsr >> 1)) & 1;
                lfsr = (lfsr >> 1) | (bit << 21);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=23
        /// trinomialで効率的。
        /// x^23 + x^5 + 1
        /// </summary>
        private byte[] Create23() {
            int order = 23;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 8388607;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // nPoly=2,23 5
                bit = ((lfsr >> 0) ^ (lfsr >> 5)) & 1;
                lfsr = (lfsr >> 1) | (bit << 22);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }

        /// <summary>
        /// order=24
        /// 比較的非効率。
        /// x^24 + x^23 + x^22 + x^17 + 1
        /// </summary>
        private byte[] Create24() {
            int order = 24;
            var b = new byte[(int)Math.Pow(2, order) - 1];

            uint start_state = 16777215;
            uint lfsr = start_state;
            uint bit;
            uint period = 0;

            do {
                // nPoly=2,24 23 22 17
                bit = ((lfsr >> 0) ^ (lfsr >> 17) ^ (lfsr >> 22) ^ (lfsr >> 23)) & 1;
                lfsr = (lfsr >> 1) | (bit << 23);

                b[period] = (byte)(lfsr & 1);

                ++period;
            } while (lfsr != start_state);

            return b;
        }
    }
}
