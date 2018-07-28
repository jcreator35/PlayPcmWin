using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WWDirectComputeCS {
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

        public int Run(int cRepeat, int stimNum,
               WWWave1DStim [] stim, float[] v, float[] p) {

            IntPtr ptr = WWWave1DStim.ToIntPtr(stim);

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
