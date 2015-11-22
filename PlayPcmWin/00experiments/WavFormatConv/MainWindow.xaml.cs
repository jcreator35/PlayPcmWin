using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WavFormatConv {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private bool mLoaded = false;
        private PcmDataLib.PcmData mPcm = null;

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            listBoxChunkLayout.Items.Clear();
            listBoxChunkLayout.Items.Add(GenListBoxItem(new RiffChunkParams(0, 0)));
            listBoxChunkLayout.Items.Add(GenListBoxItem(new FmtChunkParams(FmtChunkParams.WaveFormatStructType.WaveFormat)));
            listBoxChunkLayout.Items.Add(GenListBoxItem(new DataChunkParams(0)));

            listBoxChunkLayout.SelectedIndex = 1;

            mLoaded = true;
            UpdateUIStatus();

            textBoxLog.AppendText(string.Format("WavFormatConv version {0}", AssemblyVersion));

            LocalizeUI();
        }

        private void LocalizeUI() {
            buttonBrowseInputFile.Content = Properties.Resources.ButtonBrowse;
            buttonBrowseOutputFile.Content = Properties.Resources.ButtonBrowseWriteFile;
            buttonChunkAdd.Content = Properties.Resources.ButtonChunkAdd;
            buttonChunkDelete.Content = Properties.Resources.ButtonChunkDelete;
            buttonChunkMoveDown.Content = Properties.Resources.ButtonChunkMoveDown;
            buttonChunkMoveUp.Content = Properties.Resources.ButtonChunkMoveUp;
            buttonStartConversion.Content = Properties.Resources.ButtonStart;
            checkBoxDataChunkSizeLonger.Content = Properties.Resources.CheckBoxDataLonger;
            checkBoxRiffChunkAddFooter.Content = Properties.Resources.CheckBoxRiffAddGarbage;
            checkBoxRiffChunkSizeLonger.Content = Properties.Resources.CheckBoxRiffLonger;
            groupBoxChunkLayout.Header = Properties.Resources.GroupBoxChunkLayout;
            groupBoxData.Header = Properties.Resources.GroupBoxData;
            groupBoxFmt.Header = Properties.Resources.GroupBoxFmt;
            groupBoxReadFile.Header = Properties.Resources.GroupBoxReadFile;
            groupBoxRiff.Header = Properties.Resources.GroupBoxRiff;
            groupBoxSettings.Header = Properties.Resources.GroupBoxSettings;
            groupBoxWriteFile.Header = Properties.Resources.GroupBoxWriteFile;
        }

        private void UpdateUIStatus() {
            if (!mLoaded) {
                return;
            }

            textBoxRiffChunkSizeLongerBytes.IsEnabled = (checkBoxRiffChunkSizeLonger.IsChecked == true);
            textBoxRiffTrailingZeroesBytes.IsEnabled = (checkBoxRiffChunkAddFooter.IsChecked == true);
            listBoxWaveFormatExCbSize.IsEnabled = (radioButtonFmt18.IsChecked == true);
            textBoxDataChunkSizeAppendBytes.IsEnabled = (checkBoxDataChunkSizeLonger.IsChecked == true);

            {
                var lbi = listBoxChunkLayout.SelectedItem as ListBoxItem;
                var wcp = lbi.Tag as WavChunkParams;
                buttonChunkDelete.IsEnabled = !wcp.IsMandatoryChunk();
            }

            buttonChunkMoveUp.IsEnabled = (2 <= listBoxChunkLayout.SelectedIndex);
            buttonChunkMoveDown.IsEnabled = (1 <= listBoxChunkLayout.SelectedIndex && listBoxChunkLayout.SelectedIndex + 1 < listBoxChunkLayout.Items.Count);

            UpdateRiffParams();
            UpdateFmtParams();
            UpdateDataParams();

            foreach (var item in listBoxChunkLayout.Items) {
                var lbi = item as ListBoxItem;
                var wcp = lbi.Tag as WavChunkParams;

                wcp.UpdateText();
                lbi.Content = wcp.Text;
            }
        }

        private void buttonBrowseInputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAV files|*.wav";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
        }

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "WAV files|*.wav";

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
        }

        private bool UpdateRiffParams() {
            int longerBytes = 0;
            int garbageBytes = 0;
            if (checkBoxRiffChunkSizeLonger.IsChecked == true) {
                if (!Int32.TryParse(textBoxRiffChunkSizeLongerBytes.Text, out longerBytes)) {
                    MessageBox.Show("Error: “RIFF chunk size longer than actual” text must be integer number");
                    return false;
                }
            }

            if (checkBoxRiffChunkAddFooter.IsChecked == true) {
                if (!Int32.TryParse(textBoxRiffTrailingZeroesBytes.Text, out garbageBytes)) {
                    MessageBox.Show("Error: “RIFF chunk add garbage” text must be integer number");
                    return false;
                }
            }

            var r = new RiffChunkParams(longerBytes, garbageBytes);
            ReplaceSingleInstanceChunk(r);
            return true;
        }

        private bool UpdateFmtParams() {
            var format = FmtChunkParams.WaveFormatStructType.WaveFormat;
            int cbSize = 0;

            if (radioButtonFmt18.IsChecked == true) {
                format = FmtChunkParams.WaveFormatStructType.WaveFormatEx;
                if (listBoxWaveFormatExCbSize.SelectedIndex == 1) {
                    cbSize = 0x1234;
                }
            }

            if (radioButtonFmt40.IsChecked == true) {
                format = FmtChunkParams.WaveFormatStructType.WaveFormatExtensible;
                cbSize = 22;
            }

            var fmt = new FmtChunkParams(format);
            fmt.CbSize = cbSize;
            ReplaceSingleInstanceChunk(fmt);
            return true;
        }

        private bool LoadWavFile(string path) {
            using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                mPcm = WavRWHelper.ReadWav(br);
            }

            if (mPcm == null) {
                MessageBox.Show("Error: Read WAV file failed {0}", path);
                return false;
            }

            return true;
        }

        private bool SaveWavFile(string path) {
            var wavParamList = new List<WavChunkParams>();
            foreach (var i in listBoxChunkLayout.Items) {
                var lbi = i as ListBoxItem;
                var wcp = lbi.Tag as WavChunkParams;
                wavParamList.Add(wcp);
            }

            using (var bw = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
                if (!WavRWHelper.WriteWav(bw, mPcm, wavParamList)) {
                    return false;
                }
            }
            return true;
        }

        private bool UpdateDataParams() {
            int longerBytes = 0;
            if (checkBoxDataChunkSizeLonger.IsChecked == true) {
                if (!Int32.TryParse(textBoxDataChunkSizeAppendBytes.Text, out longerBytes)) {
                    MessageBox.Show("Error: “DATA Chunk longer than actual size” must be integer number");
                    return false;
                }
            }

            var data = new DataChunkParams(longerBytes);
            ReplaceSingleInstanceChunk(data);
            return true;
        }

        private void buttonStartConversion_Click(object sender, RoutedEventArgs e) {
            if (!UpdateRiffParams()) {
                return;
            }
            if (!UpdateFmtParams()) {
                return;
            }
            if (!UpdateDataParams()) {
                return;
            }

            if (0 == textBoxInputFile.Text.CompareTo(textBoxOutputFile.Text)) {
                MessageBox.Show(Properties.Resources.ErrorInputAndOutputIsTheSame);
                textBoxLog.AppendText(string.Format("\r\n{0}", Properties.Resources.ErrorInputAndOutputIsTheSame));
                textBoxLog.ScrollToEnd();
                return;
            }

            if (!LoadWavFile(textBoxInputFile.Text)) {
                textBoxLog.AppendText(string.Format("\r\nRead Failed: {0}", textBoxInputFile.Text));
                textBoxLog.ScrollToEnd();
                return;
            }
            textBoxLog.AppendText(string.Format("\r\n" + Properties.Resources.LogReadSucceeded, textBoxInputFile.Text)); 

            if (!SaveWavFile(textBoxOutputFile.Text)) {
                textBoxLog.AppendText(string.Format("\r\nWrite failed: {0}", textBoxOutputFile.Text));
                textBoxLog.ScrollToEnd();
                return;
            }
            textBoxLog.AppendText(string.Format("\r\n" + Properties.Resources.LogWriteSucceeded, textBoxOutputFile.Text));
            textBoxLog.ScrollToEnd();
        }

        private ListBoxItem GenListBoxItem(WavChunkParams p) {
            var item = new ListBoxItem();
            item.Content = p.Text;
            item.Tag = p;
            return item;
        }

        private bool ReplaceSingleInstanceChunk(WavChunkParams p) {
            foreach (var i in listBoxChunkLayout.Items) {
                var lbi = i as ListBoxItem;
                var wcp = lbi.Tag as WavChunkParams;
                if (wcp.ChunkType == p.ChunkType) {
                    // found
                    lbi.Content = p.Text;
                    lbi.Tag = p;
                    return true;
                }
            }

            // not found
            return false;
        }

        private void buttonChunkAdd_Click(object sender, RoutedEventArgs e)
        {
            var d = new AddChunkDialog();
            d.ShowDialog();
            if (d.DialogResult != true)
            {
                return;
            }

            if (d.WavChunk.IsSingleInstanceChunk()) {
                // Replace
                bool replaced = ReplaceSingleInstanceChunk(d.WavChunk);
                if (!replaced) {
                    listBoxChunkLayout.Items.Insert(listBoxChunkLayout.SelectedIndex+1,GenListBoxItem(d.WavChunk));
                    ++listBoxChunkLayout.SelectedIndex;
                }
            } else {
                // add
                listBoxChunkLayout.Items.Insert(listBoxChunkLayout.SelectedIndex + 1, GenListBoxItem(d.WavChunk));
                ++listBoxChunkLayout.SelectedIndex;
            }

            UpdateUIStatus();
        }

        private void checkBoxDataChunkSizeLonger_Click(object sender, RoutedEventArgs e) {
            UpdateUIStatus();
        }

        private void checkBoxRiffChunkAddFooter_Click(object sender, RoutedEventArgs e) {
            UpdateUIStatus();
        }

        private void checkBoxRiffChunkSizeLonger_Click(object sender, RoutedEventArgs e) {
            UpdateUIStatus();
        }

        private void radioButtonFmt18_Unchecked(object sender, RoutedEventArgs e) {
            UpdateUIStatus();
        }

        private void radioButtonFmt18_Checked(object sender, RoutedEventArgs e) {
            UpdateUIStatus();
        }

        private void buttonChunkMoveUp_Click(object sender, RoutedEventArgs e) {
            int idx = listBoxChunkLayout.SelectedIndex;
            var item = listBoxChunkLayout.SelectedItem;

            {   // SelectionChangedを抑制する
                mLoaded = false;

                listBoxChunkLayout.Items.RemoveAt(idx);
                listBoxChunkLayout.Items.Insert(idx - 1, item);
                listBoxChunkLayout.SelectedIndex = idx - 1;

                mLoaded = true;
            }

            UpdateUIStatus();
        }

        private void buttonChunkMoveDown_Click(object sender, RoutedEventArgs e) {
            int idx = listBoxChunkLayout.SelectedIndex;
            var item = listBoxChunkLayout.SelectedItem;

            {   // SelectionChangedを抑制する
                mLoaded = false;

                listBoxChunkLayout.Items.RemoveAt(idx);
                listBoxChunkLayout.Items.Insert(idx + 1, item);
                listBoxChunkLayout.SelectedIndex = idx + 1;

                mLoaded = true;
            }

            UpdateUIStatus();
        }

        private void buttonChunkDelete_Click(object sender, RoutedEventArgs e) {
            int idx = listBoxChunkLayout.SelectedIndex;

            {   // SelectionChangedを抑制する
                mLoaded = false;

                listBoxChunkLayout.Items.RemoveAt(idx);
                if (listBoxChunkLayout.Items.Count <= idx) {
                    --idx;
                }
                listBoxChunkLayout.SelectedIndex = idx;

                mLoaded = true;
            }

            UpdateUIStatus();
        }

        private void listBoxChunkLayout_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateUIStatus();
        }

        private void textBoxRiffChunkSizeLongerBytes_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateUIStatus();
        }

        private void textBoxRiffTrailingZeroesBytes_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateUIStatus();
        }

        private void textBoxDataChunkSizeAppendBytes_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateUIStatus();
        }

        private void listBoxWaveFormatExCbSize_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateUIStatus();
        }

        private void radioButtonFmt16_Checked(object sender, RoutedEventArgs e) {
            UpdateUIStatus();
        }

    }
}
