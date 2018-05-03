using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PlayPcmWin {
    class PPWServer {
        private const int STREAM_READ_TIMEOUT = 10000;
        private const int RECV_TOO_LARGE = 10000;
        public const int VERSION = 100;

        public delegate void RemoteCmdRecvDelegate(RemoteCommand cmd);
        public string ListenIPAddress { get { return mListenIPAddress; } }
        public int ListenPort { get { return mListenPort; } }

        private RemoteCmdRecvDelegate mRemoteCmdRecvDelegate = null;
        private TcpListener mListenSock = null;
        private NetworkStream mStream = null;
        private string mListenIPAddress = "";
        private int mListenPort = -1;
        private object mLock = new object();

        private BackgroundWorker mBgWorker;
        private List<RemoteCommand> mCmdToSend = new List<RemoteCommand>();

        internal class Utility {
            public static byte[] StreamReadBytes(BinaryReader br, int bytes) {
                return br.ReadBytes(bytes);
            }

            public static int StreamReadInt32(BinaryReader br) {
                return br.ReadInt32();
            }

            public static long StreamReadInt64(BinaryReader br) {
                return br.ReadInt64();
            }

            public static void StreamWriteBytes(BinaryWriter bw, byte[] b) {
                bw.Write(b, 0, b.Length);
            }

            public static void StreamWriteInt32(BinaryWriter bw, int v) {
                byte[] data = BitConverter.GetBytes(v);
                bw.Write(data, 0, data.Length);
            }

            public static void StreamWriteInt64(BinaryWriter bw, long v) {
                byte[] data = BitConverter.GetBytes(v);
                bw.Write(data, 0, data.Length);
            }
        }

        enum ReturnCode {
            OK,
            Timeout,
            NotPPWRemote,
            PlayPcmWinVersionIsTooLow,
            PPWRemoteVersionIsTooLow,
        };

        public void Abort() {
            Console.WriteLine("ServerController.Abort()");
            if (mStream != null) {
                Console.WriteLine("ServerController.Abort() close stream");
                mStream.Close();
                mStream = null;
            }
            if (mListenSock != null) {
                Console.WriteLine("ServerController.Abort() close listen socket");
                mListenSock.Server.Close();
            }
        }

        private static ReturnCode RecvGreetings(BinaryReader br) {
            /*
              "PPWR"
              size    = 8   (8bytes)
              version = 100 (8bytes)
             */

            int header = Utility.StreamReadInt32(br);
            if (header != RemoteCommand.FOURCC_PPWR) {
                // 最初の4バイトは "PPWR"
                return ReturnCode.NotPPWRemote;
            }

            long sz = Utility.StreamReadInt64(br);
            if (sz != 8) {
                return ReturnCode.NotPPWRemote;
            }

            long version = Utility.StreamReadInt64(br);
            if (version < VERSION) {
                return ReturnCode.PPWRemoteVersionIsTooLow;
            }
            if (VERSION < version) {
                return ReturnCode.PlayPcmWinVersionIsTooLow;
            }

            return ReturnCode.OK;
        }

        /// <returns>true: continue, false:end</returns>
        private bool RecvRequests(BinaryReader br) {
            bool bContinue = true;

            int header = Utility.StreamReadInt32(br);
            long bytes = Utility.StreamReadInt64(br);
            if (bytes < 0 || RECV_TOO_LARGE < bytes) {
                return false;
            }

            var payload = new byte[0];
            if (0 < bytes) {
                payload = Utility.StreamReadBytes(br, (int)bytes);
            }

            var cmd = new RemoteCommand(header, (int)bytes, payload);
            if (cmd.cmd == RemoteCommandType.Exit) {
                bContinue = false;
            }

            mRemoteCmdRecvDelegate(cmd);

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

        private void Send(BinaryWriter bw, RemoteCommand cmd) {
            switch (cmd.cmd) {
            case RemoteCommandType.Exit:
                Utility.StreamWriteInt32(bw, RemoteCommand.FOURCC_EXIT);
                Utility.StreamWriteInt64(bw, 0);
                break;
            case RemoteCommandType.PlaylistSend:
                /* send playlist
                    * 
                    * "PLLS"
                    * Number of payload bytes (int64)
                    * 
                    * Number of tracks (int32)
                    * selected track (int32)
                    * 
                    * Track0 duration millisec (int32)
                    * Track0 sampleRate        (int32)
                    * Track0 bitdepth          (int32)
                    * Track0 albumName bytes (int32)
                    * Track0 albumName (utf8 string)
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
                sendData.Add(BitConverter.GetBytes(cmd.trackIdx));

                foreach (var pl in cmd.playlist) {
                    sendData.Add(BitConverter.GetBytes(pl.durationMillsec));
                    sendData.Add(BitConverter.GetBytes(pl.sampleRate));
                    sendData.Add(BitConverter.GetBytes(pl.bitDepth));
                    AppendString(pl.albumName, ref sendData);
                    AppendString(pl.artistName, ref sendData);
                    AppendString(pl.titleName, ref sendData);
                    AppendByteArray(pl.albumCoverArt, ref sendData);
                    Console.WriteLine("albumCoverArt size={0}", pl.albumCoverArt.Length);
                }

                // stream output
                byte[] dataBytes = ByteArrayListToByteArray(sendData);

                Utility.StreamWriteInt32(bw, RemoteCommand.FOURCC_PLAYLIST_SEND);
                Utility.StreamWriteInt64(bw, dataBytes.LongLength);
                Utility.StreamWriteBytes(bw, dataBytes);
                break;
            default:
                break;
            }

            // Hope this flushes buffers
            bw.Flush();
        }

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

        public void SendAsync(RemoteCommand cmd) {
            lock (mLock) {
                mCmdToSend.Add(cmd);
            }
        }

        /// <returns>bContinue : true:continue to listen, false: abort and end</returns>
        private bool InteractWithClient(TcpClient client, NetworkStream stream) {
            var result = ReturnCode.Timeout;

            stream.ReadTimeout = STREAM_READ_TIMEOUT;

            // 設定情報を受信する。

            var br = new BinaryReader(stream);
            result = RecvGreetings(br);

            if (result != ReturnCode.OK) {
                // 失敗したので接続を切る。
                mBgWorker.ReportProgress(1, string.Format("Connected from {0}\nPPWServer Error: {1}. Connection closed.\nWaiting PPWRemote on: IP address = {2}, Port = {3}\n",
                    client.Client.RemoteEndPoint, result, mListenIPAddress, mListenPort));
                return true;
            }

            mBgWorker.ReportProgress(1, string.Format("Connected from {0}\n", client.Client.RemoteEndPoint));

            bool bContinue = true;
            using (var bw = new BinaryWriter(stream)) {
                while (bContinue) {
                    // たまったコマンドを全て送出する。
                    int sendCmdCount = 0;
                    lock (mLock) {
                        sendCmdCount = mCmdToSend.Count;
                    }

                    while (0 < sendCmdCount) {
                        RemoteCommand rc = null;
                        lock (mLock) {
                            rc = mCmdToSend[0];
                        }

                        Send(bw, rc);

                        lock (mLock) {
                            mCmdToSend.RemoveAt(0);
                            sendCmdCount = mCmdToSend.Count;
                        }
                    }

                    if (stream.DataAvailable) {
                        // 受信データがある。
                        bContinue = RecvRequests(br);
                    } else {
                        Thread.Sleep(STREAM_READ_TIMEOUT);
                    }

                    if (mBgWorker.CancellationPending) {
                        // 終了コマンドを送出する。
                        Send(bw, new RemoteCommand(RemoteCommandType.Exit));
                        bContinue = false;
                    }
                }
            }

            return bContinue;
        }

        public void Run(RemoteCmdRecvDelegate remoteCmdRecvDelegate, BackgroundWorker bgWorker, int port) {
            mBgWorker = bgWorker;
            mRemoteCmdRecvDelegate = remoteCmdRecvDelegate;

            try {
                // コントロールポートの待受を開始する。
                IPAddress addr = IPAddress.Any;
                mListenSock = new TcpListener(addr, port);
                mListenSock.Start();

                UpdateListenIPandPort(port);

                mBgWorker.ReportProgress(1,
                    string.Format("PPWServer started. Waiting PPWRemote on:\n    IP address = {0}\n    Port = {1}\n", mListenIPAddress, mListenPort));

                bool bContinue = true;
                while (bContinue) {
                    bool bPending = false;
                    while (!bPending) {
                        bPending = mListenSock.Pending();
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
                    try {
                        using (var client = mListenSock.AcceptTcpClient()) {
                            using (var stream = client.GetStream()) {
                                lock (mLock) {
                                    mStream = stream;
                                }

                                bContinue = InteractWithClient(client, stream);

                                if (bContinue) {
                                    bgWorker.ReportProgress(1, string.Format("Connection closed. Waiting another client to connect.\n"));
                                }
                            }
                        }
                    } catch (IOException ex) {
                        bgWorker.ReportProgress(1, string.Format("{0}\nConnection closed. Waiting another client to connect.\n", ex));
                    }

                    lock (mLock) {
                        mStream = null;
                    }
                }
                bgWorker.ReportProgress(1, string.Format("PPWServer stopped.\n"));
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\nPPWServer stopped.\n", e);
                bgWorker.ReportProgress(1, string.Format("{0}.\n", e));
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\nPPWServer stopped.\n", e);
                bgWorker.ReportProgress(1, string.Format("{0}.\n", e));
            } catch (Exception e) {
                Console.WriteLine("Exception: {0}\nPPWServer stopped.\n", e);
                bgWorker.ReportProgress(1, string.Format("{0}.\n", e));
            } finally {
            }

            if (mListenSock != null) {
                mListenSock.Stop();
            }
            mListenSock = null;

        }
    }
}
