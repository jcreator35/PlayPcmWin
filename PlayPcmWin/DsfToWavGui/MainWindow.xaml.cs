using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.ComponentModel;

namespace DsfToWavGui {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void InputFormUpdated() {

        }

        private void buttonBrowseIn_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "DSF or DFF files|*.DSF;*.DFF";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInPath.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void buttonBrowseOut_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.Filter = "WAV files|*.WAV";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutPath.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void textBoxInPath_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void textBoxInPath_Drop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder("Dropped data is not file.");

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }
            textBoxInPath.Text = paths[0];
            InputFormUpdated();
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private BackgroundWorker mBw = new BackgroundWorker();

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            string msg = string.Format(CultureInfo.CurrentCulture, "DsfToWavGui version {0}", AssemblyVersion);
            Title = msg;

            textBoxLog.Text = msg + "\n";

            mBw.DoWork += new DoWorkEventHandler(mBw_DoWork);
            mBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBw_RunWorkerCompleted);
        }

        private void AddLog(string msg) {
            textBoxLog.AppendText(msg);
            textBoxLog.ScrollToEnd();
        }

        class WorkerArgs {
            public string inPath;
            public string outPath;

            public WorkerArgs(string aIn, string aOut) {
                inPath = aIn;
                outPath = aOut;
            }
        };

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            buttonStart.IsEnabled = false;

            AddLog(string.Format(CultureInfo.CurrentCulture, "Started: input={0}, output={1}\n",
                textBoxInPath.Text, textBoxOutPath.Text));
            WorkerArgs wa = new WorkerArgs(textBoxInPath.Text, textBoxOutPath.Text);
            mBw.RunWorkerAsync(wa);
        }

        void mBw_DoWork(object sender, DoWorkEventArgs e) {
            var wa = e.Argument as WorkerArgs;

            string msg = "";

            bool result = false;
            try {
                result = DsfToWav.Program.RunDsfToWav(wa.inPath, wa.outPath);
            } catch (Exception ex) {
                msg = ex.ToString();
            }

            if (!result) {
                msg = "Failed to convert!\n" + msg + "\n";
            } else {
                msg = "Succeeded.\n";
            }

            e.Result = msg;
        }

        void mBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            AddLog(e.Result as string);
            buttonStart.IsEnabled = true;
        }
    }
}
