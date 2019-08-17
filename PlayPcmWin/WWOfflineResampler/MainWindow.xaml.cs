using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using WWMath;
using WWIIRFilterDesign;
using System.Collections.Generic;

namespace WWOfflineResampler {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private enum State {
            NotReady,
            Ready,
            ReadFile,
            FilterDesigned,
            Converting,
            WriteFile
        }

        private State mState = State.NotReady;
        private bool mInitialized = false;
        private BackgroundWorker mBw = new BackgroundWorker();

        struct TargetSampleRateProperty {
            public int sampleRate;
            public bool isPcm;
            public TargetSampleRateProperty(int sr, bool pcm) {
                sampleRate = sr;
                isPcm = pcm;
            }
        };


        private static TargetSampleRateProperty[] mTargetSampleRateList = {
            new TargetSampleRateProperty( 32000, true ),
            new TargetSampleRateProperty( 44100, true ),
            new TargetSampleRateProperty( 48000, true ),
            new TargetSampleRateProperty( 64000, true ),
            new TargetSampleRateProperty( 88200, true ),

            new TargetSampleRateProperty( 96000, true ),
            new TargetSampleRateProperty( 128000, true ),
            new TargetSampleRateProperty( 176400, true ),
            new TargetSampleRateProperty( 192000, true ),
            new TargetSampleRateProperty( 352800, true ),

            new TargetSampleRateProperty( 384000, true ),
            new TargetSampleRateProperty( 2822400, false ),
            new TargetSampleRateProperty( 5644800, false ),
            new TargetSampleRateProperty( 11289600, false ),
            new TargetSampleRateProperty( 22579200, false ),
            new TargetSampleRateProperty( 45158400, false ),
            new TargetSampleRateProperty( 90316800, false ),
        };
        
        Main mMain = new Main();

        public MainWindow() {
            InitializeComponent();

            //var args = Environment.GetCommandLineArgs();

            mPoleZeroPlotZ.Mode = WWUserControls.PoleZeroPlot.ModeType.ZPlane;
            mPoleZeroPlotZ.Update();

            Title = string.Format(CultureInfo.CurrentCulture, "WWOfflineResampler version {0}", AssemblyVersion);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mBw.DoWork += new DoWorkEventHandler(mBw_DoWork);
            mBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBw_RunWorkerCompleted);
            mBw.WorkerReportsProgress = true;
            mBw.ProgressChanged += new ProgressChangedEventHandler(mBw_ProgressChanged);
            
            mInitialized = true;

            mState = State.Ready;
            Update();

#if false
            //サンプルレートの比の計算のテスト。
            foreach (int a in mTargetSampleRateList) {
                foreach (int b in mTargetSampleRateList) {
                    long lcm = WWMath.Functions.LCM(a, b);
                    Console.WriteLine("{0}, {1}, {2},{3}", a, b, lcm/a, lcm/b);
                }
            }
#endif
        }

        private void buttonBrowseInputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterReadAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var targetSR = mTargetSampleRateList[comboBoxTargetSampleRate.SelectedIndex];

            var dlg = new Microsoft.Win32.SaveFileDialog();

            if (targetSR.isPcm) {
                dlg.Filter = Properties.Resources.FilterWriteFlacAudioFiles;
            } else {
                dlg.Filter = Properties.Resources.FilterWriteDsfAudioFiles;
            }
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void InputFormUpdated() {
            if (0 < textBoxInputFile.Text.Length &&
                    0 < textBoxOutputFile.Text.Length) {
                mState = State.Ready;
            } else {
                mState = State.NotReady;
            }

            Update();
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            switch (mState) {
            case State.NotReady:
                buttonStartConversion.IsEnabled = false;
                break;
            case State.Ready:
                buttonStartConversion.IsEnabled = true;
                break;
            case State.ReadFile:
            case State.FilterDesigned:
            case State.Converting:
            case State.WriteFile:
                buttonStartConversion.IsEnabled = false;
                break;
            }
        }

        void mBw_DoWork(object sender, DoWorkEventArgs e) {
            var param = e.Argument as Main.BWStartParams;
            var result = mMain.DoWork(param,
                (int percent, Main.BWProgressParam p) => { mBw.ReportProgress(percent, p); });
            e.Result = result;
        }

        void mBw_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var param = e.UserState as Main.BWProgressParam;

            textBoxLog.Text += param.message;
            textBoxLog.ScrollToEnd();
            switch (param.state) {
            case Main.State.Started:
            case Main.State.ReadFile:
                mState = State.ReadFile;
                break;
            case Main.State.FilterDesigned:
                mState = State.FilterDesigned;
                break;
            case Main.State.Converting:
                mState = State.Converting;
                break;
            case Main.State.WriteFile:
                mState = State.WriteFile;
                break;
            case Main.State.Finished:
                mState = State.Ready;
                break;
            }
            progressBar1.Value = e.ProgressPercentage;

            if (mState == State.FilterDesigned) {
                // 設計されたフィルターを表示する。
                mTimeDomainPlot.ImpulseResponseFunction = mMain.Afd().ImpulseResponseFunction;
                mTimeDomainPlot.StepResponseFunction = mMain.Afd().UnitStepResponseFunction;
                mTimeDomainPlot.CutoffFreq = mMain.IIRFilterDesign().CutoffFreq;
                mTimeDomainPlot.Update();

                mPoleZeroPlotZ.ClearPoleZero();
                mPoleZeroPlotZ.Mode = WWUserControls.PoleZeroPlot.ModeType.ZPlane;
                mPoleZeroPlotZ.TransferFunction = mMain.IIRFilterDesign().TransferFunction();

                for (int i = 0; i < mMain.IIRFilterDesign().NumOfPoles(); ++i) {
                    var p = mMain.IIRFilterDesign().PoleNth(i);
                    mPoleZeroPlotZ.AddPole(p.Reciplocal());
                }
                for (int i=0; i<mMain.IIRFilterDesign().NumOfZeroes(); ++i) {
                    var p = mMain.IIRFilterDesign().ZeroNth(i);
                    mPoleZeroPlotZ.AddZero(p.Reciplocal());
                }

                mPoleZeroPlotZ.Update();

                mFrequencyResponseZ.Mode = WWUserControls.FrequencyResponse.ModeType.ZPlane;
                mFrequencyResponseZ.SamplingFrequency = mMain.IIRFilterDesign().SamplingFrequency;
                mFrequencyResponseZ.TransferFunction = mMain.IIRFilterDesign().TransferFunction();
                mFrequencyResponseZ.Update();
            }

            Update();
        }

        void mBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var param = e.Result as Main.BWCompletedParam;
            mState = State.Ready;

            mStopwatch.Stop();
            textBoxLog.Text += param.message;
            textBoxLog.Text += string.Format("Finished. elapsed time: {0} sec\n", (mStopwatch.ElapsedMilliseconds/100) * 0.1);
            textBoxLog.ScrollToEnd();

            Update();
            progressBar1.Value = 0;
        }

        Stopwatch mStopwatch = new Stopwatch();

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            var targetSF = mTargetSampleRateList[comboBoxTargetSampleRate.SelectedIndex];

            IIRFilterDesign.Method method = IIRFilterDesign.Method.ImpulseInvarianceMixedPhase;
            switch (comboBoxResamplingMethod.SelectedIndex) {
            case 0:
                method = IIRFilterDesign.Method.ImpulseInvarianceMixedPhase;
                break;
            case 1:
                method = IIRFilterDesign.Method.ImpulseInvarianceMinimumPhase;
                break;
            case 2:
                method = IIRFilterDesign.Method.Bilinear;
                break;
            }

            mState = State.ReadFile;
            Update();

            progressBar1.Value = Main.START_PERCENT;

            mStopwatch.Reset();
            mStopwatch.Start();

            mBw.RunWorkerAsync(new Main.BWStartParams(textBoxInputFile.Text, targetSF.sampleRate, targetSF.isPcm, textBoxOutputFile.Text, method));
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder(Properties.Resources.DroppedDataIsNotFile);

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }
            textBoxInputFile.Text = paths[0];
            InputFormUpdated();
        }

        private void textBox_PreviewDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
        }

        private void TextBoxOutputFile_Drop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder(Properties.Resources.DroppedDataIsNotFile);

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }
            textBoxOutputFile.Text = paths[0];
            UpdateOutputFileExt();
            InputFormUpdated();
        }

        private void UpdateOutputFileExt() {
            string s = textBoxOutputFile.Text;
            string ext = System.IO.Path.GetExtension(s);
            s = s.Substring(0, s.Length - ext.Length);

            var targetSF = mTargetSampleRateList[comboBoxTargetSampleRate.SelectedIndex];
            if (targetSF.isPcm) {
                ext = ".flac";
            } else {
                ext = ".dsf";
            }

            s = s + ext;
            textBoxOutputFile.Text = s;
        }

        private void comboBoxTargetSampleRate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            UpdateOutputFileExt();
        }
    }
}
