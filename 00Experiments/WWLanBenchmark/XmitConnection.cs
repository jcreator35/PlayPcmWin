using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WWLanBenchmark {
    class XmitConnection {
        public Thread thread;
        public TcpClient client;
        public NetworkStream stream;
        public BinaryWriter bw;
        public bool bTerminate;
        public int id;

        public XmitTask task;

        // このスレッドはタスクが割り当てられるまでWaitする。外のスレッドはSetしてこのスレッドを起こす。
        public AutoResetEvent taskAssigned;

        // このスレッドはタスクが完了したらSetする。外のスレッドはタスク完了をWaitで待つ。
        public AutoResetEvent taskCompleted;

        class ThreadArgs {
            public string server;
            public int port;
            public ThreadArgs(string server, int port) {
                this.server = server;
                this.port = port;
            }
        }

        public void Initialize(string server, int port, int id) {
            this.id = id;
            bTerminate = false;
            taskAssigned = new AutoResetEvent(false);
            taskCompleted = new AutoResetEvent(false);
            thread = new Thread(new ParameterizedThreadStart(XmitThreadEntry));
            thread.Start(new ThreadArgs(server, port));
        }

        public void AssignTask(XmitTask task) {
            this.task = task;
            taskAssigned.Set();
        }

        public void Terminate() {
            if (bw != null) {
                bw.Close();
                bw.Dispose();
                bw = null;
            }

            if (stream != null) {
                stream.Close();
                stream.Dispose();
                stream = null;
            }

            if (client != null) {
                client.Close();
                client = null;
            }

            if (thread != null) {
                System.Diagnostics.Debug.Assert(taskAssigned != null);
                bTerminate = true;
                taskAssigned.Set();
                thread.Join();
                thread = null;
                taskAssigned.Dispose();
                taskAssigned = null;
                taskCompleted.Dispose();
                taskCompleted = null;
            }
        }

        private void XmitThreadEntry(object param) {
            var args = param as ThreadArgs;
            client = new TcpClient(args.server, args.port);
            stream = client.GetStream();
            bw = new BinaryWriter(stream);

            while (!bTerminate) {
                taskAssigned.WaitOne();
                if (bTerminate) {
                    break;
                }

                Xmit();

                if (bTerminate) {
                    break;
                }
                taskCompleted.Set();
            }
        }

        private void Xmit() {
            bw.Write(task.startPos);
            bw.Write(task.sizeBytes);
            bw.Write(task.xmitData);
            bw.Flush();

#if false
            stream.ReadByte();
#endif
        }
    };
}
