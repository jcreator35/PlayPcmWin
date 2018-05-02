using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayPcmWin {
    class PPWServer {
        private const int STREAM_READ_TIMEOUT = 100;

        internal class Utility {
            private const int RECV_BUFF_BYTES = 8192;
            public static byte[] StreamReadBytes(NetworkStream stream, int bytes) {
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

            public static void StreamWriteInt32(NetworkStream stream, int v) {
                byte[] data = BitConverter.GetBytes(v);
                stream.Write(data, 0, data.Length);
            }

            public static void StreamWriteInt64(NetworkStream stream, long v) {
                byte[] data = BitConverter.GetBytes(v);
                stream.Write(data, 0, data.Length);
            }
        }

        public delegate void RemoteCmdRecvDelegate(NetworkStream stream, RemoteCommand cmd);

        RemoteCmdRecvDelegate mRemoteCmdRecvDelegate = null;

        TcpListener mListener = null;

        object mLock = new object();

        public const int VERSION = 100;
        public const int FOURCC_PPWR          = 0x52575050;
        public const int FOURCC_PLAYLIST_WANT = 0x574c4c50;
        public const int FOURCC_PLAYLIST_SEND = 0x534c4c50;
        public const int FOURCC_EXIT          = 0x54495845;

        enum ReturnCode {
            OK,
            Timeout,
            NotPPWRemote,
            PlayPcmWinVersionIsTooLow,
            PPWRemoteVersionIsTooLow,
        };

        public void Abort() {
            if (mListener != null) {
                Console.WriteLine("ServerController.Abort()");
                mListener.Server.Close();
            }
        }

        private static ReturnCode RecvGreetings(NetworkStream stream) {
            int header = Utility.StreamReadInt32(stream);
            if (header != FOURCC_PPWR) {
                // 最初の4バイトは "PPWR"
                return ReturnCode.NotPPWRemote;
            }

            long version = Utility.StreamReadInt64(stream);
            if (version < VERSION) {
                return ReturnCode.PPWRemoteVersionIsTooLow;
            }
            if (VERSION < version) {
                return ReturnCode.PlayPcmWinVersionIsTooLow;
            }

            return ReturnCode.OK;
        }

        /// <returns>true: continue, false:end</returns>
        private bool RecvRequests(NetworkStream stream) {
            bool bContinue = true;

            int header = Utility.StreamReadInt32(stream);
            switch (header) {
            case FOURCC_PLAYLIST_WANT: // want playlist 
                mRemoteCmdRecvDelegate(stream, new RemoteCommand(RemoteCommandType.PlaylistWant));
                break;
            case FOURCC_EXIT:
                bContinue = false;
                break;
            default:
                break;
            }

            return bContinue;
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

        private static void AppendByteArray(byte [] b, ref List<byte[]> to) {
            if (b == null || b.Length == 0) {
                int v0 = 0;
                to.Add(BitConverter.GetBytes(v0));
                return;
            }

            to.Add(BitConverter.GetBytes(b.Length));
            to.Add(b);
        }

        public void Send(NetworkStream stream, RemoteCommand cmd) {
            lock (mLock) {
                switch (cmd.cmd) {
                case RemoteCommandType.PlaylistSend:
                    /* send playlist
                     * 
                     * "PLLS"
                     * Number of payload bytes (int64)
                     * 
                     * Number of tracks (int32)
                     * 
                     * Track0 duration millisec (int32)
                     * Track0 artistName bytes (int32)
                     * Track0 artistName (utf8 string)
                     * Track0 titleName bytes (int32)
                     * Track0 titleName (utf8 string)
                     * Track0 albumCoverArt bytes (int32)
                     * Track0 albumCoverArt (binary)
                     * 
                     * Track1 
                     * ...
                    */

                    List<byte[]> sendData = new List<byte[]>();
                    sendData.Add(BitConverter.GetBytes(cmd.playlist.Count));

                    foreach (var pl in cmd.playlist) {
                        sendData.Add(BitConverter.GetBytes(pl.durationMillsec));
                        AppendString(pl.artistName, ref sendData);
                        AppendString(pl.titleName, ref sendData);
                        AppendByteArray(pl.albumCoverArt, ref sendData);
                    }

                    // stream output
                    byte[] dataBytes = ByteArrayListToByteArray(sendData);

                    Utility.StreamWriteInt32(stream, FOURCC_PLAYLIST_SEND);
                    Utility.StreamWriteInt64(stream, dataBytes.LongLength);
                    stream.Write(dataBytes, 0, dataBytes.Length);
                    stream.Flush();
                    break;
                default:
                    break;
                }
            }
        }

        private string mListenIPAddress = "";
        private int mListenPort = -1;
        public string ListenIPAddress { get { return mListenIPAddress; } }
        public int ListenPort { get { return mListenPort; } }

        private NetworkStream mStream = null;

        private static string GetLocalIPAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void UpdateListenIPandPort(int port) {
            mListenIPAddress = GetLocalIPAddress();
            mListenPort = port;
        }

        private List<RemoteCommand> mCmdToSend = new List<RemoteCommand>();

        public void SendAsync(RemoteCommand cmd) {
            mCmdToSend.Add(cmd);
        }

        public void Run(RemoteCmdRecvDelegate remoteCmdRecvDelegate, BackgroundWorker bgWorker, int port) {
            mRemoteCmdRecvDelegate = remoteCmdRecvDelegate;

            try {
                // コントロールポートの待受を開始する。
                IPAddress addr = IPAddress.Any;
                mListener = new TcpListener(addr, port);
                mListener.Start();

                UpdateListenIPandPort(port);

                bgWorker.ReportProgress(1,
                    string.Format("PPWServer started. Waiting PPWRemote on:\n    IP address = {0}\n    Port = {1}\n", mListenIPAddress, mListenPort));

                while (true) {
                    bool bPending = false;
                    while (!bPending) {
                        bPending = mListener.Pending();
                        if (!bPending) {
                            Thread.Sleep(STREAM_READ_TIMEOUT);
                        }
                        if (bgWorker.CancellationPending) {
                            // cancel
                            break;
                        }
                    }
                    if (!bPending) {
                        // cancel
                        break;
                    }

                    // クライアントが接続してきてAcceptを待っている。
                    using (var client = mListener.AcceptTcpClient()) {
                        using (var stream = client.GetStream()) {
                            stream.ReadTimeout = STREAM_READ_TIMEOUT;

                            // 接続してきた。設定情報を受信する。
                            var result = ReturnCode.Timeout;

                            try {
                                result = RecvGreetings(stream);
                            } catch (IOException ex) {
                                //Console.WriteLine("{0}", ex);
                            }

                            if (result != ReturnCode.OK) {
                                // 失敗したので接続を切る。
                                bgWorker.ReportProgress(1, string.Format("Connected from {0}\nPPWServer Error: {1}. Connection closed.\nWaiting PPWRemote on: IP address = {2}, Port = {3}\n",
                                    client.Client.RemoteEndPoint, result, mListenIPAddress, mListenPort));
                                continue;
                            }

                            bgWorker.ReportProgress(1, string.Format("Connected from {0}\n", client.Client.RemoteEndPoint));

                            bool bContinue = true;

                            while (bContinue) {
                                while (0 < mCmdToSend.Count) {
                                    Send(stream, mCmdToSend[0]);
                                    mCmdToSend.RemoveAt(0);
                                }

                                if (stream.DataAvailable) {
                                    bContinue = RecvRequests(stream);
                                } else {
                                    Thread.Sleep(STREAM_READ_TIMEOUT);
                                }

                                if (bgWorker.CancellationPending) {
                                    bContinue = false;
                                }
                            }

                            bgWorker.ReportProgress(1, string.Format("Connection closed.\n\n"));
                        }
                    }
                }
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\n", e);
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\n", e);
            } finally {
            }

            if (mListener != null) {
                mListener.Stop();
            }
            mListener = null;

            bgWorker.ReportProgress(1, string.Format("PPWServer stopped.\n\n"));
        }
    }
}
