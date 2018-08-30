using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WWTaskManagerText {
    class ThreadLoadBitmap {
        const int W = 8;
        const int H = 8;
        const int SLEEP_MS = 100;

        BackgroundWorker[] mBW = new BackgroundWorker[W * H];

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        private ulong mPattern = 0;

        private readonly int[] mTaskManagerLogicalProcessorIdxToProcessThreadId = new int[] {
             0,  1,  2,  3,  4,  5,  6,  7,
             8,  9, 10, 11, 12, 13, 14, 15,
            32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
            48, 49, 50, 51, 52, 53, 54, 55,
            56, 57, 58, 59, 60, 61, 62, 63,
        };
        
        public void Start() {
            mPattern = 0;

            int numProcessors = Environment.ProcessorCount;
            if (W*H < numProcessors) {
                numProcessors = W * H;
            }

            for (int i = 0; i < numProcessors; ++i) {
                mBW[i] = new BackgroundWorker();
                mBW[i].DoWork += new DoWorkEventHandler(BwDoWork);
                mBW[i].WorkerSupportsCancellation = true;
                mBW[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(BwRunWorkerCompleted);
            }

            for (int i = 0; i < numProcessors; ++i) {
                mBW[i].RunWorkerAsync(new WorkerArgs(i));
            }
        }

        public void UpdatePattern(byte[] mGrayImage8x8) {
            mPattern = 0;
            for (int y = 0; y < H; ++y) {
                for (int x = 0; x < W; ++x) {
                    int pos = x + y * W;
                    if (mGrayImage8x8[pos] < 128) {
                        mPattern |= 1UL << pos;
                    }
                }
            }
        }

        public void Stop() {
            for (int i = 0; i < W*H; ++i) {
                if (mBW[i] != null) {
                    mBW[i].CancelAsync();
                    mBW[i] = null;
                }
            }
        }

        private void HeavyLifting(int tid) {
            var sw = new Stopwatch();
            sw.Start();

            uint [] v = new uint[3];
            v[1] = 1;
            v[2] = 1;

            while (sw.ElapsedMilliseconds < SLEEP_MS) {
                for (int i = 0; i < 0x7ffff; ++i) {
                    v[0] = v[1];
                    v[1] = v[2];
                    v[2] = v[1] + v[0];
                }
            }

            //Console.WriteLine("{0} {1} {2}", tid, v[2], sw.Elapsed);
            sw.Stop();
        }

        void BwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            // nothing to do.
        }

        private ProcessThread CurrentThread {
            get {
                int id = GetCurrentThreadId();
                return
                    (from ProcessThread th in Process.GetCurrentProcess().Threads
                     where th.Id == id
                     select th).Single();
            }
        }

        class WorkerArgs {
            public WorkerArgs(int tid) { threadIdx = tid; }
            public int threadIdx;
        };

        void BwDoWork(object sender, DoWorkEventArgs e) {
            var bw = sender as BackgroundWorker; 
            var args = e.Argument as WorkerArgs;

            try {
                Thread.BeginThreadAffinity();

                ulong affinity = 1UL << args.threadIdx;
                CurrentThread.ProcessorAffinity = (IntPtr)affinity;

                while (!bw.CancellationPending) {
                    int taskManagerBitmapIdx = mTaskManagerLogicalProcessorIdxToProcessThreadId[args.threadIdx];
                    if (0 != (mPattern & (1UL << taskManagerBitmapIdx))) {
                        HeavyLifting(args.threadIdx);
                    } else {
                        Thread.Sleep(SLEEP_MS);
                    }
                }

                e.Cancel = true;

                //Console.WriteLine("{0} Canceled", args.threadIdx);
            } finally {
                Thread.EndThreadAffinity();
            }
        }

    }
}
