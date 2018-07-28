using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WWDirectComputeCS {
    public class WWDirectCompute {
        /// <returns>アダプターの個数が戻る。0以下の時失敗。</returns>
        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDC_EnumAdapter(int instanceType);

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal struct AdapterDesc {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String name;
            public long videoMemoryBytes;
        };

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDC_GetAdapterDesc(int instanceType, int idx, out AdapterDesc desc);

        [DllImport("WWDirectComputeDLL.dll")]
        private extern static int
        WWDC_ChooseAdapter(int instanceType, int idx);

        static int sAdapterIdx = -1;

        public enum InstanceTypeEnum {
            Upsample,
            Wave1D,
            Wave2D,
        };

        private InstanceTypeEnum mInstanceType;

        public WWDirectCompute(InstanceTypeEnum it) {
            mInstanceType = it;
        }

        public int ChooseAdapter() {
            int hr = 0;

            do {
                hr = EnumAdapter();
                if (hr <= 0) {
                    break;
                }

                if (sAdapterIdx < 0) {
                    var cw = new ChooseGPUWindow();
                    for (int i = 0; i < hr; ++i) {
                        string desc;
                        long videoMemoryBytes;
                        GetAdapterDesc(i, out desc, out videoMemoryBytes);
                        string s = string.Format("{0}, dedicated video memory={1}MB",
                            desc, videoMemoryBytes / 1024 / 1024);

                        cw.Add(s);
                    }
                    cw.ShowDialog();
                    sAdapterIdx = cw.SelectedAdapterIdx;
                }

                hr = ChooseAdapter(sAdapterIdx);
                if (hr < 0) {
                    break;
                }
            } while (false);

            if (hr < 0) {
                Console.WriteLine("E: WWWave1DGpu::Init() failed {0:X8}", hr);
            }
            return hr;
        }

        private int EnumAdapter() {
            return WWDC_EnumAdapter((int)mInstanceType);
        }

        private int GetAdapterDesc(int idx, out string desc, out long videoMemoryBytes) {
            desc = "Unknown Adapter";
            videoMemoryBytes = 0;
            AdapterDesc ad;
            int hr = WWDC_GetAdapterDesc((int)mInstanceType, idx, out ad);
            if (hr < 0) {
                return hr;
            }

            desc = ad.name;
            videoMemoryBytes = ad.videoMemoryBytes;
            return 0;
        }

        private int ChooseAdapter(int idx) {
            return WWDC_ChooseAdapter((int)mInstanceType, idx);
        }
    }
}
