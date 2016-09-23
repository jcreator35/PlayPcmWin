using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace WWLanBenchmark {
    class ClientXmitter {
        List<XmitTask> mXmitTaskList;
        private byte[] mXmitDataHash;

        public byte[] XmitDataHash() {
            return mXmitDataHash;
        }

        private List<XmitConnection> mConnectionList = new List<XmitConnection>();

        public void CloseConnections() {
            foreach (var xc in mConnectionList) {
                xc.Terminate();
            }
            mConnectionList.Clear();
        }

        public void EstablishConnections(string server, int xmitPort, int xmitConnectionCount) {
            for (int i = 0; i < xmitConnectionCount; ++i) {
                var xc = new XmitConnection();
                xc.Initialize(server, xmitPort, i);
                mConnectionList.Add(xc);
            }
        }

        private static long FileSizeBytes(string path) {
            return new System.IO.FileInfo(path).Length;
        }

        /// <summary>
        /// ファイルをメモリに読み込んで送信の準備をする。
        /// </summary>
        /// <param name="xmitFragmentBytes">各スレッドの送出バイト数</param>
        /// <returns>送出するファイルのバイト数</returns>
        public long SetupXmitTasks(string path, int xmitFragmentBytes) {
            mXmitTaskList = new List<XmitTask>();

            long pos = 0;
            using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))) {
                do {
                    var buff = br.ReadBytes(xmitFragmentBytes);
                    if (buff.Length == 0) {
                        break;
                    }

                    var xt = new XmitTask(pos, buff.Length, buff);
                    mXmitTaskList.Add(xt);
                    pos += buff.Length;
                } while (true);
            }

            return pos;
        }

        public void CalcHash() {
            using (var hash = new MD5CryptoServiceProvider()) {
                foreach (var xt in mXmitTaskList) {
                    hash.TransformBlock(xt.xmitData, 0, xt.xmitData.Length, xt.xmitData, 0);
                }
                hash.TransformFinalBlock(new byte[0], 0, 0);
                mXmitDataHash = hash.Hash;
            }
        }

        public bool Xmit() {
            bool result = true;

            var taskCompletedEventList = new AutoResetEvent[mConnectionList.Count];
            for (int i = 0; i < mConnectionList.Count; ++i) {
                taskCompletedEventList[i] = mConnectionList[i].taskCompleted;
            }

            for (int i = 0; i < mXmitTaskList.Count; ++i) {
                if (i < mConnectionList.Count) {
                    mConnectionList[i].AssignTask(mXmitTaskList[i]);
                } else {
                    int availableConnectionIdx = WaitHandle.WaitAny(taskCompletedEventList);
                    mConnectionList[availableConnectionIdx].AssignTask(mXmitTaskList[i]);
                }
            }
            WaitHandle.WaitAll(taskCompletedEventList);

            for (int i = 0; i < mXmitTaskList.Count; ++i) {
                if (!mXmitTaskList[i].result) {
                    result = false;
                }
                mXmitTaskList[i].End();
            }
            mXmitTaskList.Clear();
            taskCompletedEventList = null;
            return result;
        }

    }
}
