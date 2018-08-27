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
        BackgroundWorker [] mBW = new BackgroundWorker[8*8];

        const int SLEEP_MS = 100;

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        private ulong mPattern = 0;
        
        public void Start() {
            mPattern = 0;

            int numProcessors = Environment.ProcessorCount;

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
            for (int y = 0; y < 8; ++y) {
                for (int x = 0; x < 8; ++x) {
                    int pos = x + y * 8;
                    if (mGrayImage8x8[pos] < 128) {
                        mPattern |= 1UL << pos;
                    }
                }
            }
        }

        public void Stop() {
            for (int i = 0; i < 64; ++i) {
                mBW[i].CancelAsync();
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
            var args = (WorkerArgs)e.Argument;

            try {
                Thread.BeginThreadAffinity();

                ulong affinity = 1UL << args.threadIdx;
                CurrentThread.ProcessorAffinity = (IntPtr)affinity;

                while (!e.Cancel) {
                    if (0 != (mPattern & (1UL << args.threadIdx))) {
                        HeavyLifting(args.threadIdx);
                    } else {
                        Thread.Sleep(SLEEP_MS);
                    }
                }

            } finally {
                Thread.EndThreadAffinity();
            }
        }

    }
}
