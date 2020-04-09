using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System;

namespace ReadAllFilesOnSpecifiedFolder {
    public class ReadAllFilesOnFolder : IDisposable {
        public enum EventType {
            CollectionFinished,
            ReadProgressed,
            ReadError,
            ReadFinished,
        };

        public delegate void ProgressCallback(EventType ev, string path, string errMsg, int finished, int total);

        private ProgressCallback mCb;

        public CancellationTokenSource mCts = new CancellationTokenSource();

        public ReadAllFilesOnFolder(ProgressCallback cb) {
            mCb = cb;
        }

        public enum Option {
            Parallel = 1,
        };

        /// <summary>
        /// Run()をキャンセルする。
        /// </summary>
        public void Cancel() {
            if (mCts.Token.CanBeCanceled) {
                mCts.Cancel();
            }
        }

        public void Run(string root, int opt) {
            if (mCts != null) {
                mCts.Dispose();
                mCts = null;
            }
            mCts = new CancellationTokenSource();

            // まずフォルダ内のファイルを列挙します。
            var pathList = WWUtil.DirectoryUtil.CollectFilesOnFolder(root, null);

            if (mCts.IsCancellationRequested) {
                return;
            }

            if (mCb != null) {
                mCb(EventType.CollectionFinished, "", "", 0, pathList.Length);
            }

            int progressCounter = 0;

            try {
                if (0 != (opt & (int)Option.Parallel)) {
                    // Parallel実行する。
                    var po = new ParallelOptions();
                    po.CancellationToken = mCts.Token;
                    po.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
                    Parallel.For(0, pathList.Length, po, idx => {
                        if (po.CancellationToken.IsCancellationRequested) {
                            po.CancellationToken.ThrowIfCancellationRequested();
                        }
                        //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                        string path = pathList[idx];

                        string msg = ReadOneFile(path);

                        int cnt = Interlocked.Increment(ref progressCounter);
                        if (mCb != null) {
                            mCb(msg.Length == 0 ? EventType.ReadProgressed : EventType.ReadError,
                                    path, msg, cnt, pathList.Length);
                        }
                    });
                } else {
                    // 直列実行する。
                    //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                    foreach (string path in pathList) {
                        string msg = ReadOneFile(path);

                        ++progressCounter;
                        if (mCb != null) {
                            mCb(msg.Length == 0 ? EventType.ReadProgressed : EventType.ReadError,
                                    path, msg, progressCounter, pathList.Length);
                        }

                        if (mCts.IsCancellationRequested) {
                            return;
                        }
                    }
                }
            } catch (OperationCanceledException ex) {
                return;
            }

            if (mCb != null) {
                mCb(EventType.ReadFinished, "", "", progressCounter, pathList.Length);
            }
        }

        private string ReadOneFile(string path) {
            var buf = new byte[1024 * 1024];

            try {
                using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))) {
                    long totalBytes = br.BaseStream.Length;

                    // bufのサイズ単位で読む。
                    for (long pos = 0; pos < totalBytes; pos += buf.Length) {
                        int readBytes = buf.Length;
                        if (totalBytes < pos + readBytes) {
                            readBytes = (int)(totalBytes - pos);
                        }
                        br.Read(buf, 0, readBytes);
                    }
                }
            } catch (System.IO.IOException ex) {
                return ex.ToString();
            } catch (System.Exception ex) {
                return ex.ToString();
            }

            return "";
        }

        public void Dispose() {
            if (mCts != null) {
                mCts.Dispose();
                mCts = null;
            }
        }
    }
}
