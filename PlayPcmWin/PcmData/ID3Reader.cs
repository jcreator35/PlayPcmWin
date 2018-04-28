using System;
using System.IO;

namespace PcmDataLib {
    public class ID3Reader {
        public enum ID3Result {
            Success,
            ReadError,
            NotSupportedID3version,
        }

        public string AlbumName { get; set; }
        public string TitleName { get; set; }
        public string ArtistName { get; set; }
        public string Composer { get; set; }

        public int MinorVersion { get { return m_tagVersion[0]; } }

        /// <summary>
        /// true: unsynchro処理をほどく(ファイルから0xffが出てきたら次のバイトを捨てる)
        /// </summary>
        private bool m_unsynchro;

        /// <summary>
        /// Presence of footer
        /// </summary>
        private bool m_footerPresent;

        /// <summary>
        /// Unsynchroの捨てデータも含めた、読み込み可能バイト数。
        /// </summary>
        private long m_bytesRemain;

        /// <summary>
        /// Unsynchroの捨てデータも含めた、読み込んだバイト数。
        /// </summary>
        private long m_readBytes;

        /// <summary>
        /// ファイルから読み込んだバイト数
        /// </summary>
        public long ReadBytes { get { return m_readBytes; } }

        /// <summary>
        /// 画像データバイト数(無いときは0)
        /// </summary>
        public int PictureBytes { get; set; }

        /// <summary>
        /// 画像データ
        /// </summary>
        public byte[] PictureData { get { return m_pictureData; } }

        private byte[] m_pictureData;

        private void Clear() {
            AlbumName = string.Empty;
            TitleName = string.Empty;
            ArtistName = string.Empty;
            Composer = string.Empty;
            m_unsynchro = false;
            m_footerPresent = false;
            m_bytesRemain = 0;
            m_readBytes = 0;

            PictureBytes = 0;
            m_pictureData = null;
        }

        private byte[] BinaryReadBytes(BinaryReader br, int bytes) {
            byte[] result = br.ReadBytes(bytes);
            m_bytesRemain -= bytes;
            m_readBytes   += bytes;
            return result;
        }

        private byte BinaryReadByte(BinaryReader br) {
            byte result = br.ReadByte();
            --m_bytesRemain;
            ++m_readBytes;
            return result;
        }

        /// <summary>
        /// unsynchronizationを考慮したバイト列読み出し処理
        /// </summary>
        private byte[] ReadBytesWithUnsynchro(BinaryReader br, int bytes) {
            if (m_unsynchro) {
                // unsynchroする
                var buff = new System.Collections.Generic.List<byte>();
                for (int i=0; i < bytes; ++i) {
                    var b = BinaryReadByte(br);
                    if (b == 0xff) {
                        BinaryReadByte(br);
                    }
                    buff.Add(b);
                }
                return buff.ToArray();
            } else {
                // unsynchroしない
                var result = BinaryReadBytes(br, bytes);
                return result;
            }
        }

        /// <summary>
        /// unsynchronizationを考慮したバイト列スキップ。
        /// </summary>
        /// <param name="bytes">unsynchro前のバイト数。unsynchro時は、スキップするバイト列に0xffが現れるとこれよりも多くスキップする</param>
        private void SkipBytesWithUnsynchro(BinaryReader br, long bytes) {
            if (m_unsynchro) {
                // unsynchroする
                for (long i=0; i < bytes; ++i) {
                    var b = BinaryReadByte(br);
                    --m_bytesRemain;
                    ++m_readBytes;
                    if (b == 0xff) {
                        BinaryReadByte(br);
                        --m_bytesRemain;
                        ++m_readBytes;
                    }
                }
                return;
            } else {
                // unsynchroしない
                PcmDataLib.Util.BinaryReaderSkip(br, bytes);
                m_bytesRemain -= bytes;
                m_readBytes   += bytes;
            }
        }

        private static UInt16 ByteArrayToBigU16(byte[] bytes, int offset=0) {
            return (UInt16)(((UInt16)bytes[offset+0] << 8) +
                            ((UInt16)bytes[offset+1] << 0));
        }

        private static UInt32 ByteArrayToBigU24(byte[] bytes, int offset = 0) {
            return  (UInt32)((UInt32)bytes[offset + 0] << 16) +
                    (UInt32)((UInt32)bytes[offset + 1] << 8) +
                    (UInt32)((UInt32)bytes[offset + 2] << 0);
        }

        private static UInt32 ByteArrayToBigU32(byte[] bytes, int offset = 0) {
            return  (UInt32)((UInt32)bytes[offset+0] << 24) +
                    (UInt32)((UInt32)bytes[offset+1] << 16) +
                    (UInt32)((UInt32)bytes[offset+2] << 8) +
                    (UInt32)((UInt32)bytes[offset+3] << 0);
        }

        private static int ID3TagHeaderSize(byte[] sizeBytes) {
            System.Diagnostics.Debug.Assert(sizeBytes.Length == 4);

            return  ((int)(sizeBytes[0] & 0x7f) << 21) +
                    ((int)(sizeBytes[1] & 0x7f) << 14) +
                    ((int)(sizeBytes[2] & 0x7f) << 7) +
                    ((int)(sizeBytes[3] & 0x7f) << 0);
        }

        private byte[] m_tagVersion;

        private ID3Result ReadTagHeader(BinaryReader br) {
            var tagIdentifier = BinaryReadBytes(br, 3);
            if (    tagIdentifier[0] != 'I' ||
                    tagIdentifier[1] != 'D' ||
                    tagIdentifier[2] != '3') {
                return ID3Result.ReadError;
            }

            m_tagVersion = BinaryReadBytes(br, 2);
            if (MinorVersion != 2 && MinorVersion != 3 && MinorVersion != 4) {
                // not ID3v2.2 nor ID3v2.3 ...
                return ID3Result.NotSupportedID3version;
            }
            var tagFlags = BinaryReadByte(br);
            m_bytesRemain = ID3TagHeaderSize(BinaryReadBytes(br, 4));

            if (0 != (tagFlags & 0x80)) {
                // Unsynchronizationモード(0xffを読んだら次の1バイトを捨てる)
                m_unsynchro = true;
            } else {
                m_unsynchro = false;
            }

            // ID3v2 tagヘッダー読み込み終了。

            // extended headerがもしあれば読み込む。
            if (0 != (tagFlags & 0x40)) {
                switch (MinorVersion) {
                case 3: // v2.3
                    {
                        var ehSize  = ByteArrayToBigU32(ReadBytesWithUnsynchro(br, 4));
                        var ehFlags = ByteArrayToBigU16(ReadBytesWithUnsynchro(br, 2));
                        var ehPad   = ByteArrayToBigU32(ReadBytesWithUnsynchro(br, 4));
                        SkipBytesWithUnsynchro(br, ehSize + ehPad - 6);
                    }
                    break;
                case 4: // v2.4
                    {
                        var ehSize = ByteArrayToBigU32(ReadBytesWithUnsynchro(br, 4));
                        var numberOfFlagBytes = BinaryReadByte(br);
                        var extendedFlags = BinaryReadByte(br);
                        SkipBytesWithUnsynchro(br, ehSize - 6);
                    }
                    break;
                default:
                    throw new NotImplementedException("Unknown ID3 version");
                }
            }

            if (0 != (tagFlags & 0x10)) {
                // v2.4 Footer present
                m_footerPresent = true;
            } else {
                m_footerPresent = false;
            }

            return ID3Result.Success;
        }

        private ID3Result ReadTagFooter(BinaryReader br) {
            var tagIdentifier = BinaryReadBytes(br, 3);
            if (tagIdentifier[0] != '3' ||
                    tagIdentifier[1] != 'D' ||
                    tagIdentifier[2] != 'I') {
                return ID3Result.ReadError;
            }
            var tagVersion = BinaryReadBytes(br, 2);
            var tagFlags = BinaryReadByte(br);
            var tagSize = ID3TagHeaderSize(BinaryReadBytes(br, 4));

            return ID3Result.Success;
        }

        private string ReadTextFrame(BinaryReader br, int frameSize, int frameFlags) {
            // フラグを見る
            bool compression = (frameFlags & 0x0080) != 0;
            bool encryption  = (frameFlags & 0x0040) != 0;

            if (compression || encryption) {
                SkipFrameContent(br, frameSize);
                return string.Empty;
            }

            var encoding = ReadBytesWithUnsynchro(br, 1);
            var text = ReadBytesWithUnsynchro(br, frameSize - 1);

            string result = string.Empty;
            switch (encoding[0]) {
            case 0: // ISO-8859-1だがSJISで読む
                result = System.Text.Encoding.Default.GetString(text, 0, text.Length).Trim(new char[] { '\0' });
                break;
            case 1: // UTF-16 with BOM
                if (2 < text.Length && text[0] == 0xfe && text[1] == 0xff) {
                    // UTF-16BE
                    result = System.Text.Encoding.BigEndianUnicode.GetString(text, 2, text.Length - 2).Trim(new char[] { '\0' });
                } else if (2 < text.Length && text[0] == 0xff && text[1] == 0xfe) {
                    // UTF-16LE
                    result = System.Text.Encoding.Unicode.GetString(text, 2, text.Length - 2).Trim(new char[] { '\0' });
                } else {
                    // unknown encoding!
                }
                break;
            case 2: // UTF-16BE without BOM
                result = System.Text.Encoding.BigEndianUnicode.GetString(text, 0, text.Length).Trim(new char[] { '\0' });
                break;
            case 3: // UTF-8
                if (3 < text.Length && text[0] == 0xef && text[1] == 0xbb && text[2] == 0xbf) {
                    // UTF-8 with BOM
                    result = System.Text.Encoding.UTF8.GetString(text, 3, text.Length-3).Trim(new char[] { '\0' });
                } else {
                    // UTF-8 without BOM
                    result = System.Text.Encoding.UTF8.GetString(text, 0, text.Length).Trim(new char[] { '\0' });
                }
                break;
            default:
                // Unknown encoding!
                break;
            }
            return result;
        }

        private int SkipTextString(BinaryReader br) {
            byte[] textChar;

            int advance = 0;
            do {
                textChar = ReadBytesWithUnsynchro(br, 1);
                ++advance;
            } while (textChar[0] != 0);

            return advance;
        }

        private byte[] ReadAttachedPictureFrameV22(BinaryReader br, int frameSize) {
            var encoding    = ReadBytesWithUnsynchro(br, 1);
            var imageFormat = ReadBytesWithUnsynchro(br, 3);
            var pictureType = ReadBytesWithUnsynchro(br, 1);

            int pos = 5;

            // skip description string
            pos += SkipTextString(br);

            // pictureData
            return ReadBytesWithUnsynchro(br, frameSize - pos);
        }

        private byte[] ReadAttachedPictureFrameV23(BinaryReader br, int frameSize) {
            var encoding    = ReadBytesWithUnsynchro(br, 1);

            int pos = 1;
            // skip MIME type
            pos += SkipTextString(br);

            var pictureType = ReadBytesWithUnsynchro(br, 1);
            ++pos;

            // skip Description string
            pos += SkipTextString(br);

            // pictureData
            return ReadBytesWithUnsynchro(br, frameSize - pos);
        }

        private void SkipFrameContent(BinaryReader br, int frameSize) {
            ReadBytesWithUnsynchro(br, frameSize);
        }

        private ID3Result ReadFramesV23(BinaryReader br) {
            // ID3v2.3 frameを読み込み。
            while (0 < m_bytesRemain) {
                // ID3v2 frame header
                var frameId    = ByteArrayToBigU32(ReadBytesWithUnsynchro(br, 4));
                int frameSize  = (int)ByteArrayToBigU32(ReadBytesWithUnsynchro(br, 4));
                var frameFlags = ByteArrayToBigU16(ReadBytesWithUnsynchro(br, 2));

                switch (frameId) {
                case 0x54414c42:
                    // "TALB" Album
                    AlbumName = ReadTextFrame(br, frameSize, frameFlags);
                    break;
                case 0x54495432:
                    // "TIT2" Title
                    TitleName = ReadTextFrame(br, frameSize, frameFlags);
                    break;
                case 0x54504531:
                    // "TPE1" artist
                    ArtistName = ReadTextFrame(br, frameSize, frameFlags);
                    break;
                case 0x54434f4d:
                    // "TCOM" composer
                    Composer = ReadTextFrame(br, frameSize, frameFlags);
                    break;
                case 0x41504943:
                    // "APIC" attached picture
                    m_pictureData = ReadAttachedPictureFrameV23(br, frameSize);
                    PictureBytes = m_pictureData.Length;
                    break;
                case 0:
                    // 終わり
                    return ID3Result.Success;
                default:
                    SkipFrameContent(br, frameSize);
                    break;
                }
            }

            return ID3Result.Success;
        }

        private ID3Result ReadFramesV22(BinaryReader br) {
            // ID3v2.2 frameを読み込み。
            while (0 < m_bytesRemain) {
                // ID3v2 frame header
                var frameId    = ByteArrayToBigU24(ReadBytesWithUnsynchro(br, 3));
                int frameSize  = (int)ByteArrayToBigU24(ReadBytesWithUnsynchro(br, 3));

                /*
                Console.WriteLine("ReadFramesV22 {5:X8} \"{0}{1}{2}\" {3}bytes remain={4}",
                    (char)(0xff & (frameId >> 16)),
                    (char)(0xff & (frameId >> 8)),
                    (char)(0xff & (frameId >> 0)),
                    frameSize,
                    m_bytesRemain,
                    frameId);
                */

                switch (frameId) {
                case 0x54414c:
                    // "TAL" Album name
                    AlbumName = ReadTextFrame(br, frameSize, 0);
                    break;
                case 0x545432:
                    // "TT2" Title
                    TitleName = ReadTextFrame(br, frameSize, 0);
                    break;
                case 0x545031:
                    // "TP1" Artist
                    ArtistName = ReadTextFrame(br, frameSize, 0);
                    break;
                case 0x54434d:
                    // "TCM" composer
                    Composer = ReadTextFrame(br, frameSize, 0);
                    break;
                case 0x504943:
                    // "PIC" Attached Picture
                    m_pictureData = ReadAttachedPictureFrameV22(br, frameSize);
                    PictureBytes = m_pictureData.Length;
                    break;
                case 0:
                    // 終わり
                    return ID3Result.Success;
                default:
                    SkipFrameContent(br, frameSize);
                    break;
                }
            }

            return ID3Result.Success;
        }

        private void SkipPadding(BinaryReader br) {
            long skipBytes = m_bytesRemain;
            if (m_footerPresent) {
                skipBytes = m_bytesRemain - 10;
            }

            if (skipBytes < 0) {
                return;
            }

            SkipBytesWithUnsynchro(br, skipBytes);
        }

        public ID3Result Read(BinaryReader br) {
            Clear();

            // ID3v2 tagヘッダー読み込み。
            var result = ReadTagHeader(br);
            if (result != ID3Result.Success) {
                return result;
            }

            Console.WriteLine("read  ={0}", m_readBytes);
            Console.WriteLine("remain={0}", m_bytesRemain);

            switch (m_tagVersion[0]) {
            case 4:
            case 3:
                result = ReadFramesV23(br);
                break;
            case 2:
                result = ReadFramesV22(br);
                break;
            default:
                throw new NotImplementedException("Unknown ID3 version");
            }
            if (result != ID3Result.Success) {
                return result;
            }

            Console.WriteLine("read  ={0}", m_readBytes);
            Console.WriteLine("remain={0}", m_bytesRemain);

            SkipPadding(br);

            if (m_footerPresent) {
                result = ReadTagFooter(br);
                if (result != ID3Result.Success) {
                    return result;
                }
            }

            return result;
        }

    }
}
