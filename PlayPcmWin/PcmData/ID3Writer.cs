using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PcmDataLib {
    public class ID3Writer {
        /// <summary>
        /// Write ID3v2.3
        /// </summary>
        public bool Write(BinaryWriter bw, string title, string album, string artists, byte[] albumCoverArt, string mimeType) {
            long posBegin = bw.BaseStream.Position;
            WriteTagHeader(bw, 0);
            WriteFrameV23(bw, title, album, artists, albumCoverArt, mimeType);

            long posEnd = bw.BaseStream.Position;

            bw.BaseStream.Seek(posBegin, SeekOrigin.Begin);
            WriteTagHeader(bw, (int)(posEnd - posBegin - 10));
            bw.BaseStream.Seek(posEnd, SeekOrigin.Begin);
            return true;
        }

        private void WriteInt32ValueEncoded(BinaryWriter bw, int v) {
            int encoded = ((v&0x0000007f) <<24)
                          + ((v&0x00003f80)<<9)
                          + ((v&0x001fc000)>>6)
                          + ((v&0x0fe00000)>>21);
            bw.Write(encoded);
        }

        private void WriteInt32Value(BinaryWriter bw, int v) {
            uint u = (uint)v;

            uint big = ((u & 0xff000000) >> 24)
                + ((u & 0x00ff0000) >> 8)
                + ((u & 0x0000ff00) << 8)
                + ((u & 0x000000ff) << 24);
            bw.Write(big);
        }

        private void WriteInt16Value(BinaryWriter bw, short v) {
            uint u = (ushort)v;

            ushort big = (ushort)(((u & 0xff00) >> 8)
                + ((u & 0x00ff) << 8));
            bw.Write(big);
        }

        /*
          ID3v2/file identifier   "ID3"
          ID3v2 version           $03 00
          ID3v2 flags             %abc00000
          ID3v2 size              4 * %0xxxxxxx
         */
        private bool WriteTagHeader(BinaryWriter bw, int tagSizeInBytes) {
            var id = new byte[3];
            id[0] = (byte)'I';
            id[1] = (byte)'D';
            id[2] = (byte)'3';

            bw.Write(id);

            short version = 3;
            bw.Write(version);

            byte flags = 0;
            bw.Write(flags);

            WriteInt32ValueEncoded(bw, tagSizeInBytes);
            return true;
        }

        /// <param name="bytes">frame size - 10 (==frame header size)</param>
        private void WriteFrameHeader(BinaryWriter bw, string id, int bytes, short flags) {
            var idArray = UTF7Encoding.UTF7.GetBytes(id);
            bw.Write(idArray[0]);
            bw.Write(idArray[1]);
            bw.Write(idArray[2]);
            bw.Write(idArray[3]);

            WriteInt32Value(bw, bytes);
            WriteInt16Value(bw, flags);
        }

        private bool WriteTextFrame(BinaryWriter bw, string id, string text) {
            if (text == null || text.Length == 0) {
                return false;
            }

            // UTF8 encoding
            byte encoding = 3;

            var utf8 = System.Text.Encoding.UTF8.GetBytes(text);
            int bytes = utf8.Length + 2;

            WriteFrameHeader(bw, id, bytes, 0);
            bw.Write(encoding);
            bw.Write(utf8);

            byte zero = 0;
            bw.Write(zero);

            return true;
        }

        /// <param name="pictureType">3 == cover(front)</param>
        private bool WriteAttachedPictureFrameV23(BinaryWriter bw, string id, byte[] image, byte pictureType, string mimeType) {
            WriteFrameHeader(bw, id, 0, 0);

            byte encoding = 0;
            bw.Write(encoding);

            var utf7 = System.Text.Encoding.UTF7.GetBytes(mimeType);
            bw.Write(utf7);
            byte zero = 0;
            bw.Write(zero);

            bw.Write(pictureType);

            // description string
            bw.Write(zero);

            // pictureData
            bw.Write(image);

            return true;
        }

        private bool WriteFrameV23(BinaryWriter bw, string title, string album, string artists, byte[] albumCoverArt, string mimeType) {
            WriteTextFrame(bw, "TIT2", title);
            WriteTextFrame(bw, "TALB", album);
            WriteTextFrame(bw, "TPE1", artists);

            if (albumCoverArt != null && 0 < albumCoverArt.Length) {
                long posBegin = bw.BaseStream.Position;
                WriteAttachedPictureFrameV23(bw, "APIC", albumCoverArt, 3, mimeType);

                long posEnd = bw.BaseStream.Position;

                bw.BaseStream.Seek(posBegin, SeekOrigin.Begin);
                WriteFrameHeader(bw, "APIC", (int)(posEnd - posBegin - 10), 0);
                bw.BaseStream.Seek(posEnd, SeekOrigin.Begin);
            }

            return true;
        }
    }
}
