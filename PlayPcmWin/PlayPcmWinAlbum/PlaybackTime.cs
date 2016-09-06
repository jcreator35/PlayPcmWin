using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PlayPcmWinAlbum {
    class PlaybackTime {
        public const string PLAYING_TIME_UNKNOWN = "--:-- / --:--";
        public const string PLAYING_TIME_ALLZERO = "00:00 / 00:00";

        public static string SecondsToMSString(int seconds) {
            int m = seconds / 60;
            int s = seconds - m * 60;
            return string.Format(CultureInfo.CurrentCulture, "{0:D2}:{1:D2}", m, s);
        }

        public static string CreateDisplayString(int nowSeconds, int totalSeconds) {
            return string.Format(CultureInfo.InvariantCulture, "{0} / {1}",
                     SecondsToMSString(nowSeconds),
                     SecondsToMSString(totalSeconds));
        }
    }
}
