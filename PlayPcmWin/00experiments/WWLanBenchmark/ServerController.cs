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

namespace WWLanBenchmark {
    class ServerController {
        private const int HASH_BYTES = 16;
        private const long ONE_MEGA = 1000 * 1000;
        private const long ONE_GIGA = 1000 * 1000 * 1000;

        TcpListener mListener = null;
        private BackgroundWorker mBackgroundWorker;

        private static int ReadInt32(NetworkStream stream) {
            var data = new byte[4];
            stream.Read(data, 0, data.Count());
            using (var ms = new MemoryStream(data)) {
                using (var br = new BinaryReader(ms)) {
                    return br.ReadInt32();
                }
            }
        }

        private static void WriteInt64(NetworkStream stream, long v) {
            var data = new byte[8];
            using (var ms = new MemoryStream(data)) {
                using (var bw = new BinaryWriter(ms)) {
                    bw.Write(v);
                    bw.Flush();
                }
            }
            stream.Write(data, 0, 8);
        }

        private static void TouchMemory(Byte[] buff) {
            for (int i = 0; i < buff.Length; ++i) {
                buff[i] = 0;
            }
        }

        struct Settings {
            public int xmitConnectionCount;
            public long xmitFragmentBytes;
            public long totalBytes;
        };

        private static Settings RecvSettings(NetworkStream stream) {
            var settings = new Settings();
            settings.xmitConnectionCount = Utility.StreamReadInt32(stream);
            settings.xmitFragmentBytes = Utility.StreamReadInt64(stream);
            settings.totalBytes = Utility.StreamReadInt64(stream);
            return settings;
        }

        public void Abort() {
            if (mListener != null) {
                Console.WriteLine("ServerController.Abort()");
                mListener.Server.Close();
            }
        }

        public void Run(BackgroundWorker backgroundWorker, int controlPort, int dataPort, int recvTimeoutMillisec, string recvFolder) {
            mBackgroundWorker = backgroundWorker;
            var serverReceiver = new ServerReceiver();

            try {
                // コントロールポートの待受を開始する。
                IPAddress addr = IPAddress.Any;
                mListener = new TcpListener(addr, controlPort);
                mListener.Start();

                while (true) {
                    mBackgroundWorker.ReportProgress(1, "Waiting for a connection...\n");

                    using (var client = mListener.AcceptTcpClient()) {
                        using (var stream = client.GetStream()) {
                            mBackgroundWorker.ReportProgress(1, string.Format("Connected from {0}\n", client.Client.RemoteEndPoint));
                            // 接続してきた。設定情報を受信する。
                            Settings settings = RecvSettings(stream);

                            GC.Collect();

                            mBackgroundWorker.ReportProgress(1, string.Format("Settings: To recv {0}GB of data. TCP connection count={1}. Fragment size={2}Mbytes\n",
                                settings.totalBytes / ONE_GIGA,
                                settings.xmitConnectionCount,
                                settings.xmitFragmentBytes / ONE_MEGA));

                            // データポートの待受を開始する。
                            if (!serverReceiver.Initialize(dataPort, settings.totalBytes)) {
                                mBackgroundWorker.ReportProgress(1, "Error: failed to listen data port!\n");
                                // 失敗したので終了する。
                                mListener.Stop();
                                return;
                            }

                            // 準備OKを戻す。
                            stream.WriteByte(0);

                            byte[] recvHash = Utility.StreamReadBytes(stream, HASH_BYTES);

                            var sw = new Stopwatch();
                            sw.Start();

                            serverReceiver.Wait(recvTimeoutMillisec);
                            sw.Stop();

                            mBackgroundWorker.ReportProgress(1, string.Format("Received {0}GB in {1} seconds. ({2:0.###}Gbps)\n",
                                settings.totalBytes / ONE_GIGA, sw.ElapsedMilliseconds / 1000.0,
                                (double)settings.totalBytes * 8 / ONE_GIGA / (sw.ElapsedMilliseconds / 1000.0)));

                            sw.Reset();
                            sw.Start();
                            mBackgroundWorker.ReportProgress(1, string.Format("Checking consistency of received data... "));
                            var calcHash = serverReceiver.CalcHash();
                            sw.Stop();
                            if (calcHash.SequenceEqual(recvHash)) {
                                mBackgroundWorker.ReportProgress(1, string.Format("Success! {0} seconds\n", sw.ElapsedMilliseconds / 1000.0));
                                string path = string.Format("{0}\\{1}", recvFolder, Guid.NewGuid());
                                mBackgroundWorker.ReportProgress(1, string.Format("Saving received data as {0} ... ", path));
                                sw.Reset();
                                sw.Start();
                                serverReceiver.SaveReceivedFileAs(path);
                                sw.Stop();
                                mBackgroundWorker.ReportProgress(1, string.Format("{0} seconds\n", sw.ElapsedMilliseconds / 1000.0));
                            } else {
                                mBackgroundWorker.ReportProgress(1, string.Format("Error: MD5 hash consistency check FAILED !!\n"));
                            }

                            serverReceiver.Terminate();

                            stream.WriteByte(0);
                            mBackgroundWorker.ReportProgress(1, string.Format("Connection closed.\n\n"));

                        }
                    }
                }
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\n", e);
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\n", e);
            } finally {
                mListener.Stop();

                if (serverReceiver != null) {
                    serverReceiver.Terminate();
                }
            }
            mListener = null;

            Console.WriteLine("ServerController.Run() end");
        }
    }
}
