using System;
using System.Runtime.InteropServices;

namespace WWFilterCppCs {
    public class WWFilterCpp : IDisposable {
        private enum FilterType {
            Crfb,
            ZohCompensation,
            IIRParallel,
            IIRSerial
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

        public void BuildIIRParallel(int nBlock) {
            if (0 < mIdx) {
                throw new InvalidOperationException();
            }

            mIdx = WWFilterCpp_IIRParallel_Build(nBlock);
            if (mIdx <= 0) {
                // こんな事は起こらないと思う。
                throw new NotImplementedException();
            }

            mFilterType = FilterType.IIRParallel;
        }

        public void BuildIIRSerial(int nBlock) {
            if (0 < mIdx) {
                throw new InvalidOperationException();
            }

            mIdx = WWFilterCpp_IIRSerial_Build(nBlock);
            if (mIdx <= 0) {
                // こんな事は起こらないと思う。
                throw new NotImplementedException();
            }

            mFilterType = FilterType.IIRSerial;
        }

        public void AddIIRBlock(int nA, double[] a, int nB, double[] b) {
            if (mIdx <= 0) {
                throw new InvalidOperationException();
            }
            switch (mFilterType) {
            case FilterType.IIRSerial:
                WWFilterCpp_IIRSerial_Add(mIdx, nA, a, nB, b);
                break;
            case FilterType.IIRParallel:
                WWFilterCpp_IIRParallel_Add(mIdx, nA, a, nB, b);
                break;
            default:
                throw new InvalidOperationException();
            }
        }

        public double[] FilterIIR(double[] buffIn, int osr) {
            if (mIdx <= 0) {
                throw new InvalidOperationException();
            }

            var buffOut = new double[buffIn.Length * osr];
            switch (mFilterType) {
            case FilterType.IIRParallel:
                WWFilterCpp_IIRParallel_Filter(mIdx, buffIn.Length, buffIn, buffOut);
                break;
            case FilterType.IIRSerial:
                WWFilterCpp_IIRSerial_Filter(mIdx, buffIn.Length, buffIn, buffOut);
                break;
            default:
                throw new InvalidOperationException();
            }
            return buffOut;
        }

        public void SetParam(int osr) {
            switch (mFilterType) {
            case FilterType.IIRParallel:
                WWFilterCpp_IIRParallel_SetParam(mIdx, osr);
                break;
            case FilterType.IIRSerial:
                WWFilterCpp_IIRSerial_SetParam(mIdx, osr);
                break;
            default:
                throw new InvalidOperationException();
            }
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
            case FilterType.IIRParallel:
                WWFilterCpp_IIRParallel_Destroy(mIdx);
                break;
            case FilterType.IIRSerial:
                WWFilterCpp_IIRSerial_Destroy(mIdx);
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


        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRSerial_Build(int nBlock);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        void WWFilterCpp_IIRSerial_Destroy(int idx);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRSerial_Add(int idx, int nA, double[] a, int nB, double[] b);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRSerial_Filter(int idx, int n, double[] buffIn, double[] buffOut);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRSerial_SetParam(int idx, int osr);


        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRParallel_Build(int nBlock);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        void WWFilterCpp_IIRParallel_Destroy(int idx);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRParallel_Add(int idx, int nA, double[] a, int nB, double[] b);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRParallel_Filter(int idx, int n, double[] buffIn, double[] buffOut);

        [DllImport("WWFilterCpp.dll", CharSet = CharSet.Unicode)]
        internal extern static
        int WWFilterCpp_IIRParallel_SetParam(int idx, int osr);

    }
}

