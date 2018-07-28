using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using WWMath;

namespace WWDirectComputeCS {
    [StructLayout(LayoutKind.Sequential)]
    public struct WWWave2DParams {
        public int fieldW;
        public int fieldH;
        public float deltaT;
        public float sc;
        public float c0;
    };

    public class WWWave2DGpu {
        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCWave2D_Init();

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave2D_Setup(WWWave2DParams p, float [] loss, float [] roh, float [] cr);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave2D_Run(int cRepeat, int stimNum, IntPtr stim, float[] v, float[] p);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave2D_GetResult(
                int outputToElemNum,
                float [] outputVTo,
                float [] outputPTo);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCWave2D_Term();

        public bool Available { get; set; }

        public int Init() {
            WWDCWave2D_Init();
            return new WWDirectCompute(WWDirectCompute.InstanceTypeEnum.Wave2D).ChooseAdapter();
        }

        public int Setup(WWWave2DParams p, float[] loss, float[] roh, float[] cr) {
            int hr = WWDCWave2D_Setup(p, loss, roh, cr);

            Available = 0 <= hr;
            return hr;
        }

        public int Run(int cRepeat, int stimNum,
               WWWave1DStim [] stim, WWVectorF2[] v, float[] p) {

            IntPtr ptr = WWWave1DStim.ToIntPtr(stim);

            float[] vF = new float[v.Length * 2];
            for (int i = 0; i < v.Length; ++i) {
                vF[i * 2 + 0] = v[i].X;
                vF[i * 2 + 1] = v[i].Y;
            }

            int hr = WWDCWave2D_Run(cRepeat, stimNum, ptr, vF, p);

            Marshal.FreeHGlobal(ptr);

            return hr;
        }

        public int GetResultVP(int dataCount, WWVectorF2[] v, float[] p) {
            float[] vF = new float[v.Length * 2];
            for (int i = 0; i < v.Length; ++i) {
                vF[i] = 0;
            }

            int rv = WWDCWave2D_GetResult(dataCount, vF, p);
            if (0 <= rv) {
                for (int i = 0; i < v.Length; ++i) {
                    v[i] = new WWVectorF2(vF[i*2+0], vF[i*2+1]);
                }
            }

            return rv;
        }

        public void Term() {
            WWDCWave2D_Term();
        }
    }
}
