using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using WavDiff;

namespace Wav2Gnuplot
{
    public partial class Form1 : Form
    {
        private System.Resources.ResourceManager rm;

        public Form1()
        {
            InitializeComponent();
            rm = Properties.Resources.ResourceManager;
            textBoxConsole.Text = rm.GetString("Introduction") + "\r\n";
            Console.WriteLine(rm.GetString("ConsoleIntroduction"));
            GuiStatusUpdate();
        }

        private void GuiStatusUpdate()
        {
            if (string.Empty != textBoxReadWavFile.Text &&
                string.Empty != textBoxWriteFile.Text) {
                buttonStart.Enabled = true;
            } else {
                buttonStart.Enabled = false;
            }
            if (!checkBoxCh0.Checked && !checkBoxCh1.Checked) {
                buttonStart.Enabled = false;
            }
        }

        private string OpenDialogAndAskPath(bool bReadFile, string filter)
        {
            string ret = string.Empty;

            using (OpenFileDialog ofd = new OpenFileDialog()) {
                ofd.ReadOnlyChecked = bReadFile;
                ofd.Multiselect = false;
                ofd.Filter = filter;
                ofd.CheckPathExists = bReadFile;
                ofd.CheckFileExists = bReadFile;
                ofd.AutoUpgradeEnabled = true;
                DialogResult dr = ofd.ShowDialog();
                if (DialogResult.OK == dr) {
                    ret = ofd.FileName;
                }
            }

            return ret;
        }

        private string ReadFilePathToWriteFilePath(string path)
        {
            return path + ".dat";
        }

        private void buttonBrowseReadWavFile_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(true, rm.GetString("WavFileFilter"));
            if (string.Empty != path) {
                textBoxReadWavFile.Text = path;
                textBoxWriteFile.Text = ReadFilePathToWriteFilePath(textBoxReadWavFile.Text);
                GuiStatusUpdate();
                textBoxConsole.Text += string.Format(rm.GetString("FileSpecified"), path, textBoxWriteFile.Text) + "\r\n";
            }
        }

        private void buttonBrowseWriteFile_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(false, rm.GetString("GnuplotFileFilter"));
            if (string.Empty != path) {
                textBoxWriteFile.Text = path;
                GuiStatusUpdate();
                textBoxConsole.Text += string.Format(rm.GetString("FileSpecified"), 1, path, textBoxWriteFile.Text) + "\r\n";
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            textBoxConsole.Clear();

            textBoxConsole.Text += string.Format(rm.GetString("ProcessStarted"),
                textBoxReadWavFile.Text, textBoxWriteFile.Text) + "\r\n";
            backgroundWorker1.RunWorkerAsync();
            buttonStart.Enabled = false;
        }

        string resultString = string.Empty;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int startPos = (int)numericStartPos.Value;
            int outputSamples = (int)numericOutputSamples.Value;
            string path = textBoxReadWavFile.Text;


            WavData wavData = new WavData();
            Console.WriteLine(rm.GetString("ReadFileStarted"), path);
            using (BinaryReader br1 = new BinaryReader(File.Open(path, FileMode.Open))) {
                if (!wavData.Read(br1)) {
                    resultString = string.Format(rm.GetString("ReadFileFailFormat"), path) + "\r\n";
                    e.Result = false;
                    return;
                }
                if (wavData.NumSamples < startPos + outputSamples) {
                    resultString = string.Format(rm.GetString("WavFileTooShort"), startPos + outputSamples, wavData.NumSamples, path) + "\r\n";
                    e.Result = false;
                    return;
                }
            }

            Console.WriteLine(rm.GetString("WavFileSummary"), wavData.NumSamples);
            resultString += string.Format(rm.GetString("WavFileSummary"), wavData.NumSamples);
            double offset = (double)numericUpDownSubSampleOffset.Value;

            if (checkBoxCh0.Checked && !checkBoxCh1.Checked) {
                using (StreamWriter sw = new StreamWriter(textBoxWriteFile.Text)) {
                    for (int i=startPos; i < startPos + outputSamples; ++i) {
                        sw.WriteLine("{0} {1}", i+offset, wavData.Sample16Get(0, i));
                    }
                }
            } else if (!checkBoxCh0.Checked && checkBoxCh1.Checked) {
                using (StreamWriter sw = new StreamWriter(textBoxWriteFile.Text)) {
                    for (int i=startPos; i < startPos + outputSamples; ++i) {
                        sw.WriteLine("{0} {1}", i + offset, wavData.Sample16Get(1, i));
                    }
                }
            } else if (checkBoxCh0.Checked && checkBoxCh1.Checked) {
                using (StreamWriter sw = new StreamWriter(textBoxWriteFile.Text)) {
                    for (int i=startPos; i < startPos + outputSamples; ++i) {
                        sw.WriteLine("{0} {1} {2}", i + offset, wavData.Sample16Get(0, i), wavData.Sample16Get(1, i));
                    }
                }
            }
            e.Result = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (true == (bool)e.Result) {
                textBoxConsole.Text += string.Format(rm.GetString("WriteSucceeded"), textBoxWriteFile.Text) + "\r\n";
            } else {
                textBoxConsole.Text += resultString + "\r\n";
                textBoxConsole.Text += string.Format(rm.GetString("WriteFailed"), textBoxWriteFile.Text) + "\r\n";
            }
        }

        private void checkBoxCh0_CheckedChanged(object sender, EventArgs e)
        {
            GuiStatusUpdate();
        }

        private void checkBoxCh1_CheckedChanged(object sender, EventArgs e)
        {
            GuiStatusUpdate();
        }
    }
}
