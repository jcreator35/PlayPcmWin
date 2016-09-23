using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WavRWLib2;
using System.IO;

namespace WavInvert
{
    public partial class Form1 : Form
    {
        private System.Resources.ResourceManager rm;
        
        public Form1()
        {
            InitializeComponent();

            rm = WavInvert.Properties.Resources.ResourceManager;
            GuiStatusUpdate();
        }

        private void GuiStatusUpdate()
        {
            if (0 < textBoxInput.Text.Length &&
                0 < textBoxOutput.Text.Length) {
                buttonStart.Enabled = true;
            } else {
                buttonStart.Enabled = false;
            }
        }

        private string OpenDialogAndAskPath(bool bReadFile)
        {
            string ret = string.Empty;

            using (OpenFileDialog ofd = new OpenFileDialog()) {
                ofd.ReadOnlyChecked = bReadFile;
                ofd.Multiselect = false;
                ofd.Filter = rm.GetString("WavFileFilter");
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

        private void buttonBrowseInput_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(true);
            if (string.Empty != path) {
                textBoxInput.Text = path;
                GuiStatusUpdate();
            }
        }

        private void buttonBrowseOutput_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(false);
            if (string.Empty != path) {
                textBoxOutput.Text = path;
                GuiStatusUpdate();
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            buttonStart.Enabled = false;
            textBoxConsole.Text += rm.GetString("Started") + "\r\n";
            backgroundWorker1.RunWorkerAsync();
        }

        private WavData ReadWavFile(string path)
        {
            WavData wavData = new WavData();

            Console.WriteLine(rm.GetString("ReadWavFileStarted"), path);

            using (BinaryReader br1 = new BinaryReader(File.Open(path, FileMode.Open))) {
                if (!wavData.Read(br1)) {
                    return null;
                }
                if (16 != wavData.BitsPerSample) {
                    return null;
                }
            }

            return wavData;
        }

        private bool WriteWavFile(WavData wavData, string path)
        {
            Console.WriteLine(rm.GetString("WriteWavFileStarted"), path);

            bool rv = true;
            try {
                using (BinaryWriter bw1 = new BinaryWriter(File.Open(path, FileMode.Create))) {
                    wavData.Write(bw1);
                }
            } catch (System.Exception ex) {
                Console.WriteLine(ex.ToString());
                rv = false;
            }
            return rv;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            WavData wavData = ReadWavFile(textBoxInput.Text);
            if (null == wavData) {
                e.Result = string.Format(rm.GetString("ReadWavFileFailed"), textBoxInput.Text) + "\r\n";
                return;
            }

            Effect(wavData);

            if (!WriteWavFile(wavData, textBoxOutput.Text)) {
                e.Result = string.Format(rm.GetString("WriteWavFileFailed"), textBoxOutput.Text) + "\r\n";
                return;
            }
            e.Result = string.Format(rm.GetString("WriteWavFileSucceeded"), textBoxOutput.Text) + "\r\n";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            textBoxConsole.Text += (string)e.Result + "\n";
            buttonStart.Enabled = true;
        }

        private void Effect(WavData wavData)
        {
            if (checkBoxInvert.Checked) {
                for (int ch=0; ch < wavData.NumChannels; ++ch) {
                    for (int pos=0; pos < wavData.NumSamples; ++pos) {
                        short val = wavData.Sample16Get(ch, pos);

                        // Invert! we want to do this effect
                        val = (short)-val;

                        wavData.Sample16Set(ch, pos, val);
                    }
                }
            }
        }

    }
}
