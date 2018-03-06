
namespace WWDirectComputeCS {
    class WWFFTResampleCpu {

        private int  m_sampleRateFrom;
        private int  m_sampleRateTo;

        /// <summary>
        /// 最大公約数。ユークリッドの方法
        /// </summary>
        private int Gcd(int a, int b) {
            if (a == 0) {
                return b;
            }
            while (b != 0) {
                if (a > b) {
                    a -= b;
                } else {
                    b -= a;
                }
            }
            return a;
        }

        /// <summary>
        /// 最小公倍数
        /// </summary>
        private long Lcm(int a, int b) {
            System.Diagnostics.Debug.Assert(0 <= a);
            System.Diagnostics.Debug.Assert(0 <= b);

            return (long)a * b / Gcd(a, b);
        }

        private int CountBit(int v) {
            int c;
            for (c = 0; v!= 0; ++c) {
              v &= v - 1;
            }
            return c;
        }

        private int m_upSampleRatio;
        private int m_decimationInterval;
        private int m_fftLength;
        private int m_firTapN;
        private double [] m_lpfTable;

        /// <summary>
        /// sampleTotalFromサンプルあるサンプルデータsampleFromをsampleRateFromからsamleRateToにリサンプルする。
        /// </summary>
        public int Setup(
                int sampleRateFrom,
                int sampleRateTo,
                int firTapN,
                int fftLength,
                double kaiserAlpha) {
            int hr = 0;

            System.Diagnostics.Debug.Assert(CountBit(firTapN) == 1);
            System.Diagnostics.Debug.Assert(4 <= firTapN);
            System.Diagnostics.Debug.Assert(CountBit(fftLength) == 1);
            System.Diagnostics.Debug.Assert(firTapN < fftLength);

            m_sampleRateFrom = sampleRateFrom;
            m_sampleRateTo = sampleRateTo;

            m_firTapN = firTapN;
            m_fftLength = fftLength;

            /* 
             * 44100Hz → 48000Hz:  m_upSampleRatio = 160, decimationInterval = 147
             * 44100Hz → 96000Hz:  m_upSampleRatio = 320, decimationInterval = 147
             * 44100Hz → 192000Hz: m_upSampleRatio = 640, decimationInterval = 147
             * 48000Hz → 96000Hz:  m_upSampleRatio = 2,   decimationInterval = 1
             * 48000Hz → 192000Hz: m_upSampleRatio = 4,   decimationInterval = 1
             * 
             * となるような計算。
             * ① 44100と48000の最小公倍数=7056000
             * ② 7056000 / 44100 = 160 = m_upSampleRatio
             *    7056000 / 48000 = 147 = decimationInterval
             * という具合に求める。
             */

            var lcm = Lcm(m_sampleRateFrom, m_sampleRateTo);
            m_upSampleRatio      = (int)lcm / m_sampleRateFrom;
            m_decimationInterval = (int)lcm / m_sampleRateTo;

            /* ローパスフィルターのcoeffを作る。
             * とりあえず
             *     sampleRateFrom = 44100
             *     sampleRateTo   = 48000
             *     引き伸ばしたデータのサンプリング周波数=7056000Hz
             *     IDFT n=65536
             * とする
             * 
             * ローパスフィルターは、65536/160までは1、そこから先は0
             * というのを試す。
             */
            m_lpfTable = new double[m_firTapN * 2];

            // 周波数は、リニアスケール
            // i==freqResponse.Length/2のとき freq = sampleRate/2 これが最大周波数。
            // 左右対称な感じで折り返す。

            // DC
            m_lpfTable[0] = 1.0;
            m_lpfTable[1] = 0.0;
            for (int i=1; i <= m_firTapN / 2; ++i) {
                var freq = (double)i * (lcm / 2) / (m_firTapN / 2);
                double re = 0.0;
                double im = 0.0;
                if (freq < (m_sampleRateFrom/2)) {
                    // 元データのサンプリング周波数/2以下の周波数帯域のデータは残す。
                    re = 1.0;
                }

                m_lpfTable[i * 2 + 0] = re;
                m_lpfTable[i * 2 + 1] = im;
                m_lpfTable[m_lpfTable.Length - i * 2 + 0] = re;
                m_lpfTable[m_lpfTable.Length - i * 2 + 1] = im;
            }

            return hr;
        }

        public void Unsetup() {
            m_lpfTable = null;
        }

        /// <summary>
        /// データを水増しする。
        /// </summary>
        /// <param name="from">水増し元データ</param>
        /// <param name="offset">水増し元データ開始位置</param>
        /// <param name="ratio">水増し倍率</param>
        /// <param name="remainder">水増し元データ開始位置サブサンプルオフセット(水増し倍率160倍の時0～159の値を取りうる)</param>
        /// <param name="toArrayLength">水増し結果データのサンプル数</param>
        /// <returns></returns>
        private double [] Interpolate(double [] from, long offset, int ratio, int remainder, long toArrayLength) {
            // サンプルホールド水増しアルゴリズム

            // C♯では、newで0埋めされる
            var result = new double[toArrayLength];

            for (long toPos = offset * ratio + remainder; toPos < toArrayLength; ++toPos) {
                var fromPos = toPos / ratio;
                if (fromPos < from.LongLength) {
                    result[toPos] = from[fromPos];
                }
            }

            return result;
        }

        /// <summary>
        /// リサンプルする。
        /// </summary>
        /// <param name="sampleFromArg">入力データ</param>
        /// <param name="countTo">入力データリサンプル開始オフセット</param>
        /// <returns>リサンプル出力</returns>
        public double[] Do() {
            // 水増し処理 160倍ｗｗｗｗ
            //var sampleInterpolation = Interpolate();

            // @todo FFTする

            // @todo m_lpfTableを掛ける

            // @todo IFFTする

            // @todo 160個に1個の割合でデータを取り出す

            return null;
        }
    }
}
