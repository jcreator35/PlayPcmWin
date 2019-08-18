// 日本語。

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WWAudioFilterCore {
    public class OnebitConversionFilter : FilterBase {
        const int INSPECT_BITS = 4;
        public int InspectCandidates() {
            return 1 << INSPECT_BITS;
        }


        public OnebitConversionFilter()
                : base(FilterType.OnebitConversion) {
        }

        public override FilterBase CreateCopy() {
            return new OnebitConversionFilter();
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterOnebitConversionDesc);
        }

        public override string ToSaveText() {
            return String.Empty;
        }

        public static FilterBase Restore(string[] tokens) {
            return new OnebitConversionFilter();
        }

        class Lpf {
            public WWIIRFilterDesign.IIRFilterGraph fg;
            public WWUtil.Delay delay;

            public Lpf(WWIIRFilterDesign.IIRFilterGraph aFG) {
                fg = aFG;
                delay = new WWUtil.Delay(INSPECT_BITS);
            }

            public Lpf(Lpf rhs) {
                fg = rhs.fg.CreateCopy();
                delay = rhs.delay.CreateCopy();
            }

            public double Filter(double x) {
                double y = fg.Filter(x);
                delay.Filter(y);
                return y;
            }
        };

        private Lpf mOriginalSignal = null;
        private Lpf[] mCandidates = null;
        private long mProcessed = 0;

        public override PcmFormat Setup(PcmFormat inputFormat) {
            double fc = 20 * 1000;
            double fs = 150 * 1000;
            int sampleFreq = 44100 * 64;

            var iir = new WWIIRFilterDesign.IIRFilterDesign();

            if (!iir.Design(fc, fs, sampleFreq, WWIIRFilterDesign.IIRFilterDesign.Method.Bilinear)) {
                Console.WriteLine("Error: iir.Design failed");
                return null;
            }

#if false
            // インパルス応答波形を出力。
            {
                var fg = iir.CreateIIRFilterGraph();
                lock (fg) {
                    using (var sw = new System.IO.StreamWriter("C:/audio/impulseResponseBW.csv")) {
                        for (int i = 0; i < 8000; ++i) {
                            double r = fg.Filter(i == 0 ? 1 : 0);
                            sw.WriteLine("{0} {1}", i, r);
                        }
                    }
                }
            }
#endif

            var filterGraph = iir.CreateIIRFilterGraph();
            mOriginalSignal = new Lpf(filterGraph);

            mCandidates = new Lpf[InspectCandidates()];
            for (int i = 0; i < InspectCandidates(); ++i) {
                mCandidates[i] = new Lpf(mOriginalSignal);
            }

            // INSPECT_BITSビットのあらゆるビットパターンをそれぞれのフィルターに投入。
            /* INSPECT_BITS == 3のとき 8通り。
             * idx==0 000 : -1, -1, -1
             * idx==1 001 : +1, -1, -1
             * idx==2 010 : -1, +1, -1
             * idx==3 011 : +1, +1, -1
             * idx==4 100 : -1, -1, +1
             * idx==5 101 : +1, -1, +1
             * idx==6 110 : -1, +1, +1
             * idx==7 111 : +1, +1, +1
             */
            Parallel.For(0, InspectCandidates(), idx => {
                var lpf = mCandidates[idx];
                for (int i = 0; i < INSPECT_BITS; ++i) {
                    int bit = 1 &(idx >> i);
                    double v = (bit == 1) ? +1.0 : -1.0;
                    lpf.Filter(v);
                }
            });

            mProcessed = 0;

            return new PcmFormat(inputFormat);
        }

        private double CalcCost(Lpf a, Lpf b) {
            double cost = 0;

            int N = a.delay.DelaySamples;
            for (int i = 0; i < N; ++i) {
                double aD = a.delay.GetNthDelayedSampleValue(i);
                double bD = b.delay.GetNthDelayedSampleValue(i);
                cost += (aD - bD) * (aD - bD);
            }

            return cost;
        }

        private bool Process(double x, out double y) {
            bool result = false;
            y = 0;

            if (mProcessed < INSPECT_BITS-1) {
                mOriginalSignal.Filter(x);
            } else {
                result = true;
                
                // オリジナル信号を更新。
                mOriginalSignal.Filter(x);

                double smallestCost = double.MaxValue;
                int chosenIdx = -1;

                // 候補ビット列から、最もオリジナル信号と似ているものを選ぶ ==> y。
                for (int i = 0; i < InspectCandidates(); ++i) {
                    var lpf = mCandidates[i];
                    double cost = CalcCost(mOriginalSignal, lpf);
                    if (cost < smallestCost) {
                        y = ((i & 1) == 1) ? +1.0 : -1.0;
                        smallestCost = cost;
                        chosenIdx = i;
                    }
                }

                // yが決定した。
                // 候補ビット列を1ビット進めます。
                if (y == -1.0) {
                    /* INSPECT_BITS == 3のとき
                     *            -1で始まるシーケンスが選択された。
                     *            vv
                     * i==0 000 : -1, -1, -1 ==> そのまま使用。-1を投入。
                     * i==1 001 : +1, -1, -1 ==> i==0をコピー。+1を投入。
                     * i==2 010 : -1, +1, -1 ==> そのまま使用。-1を投入。
                     * i==3 011 : +1, +1, -1 ==> i==2をコピー。+1を投入。
                     * i==4 100 : -1, -1, +1 ==> そのまま使用。-1を投入。
                     * i==5 101 : +1, -1, +1 ==> i==4をコピー。+1を投入。
                     * i==6 110 : -1, +1, +1 ==> そのまま使用。-1を投入。
                     * i==7 111 : +1, +1, +1 ==> i==6をコピー。+1を投入。
                     */
                    var newLpfCand = new Lpf[InspectCandidates()];
                    //for (int i = 0; i < InspectCandidates()/2; ++i) {
                    Parallel.For(0, InspectCandidates()/2, i => {
                        newLpfCand[i * 2 + 0] = mCandidates[i*2];
                        newLpfCand[i * 2 + 1] = new Lpf(mCandidates[i*2]);
                        newLpfCand[i * 2 + 0].Filter(-1.0);
                        newLpfCand[i * 2 + 1].Filter(+1.0);
                    });
                    mCandidates = newLpfCand;
                } else {
                    // y == +1.0
                    /* INSPECT_BITS == 3のとき
                     *            +1で始まるシーケンスが選択された。
                     *            vv
                     * i==0 000 : -1, -1, -1 ==> i==1をコピー。-1を投入。
                     * i==1 001 : +1, -1, -1 ==> そのまま使用。+1を投入。
                     * i==2 010 : -1, +1, -1 ==> i==3をコピー。-1を投入。
                     * i==3 011 : +1, +1, -1 ==> そのまま使用。+1を投入。
                     * i==4 100 : -1, -1, +1 ==> i==5をコピー。-1を投入。
                     * i==5 101 : +1, -1, +1 ==> そのまま使用。+1を投入。
                     * i==6 110 : -1, +1, +1 ==> i==7をコピー。-1を投入。
                     * i==7 111 : +1, +1, +1 ==> そのまま使用。+1を投入。
                     */
                    var newLpfCand = new Lpf[InspectCandidates()];
                    //for (int i=0; i<InspectCandidates()/2; ++i) {
                    Parallel.For(0, InspectCandidates()/2, i => {
                        newLpfCand[i * 2 + 0] = mCandidates[i * 2 + 1];
                        newLpfCand[i * 2 + 1] = new Lpf(mCandidates[i * 2 + 1]);
                        newLpfCand[i * 2 + 0].Filter(-1.0);
                        newLpfCand[i * 2 + 1].Filter(+1.0);
                    });
                    mCandidates = newLpfCand;
                }
            }

            ++mProcessed;
            return result;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            double[] outPcm = null;
            if (mProcessed == 0) {
                outPcm = new double[inPcm.Length - INSPECT_BITS+1];
            } else {
                outPcm = new double[inPcm.Length];
            }

            int writePos = 0;
            for (int readPos = 0; readPos < inPcm.Length; ++readPos) {
                double y = 0;
                if (!Process(inPcm[readPos], out y)) {
                    // まだ出力から値が出てこない。
                } else {
                    outPcm[writePos++] = y;
                }
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
