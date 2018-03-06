using System;
using System.Threading.Tasks;

namespace WWDirectComputeCS
{
    public class WWUpsampleCpu
    {
        private static void
        PrepareResamplePosArray(
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo,
                int [] resamplePosArray,
                double [] fractionArrayD) {
            for (int i = 0; i < sampleTotalTo; ++i) {
                double resamplePos = (double)i * sampleRateFrom / sampleRateTo;
                /* -0.5 <= fraction<+0.5になるようにresamplePosを選ぶ。
                 * 最後のほうで範囲外を指さないようにする。
                 */
                int resamplePosI = (int)(resamplePos + 0.5);
                if (resamplePosI < 0) {
                    resamplePosI = 0;
                }
                if (sampleTotalFrom <= resamplePosI) {
                    resamplePosI = sampleTotalFrom - 1;
                }
                double fraction = resamplePos - resamplePosI;

                resamplePosArray[i] = resamplePosI;
                fractionArrayD[i] = fraction;
            }
        }

        private static void
        PrepareSinPreComputeArray(
                double []fractionArray,
                int sampleTotalTo,
                float []sinPreComputeArray)
        {
            for (int i=0; i<sampleTotalTo; ++i) {
                sinPreComputeArray[i] = (float)Math.Sin(-Math.PI * fractionArray[i]);
            }
        }

        private int m_convolutionN;
        private int m_sampleTotalFrom;
        private int m_sampleRateFrom;
        private int m_sampleRateTo;
        private int m_sampleTotalTo;
        private int [] m_resamplePosArray;
        private double [] m_fractionArray;
        private float [] m_sampleFrom;
        private float [] m_sinPreComputeArray;

        public int Setup(
                int convolutionN,
                float [] sampleFrom,
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo,
                int [] resamplePosArray,
                double [] fractionArrayD)
        {
            int hr = 0;

            System.Diagnostics.Debug.Assert(0 < convolutionN);
            System.Diagnostics.Debug.Assert(sampleFrom != null);
            System.Diagnostics.Debug.Assert(0 < sampleTotalFrom);
            System.Diagnostics.Debug.Assert(sampleRateFrom <= sampleRateTo);
            System.Diagnostics.Debug.Assert(0 < sampleTotalTo);

            m_convolutionN    = convolutionN;
            m_sampleTotalFrom = sampleTotalFrom;
            m_sampleRateFrom  = sampleRateFrom;
            m_sampleRateTo    = sampleRateTo;
            m_sampleTotalTo   = sampleTotalTo;

            m_resamplePosArray = resamplePosArray;

            m_fractionArray = fractionArrayD;

            m_sampleFrom      = sampleFrom;

            m_sinPreComputeArray = new float[sampleTotalTo];

            PrepareSinPreComputeArray(m_fractionArray, sampleTotalTo, m_sinPreComputeArray);

            return hr;
        }

        private static double
        SincD(double sinx, double x)
        {
            if (-2.2204460492503131e-016 < x && x < 2.2204460492503131e-016) {
                return 1.0;
            } else {
                return sinx / x;
            }
        }

        // without resamplePosArray
        public int Setup(
                int convolutionN,
                float [] sampleFrom,
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo)
        {
            int hr = 0;

            System.Diagnostics.Debug.Assert(0 < convolutionN);
            System.Diagnostics.Debug.Assert(null != sampleFrom);
            System.Diagnostics.Debug.Assert(0 < sampleTotalFrom);
            System.Diagnostics.Debug.Assert(sampleRateFrom <= sampleRateTo);
            System.Diagnostics.Debug.Assert(0 < sampleTotalTo);

            // 多少無駄だが…
            int []    resamplePosArray = new int[sampleTotalTo];
            double [] fractionArrayD   = new double[sampleTotalTo];

            PrepareResamplePosArray(
                sampleTotalFrom, sampleRateFrom, sampleRateTo, sampleTotalTo,
                resamplePosArray, fractionArrayD);

            hr = Setup(
                  convolutionN,
                  sampleFrom,
                  sampleTotalFrom,
                  sampleRateFrom,
                  sampleRateTo,
                  sampleTotalTo,
                  resamplePosArray,
                  fractionArrayD);

            fractionArrayD   = null;
            resamplePosArray = null;

            return hr;
        }

        public int Do(
                int startPos,
                int count,
                float [] output)
        {
            int hr = 0;

            Parallel.For(startPos, startPos + count, delegate(int toPos)
            {
                int fromPos = m_resamplePosArray[toPos];
                double fraction = m_fractionArray[toPos];
                double sinPreCompute = m_sinPreComputeArray[toPos];

                double v = 0.0;

                for (int convOffs = -m_convolutionN; convOffs < m_convolutionN; ++convOffs) {
                    int pos = convOffs + fromPos;
                    if (0 <= pos && pos < m_sampleTotalFrom) {
                        double x = Math.PI * (convOffs - fraction);

                        double sinX = sinPreCompute;
                        if (0 != (convOffs & 1)) {
                            sinX *= -1.0;
                        }

                        double sinc = SincD(sinX, x);

                        v += m_sampleFrom[pos] * sinc;
                    }
                }
                // output[0]～output[count-1]に書き込む。
                output[toPos - startPos] = (float)v;
            });

            return hr;
        }

        public void Unsetup()
        {
            m_sinPreComputeArray = null;
            m_fractionArray = null;
            m_resamplePosArray = null;
            m_sampleFrom = null;
        }

    }
}
