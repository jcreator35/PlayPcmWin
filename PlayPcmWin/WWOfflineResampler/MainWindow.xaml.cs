using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using WWMath;
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

        private static int [] mTargetSampleRateList = {
            32000,
            44100,
            48000,
            64000,
            88200,
            96000,
            128000,
            176400,
            192000,
            352800,
            384000,
        };
        
        Main mMain = new Main();

        public MainWindow() {
            InitializeComponent();

            if (mMain.ParseCommandLine()) {
                Application.Current.Shutdown();
                return;
            }

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

            JenkinsTraubRpoly.Test();
            PolynomialRootFinding.Test();
            WWPolynomial.Test();
            NewtonsMethod.Test();
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
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWriteAudioFiles;
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
                mTimeDomainPlot.TimeScale = mMain.Afd().TimeDomainFunctionTimeScale;
                mTimeDomainPlot.Update();

                mPoleZeroPlotZ.Mode = WWUserControls.PoleZeroPlot.ModeType.ZPlane;
                mPoleZeroPlotZ.TransferFunction = mMain.IIRiim().TransferFunction;

                for (int i = 0; i < mMain.IIRiim().HzCount(); ++i) {
                    var p = mMain.IIRiim().Hz(i);

                    if (p.DenomDegree() == 1) {
                        // ポールの位置。
                        mPoleZeroPlotZ.AddPole(WWComplex.Minus(WWComplex.Div(p.D(1), p.D(0))));
                    }
                }

                {
                    // 零の位置を計算する。
                    // 合体したH(z)の分子の実係数多項式の根が零の位置。
                    HighOrderComplexRationalPolynomial HzCombined = mMain.IIRiim().HzCombined();
                    var coeffs = new List<double>();
                    for (int i = 0; i < HzCombined.DenomDegree(); ++i) {
                        coeffs.Add(HzCombined.D(i).real);
                    }
                    if ((coeffs.Count & 1) == 0) {
                        // 奇数次(係数の数が偶数)のときx倍して偶数次にする。
                        coeffs.Insert(0, 0.0);
                    }

                    var rf = new PolynomialRootFinding();
                    var roots = rf.FindRoots(coeffs.ToArray());
                    foreach (var r in roots) {
                        mPoleZeroPlotZ.AddZero(WWComplex.Reciprocal(r));
                    }
                }

                mPoleZeroPlotZ.Update();

                mFrequencyResponseZ.Mode = WWUserControls.FrequencyResponse.ModeType.ZPlane;
                mFrequencyResponseZ.SamplingFrequency = mMain.IIRiim().SamplingFrequency();
                mFrequencyResponseZ.TransferFunction = mMain.IIRiim().TransferFunction;
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
            int targetSampleRate = mTargetSampleRateList[comboBoxTargetSampleRate.SelectedIndex];

            mState = State.ReadFile;
            Update();

            progressBar1.Value = Main.START_PERCENT;

            mStopwatch.Reset();
            mStopwatch.Start();

            mBw.RunWorkerAsync(new Main.BWStartParams(textBoxInputFile.Text, targetSampleRate, textBoxOutputFile.Text));
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
    }
}
