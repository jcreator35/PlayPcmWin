using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WWDirectCompute12 {
    public class WWResampleGpu {
        public class AdapterDesc {
            public int gpuId;
            public string name;
            public int videoMemMiB;
            public int sharedMemMiB;
            public bool remote;
            public bool software;
            public AdapterDesc(int aGpuId, string aName, int aVideoMem, int aSharedMem, bool aRemote, bool aSoftware) {
                gpuId = aGpuId;
                name = aName;
                videoMemMiB = aVideoMem;
                sharedMemMiB = aSharedMem;
                remote = aRemote;
                software = aSoftware;
            }
        }

        private List<AdapterDesc> mAdapterList = new List<AdapterDesc>();
        
        public List<AdapterDesc> AdapterList {
            get { return mAdapterList; }
        }

        public int Init() {
            AdapterList.Clear();

            return NativeMethods.WWDC12_Init();
        }

        public void Term() {
            NativeMethods.WWDC12_Term();
        }

        public int UpdateAdapterList() {
            int hr = 0;
            hr = NativeMethods.WWDC12_EnumAdapter();
            if (hr < 0) {
                return hr;
            }

            AdapterList.Clear();

            int nAdapters = hr;

            for (int i = 0; i < nAdapters; ++i) {
                var adn = new NativeMethods.WWDirectComputeAdapterDesc();
                hr = NativeMethods.WWDC12_GetAdapterDesc(i, ref adn);
                if (hr < 0) {
                    return hr;
                }
                if (0 == adn.featureSupported) {
                    continue;
                }

                AdapterList.Add(new AdapterDesc(
                    i, adn.name, adn.videoMemoryMiB, adn.sharedMemoryMiB,
                    0 != (adn.dxgiAdapterFlags & NativeMethods.DXGI_ADAPTER_FLAG_REMOTE),
                    0 != (adn.dxgiAdapterFlags & NativeMethods.DXGI_ADAPTER_FLAG_SOFTWARE)));
            }

            return hr;
        }

        public int ChooseAdapter(int idx) {
            return NativeMethods.WWDC12_ChooseAdapter(idx);
        }

        public int Setup(int convolutionN,
                float[] sampleFrom,
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo) {
            return NativeMethods.WWDC12_Resample_Setup(
                convolutionN, sampleFrom, sampleTotalFrom,
                sampleRateFrom, sampleRateTo, sampleTotalTo);
        }

        public int Dispatch(
                int startPos,
                int count) {
            return NativeMethods.WWDC12_Resample_Dispatch(startPos, count);
        }

        public int ResultGetFromGpuMemory(
                float[] outputTo) {
            return NativeMethods.WWDC12_Resample_ResultGetFromGpuMemory(
                outputTo, outputTo.Length);
        }

        public void Unsetup() {
            NativeMethods.WWDC12_Resample_Unsetup();
        }


        internal static class NativeMethods {
            public const int DXGI_ADAPTER_FLAG_REMOTE = 1;
            public const int DXGI_ADAPTER_FLAG_SOFTWARE = 2;

            [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
            public struct WWDirectComputeAdapterDesc {
                public int videoMemoryMiB;
                public int systemMemoryMiB;
                public int sharedMemoryMiB;
                public int featureSupported;

                public int dxgiAdapterFlags;
                public int pad0;
                public int pad1;
                public int pad2;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public String name;
            };

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_Init();

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static void
            WWDC12_Term();
            
            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_EnumAdapter();

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_GetAdapterDesc(int idx, ref WWDirectComputeAdapterDesc ad);

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_ChooseAdapter(int idx);

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_Resample_Setup(
                int convolutionN,
                float[] sampleFrom,
                int sampleTotalFrom,
                int sampleRateFrom,
                int sampleRateTo,
                int sampleTotalTo);

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_Resample_Dispatch(
                int startPos,
                int count);

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static int
            WWDC12_Resample_ResultGetFromGpuMemory(
                float[] outputTo,
                int outputToElemNum);

            [DllImport("WWDirectCompute12DLL2019.dll")]
            public extern static void
            WWDC12_Resample_Unsetup();
        };
    };
}
