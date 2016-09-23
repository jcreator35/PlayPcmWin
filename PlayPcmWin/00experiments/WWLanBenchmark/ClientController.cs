using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace WWLanBenchmark {
    class ClientController {
        private const long ONE_GIGA = 1000 * 1000 * 1000;
        private const long ONE_MEGA = 1000 * 1000;

        private BackgroundWorker mBackgroundWorker;
        private ClientXmitter mClientXmitter = new ClientXmitter();

        public void Run(BackgroundWorker backgroundWorker, string path, string server, int controlPort, int dataPort,
                int xmitConnectionCount, int xmitFragmentBytes) {
            mBackgroundWorker = backgroundWorker;

            GC.Collect();

            try {
                using (var client = new TcpClient(server, controlPort)) {
                    using (var stream = client.GetStream()) {
                        using (var bw = new BinaryWriter(stream)) {
                            mBackgroundWorker.ReportProgress(1, "Connected to Server.\n");

                            // XmitTaskのリストを準備。
                            mBackgroundWorker.ReportProgress(1, string.Format("Reading {0} onto memory... ", path));
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            var totalBytes = mClientXmitter.SetupXmitTasks(path, xmitFragmentBytes);
                            sw.Stop();
                            mBackgroundWorker.ReportProgress(1, string.Format("{0} seconds\n", sw.ElapsedMilliseconds / 1000.0));

                            mBackgroundWorker.ReportProgress(1, "Calculating MD5 hash... ");
                            sw.Reset();
                            sw.Start();
                            mClientXmitter.CalcHash();
                            sw.Stop();
                            mBackgroundWorker.ReportProgress(1, string.Format("{0} seconds\n", sw.ElapsedMilliseconds / 1000.0));

                            // 設定情報を送出。
                            if (!SendSettings(stream, bw, xmitConnectionCount, xmitFragmentBytes, totalBytes)) {
                                mBackgroundWorker.ReportProgress(100, "Error: Unexpected server response. Exit.");
                                return;
                            }

                            // 2. 送出データを準備し、Xmit用TCP接続をxmitConnectionCount個確立する。
                            // Xmit用TCP接続を確立
                            mClientXmitter.EstablishConnections(server, dataPort, xmitConnectionCount);

                            mBackgroundWorker.ReportProgress(1, string.Format("Data connection established. sending {0}MB stream...\n",
                                totalBytes / ONE_MEGA));

                            // 3. xmitConnectionCount個のTCP接続を使用して送出。
                            Xmit(stream, bw);

                            mClientXmitter.CloseConnections();
                        }
                    }
                }
            } catch (ArgumentNullException e) {
                mBackgroundWorker.ReportProgress(0, string.Format("Error: ArgumentNullException: {0}\n", e));
            } catch (SocketException e) {
                mBackgroundWorker.ReportProgress(0, string.Format("Error: SocketException: {0}\n", e));
            }
            mBackgroundWorker.ReportProgress(100, "Done.\n");
        }

        private static bool SendSettings(NetworkStream stream, BinaryWriter bw, int xmitConnectionCount, long xmitFragmentBytes, long totalBytes) {
            bw.Write(xmitConnectionCount);
            bw.Write(xmitFragmentBytes);
            bw.Write(totalBytes);
            bw.Flush();

            // サーバーからのACKを待つ。
            int ack = stream.ReadByte();
            if (ack != 0) {
                return false;
            }
            return true;
        }

        private void Xmit(NetworkStream stream, BinaryWriter bw) {
            bw.Write(mClientXmitter.XmitDataHash());
            bw.Flush();

            bool result = mClientXmitter.Xmit();
            if (!result) {
                mBackgroundWorker.ReportProgress(1, "Error: ClientXmitter.Xmit failed!\n");
            }

            mBackgroundWorker.ReportProgress(100, "Waiting server...\n");
            stream.ReadByte();
        }

    }
}
