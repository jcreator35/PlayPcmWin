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
        public int pos;
        public float magnitude;
        public float halfPeriod;
        public float width;
        public float omega;
        public float period;

        private const int STIM_BYTES = 32;
        private const int N_STIM = 4;

        public static IntPtr ToIntPtr(WWWave1DStim[] stim) {
            IntPtr ptr = Marshal.AllocHGlobal(STIM_BYTES * N_STIM);
            long longPtr = ptr.ToInt64();
            for (int i = 0; i < N_STIM; ++i) {
                IntPtr rectPtr = new IntPtr(longPtr);
                Marshal.StructureToPtr(stim[i], rectPtr, false);
                longPtr += STIM_BYTES;
            }

            return ptr;
        }
    };
}
