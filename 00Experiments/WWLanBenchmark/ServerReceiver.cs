using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WWLanBenchmark {
    class ServerReceiver {
        private const int HASH_BYTES = 16;
        private const long ONE_MEGA = 1000 * 1000;
        private const long ONE_GIGA = 1000 * 1000 * 1000;

        private int mDataPort;
        private long mTotalBytes;
        private Thread mListenThread;

        struct RecvThreadInfo {
            public Thread thread;
            public TcpClient client;

            public RecvThreadInfo(Thread thread, TcpClient client) {
                this.thread = thread;
                this.client = client;
            }
        };

        private List<RecvThreadInfo> mRecvThreadList;
        private ManualResetEventSlim mListenStarted;
        private ManualResetEventSlim mRecvCompleted;

        private List<RecvFragment> mRecvFragmentList;
        private TcpListener mServer = null;

        private const int THREAD_START_WAIT_MILLISECONDS = 2000;

        public bool Initialize(int dataPort, long totalBytes) {
            mDataPort = dataPort;
            mTotalBytes = totalBytes;
            mListenStarted = new ManualResetEventSlim(false);
            mRecvCompleted = new ManualResetEventSlim(false);
            mRecvFragmentList = new List<RecvFragment>();
            mRecvThreadList = new List<RecvThreadInfo>();

            mListenThread = new Thread(new ThreadStart(ListenThreadEntry));
            mListenThread.Start();
            return mListenStarted.Wait(THREAD_START_WAIT_MILLISECONDS);
        }

        public void Terminate() {
            Console.WriteLine("ServerReceiver.Terminate()");
            if (mListenStarted != null) {
                mListenStarted.Dispose();
                mListenStarted = null;
            }
            if (mRecvCompleted != null) {
                mRecvCompleted.Dispose();
                mRecvCompleted = null;
            }

            mRecvFragmentList = null;

            if (mRecvThreadList!= null) {
                lock (mRecvThreadList) {
                    foreach (var rti in mRecvThreadList) {
                        rti.client.Close();
                    }
                }
            }

            if (mServer != null) {
                mServer.Server.Close();
                mListenThread.Join();
                mListenThread = null;
            }
            Console.WriteLine("ServerReceiver.Terminate() end");
        }

        private void ListenThreadEntry() {
            try {
                // データポートの待受を開始する。
                var addr = IPAddress.Any;
                mServer = new TcpListener(addr, mDataPort);
                mServer.Start();

                mListenStarted.Set();

                while (true) {
                    var client = mServer.AcceptTcpClient();
                    Thread t = new Thread(new ParameterizedThreadStart(RecvThreadEntry));
                    t.Start(client);
                    mRecvThreadList.Add(new RecvThreadInfo(t, client));
                }
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\n", e);
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\n", e);
            } finally {
                mServer.Stop();
            }
            mServer = null;

            Console.WriteLine("ListenThread() end.");
        }

        private void RecvThreadEntry(object param) {
            var client = param as TcpClient;
            try {
                using (var stream = client.GetStream()) {
                    while (true) {
                        mRecvFragmentList.Add(Recv(stream));

                        CheckIfRecvCompleted();

#if false
                        // OKを戻す。
                        stream.WriteByte(0);
#endif
                    }
                }
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}\n", e);
            } catch (IOException e) {
                Console.WriteLine("IOException: {0}\n", e);
            }

            client.Close();

            lock (mRecvThreadList) {
                mRecvThreadList.RemoveAll(item => item.client == client);
            }
            Console.WriteLine("RecvThread() end.");
        }

        private static RecvFragment Recv(NetworkStream stream) {
            var startPos  = Utility.StreamReadInt64(stream);
            var sizeBytes = Utility.StreamReadInt32(stream);
            var buff = Utility.StreamReadBytes(stream, sizeBytes);
            return new RecvFragment(startPos, sizeBytes, buff);
        }

        private void CheckIfRecvCompleted() {
            long recvBytes = 0;
            foreach (var f in mRecvFragmentList) {
                recvBytes += f.SizeBytes;
            }

            if (recvBytes == mTotalBytes) {
                mRecvCompleted.Set();
            }
        }

        public bool Wait(int recvTimeoutMillisec) {
            return mRecvCompleted.Wait(recvTimeoutMillisec);
        }

        public byte[] CalcHash() {
            mRecvFragmentList = mRecvFragmentList.OrderBy(o => o.StartPos).ToList();

            using (var hash = new MD5CryptoServiceProvider()) {
                foreach (var f in mRecvFragmentList) {
                    hash.TransformBlock(f.Content, 0, f.Content.Length, f.Content, 0);
                }
                hash.TransformFinalBlock(new byte[0], 0, 0);

                byte[] result = new byte[HASH_BYTES];
                Array.Copy(hash.Hash, result, HASH_BYTES);
                return result;
            }
        }

        public void SaveReceivedFileAs(string path) {
            using (var bw = new BinaryWriter(File.Open(path, FileMode.CreateNew, FileAccess.Write))) {
                foreach (var f in mRecvFragmentList) {
                    bw.Write(f.mContent);
                }
            }
        }

    }
}
