using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WWDirectComputeCS {
    [StructLayout(LayoutKind.Sequential)]
    public struct WWWave1DStim {
        public int type; //< STIM_GAUSSIAN or STIM_SINE
        public int counter;
        public int posX;
        public float magnitude;
        public float halfPeriod;
        public float width;
        public float omega;
        public float period;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct WWWave1DParams {
        public int dataCount;
        public float deltaT;
        public float sc;
        public float c0;
    };

    public class WWWave1DGpu {
        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCWave1D_Init();

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave1D_Setup(WWWave1DParams p, float [] loss, float [] roh, float [] cr);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave1D_Run(int cRepeat, int stimNum, IntPtr stim, float[] v, float[] p);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave1D_GetResult(
                int outputToElemNum,
                float [] outputVTo,
                float [] outputPTo);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCWave1D_Term();

        public bool Available { get; set; }

        public int Init() {
            WWDCWave1D_Init();
            return new WWDirectCompute(WWDirectCompute.InstanceTypeEnum.Wave1D).ChooseAdapter();
        }

        public int Setup(WWWave1DParams p, float[] loss, float[] roh, float[] cr) {
            int hr = WWDCWave1D_Setup(p, loss, roh, cr);

            Available = 0 <= hr;
            return hr;
        }

        const int STIM_BYTES = 32;
        const int N_STIM = 4;

        public int Run(int cRepeat, int stimNum,
               WWWave1DStim [] stim, float[] v, float[] p) {

            IntPtr ptr = Marshal.AllocHGlobal(STIM_BYTES * N_STIM);
            long longPtr = ptr.ToInt64();
            for (int i = 0; i < N_STIM; ++i) {
                IntPtr rectPtr = new IntPtr(longPtr);
                Marshal.StructureToPtr(stim[i], rectPtr, false);
                longPtr += STIM_BYTES;
            }

            int hr = WWDCWave1D_Run(cRepeat, stimNum, ptr, v, p);

            Marshal.FreeHGlobal(ptr);

            return hr;
        }

        public int GetResultVP(int dataCount, float[] v, float[] p) {
            return WWDCWave1D_GetResult(dataCount, v, p);
        }

        public void Term() {
            WWDCWave1D_Term();
        }
    }
}
