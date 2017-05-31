using System;
using System.Runtime.InteropServices;

namespace WWFilterCppCs {
    public class WWFilterCpp : IDisposable {

        public void BuildCrfb(int order, double[] a,
                double[] b, double[] g, double gain) {
            if (0<mIdx) {
                throw new InvalidOperationException();
            }

            mIdx = WWFilterCpp_Crfb_Build(order, a, b, g, gain);
            if (mIdx<=0) {
                // こんな事は起こらないと思う。
                throw new NotImplementedException();
            }

            mFilterType = FilterType.Crfb;
        }

        public int FilterCrfb(int n, double []buffIn, byte []buffOut) {
            if (mIdx <= 0 || mFilterType!=FilterType.Crfb) {
                throw new InvalidOperationException();
            }
            return WWFilterCpp_Crfb_Filter(mIdx, n, buffIn, buffOut);
        }

        private enum FilterType {
            Crfb,
        }
        private FilterType mFilterType;

        private int mIdx;

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_Crfb_Build(int order, double []a,
            double []b, double []g, double gain);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        void WWFilterCpp_Crfb_Destroy(int idx);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_Crfb_Filter(int idx, int n, double []buffIn, byte []buffOut);

        public void Dispose() {
            if (mIdx <= 0) {
                return;
            }

            if (mFilterType == FilterType.Crfb) {
                WWFilterCpp_Crfb_Destroy(mIdx);
                mIdx = 0;
            }

            throw new NotImplementedException();
        }
    }
}

