using System;
using System.Runtime.InteropServices;

namespace WWFilterCppCs {
    public class WWFilterCpp : IDisposable {
        private enum FilterType {
            Crfb,
            ZohCompensation,
        }

        private FilterType mFilterType;
        private int mIdx;

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

        public void BuildZohCompensation() {
            if (0 < mIdx) {
                throw new InvalidOperationException();
            }

            mIdx = WWFilterCpp_ZohCompensation_Build();
            if (mIdx <= 0) {
                // こんな事は起こらないと思う。
                throw new NotImplementedException();
            }

            mFilterType = FilterType.ZohCompensation;
        }

        public double [] FilterZohCompensation(double[] buffIn) {
            if (mIdx <= 0 || mFilterType != FilterType.ZohCompensation) {
                throw new InvalidOperationException();
            }

            var buffOut = new double[buffIn.Length];
            WWFilterCpp_ZohCompensation_Filter(mIdx, buffIn.Length, buffIn, buffOut);
            return buffOut;
        }

        public void Dispose() {
            if (mIdx <= 0) {
                return;
            }

            switch (mFilterType) {
            case FilterType.Crfb:
                WWFilterCpp_Crfb_Destroy(mIdx);
                break;
            case FilterType.ZohCompensation:
                WWFilterCpp_ZohCompensation_Destroy(mIdx);
                break;
            default:
                throw new NotImplementedException();
            }
            mIdx = 0;
        }

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

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_ZohCompensation_Build();

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        void WWFilterCpp_ZohCompensation_Destroy(int idx);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_ZohCompensation_Filter(int idx, int n, double[] buffIn, double[] buffOut);
    }
}

