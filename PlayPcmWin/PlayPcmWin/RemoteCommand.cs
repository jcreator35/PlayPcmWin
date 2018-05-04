using System.Collections.Generic;
using System;

namespace PlayPcmWin {
    class RemoteCommandPlayListItem {
        public int durationMillsec;
        public int sampleRate;
        public int bitDepth;
        public string albumName;
        public string artistName;
        public string titleName;
        public byte[] albumCoverArt;
        public RemoteCommandPlayListItem(int aDurationMilliSec,
            int aSampleRate, int aBitDepth, string aAlbumName,
            string aArtistName, string aTitleName, byte[] aAlbumCoverArt) {
            durationMillsec = aDurationMilliSec;
            sampleRate = aSampleRate;
            bitDepth = aBitDepth;
            albumName = aAlbumName;
            artistName = aArtistName;
            titleName = aTitleName;
            albumCoverArt = aAlbumCoverArt;
        }
    };

    enum RemoteCommandType {
        PlaylistData,
        Exit,
        Pause,
        Stop,
        PlayPositionUpdate,

        PPWR,
        PlaylistWant,
        Play,
        Seek,
        SelectTrack,
    };

    class RemoteCommand {
        public const int FOURCC_PLAYLIST_DATA        = 0x534c4c50; // "PLLS" Server -> Client
        public const int FOURCC_EXIT                 = 0x54495845; // "EXIT" both direction
        public const int FOURCC_PAUSE                = 0x53554150; // "PAUS" both direction
        public const int FOURCC_STOP                 = 0x504f5453; // "STOP" Server -> Client
        public const int FOURCC_PLAY_POSITION_UPDATE = 0x55504c50; // "PLPU" Server -> Client

        public const int FOURCC_PPWR                 = 0x52575050; // "PPWR" Client -> Server
        public const int FOURCC_PLAYLIST_WANT        = 0x574c4c50; // "PLLW" Client -> Server
        public const int FOURCC_PLAY                 = 0x59414c50; // "PLAY" Client -> Server
        public const int FOURCC_SEEK                 = 0x4b454553; // "SEEK" Client -> Server
        public const int FOURCC_SELECT_TRACK         = 0x544c4553; // "SELT" Client -> Server

        private static int RemoteCommandTypeToFourCC(RemoteCommandType t) {
            switch (t) {
            case RemoteCommandType.PlaylistData: return FOURCC_PLAYLIST_DATA;
            case RemoteCommandType.Exit: return FOURCC_EXIT;
            case RemoteCommandType.Pause: return FOURCC_PAUSE;
            case RemoteCommandType.Stop: return FOURCC_STOP;
            case RemoteCommandType.PlayPositionUpdate: return FOURCC_PLAY_POSITION_UPDATE;

            default:
                System.Diagnostics.Debug.Assert(false);
                return FOURCC_EXIT;
            }
        }

        // 受信データ用
        private static RemoteCommandType FourCCToRemoteCommandType(int fourcc) {
            switch (fourcc) {
            case FOURCC_EXIT: return RemoteCommandType.Exit;
            case FOURCC_PAUSE: return RemoteCommandType.Pause;
            case FOURCC_PLAYLIST_WANT: return RemoteCommandType.PlaylistWant;
            case FOURCC_PLAY: return RemoteCommandType.Play;
            case FOURCC_SEEK: return RemoteCommandType.Seek;
            case FOURCC_SELECT_TRACK: return RemoteCommandType.SelectTrack;
            default:
                Console.WriteLine("D: Unknown fourcc {0:X8}. defaults to EXIT", fourcc);
                return RemoteCommandType.Exit;
            }
        }

        // 送信用
        public enum PlaybackState {
            Stopped = 0,
            Playing = 1,
            Paused  = 2,
        }

        public RemoteCommandType cmd;
        public int trackIdx;
        public int positionMillisec;
        public PlaybackState state;
        public List<RemoteCommandPlayListItem> playlist = new List<RemoteCommandPlayListItem>();

        public RemoteCommand(RemoteCommandType t, int aTrackIdx=0, int aPositionMs=0) {
            cmd = t;
            trackIdx = aTrackIdx;
            positionMillisec = aPositionMs;
        }

        // 受信したとき用ctor
        public RemoteCommand(int header, int bytes, byte[] payload) {
            cmd = FourCCToRemoteCommandType(header);

            switch (header) {
            case FOURCC_PLAYLIST_WANT:
            case FOURCC_EXIT:
            case FOURCC_PAUSE:
                // no payload
                if (payload.Length != 0) {
                    Console.WriteLine("D: RemoteCommand {0} and payload length is not 0! {1}.", cmd, payload.Length);
                    break;
                }
                break;
            case FOURCC_PLAY:
            case FOURCC_SELECT_TRACK:
                /* trackIdx (4 bytes)
                 */
                if (payload.Length != 4) {
                    Console.WriteLine("D: RemoteCommand {0} and payload length is not 4! {1}.", cmd, payload.Length);
                    break;
                }
                trackIdx = BitConverter.ToInt32(payload, 0);
                if (trackIdx < 0) {
                    Console.WriteLine("D: RemoteCommand {0} and trackIdx is negative value {1}.", cmd, payload.Length);
                    trackIdx = 0;
                    break;
                }
                break;
            case FOURCC_SEEK:
                /* trackIdx (4bytes)
                 * positionMillisec (4bytes)
                 */
                if (payload.Length != 8) {
                    Console.WriteLine("D: RemoteCommand Seek and payload length is not 8 {0}.", payload.Length);
                    break;
                }
                trackIdx = BitConverter.ToInt32(payload, 0);
                if (trackIdx < 0) {
                    Console.WriteLine("D: RemoteCommand Play and trackIdx is negative value {0}.", trackIdx);
                    trackIdx = 0;
                    break;
                }
                positionMillisec = BitConverter.ToInt32(payload, 4);
                if (positionMillisec < 0) {
                    Console.WriteLine("D: RemoteCommand Play and positionMillisec is negative value {0}.", positionMillisec);
                    positionMillisec = 0;
                    break;
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        // 送出用バイナリデータを生成する。
        public byte[] GenerateMessage() {
            byte[] r = new byte[0];

            Console.WriteLine("GenerateMessage({0})\n", cmd);

            switch (cmd) {
            case RemoteCommandType.Exit:
            case RemoteCommandType.Stop: {
                // fourcc             (4bytes)
                // payload bytes == 0 (8bytes)
                    r = new byte[12];
                    int fourcc = RemoteCommandTypeToFourCC(cmd);
                    long payloadBytes = 0;
                    Array.Copy(BitConverter.GetBytes(fourcc), 0, r, 0, 4);
                    Array.Copy(BitConverter.GetBytes(payloadBytes), 0, r, 4, 8);
                } break;

            case RemoteCommandType.Pause: {
                    /* fourcc             (4bytes)
                     * payload bytes == 8 (8bytes)
                     * trackIdx           (4bytes)
                     */
                    r = new byte[16];
                    int fourcc = RemoteCommandTypeToFourCC(cmd);
                    long payloadBytes = 4;
                    Array.Copy(BitConverter.GetBytes(fourcc), 0, r, 0, 4);
                    Array.Copy(BitConverter.GetBytes(payloadBytes), 0, r, 4, 8);
                    Array.Copy(BitConverter.GetBytes(trackIdx), 0, r, 12, 4);
                }
                break;

            case RemoteCommandType.PlayPositionUpdate: {
                    /* fourcc              (4bytes)
                     * payload bytes == 12 (8bytes)
                     * state               (4bytes)
                     * trackIdx            (4bytes)
                     * positionMillisec    (4bytes)
                     */
                    r = new byte[24];
                    int fourcc = RemoteCommandTypeToFourCC(cmd);
                    long payloadBytes = 12;
                    Array.Copy(BitConverter.GetBytes(fourcc), 0, r, 0, 4);
                    Array.Copy(BitConverter.GetBytes(payloadBytes), 0, r, 4, 8);
                    Array.Copy(BitConverter.GetBytes((int)state), 0, r, 12, 4);
                    Array.Copy(BitConverter.GetBytes(trackIdx), 0, r, 16, 4);
                    Array.Copy(BitConverter.GetBytes(positionMillisec), 0, r, 20, 4);
                }
                break;

            case RemoteCommandType.PlaylistData: {
                /* 
                * "PLLS"
                * Number of payload bytes (int64)
                * 
                * playbackState    (int32)
                * 
                * Number of tracks (int32)
                * if (0 < number of tracks) {
                *   selected track (int32)
                * 
                *   Track0 duration millisec (int32)
                *   Track0 sampleRate        (int32)
                *   Track0 bitdepth          (int32)
                *   Track0 albumName bytes (int32)
                *   Track0 albumName (utf8 string)
                *   Track0 artistName bytes (int32)
                *   Track0 artistName (utf8 string)
                *   Track0 titleName bytes (int32)
                *   Track0 titleName (utf8 string)
                *   Track0 albumCoverArt bytes (int32)
                *   Track0 albumCoverArt (binary)
                * 
                *   Track1 
                *   ...
                * }
                */
                    List<byte[]> sendData = new List<byte[]>();

                    sendData.Add(BitConverter.GetBytes((int)state));

                    sendData.Add(BitConverter.GetBytes(playlist.Count));

                    if (0 < playlist.Count) {
                        sendData.Add(BitConverter.GetBytes(trackIdx));

                        int idx = 0;
                        foreach (var pl in playlist) {
                            sendData.Add(BitConverter.GetBytes(pl.durationMillsec));
                            sendData.Add(BitConverter.GetBytes(pl.sampleRate));
                            sendData.Add(BitConverter.GetBytes(pl.bitDepth));
                            AppendString(pl.albumName, ref sendData);
                            AppendString(pl.artistName, ref sendData);
                            AppendString(pl.titleName, ref sendData);
                            AppendByteArray(pl.albumCoverArt, ref sendData);
                            ++idx;
                        }
                    }
                    sendData.Insert(0, BitConverter.GetBytes(ByteArrayListBytes(sendData)));
                    sendData.Insert(0, BitConverter.GetBytes(FOURCC_PLAYLIST_DATA));

                    r = ByteArrayListToByteArray(sendData);
                } break;
            default:
                throw new NotImplementedException();
            }

            return r;
        }

        private static long ByteArrayListBytes(List<byte[]> a) {
            long bytes = 0;
            foreach (var item in a) {
                bytes += item.Length;
            }
            return bytes;
        }

        private static byte[] ByteArrayListToByteArray(List<byte[]> a) {
            int bytes = 0;
            foreach (var item in a) {
                bytes += item.Length;
            }

            var result = new byte[bytes];
            int offs = 0;
            foreach (var item in a) {
                Array.Copy(item, 0, result, offs, item.Length);
                offs += item.Length;
            }

            return result;
        }

        private static void AppendString(string s, ref List<byte[]> to) {
            if (s.Length == 0) {
                int v0 = 0;
                to.Add(BitConverter.GetBytes(v0));
                return;
            }

            var sBytes = System.Text.Encoding.UTF8.GetBytes(s);
            to.Add(BitConverter.GetBytes(sBytes.Length));
            to.Add(sBytes);
        }

        private static void AppendByteArray(byte[] b, ref List<byte[]> to) {
            if (b == null || b.Length == 0) {
                int v0 = 0;
                to.Add(BitConverter.GetBytes(v0));
                return;
            }

            to.Add(BitConverter.GetBytes(b.Length));
            to.Add(b);
        }
    };
}
