using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using WWDirectCompute12;
using System.Collections.Generic;
using System.Diagnostics;

namespace WWArbitraryResampler {
    public partial class MainWindow : Window {
        private Converter mConv = new Converter();
        public MainWindow() {
            InitializeComponent();
        }

        private StringBuilder mSB = new StringBuilder();
        private void AddLog(string s) {
            mSB.Append(s);
            mTextBoxLog.Text = mSB.ToString();
            mTextBoxLog.ScrollToEnd();
        }

        private void ClearLog() {
            mSB.Clear();
            mTextBoxLog.Text = "";
        }

        private List<int> mAdapterIdxList = new List<int>();

        private void UpdateAdapterList() {
            mConv.UpdateAdapterList();

            mAdapterIdxList.Clear();
            mComboBoxAdapterList.Items.Clear();
            foreach (var item in mConv.AdapterList) {
                mComboBoxAdapterList.Items.Add(
                    string.Format("{0}: VideoMem={1}MiB, SharedMem={2}MiB {3} {4}",
                    item.name,
                    item.videoMemMiB, item.sharedMemMiB,
                    item.remote ? "Remote" : "",
                    item.software ? "Software" : ""));
                mAdapterIdxList.Add(item.idx);
            }
            if (0 < mComboBoxAdapterList.Items.Count) {
                mComboBoxAdapterList.SelectedIndex = 0;
                mButtonStart.IsEnabled = true;
            } else {
                MessageBox.Show("Error: No DirectX12 GPU found!",
                    "Error: No DirectX12 GPU Found",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                mButtonStart.IsEnabled = false;
                return;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mBW = new BackgroundWorker();
            mBW.DoWork += MBW_DoWork;
            mBW.RunWorkerCompleted += MBW_RunWorkerCompleted;
            mBW.WorkerReportsProgress = true;
            mBW.ProgressChanged += MBW_ProgressChanged;

            mConv.Init();
            UpdateAdapterList();
        }

        private void Window_Closed(object sender, System.EventArgs e) {
            mConv.Term();
        }

        private void mButtonInput_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "FLAC files(*.flac)|*.flac";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxInput.Text = dlg.FileName;
        }

        private void mButtonOutput_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "FLAC files(*.flac)|*.flac";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxOutput.Text = dlg.FileName;
        }

        BackgroundWorker mBW = new BackgroundWorker();

        private void mButtonStart_Click(object sender, RoutedEventArgs e) {
            double pitchScale = 1.0;
            if (!double.TryParse(mTextBoxPitchScale.Text, out pitchScale) || pitchScale < 0.5 || 2.0 < pitchScale) {
                MessageBox.Show("Error: pitch scale value is not valid number. specify value from 0.5 to 2.0", "Error: pitch scale value", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mGroupBoxSettings.IsEnabled = false;
            mButtonStart.IsEnabled = false;
            ClearLog();

            int gpuIdx = mAdapterIdxList[mComboBoxAdapterList.SelectedIndex];

            var ca = new Converter.ConvertArgs(gpuIdx, mTextBoxInput.Text, mTextBoxOutput.Text, 1.0/pitchScale);
            mBW.RunWorkerAsync(ca);

        }

        private class WorkerResultArgs {
            public int mHr = 0;
            public WorkerResultArgs(int hr) {
                mHr = hr;
            }
        }

        private Stopwatch mSW = new Stopwatch();

        private void MBW_DoWork(object sender, DoWorkEventArgs e) {
            int hr = 0;
            e.Result = null;
            var ca = e.Argument as Converter.ConvertArgs;

            mSW.Start();

            hr = mConv.Convert(ca, (a) => {
                if (a.mCBT == Converter.EventCallbackTypes.ConvProgress && mSW.ElapsedMilliseconds < 1000) {
                    return;
                }
                mSW.Restart();
                mBW.ReportProgress(a.mProgressPercentage, a);
            });

            mSW.Stop();

            var r = new WorkerResultArgs(hr);
            e.Result = r;
        }
        private void MBW_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var a = e.UserState as Converter.EventCallbackArgs;

            mProgressBar.Value = e.ProgressPercentage;

            if (a.mCBT == Converter.EventCallbackTypes.ConvProgress) {
                return;
            }

            AddLog(string.Format("{0}.\n", a.mCBT));
        }


        private void MBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as WorkerResultArgs;
            System.Diagnostics.Debug.Assert(r != null);

            mButtonStart.IsEnabled = true;
            mGroupBoxSettings.IsEnabled = true;
            mProgressBar.Value = 0;

            AddLog(string.Format("End. Result = {0:X}\n", r.mHr));
        }

        private void mButtonUpdateAdapterList_Click(object sender, RoutedEventArgs e) {
            UpdateAdapterList();
        }
    }
}
