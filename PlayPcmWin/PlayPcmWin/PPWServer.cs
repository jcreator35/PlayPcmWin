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
        private const int IDLE_SLEEP_MS = 100;
        private const int RECV_TOO_LARGE = 10000;
        public const int VERSION = 100;

        public const int PROGRESS_STARTED = 0;
        public const int PROGRESS_REPORT = 10;

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
                Console.WriteLine("D: PPWServer RemoteCommand EXIT received\n");
                bContinue = false;
            }

            //Console.WriteLine("PPWServer RecvRequests {0} {1}", DateTime.Now.Second, cmd.cmd);

            mRemoteCmdRecvDelegate(cmd);

            return bContinue;
        }

        private void Send(BinaryWriter bw, RemoteCommand cmd) {
            Utility.StreamWriteBytes(bw, cmd.GenerateMessage());

            // Hope this flushes buffers!
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

        private void SendQueuedMsg(BinaryWriter bw) {
            int sendCmdCount = 0;
            lock (mLock) {
                sendCmdCount = mCmdToSend.Count;
            }

            while (0 < sendCmdCount) {
                RemoteCommand rc = null;
                lock (mLock) {
                    rc = mCmdToSend[0];
                }

                //Console.WriteLine("PPWServer SendQueuedMsg() {0} Sending {1}", DateTime.Now.Second, rc.cmd);
                Send(bw, rc);

                lock (mLock) {
                    mCmdToSend.RemoveAt(0);
                    sendCmdCount = mCmdToSend.Count;
                }
            }
        }

        /// <returns>true: listen and accept next client, false: abort and end!</returns>
        private bool InteractWithClient(TcpClient client, NetworkStream stream) {
            //Console.WriteLine("InteractWithClient() started {0}", DateTime.Now.Second);
            stream.ReadTimeout = STREAM_READ_TIMEOUT;
            var br = new BinaryReader(stream);

            {
                var greetResult = ReturnCode.Timeout;

                // 設定情報を受信する。

                greetResult = RecvGreetings(br);

                if (greetResult != ReturnCode.OK) {
                    // 失敗したので接続を切る。
                    mBgWorker.ReportProgress(PROGRESS_REPORT, string.Format("Connected from {0}\nPPWServer Error: {1}. Connection closed.\nWaiting PPWRemote on: IP address = {2}, Port = {3}\n",
                        client.Client.RemoteEndPoint, greetResult, mListenIPAddress, mListenPort));
                    return true;
                }
            }
            //Console.WriteLine("InteractWithClient() Recv Greetings {0}", DateTime.Now.Second);

            mBgWorker.ReportProgress(PROGRESS_REPORT, string.Format("Connected from {0}\n", client.Client.RemoteEndPoint));

            bool result = true;
            using (var bw = new BinaryWriter(stream)) {
                while (true) {
                    // たまったコマンドを全て送出する。
                    SendQueuedMsg(bw);
                    stream.Flush();

                    if (stream.DataAvailable) {
                        // 受信データがある。
                        var bExit = !RecvRequests(br);
                        if (bExit) {
                            // EXITを受信。
                            // 切断し、次の接続を待ち受ける。
                            result = true;
                            break;
                        }
                    } else {
                        Thread.Sleep(IDLE_SLEEP_MS);
                    }

                    if (mBgWorker.CancellationPending) {
                        // 終了コマンドを送出。
                        Send(bw, new RemoteCommand(RemoteCommandType.Exit));
                        stream.Flush();
                        // リッスンソケットを閉じてサーバーを終了する。
                        result = false;
                        break;
                    }
                }
            }

            return result;
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

                mBgWorker.ReportProgress(PROGRESS_STARTED,
                    string.Format("PPWServer started. Waiting PPWRemote on:\n    IP address = {0}\n    Port = {1}\n", mListenIPAddress, mListenPort));

                bool bContinue = true;
                while (bContinue) {
                    bool bPending = false;
                    while (!bPending) {
                        bPending = mListenSock.Pending();
                        if (!bPending) {
                            Thread.Sleep(IDLE_SLEEP_MS);
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
                                    bgWorker.ReportProgress(PROGRESS_REPORT, string.Format("Connection closed. Waiting another client to connect on IP address = {0} Port = {1}\n", mListenIPAddress, mListenPort));
                                }
                            }
                        }
                    } catch (IOException ex) {
                        bgWorker.ReportProgress(PROGRESS_REPORT, string.Format("{0}\nConnection closed. Waiting another client to connect on IP address = {1} Port = {2}\n", ex, mListenIPAddress, mListenPort));
                    }

                    lock (mLock) {
                        mStream = null;
                    }
                }
                bgWorker.ReportProgress(PROGRESS_REPORT, string.Format("PPWServer stopped.\n"));
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\nPPWServer stopped.\n", e);
                bgWorker.ReportProgress(PROGRESS_REPORT, string.Format("{0}.\n", e));
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\nPPWServer stopped.\n", e);
                bgWorker.ReportProgress(PROGRESS_REPORT, string.Format("{0}.\n", e));
            } catch (Exception e) {
                Console.WriteLine("Exception: {0}\nPPWServer stopped.\n", e);
                bgWorker.ReportProgress(10, string.Format("{0}.\n", e));
            } finally {
            }

            if (mListenSock != null) {
                mListenSock.Stop();
            }
            mListenSock = null;

        }
    }
}
