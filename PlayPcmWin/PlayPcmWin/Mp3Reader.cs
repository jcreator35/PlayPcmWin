// 日本語UTF-8

using System;
using System.IO;
using System.Threading.Tasks;
using WWMFReaderCs;
using System.Linq;

namespace PlayPcmWin {

    class Mp3Reader {
        public string path;
        public byte[] data;
        public WWMFReader.Metadata meta;

        public int Read(string path) {
            this.path = path;

            int hr = WWMFReader.ReadHeaderAndData(path, out meta, out data);
            if (hr < 0) {
                return hr;
            }

            return 0;
        }

        public void ReadStreamEnd() {
            data = null;
        }
    };
}
