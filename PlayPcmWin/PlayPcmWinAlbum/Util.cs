using System.Windows.Media;
using System.IO;
using System.Globalization;

namespace PlayPcmWinAlbum {
    class Util {

        private Util() {
        }

        /*
        public static string SecondsToHMSString(int seconds) {
            int h = seconds / 3600;
            int m = seconds / 60 - h * 60;
            int s = seconds - h * 3600 - m * 60;
            return string.Format(CultureInfo.CurrentCulture, "{0:D1}:{1:D2}:{2:D2}", h, m, s);
        }
        */

        public static string SecondsToMSString(int seconds) {
            int m = seconds / 60;
            int s = seconds - m * 60;
            return string.Format(CultureInfo.CurrentCulture, "{0:D2}:{1:D2}", m, s);
        }

        public static Color ColorFromArgb(long argb) {
            return Color.FromArgb(
                (byte)((argb & 0xff000000U) >> 24),
                (byte)((argb & 0x00ff0000U) >> 16),
                (byte)((argb & 0x0000ff00U) >> 8),
                (byte)((argb & 0x000000ffU) >> 0));
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
