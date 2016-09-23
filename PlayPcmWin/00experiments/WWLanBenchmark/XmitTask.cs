using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WWLanBenchmark {
    class XmitTask : IDisposable {
        public long startPos;
        public int sizeBytes;
        public ManualResetEvent doneEvent;
        public bool result;

        public byte[] xmitData;

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                doneEvent.Close();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public XmitTask(long startPos, int sizeBytes, byte [] xmitData) {
            this.startPos = startPos;
            this.sizeBytes = sizeBytes;
            this.xmitData = xmitData;
            doneEvent = new ManualResetEvent(false);
            result = true;
        }

        public void Done() {
            doneEvent.Set();
        }

        /// <summary>
        /// このインスタンスの使用を終了する。再利用はできない。
        /// </summary>
        public void End() {
            doneEvent = null;
            result = true;
        }
    };


}
