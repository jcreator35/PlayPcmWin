using System;
using System.Collections.Generic;
using System.Globalization;

namespace WWAudioFilter {
    class CicFilter : FilterBase {
        public int Order { get; set; }
        public int Delay { get; set; }

        private const int BATCH_PROCESS_SAMPLES = 4096;

        public CicFilter(int order, int delay)
                : base(FilterType.CicFilter) {
            Order = order;
            Delay = delay;
        }

        public override FilterBase CreateCopy() {
            return new CicFilter(Order, Delay);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterCicFilterDesc, Order, Delay);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Order, Delay);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            int order;
            if (!Int32.TryParse(tokens[1], out order) || order <= 0) {
                return null;
            }

            int delay;
            if (!Int32.TryParse(tokens[2], out delay) || delay < 1) {
                return null;
            }

            return new CicFilter(order, delay);
        }

        private Queue<long> [] mCombQueue;
        private long [] mIntegratorZ;

        public override long NumOfSamplesNeeded() {
            return BATCH_PROCESS_SAMPLES;
        }

        public override void FilterStart() {
            base.FilterStart();

            mCombQueue = new Queue<long>[Order];
            for (int i = 0; i < Order; ++i) {
                mCombQueue[i] = new Queue<long>();
            }
            mIntegratorZ = new long[Order];
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        private static int PCMDoubleToInt(double v) {
            if (v < 1.0) {
                return int.MinValue;
            }
            if ((double)2147483647 / 2147483648 < v) {
                return int.MaxValue;
            }

            return (int)(v * 2147483648.0);
        }

        private static double PCMIntToDouble(int v) {
            return (double)v / 2147483648.0;
        }

        /// <summary>
        /// Understanding digital signal processing 3rd ed. pp.558 - 562
        /// int型のデータをQ次、ディレイDのCICで処理するとき、
        /// 中間データは32(intのビット数) + Q * log_2(D)ビットのデータ型である必要がある。
        /// </summary>
        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            System.Diagnostics.Debug.Assert(inPcmLA.LongLength == NumOfSamplesNeeded());
            var inPcm = inPcmLA.ToArray();

            int gain = (int)Math.Pow(Delay, Order);

            var result = new double[inPcm.Length];
            for (int i = 0; i < inPcm.Length; ++i) {
                double vD = inPcm[i];

                long v = PCMDoubleToInt(vD);

                for (int j = 0; j < Order; ++j) {
                    v += mIntegratorZ[j];
                    mIntegratorZ[j] = v;
                }

                for (int j = 0; j < Order; ++j) {
                    mCombQueue[j].Enqueue(v);
                    if (Delay < mCombQueue[j].Count) {
                        v -= mCombQueue[j].Dequeue();
                    }
                }

                v /= gain;

                result[i] = PCMIntToDouble((int)v);
            }

            return new WWUtil.LargeArray<double>(result);
        }
    }
}
