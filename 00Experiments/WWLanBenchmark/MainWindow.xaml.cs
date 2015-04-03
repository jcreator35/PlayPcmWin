using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace WWLanBenchmark {
    public partial class MainWindow : Window {
        private const int CONTROL_PORT = 9880;
        private const int DATA_PORT    = 9881;

        private const long ONE_MEGA = 1000 * 1000;
        private const long ONE_GIGA = 1000 * 1000 * 1000;

        private bool mWindowLoaded = false;
        private BackgroundWorker mBackgroundWorker;
        private ServerController mServerController;
        private ClientController mClientController;


        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mWindowLoaded = true;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e) {
            if (true == radioButtonClient.IsChecked) {
                StartClient();
                return;
            }

            if (true == radioButtonServer.IsChecked) {  
                StartServer();
                return;
            }
        }

        struct ClientArgs {
            public string sendFilePath;
            public string serverIP;
            public int xmitConnectionCount;
            public int xmitFragmentMB;
        };

        private void StartClient() {
            var args = new ClientArgs();
            args.serverIP = textBoxServerIP.Text;

            if (!Int32.TryParse(textBoxXmitConnectionCount.Text, out args.xmitConnectionCount)) {
                MessageBox.Show("Parse error of Xmit connection count");
                return;
            }
            if (args.xmitConnectionCount < 1) {
                MessageBox.Show("Xmit connection count must be integer value greater than 0");
                return;
            }

            if (!Int32.TryParse(textBoxXmitFragmentMB.Text, out args.xmitFragmentMB)) {
                MessageBox.Show("Parse error of Xmit fragment size (MB)");
                return;
            }
            if (args.xmitFragmentMB < 1) {
                MessageBox.Show("Xmit fragment size (MB) must be integer value greater than 0");
                return;
            }
            if (1000 < args.xmitFragmentMB) {
                MessageBox.Show("Xmit fragment size (MB) must be integer value smaller than 1000");
                return;
            }

            args.sendFilePath = textBoxSendFile.Text;

            mBackgroundWorker = new BackgroundWorker();
            mBackgroundWorker.DoWork += Client_DoWork;
            mBackgroundWorker.WorkerReportsProgress = true;
            mBackgroundWorker.ProgressChanged += Client_ProgressChanged;
            mBackgroundWorker.RunWorkerCompleted += Client_RunWorkerCompleted;

            buttonStart.IsEnabled = false;

            mBackgroundWorker.RunWorkerAsync(args);
        }

        struct ServerArgs {
            public string recvFolder;
            public int timeoutSec;
        };

        private void StartServer() {
            var args = new ServerArgs();
            if (!Int32.TryParse(textBoxRecvTimeoutSec.Text, out args.timeoutSec)) {
                MessageBox.Show("Parse error of Recv timeout");
                return;
            }
            if (args.timeoutSec < 1) {
                MessageBox.Show("Recv timeout must be integer value greater than 0");
                return;
            }
            if (1000 * 100 < args.timeoutSec) {
                MessageBox.Show("Recv timeout (sec) must be smaller than 100000");
                return;
            }

            args.recvFolder = textBoxRecvFolder.Text;

            mBackgroundWorker = new BackgroundWorker();
            mBackgroundWorker.DoWork += Server_DoWork;
            mBackgroundWorker.WorkerReportsProgress = true;
            mBackgroundWorker.ProgressChanged += Server_ProgressChanged;
            mBackgroundWorker.RunWorkerCompleted += Server_RunWorkerCompleted;

            buttonStart.IsEnabled = false;

            mBackgroundWorker.RunWorkerAsync(args);
        }

        private void Client_DoWork(object sender, DoWorkEventArgs e) {
            var args = (ClientArgs)e.Argument;

            mClientController = new ClientController();
            mClientController.Run(mBackgroundWorker, args.sendFilePath, args.serverIP, CONTROL_PORT, DATA_PORT,
                args.xmitConnectionCount, (int)(args.xmitFragmentMB * ONE_MEGA));
            mClientController = null;
        }


        private void Server_DoWork(object sender, DoWorkEventArgs e) {
            var args = (ServerArgs)e.Argument;

            mServerController = new ServerController();
            mServerController.Run(mBackgroundWorker, CONTROL_PORT, DATA_PORT, args.timeoutSec * 1000, args.recvFolder);
            mServerController = null;
            Console.WriteLine("Server_DoWork() end");
        }

        private void Client_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            buttonStart.IsEnabled = true;
            textBoxLog.AppendText("\n");
        }

        private void Server_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            buttonStart.IsEnabled = true;
            textBoxLog.AppendText("\n");
        }

        private void Client_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            string s = e.UserState as string;
            textBoxLog.AppendText(s);
            textBoxLog.ScrollToEnd();
        }

        private void Server_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            string s = e.UserState as string;
            textBoxLog.AppendText(s);
            textBoxLog.ScrollToEnd();
        }

        private void radioButtonClient_Checked(object sender, RoutedEventArgs e) {
            if (!mWindowLoaded) {
                return;
            }

            groupBoxClientSettings.IsEnabled = true;
            groupBoxServerSettings.IsEnabled = false;
        }

        private void radioButtonServer_Checked(object sender, RoutedEventArgs e) {
            if (!mWindowLoaded) {
                return;
            }

            groupBoxClientSettings.IsEnabled = false;
            groupBoxServerSettings.IsEnabled = true;
        }

        private void Window_Closed(object sender, EventArgs e) {
            if (mServerController != null) {
                mServerController.Abort();
            }
        }

        private void buttonBrowseServerRecvFolderBrowse_Click(object sender, RoutedEventArgs e) {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                textBoxRecvFolder.Text = dlg.SelectedPath;
            }
        }

        private void buttonClientSendFileBrowse_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            var result = dlg.ShowDialog();
            if (result == true) {
                textBoxSendFile.Text = dlg.FileName;
            }
        }
    }
}
