using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWFlacRWCS {
    public class Metadata {
        public int sampleRate;
        public int channels;
        public int bitsPerSample;
        public int pictureBytes;
        public long totalSamples;

        public string titleStr = string.Empty;
        public string artistStr = string.Empty;
        public string albumStr = string.Empty;
        public string albumArtistStr = string.Empty;
        public string composerStr = string.Empty;

        public string genreStr = string.Empty;
        public string dateStr = string.Empty;
        public string trackNumberStr = string.Empty;
        public string discNumberStr = string.Empty;
        public string pictureMimeTypeStr = string.Empty;

        public string pictureDescriptionStr = string.Empty;

        public byte[] md5sum = new byte[NativeMethods.WWFLAC_MD5SUM_BYTES];

        public Metadata() {
        }

        /// <summary>
        /// 全チャンネル1サンプルのバイト数。
        /// </summary>
        public int BytesPerFrame {
            get { return channels * bitsPerSample / 8; }
        }

        /// <summary>
        /// 1チャンネル1サンプルのバイト数。
        /// </summary>
        public int BytesPerSample {
            get { return bitsPerSample / 8; }
        }

        /// <summary>
        /// PCMデータのバイト数。
        /// </summary>
        public long PcmBytes {
            get { return totalSamples * BytesPerFrame; }
        }

        private void SafeCopy(string from, ref string to) {
            if (from != null && from != string.Empty) {
                to = string.Copy(from);
            }
        }

        public Metadata(Metadata rhs) {
            sampleRate = rhs.sampleRate;
            channels = rhs.channels;
            bitsPerSample = rhs.bitsPerSample;
            pictureBytes = rhs.pictureBytes;
            totalSamples = rhs.totalSamples;

            SafeCopy(rhs.titleStr, ref titleStr);
            SafeCopy(rhs.artistStr, ref artistStr);
            SafeCopy(rhs.albumStr, ref albumStr);
            SafeCopy(rhs.albumArtistStr, ref albumArtistStr);
            SafeCopy(rhs.composerStr, ref composerStr);

            SafeCopy(rhs.genreStr, ref genreStr);
            SafeCopy(rhs.dateStr, ref dateStr);
            SafeCopy(rhs.trackNumberStr, ref trackNumberStr);
            SafeCopy(rhs.discNumberStr, ref discNumberStr);
            SafeCopy(rhs.pictureMimeTypeStr, ref pictureMimeTypeStr);

            SafeCopy(rhs.pictureDescriptionStr, ref pictureDescriptionStr);

            if (rhs.md5sum != null && rhs.md5sum.Length != 0) {
                md5sum = new byte[rhs.md5sum.Length];
                System.Array.Copy(rhs.md5sum, md5sum, md5sum.Length);
            } else {
                md5sum = new byte[NativeMethods.WWFLAC_MD5SUM_BYTES];
            }
        }
    };
}
