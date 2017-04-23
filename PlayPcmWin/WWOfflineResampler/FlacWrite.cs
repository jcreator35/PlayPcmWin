using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWUtil;

namespace WWOfflineResampler {
    class FlacWrite {
        private WWFlacRWCS.FlacRW mFlacW;
        public void Setup(WWFlacRWCS.Metadata metaW, byte[] picture) {
            mFlacW = new WWFlacRWCS.FlacRW();
            mFlacW.EncodeInit(metaW);
            if (picture != null) {
                mFlacW.EncodeSetPicture(picture);
            }
        }

        public int AddPcm(int ch, LargeArray<byte> pcmW) {
            int rv;
            rv = mFlacW.EncodeAddPcm(ch, pcmW);
            return rv;
        }

        public int OutputFile(string path) {
            int rv;
            rv = mFlacW.EncodeRun(path);
            mFlacW.EncodeEnd();
            return rv;
        }
    }
}
