using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WWDirectComputeCS {
    class WWWave1DGpu {
        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCWave1D_Init();

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave1D_Run(int cRepeat, float sc, float c0, int stimCounter,
                int stimPosX, float stimMagnitude, float stimHalfPeriod,
                float stimWidth, int dataCount, float [] loss,
                float [] roh, float [] cr, float [] v, float [] p);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDCWave1D_GetResultFromGpuMemory(
                int outputToElemNum,
                float [] outputVTo,
                float [] outputPTo);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static void
        WWDCWave1D_Term();

        public int Run(int cRepeat, float sc, float c0, int stimCounter,
                int stimPosX, float stimMagnitude, float stimHalfPeriod,
                float stimWidth, int dataCount, float[] loss,
                float[] roh, float[] cr, float[] v, float[] p) {
            WWDCWave1D_Init();

            return WWDCWave1D_Run(cRepeat, sc, c0, stimCounter, stimPosX, stimMagnitude,
                stimHalfPeriod, stimWidth, dataCount, loss, roh, cr, v, p);
        }

        public int GetResultVP(int dataCount, float[] v, float[] p) {
            return WWDCWave1D_GetResultFromGpuMemory(dataCount, v, p);
        }

        public void Term() {
            WWDCWave1D_Term();
        }
    }
}
