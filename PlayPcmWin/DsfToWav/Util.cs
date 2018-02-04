using System.IO;
using System.Globalization;

namespace PlayPcmWin {
    class Util {

        private Util() {
        }

        public static string SecondsToMSString(int seconds) {
            int m = seconds / 60;
            int s = seconds - m * 60;
            return string.Format(CultureInfo.CurrentCulture, "{0:D2}:{1:D2}", m, s);
        }

        public static ushort ReadBigU16(BinaryReader br) {
            ushort result = (ushort)(((int)br.ReadByte() << 8) + br.ReadByte());
            return result;
        }

        public static uint ReadBigU32(BinaryReader br) {
            uint result = 
                (uint)((uint)br.ReadByte() << 24) +
                (uint)((uint)br.ReadByte() << 16) +
                (uint)((uint)br.ReadByte() << 8) +
                (uint)((uint)br.ReadByte() << 0);
            return result;
        }

        public static ulong ReadBigU64(BinaryReader br) {
            ulong result = 
                (ulong)((ulong)br.ReadByte() << 56) +
                (ulong)((ulong)br.ReadByte() << 48) +
                (ulong)((ulong)br.ReadByte() << 40) +
                (ulong)((ulong)br.ReadByte() << 32) +
                (ulong)((ulong)br.ReadByte() << 24) +
                (ulong)((ulong)br.ReadByte() << 16) +
                (ulong)((ulong)br.ReadByte() << 8) +
                (ulong)((ulong)br.ReadByte() << 0);
            return result;
        }
    }
}
