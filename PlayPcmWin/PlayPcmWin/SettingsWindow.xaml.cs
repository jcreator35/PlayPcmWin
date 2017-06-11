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
using System.Windows.Shapes;
using WasapiPcmUtil;
using System.Globalization;
using PcmDataLib;
using Wasapi;

namespace PlayPcmWin {
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public sealed partial class SettingsWindow : Window {
        enum SettingsBitFormatType {
            Sint16,
            Sint24,

            Sint32V24,
            Sint32,
            Sfloat32,
            AutoSelect,
        };

        private bool mWindowLoaded = false;
        private Preference m_preference = null;
        private long mPlaylistAlternateBackgroundArgb;


        public SettingsWindow() {
            InitializeComponent();

            Title = Properties.Resources.SettingsWindowTitle;
            
            groupBoxCuesheetSettings.Header = Properties.Resources.SettingsGroupBoxCuesheetSettings;
            groupBoxDeviceBufferFlush.Header = Properties.Resources.SettingsGroupBoxDeviceBufferFlush;
            groupBoxDisplaySettings.Header = Properties.Resources.SettingsGroupBoxDisplaySettings;
            groupBoxWasapiExclusive.Header = Properties.Resources.SettingsGroupBoxWasapiExclusive;
            groupBoxPlaybackThread.Header = Properties.Resources.SettingsGroupBoxPlaybackThread;
            groupBoxFileSettings.Header = Properties.Resources.SettingsGroupBoxFile;
            labelQuantizationBitrate.Content = Properties.Resources.SettingsLabelQuantizationBitrate;

            labelNoiseShaping.Content = Properties.Resources.SettingsLabelNoiseShaping;

            // 順番をPcmDataLib.NoiseShapingTypeと合わせる
            comboBoxNoiseShaping.Items.Add(Properties.Resources.SettingsNoNoiseShaping);
            comboBoxNoiseShaping.Items.Add(Properties.Resources.SettingsPerformDither);
            comboBoxNoiseShaping.Items.Add(Properties.Resources.SettingsPerformNoiseShaping);
            comboBoxNoiseShaping.Items.Add(Properties.Resources.SettingsPerformDitheredNoiseShaping);

            groupBoxTimerResolution.Header = Properties.Resources.SettingsGroupBoxTimerResolution;
            groupBoxRenderThreadTaskType.Header = Properties.Resources.SettingsGroupBoxRenderThreadTaskType;
            groupBoxWasapiShared.Header = Properties.Resources.SettingsGroupBoxWasapiShared;

            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint16);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint24);

            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint32V24);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint32);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSfloat32);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsAutoSelect);

            cbItemTaskAudio.Content = Properties.Resources.SettingsRadioButtonTaskAudio;

            cbItemTaskNone.Content = Properties.Resources.SettingsRadioButtonTaskNone;
            cbItemTaskPlayback.Content = Properties.Resources.SettingsRadioButtonTaskPlayback;
            cbItemTaskProAudio.Content = Properties.Resources.SettingsRadioButtonTaskProAudio;

            checkBoxAlternateBackground.Content = Properties.Resources.SettingsCheckBoxAlternateBackground;

            checkBoxSootheLimiterApo.Content = Properties.Resources.SettingsSootheLimiterApo;

            checkBoxCoverart.Content = Properties.Resources.SettingsCheckBoxCoverart;
            checkBoxManuallySetMainWindowDimension.Content = Properties.Resources.SettingsCheckBoxManuallySetMainWindowDimension;
            checkBoxReduceVolume.Content = Properties.Resources.SettingsCheckBoxReduceVolume;
            checkBoxPlaceKokomadeAfterIndex00.Content = Properties.Resources.SettingsCheckBoxPlaceKokomadeAterIndex00;

            checkBoxPlayingTimeBold.Content = Properties.Resources.SettingsCheckBoxPlayingTimeBold;
            checkBoxStorePlaylistContent.Content = Properties.Resources.SettingsCheckBoxStorePlaylistContent;
            cbItemTimerResolutionDefault.Content = Properties.Resources.SettingsTimerResolutionDefault;
            cbItemTimerResolution1Millisec.Content = Properties.Resources.SettingsTimerResolution1Millisec;

            labelConversionQuality.Content = Properties.Resources.SettingsLabelConversionQuality;
            labelCueEncoding.Content = Properties.Resources.SettingsCueEncoding;
            labelFontPoints.Content = Properties.Resources.SettingsLabelFontPoints;
            labelPlayingTimeFont.Content = Properties.Resources.SettingsLabelPlayingTimeFont;
            labelZeroFlushSeconds.Content = Properties.Resources.SettingsLabelZeroFlushSeconds;
            labelZeroFlushUnit.Content = Properties.Resources.SettingsLabelZeroFlushUnit;

            buttonCancel.Content = Properties.Resources.SettingsButtonCancel;
            buttonChangeColor.Content = Properties.Resources.SettingsButtonChangeColor;
            buttonOK.Content = Properties.Resources.SettingsButtonOK;
            buttonReset.Content = Properties.Resources.SettingsButtonReset;

            checkBoxSortDropFolder.Content = Properties.Resources.SettingsCheckboxSortDropFolder;
            checkBoxSortDroppedFiles.Content = Properties.Resources.SettingsCheckBoxSortDroppedFiles;
            checkBoxBatchReadEndpointToEveryTrack.Content = Properties.Resources.SettingsCheckBoxSetBatchReadEndpoint;
            checkBoxVerifyFlacMD5Sum.Content = Properties.Resources.SettingsCheckBoxVerifyFlacMD5Sum;
            checkBoxGpuRendering.Content = Properties.Resources.SettingsCheckBoxGpuRendering;
            checkBoxChannelCountEven.Content = Properties.Resources.SettingsCheckBoxChannelCountEven;

            groupBoxChannelCountSettings.Header = Properties.Resources.SettingsGroupBoxChannelCount;
            cbItemChannelCountNotChange.Content = Properties.Resources.SettingsCbItemChannelCountNotChanged;
            cbItemChannelCount2.Content = Properties.Resources.SettingsCbItemChannelCount2;
            cbItemChannelCount4.Content = Properties.Resources.SettingsCbItemChannelCount4;
            cbItemChannelCount6.Content = Properties.Resources.SettingsCbItemChannelCount6;
            cbItemChannelCount8.Content = Properties.Resources.SettingsCbItemChannelCount8;
            cbItemChannelCount10.Content = Properties.Resources.SettingsCbItemChannelCount10;
            cbItemChannelCount16.Content = Properties.Resources.SettingsCbItemChannelCount16;
            cbItemChannelCount18.Content = Properties.Resources.SettingsCbItemChannelCount18;
            cbItemChannelCount24.Content = Properties.Resources.SettingsCbItemChannelCount24;
            cbItemChannelCount26.Content = Properties.Resources.SettingsCbItemChannelCount26;
            cbItemChannelCount32.Content = Properties.Resources.SettingsCbItemChannelCount32;
            cbItemChannelCountMixFormat.Content = Properties.Resources.SettingsCbItemChannelCountMixFormat;

            checkBoxIsFormatSupported.Content = Properties.Resources.SettingsCheckBoxIsFormatSupportedCall;
        }

        public void SetPreference(Preference preference) {
            m_preference = preference;
        }

        private void UpdateUIFromPreference(Preference preference) {
            switch (preference.BitsPerSampleFixType) {
            case BitsPerSampleFixType.Sint16:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint16;
                break;
            case BitsPerSampleFixType.Sint24:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint24;
                break;
            case BitsPerSampleFixType.Sint32:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint32;
                break;
            case BitsPerSampleFixType.Sfloat32:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sfloat32;
                break;
            case BitsPerSampleFixType.Sint32V24:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint32V24;
                break;
            case BitsPerSampleFixType.AutoSelect:
            default:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.AutoSelect;
                break;
            }

            comboBoxNoiseShaping.SelectedIndex = (int)preference.BpsConvNoiseShaping;

            checkBoxPlaceKokomadeAfterIndex00.IsChecked =
                preference.ReplaceGapWithKokomade;

            checkBoxManuallySetMainWindowDimension.IsChecked =
                preference.ManuallySetMainWindowDimension;

            checkBoxStorePlaylistContent.IsChecked =
                preference.StorePlaylistContent;

            checkBoxCoverart.IsChecked =
                preference.DispCoverart;

            checkBoxReduceVolume.IsChecked =
                preference.ReduceVolume;

            checkBoxIsFormatSupported.IsChecked = preference.IsFormatSupportedCall;

            if (10000 == preference.TimePeriodHundredNanosec) {
                comboBoxTimePeriod.SelectedItem = cbItemTimerResolution1Millisec;
            } else {
                comboBoxTimePeriod.SelectedItem = cbItemTimerResolutionDefault;
            }

            textBoxPlayingTimeSize.Text =
                preference.PlayingTimeSize.ToString(CultureInfo.CurrentCulture);

            textBoxZeroFlushSeconds.Text =
                string.Format(CultureInfo.CurrentCulture, "{0}", preference.ZeroFlushMillisec * 0.001);

            textBoxConversionQuality.Text =
                string.Format(CultureInfo.CurrentCulture, "{0}", preference.ResamplerConversionQuality);

            checkBoxSootheLimiterApo.IsChecked = preference.SootheLimiterApo;

            sliderWindowScaling.Value =
                preference.WindowScale;

            checkBoxPlayingTimeBold.IsChecked =
                preference.PlayingTimeFontBold;

            var fontFamilies = new Dictionary<string, FontFamily>();

            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies) {
                if (!fontFamilies.ContainsKey(fontFamily.ToString())) {
                    fontFamilies.Add(fontFamily.ToString(), fontFamily);
                }
            }

            foreach (var kvp in fontFamilies) {
                var item = new ComboBoxItem();
                item.Content = kvp.Value;
                //item.FontFamily = fontFamily;
                comboBoxPlayingTimeFontNames.Items.Add(item);
                if (kvp.Key.Equals(preference.PlayingTimeFontName)) {
                    comboBoxPlayingTimeFontNames.SelectedItem = item;
                }
            }

            {
                mPlaylistAlternateBackgroundArgb = preference.AlternatingRowBackgroundArgb;
                rectangleColor.Fill = new SolidColorBrush(Util.ColorFromArgb(
                        preference.AlternatingRowBackgroundArgb));
                checkBoxAlternateBackground.IsChecked =
                        preference.AlternatingRowBackground;
                if (preference.AlternatingRowBackground) {
                    rectangleColor.IsEnabled = true;
                    buttonChangeColor.IsEnabled = true;
                } else {
                    rectangleColor.IsEnabled = false;
                    buttonChangeColor.IsEnabled = false;
                }
            }

            switch (preference.RenderThreadTaskType) {
            case RenderThreadTaskType.Audio:
                comboBoxRenderThreadTaskType.SelectedItem = cbItemTaskAudio;
                break;
            case RenderThreadTaskType.None:
                comboBoxRenderThreadTaskType.SelectedItem = cbItemTaskNone;
                break;
            case RenderThreadTaskType.Playback:
                comboBoxRenderThreadTaskType.SelectedItem = cbItemTaskPlayback;
                break;
            case RenderThreadTaskType.ProAudio:
            default:
                comboBoxRenderThreadTaskType.SelectedItem = cbItemTaskProAudio;
                break;
            }

            comboBoxRenderThreadPriority.SelectedIndex = (int)preference.MMThreadPriority;
            if (preference.RenderThreadTaskType == RenderThreadTaskType.None) {
                comboBoxRenderThreadPriority.IsEnabled = false;
            } else {
                comboBoxRenderThreadPriority.IsEnabled = true;
            }

            comboBoxCueEncoding.Items.Clear();
            foreach (var encoding in Encoding.GetEncodings()) {
                int pos = comboBoxCueEncoding.Items.Add(encoding.DisplayName);
                if (preference.CueEncodingCodePage == encoding.CodePage) {
                    comboBoxCueEncoding.SelectedIndex = pos;
                }
            }

            checkBoxSortDropFolder.IsChecked =
                preference.SortDropFolder;

            checkBoxSortDroppedFiles.IsChecked =
                preference.SortDroppedFiles;

            checkBoxBatchReadEndpointToEveryTrack.IsChecked =
                preference.BatchReadEndpointToEveryTrack;

            checkBoxVerifyFlacMD5Sum.IsChecked = preference.VerifyFlacMD5Sum;
            checkBoxVerifyFlacMD5Sum.IsEnabled = preference.ParallelRead == false;

            checkBoxGpuRendering.IsChecked = preference.GpuRendering;
            checkBoxChannelCountEven.IsChecked = preference.AddSilentForEvenChannel;

            switch (preference.ChannelCount2) {
            case ChannelCount2Type.MixFormatChannelCount:
                comboBoxChannelCount.SelectedItem = cbItemChannelCountMixFormat;
                break;
            case ChannelCount2Type.SourceChannelCount:
                comboBoxChannelCount.SelectedItem = cbItemChannelCountNotChange;
                break;
            case ChannelCount2Type.Ch2:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount2;
                break;
            case ChannelCount2Type.Ch4:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount4;
                break;
            case ChannelCount2Type.Ch6:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount6;
                break;
            case ChannelCount2Type.Ch8:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount8;
                break;
            case ChannelCount2Type.Ch10:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount10;
                break;
            case ChannelCount2Type.Ch16:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount16;
                break;
            case ChannelCount2Type.Ch18:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount18;
                break;
            case ChannelCount2Type.Ch24:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount24;
                break;
            case ChannelCount2Type.Ch26:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount26;
                break;
            case ChannelCount2Type.Ch32:
                comboBoxChannelCount.SelectedItem = cbItemChannelCount32;
                break;
            default:
                comboBoxChannelCount.SelectedItem = cbItemChannelCountMixFormat;
                break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(null != m_preference);
            UpdateUIFromPreference(m_preference);

            mWindowLoaded = true;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(0 <= comboBoxOutputFormat.SelectedIndex);
            var settingsBitFormat = (SettingsBitFormatType)comboBoxOutputFormat.SelectedIndex;
            switch (settingsBitFormat) {
            case SettingsBitFormatType.Sint16:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint16;
                break;
            case SettingsBitFormatType.Sint24:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint24;
                break;
            case SettingsBitFormatType.Sint32:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint32;
                break;
            case SettingsBitFormatType.Sfloat32:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sfloat32;
                break;
            case SettingsBitFormatType.Sint32V24:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint32V24;
                break;
            case SettingsBitFormatType.AutoSelect:
            default:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.AutoSelect;
                break;
            }

            m_preference.BpsConvNoiseShaping = (NoiseShapingType)comboBoxNoiseShaping.SelectedIndex;
            m_preference.EnableNoiseShaping = m_preference.BpsConvNoiseShaping != NoiseShapingType.None;

            m_preference.ReplaceGapWithKokomade
                = checkBoxPlaceKokomadeAfterIndex00.IsChecked == true;

            m_preference.ManuallySetMainWindowDimension
                = checkBoxManuallySetMainWindowDimension.IsChecked == true;

            m_preference.StorePlaylistContent
                = checkBoxStorePlaylistContent.IsChecked == true;

            m_preference.DispCoverart
                = checkBoxCoverart.IsChecked == true;

            m_preference.ReduceVolume
                = checkBoxReduceVolume.IsChecked == true;

            m_preference.IsFormatSupportedCall
                = checkBoxIsFormatSupported.IsChecked == true;
            
            if (comboBoxTimePeriod.SelectedItem == cbItemTimerResolution1Millisec) {
                m_preference.TimePeriodHundredNanosec = 10000;
            } else {
                m_preference.TimePeriodHundredNanosec = 0;
            }

            m_preference.WindowScale = sliderWindowScaling.Value;

            {
                int playingTimeSize;
                if (Int32.TryParse(textBoxPlayingTimeSize.Text, out playingTimeSize)) {
                    if (playingTimeSize <= 0 || 100 < playingTimeSize) {
                        MessageBox.Show("再生時間表示文字の大きさは 1～100の範囲の数字を入力してください。");
                        return;
                    }
                    m_preference.PlayingTimeSize = playingTimeSize;
                } else {
                    MessageBox.Show("再生時間表示文字の大きさは 1～100の範囲の数字を入力してください。");
                }
            }
            {
                double zeroFlushSeconds;
                if (Double.TryParse(textBoxZeroFlushSeconds.Text, out zeroFlushSeconds)) {
                    if (zeroFlushSeconds < 0 || 1000 < zeroFlushSeconds) {
                        MessageBox.Show("再生前無音送信時間の大きさは 0.0～1000.0の範囲の数字を入力してください。");
                        return;
                    }
                    m_preference.ZeroFlushMillisec = (int)(zeroFlushSeconds * 1000);
                } else {
                    MessageBox.Show("再生前無音送信時間の大きさは 0.0～1000.0の範囲の数字を入力してください。");
                }
            }
            {
                int v;
                if (Int32.TryParse(textBoxConversionQuality.Text, out v)) {
                    if (v <= 0 || 60 < v) {
                        MessageBox.Show("Wasapi Shared Resampler Qualityの大きさは 1～60の範囲の数字を入力してください。");
                        return;
                    }
                    m_preference.ResamplerConversionQuality = v;
                } else {
                    MessageBox.Show("Wasapi Shared Resampler Qualityの大きさは 1～60の範囲の数字を入力してください。");
                }
            }

            m_preference.SootheLimiterApo = (checkBoxSootheLimiterApo.IsChecked == true);

            m_preference.PlayingTimeFontBold = (checkBoxPlayingTimeBold.IsChecked == true);

            if (null != comboBoxPlayingTimeFontNames.SelectedItem) {
                ComboBoxItem item = (ComboBoxItem)comboBoxPlayingTimeFontNames.SelectedItem;
                FontFamily ff = (FontFamily)item.Content;
                m_preference.PlayingTimeFontName = ff.ToString();
            }
            {
                m_preference.AlternatingRowBackground
                    = checkBoxAlternateBackground.IsChecked == true;
                m_preference.AlternatingRowBackgroundArgb
                    = mPlaylistAlternateBackgroundArgb;
            }

            if (comboBoxRenderThreadTaskType.SelectedItem == cbItemTaskAudio) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.Audio;
            }
            if (comboBoxRenderThreadTaskType.SelectedItem == cbItemTaskNone) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.None;
            }
            if (comboBoxRenderThreadTaskType.SelectedItem == cbItemTaskPlayback) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.Playback;
            }
            if (comboBoxRenderThreadTaskType.SelectedItem == cbItemTaskProAudio) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.ProAudio;
            }

            m_preference.MMThreadPriority = (WasapiCS.MMThreadPriorityType)comboBoxRenderThreadPriority.SelectedIndex;

            if (0 <= comboBoxCueEncoding.SelectedIndex) {
                var encodingInfoArray = Encoding.GetEncodings();
                if (comboBoxCueEncoding.SelectedIndex < encodingInfoArray.Length) {
                    var encodingInfo = encodingInfoArray[comboBoxCueEncoding.SelectedIndex];
                    m_preference.CueEncodingCodePage = encodingInfo.CodePage;
                }
            }

            m_preference.SortDropFolder = (checkBoxSortDropFolder.IsChecked == true);
            m_preference.SortDroppedFiles = (checkBoxSortDroppedFiles.IsChecked == true);
            m_preference.BatchReadEndpointToEveryTrack = (checkBoxBatchReadEndpointToEveryTrack.IsChecked == true);
            m_preference.VerifyFlacMD5Sum = (checkBoxVerifyFlacMD5Sum.IsChecked == true);
            m_preference.GpuRendering = (checkBoxGpuRendering.IsChecked == true);
            m_preference.AddSilentForEvenChannel = (checkBoxChannelCountEven.IsChecked == true);

            if (comboBoxChannelCount.SelectedItem == cbItemChannelCountNotChange) {
                m_preference.ChannelCount2 = ChannelCount2Type.SourceChannelCount;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount2) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch2;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount4) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch4;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount6) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch6;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount8) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch8;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount10) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch10;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount16) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch16;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount18) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch18;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount24) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch24;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount26) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch26;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCount32) {
                m_preference.ChannelCount2 = ChannelCount2Type.Ch32;
            }
            if (comboBoxChannelCount.SelectedItem == cbItemChannelCountMixFormat) {
                m_preference.ChannelCount2 = ChannelCount2Type.MixFormatChannelCount;
            }

            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void buttonScale1X_Click(object sender, RoutedEventArgs e) {
            sliderWindowScaling.Value = 1.0;
        }

        private void sliderWindowScaling_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (null != labelWindowScaling) {
                labelWindowScaling.Content = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", e.NewValue);
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl)) {
                // CTRL + マウスホイールで画面のスケーリング

                double scaling = sliderWindowScaling.Value;
                if (e.Delta < 0) {
                    // 1.25の128乗根
                    scaling /= 1.001744829441175331741294013303;
                } else {
                    scaling *= 1.001744829441175331741294013303;
                }
                sliderWindowScaling.Value = scaling;
            }
        }

        private void comboBoxPlayingTimeFontNames_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (null == labelPlayingTime) {
                return;
            }

            ComboBox cb = sender as ComboBox;
            if (null == cb) {
                return;
            }
            ComboBoxItem item = cb.SelectedItem as ComboBoxItem;
            if (null == item) {
                return;
            }

            labelPlayingTime.FontFamily = (FontFamily)item.Content;
        }

        private void checkBoxPlayingTimeBold_Checked(object sender, RoutedEventArgs e) {
            if (null != labelPlayingTime) {
                labelPlayingTime.FontWeight = FontWeights.Bold;
            }
        }

        private void checkBoxPlayingTimeBold_Unchecked(object sender, RoutedEventArgs e) {
            if (null != labelPlayingTime) {
                labelPlayingTime.FontWeight = FontWeights.Normal;
            }
        }

        private void textBoxPlayingTimeSize_TextChanged(object sender, TextChangedEventArgs e) {
            if (null == labelPlayingTime) {
                return;
            }

            int fontSize;
            if (!Int32.TryParse(textBoxPlayingTimeSize.Text, out fontSize)) {
                return;
            }
            if (0 < fontSize && fontSize <= 100) {
                labelPlayingTime.FontSize = fontSize;
            }
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e) {
            var preference = new Preference();
            UpdateUIFromPreference(preference);
        }

        private void buttonChangeColor_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog d = new System.Windows.Forms.ColorDialog();
            d.Color = System.Drawing.Color.FromArgb((int)mPlaylistAlternateBackgroundArgb);
            System.Windows.Forms.DialogResult result = d.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) {
                return;
            }
            mPlaylistAlternateBackgroundArgb =
                ((uint)d.Color.A << 24) +
                ((uint)d.Color.R << 16) +
                ((uint)d.Color.G << 8) +
                ((uint)d.Color.B << 0);
            rectangleColor.Fill = new SolidColorBrush(
                Util.ColorFromArgb(mPlaylistAlternateBackgroundArgb));
        }

        private void checkBoxAlternateBackground_Checked(object sender, RoutedEventArgs e) {
            rectangleColor.IsEnabled = true;
            buttonChangeColor.IsEnabled = true;
        }

        private void checkBoxAlternateBackground_Unchecked(object sender, RoutedEventArgs e) {
            rectangleColor.IsEnabled = false;
            buttonChangeColor.IsEnabled = false;
        }

        private void rectangleColor_MouseUp(object sender, MouseButtonEventArgs e) {
            buttonChangeColor_Click(sender, e);
        }

        private void comboBoxRenderThreadTaskType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mWindowLoaded) {
                return;
            }

            if (comboBoxRenderThreadTaskType.SelectedItem == cbItemTaskNone) {
                comboBoxRenderThreadPriority.IsEnabled = false;
            } else {
                comboBoxRenderThreadPriority.IsEnabled = true;
            }
        }
    }
}
