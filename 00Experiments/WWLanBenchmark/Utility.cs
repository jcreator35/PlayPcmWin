using System.IO;
using System.Net.Sockets;

namespace WWLanBenchmark {
    class Utility {
        private const int RECV_BUFF_BYTES = 8192;
        public static byte [] StreamReadBytes(NetworkStream stream, int bytes) {
            var output = new byte[bytes];
            int readBytes = 0;
            do {
                int wantBytes = bytes - readBytes;
                if (RECV_BUFF_BYTES < wantBytes) {
                    wantBytes = RECV_BUFF_BYTES;
                }
                readBytes += stream.Read(output, readBytes, wantBytes);
            } while (readBytes < bytes);

            return output;
        }

        public static long StreamReadInt64(NetworkStream stream) {
            byte[] data = StreamReadBytes(stream, 8);
            using (var ms = new MemoryStream(data)) {
                using (var br = new BinaryReader(ms)) {
                    return br.ReadInt64();
                }
            }
        }

        public static int StreamReadInt32(NetworkStream stream) {
            byte[] data = StreamReadBytes(stream, 4);
            using (var ms = new MemoryStream(data)) {
                using (var br = new BinaryReader(ms)) {
                    return br.ReadInt32();
                }
            }
        }


    }
}
