using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace WWArbitraryResampler {
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private Converter mConv = new Converter();
        private StringBuilder mSB = new StringBuilder();

        public MainWindow() {
            InitializeComponent();

            Title = string.Format("WWArbitraryResampler version {0}", AssemblyVersion);
        }

        private void AddLog(string s) {
            mSB.Append(s);
            mTextBoxLog.Text = mSB.ToString();
            mTextBoxLog.ScrollToEnd();
        }

        private void ClearLog() {
            mSB.Clear();
            mTextBoxLog.Text = "Ready.\n";
        }

        private List<int> mAdapterIdxList = new List<int>();

        private void UpdateAdapterList() {
            mConv.UpdateAdapterList();

            mAdapterIdxList.Clear();
            mComboBoxAdapterList.Items.Clear();
            foreach (var item in mConv.AdapterList) {
                mComboBoxAdapterList.Items.Add(
                    string.Format("gpuId={0}: {1}, VideoMem={2}MiB, SharedMem={3}MiB {4} {5}",
                    item.gpuId,
                    item.name,
                    item.videoMemMiB, item.sharedMemMiB,
                    item.remote ? "Remote" : "",
                    item.software ? "Software" : ""));
                mAdapterIdxList.Add(item.gpuId);
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
            ClearLog();

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

        private Stopwatch mSW = new Stopwatch();

        private void mButtonStart_Click(object sender, RoutedEventArgs e) {
            double pitchScale = 1.0;
            if (!double.TryParse(mTextBoxPitchScale.Text, out pitchScale) || pitchScale < 0.5 || 2.0 < pitchScale) {
                MessageBox.Show("Error: pitch scale value is not valid number. specify value from 0.5 to 2.0", "Error: pitch scale value", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mGroupBoxSettings.IsEnabled = false;
            mButtonStart.IsEnabled = false;
            ClearLog();

            mSW.Start();

            int gpuId = mAdapterIdxList[mComboBoxAdapterList.SelectedIndex];

            var ca = new Converter.ConvertArgs(gpuId, mTextBoxInput.Text, mTextBoxOutput.Text, 1.0/pitchScale);
            mBW.RunWorkerAsync(ca);

        }

        private class WorkerResultArgs {
            public int mHr = 0;
            public WorkerResultArgs(int hr) {
                mHr = hr;
            }
        }

        private Stopwatch mReportSW = new Stopwatch();

        private void MBW_DoWork(object sender, DoWorkEventArgs e) {
            int hr = 0;
            e.Result = null;
            var ca = e.Argument as Converter.ConvertArgs;

            mReportSW.Start();

            hr = mConv.Convert(ca, (a) => {
                if (a.mCBT == Converter.EventCallbackTypes.ConvProgress && mReportSW.ElapsedMilliseconds < 1000) {
                    return;
                }
                mReportSW.Restart();
                mBW.ReportProgress(a.mProgressPercentage, a);
            });

            mReportSW.Stop();

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


            AddLog(string.Format("End. Result = {0:X} Elapsed Time={1}\n", r.mHr, mSW.Elapsed));
            mSW.Stop();
        }

        private void mButtonUpdateAdapterList_Click(object sender, RoutedEventArgs e) {
            UpdateAdapterList();
        }
    }
}
