﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using Microsoft.Win32;

namespace WWAudioFilter {
    /// <summary>
    /// FilterConfiguration.xaml の相互作用ロジック
    /// </summary>
    public partial class FilterConfiguration : Window {
        private TextChangedEventHandler mTextBoxGainInDbChangedEH;
        private TextChangedEventHandler mTextBoxGainInAmplitudeChangedEH;

        private FilterBase mFilter = null;

        private bool mInitialized = false;

        public FilterConfiguration(FilterBase filter) {
            InitializeComponent();

            if (filter != null) {
                mFilter = filter;
            }

            SetLocalizedTextToUI();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mTextBoxGainInDbChangedEH = new TextChangedEventHandler(textBoxGainInDB_TextChanged);
            mTextBoxGainInAmplitudeChangedEH = new TextChangedEventHandler(textBoxGainInAmplitude_TextChanged);

            if (mFilter != null) {
                // filterの設定をUIに反映する
                InitializeUIbyFilter(mFilter);
            }

            mInitialized = true;
        }

        private void SetLocalizedTextToUI() {
            groupBoxGain.Header = Properties.Resources.GroupGain;
            labelGainInAmplitude.Content = Properties.Resources.LabelGainInAmplitude;
            labelGainInDB.Content = Properties.Resources.LabelGainInDb;
            labelGainAmplitudeUnit.Content = Properties.Resources.LabelX;
            buttonUseGain.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxLPF.Header = Properties.Resources.GroupLPF;
            labelLpfCutoff.Content = Properties.Resources.LabelCutoffFreq;
            labelLpfSlope.Content = Properties.Resources.LabelGainRolloffSlopes;
            labelLpfLen.Content = Properties.Resources.LabelFilterLength;
            labelLpfLenUnit.Content = Properties.Resources.LabelSamples;
            buttonUseLpf.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxUpsampler.Header = Properties.Resources.GroupUpsampler;
            labelUpsamplerType.Content = Properties.Resources.LabelUpsamplerType;
            cbItemFftUpsampler.Content = Properties.Resources.CbItemFftUpsampler;
            cbItemZohUpsampler.Content = Properties.Resources.CbItemZohUpsampler;
            cbItemInsertZeroesUpsampler.Content = Properties.Resources.CbItemInsertZeroesUpsampler;
            labelUpsampleFactor.Content = Properties.Resources.LabelUpsamplingFactor;
            labelUpsampleLen.Content = Properties.Resources.LabelUpsamplerLength;
            labelUpsampleLenUnit.Content = Properties.Resources.LabelSamples;
            buttonUseUpsampler.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxNoiseShaping.Header = Properties.Resources.GroupNoiseShaping;
            labelNoiseShapingTargetBit.Content = Properties.Resources.LabelNoiseShapingTargetBit;
            labelNoiseShapingMethod.Content = Properties.Resources.LabelNoiseShapingMethod;
            cbItemNoiseShaping2nd.Content = Properties.Resources.CbItemNoiseShaping2nd;
            cbItemNoiseShaping4th.Content = Properties.Resources.CbItemNoiseShaping4th;
            buttonUseNoiseShaping.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxTagEdit.Header = Properties.Resources.GroupTagEdit;
            labelTagType.Content = Properties.Resources.LabelTagType;
            labelTagText.Content = Properties.Resources.LabelTagText;
            buttonUseTagEdit.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxDownsampler.Header = Properties.Resources.GroupDownsampler;
            labelDownsamplerOption.Content = Properties.Resources.LabelDownsamplerOption;
            labelDownsamplerType.Content = Properties.Resources.LabelDownsamplerType;
            cbItemDownsamplerType2x.Content = Properties.Resources.CbItemDownsamplerType2x;
            cbItemDownsamplerOption0.Content = Properties.Resources.CbItemDownsamplerOption0;
            cbItemDownsamplerOption1.Content = Properties.Resources.CbItemDownsamplerOption1;
            buttonUseDownsampler.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxCic.Header = Properties.Resources.GroupCic;
            labelCicFilterType.Content = Properties.Resources.LabelCicFilterType;
            cbItemCicTypeSingleStage.Content = Properties.Resources.CbItemCicTypeSingleStage;
            labelCicDelay.Content = Properties.Resources.LabelCicDelay;
            labelCicDelaySamples.Content = Properties.Resources.LabelCicDelaySamples;
            buttonUseCic.Content = Properties.Resources.ButtonUseThisFilter;

            groupBoxHalfBandFilter.Header = Properties.Resources.GroupHalfbandFilter;
            labelHalfBandFilterTap.Content = Properties.Resources.LabelHalfBandFilterTaps;
            buttonUseHalfBandFilter.Content = Properties.Resources.ButtonUseThisFilter;
        }

        public FilterBase Filter {
            get { return mFilter; }
        }

        private void InitializeUIbyFilter(FilterBase filter) {
            switch (filter.FilterType) {
            case FilterType.Gain:
                textBoxGainInDB.TextChanged        -= mTextBoxGainInDbChangedEH;
                textBoxGainInAmplitude.TextChanged -= mTextBoxGainInAmplitudeChangedEH;

                var gain = filter as GainFilter;
                textBoxGainInDB.Text = string.Format(CultureInfo.CurrentCulture, "{0}", 20.0 * Math.Log10(gain.Amplitude));
                textBoxGainInAmplitude.Text = string.Format(CultureInfo.CurrentCulture, "{0}", gain.Amplitude);

                textBoxGainInDB.TextChanged        += mTextBoxGainInDbChangedEH;
                textBoxGainInAmplitude.TextChanged += mTextBoxGainInAmplitudeChangedEH;
                break;
            case FilterType.ZohUpsampler:
                var zoh = filter as ZeroOrderHoldUpsampler;
                comboBoxUpsamplingFactor.SelectedIndex = (int)UpsamplingFactorToUpsamplingFactorType(zoh.Factor);
                comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.ZOH;
                break;
            case FilterType.LowPassFilter:
                var lpf = filter as LowpassFilter;
                textBoxLpfCutoff.Text = string.Format(CultureInfo.CurrentCulture, "{0}", lpf.CutoffFrequency);
                comboBoxLpfLen.SelectedIndex = (int)LpfLenToLpfLenType(lpf.FilterLength);
                textBoxLpfSlope.Text = string.Format(CultureInfo.CurrentCulture, "{0}", lpf.FilterSlopeDbOct);
                break;
            case FilterType.FftUpsampler:
                var fftu = filter as FftUpsampler;
                comboBoxUpsamplingFactor.SelectedIndex = (int)UpsamplingFactorToUpsamplingFactorType(fftu.Factor);
                comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.FFT;
                comboBoxUpsampleLen.SelectedIndex = (int)UpsampleLenToUpsampleLenType(fftu.FftLength);
                break;
            case FilterType.Mash2:
                var mash = filter as MashFilter;
                textBoxNoiseShapingTargetBit.Text = string.Format(CultureInfo.CurrentCulture, "{0}", mash.TargetBitsPerSample);
                break;

            case FilterType.NoiseShaping:
                var ns = filter as NoiseShapingFilter;
                textBoxNoiseShapingTargetBit.Text = string.Format(CultureInfo.CurrentCulture, "{0}", ns.TargetBitsPerSample);
                comboBoxNoiseShapingMethod.SelectedIndex = (int)NoiseShapingCbItemType.NoiseShaping2nd;
                break;
            case FilterType.NoiseShaping4th:
                var ns4 = filter as NoiseShaping4thFilter;
                textBoxNoiseShapingTargetBit.Text = string.Format(CultureInfo.CurrentCulture, "{0}", ns4.TargetBitsPerSample);
                comboBoxNoiseShapingMethod.SelectedIndex = (int)NoiseShapingCbItemType.NoiseShaping4th;
                break;
            case FilterType.TagEdit:
                var te = filter as TagEditFilter;
                comboBoxTagType.SelectedIndex = (int)te.TagType;
                textBoxTagText.Text = te.Text;
                break;
            case FilterType.Downsampler:
                var ds = filter as Downsampler;
                comboBoxDownsampleOption.SelectedIndex = ds.PickSampleIndex;
                break;
            case FilterType.CicFilter:
                var cic = filter as CicFilter;
                textBoxCicDelay.Text = string.Format(CultureInfo.CurrentCulture, "{0}", cic.Delay);
                break;

            case FilterType.InsertZeroesUpsampler:
                var izu = filter as InsertZeroesUpsampler;
                comboBoxUpsamplingFactor.SelectedIndex = (int)UpsamplingFactorToUpsamplingFactorType(izu.Factor);
                comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.InsertZeroes;
                break;
            case FilterType.HalfbandFilter:
                var hbf = filter as HalfbandFilter;
                textBoxHalfBandFilterTap.Text = string.Format(CultureInfo.CurrentCulture, "{0}", hbf.FilterLength);
                break;
            case FilterType.Crossfeed:
                var cf = filter as CrossfeedFilter;
                textBoxCrossfeedCoefficientFile.Text = cf.FilterFilePath;
                break;
            }
        }

        void textBoxGainInDB_TextChanged(object sender, TextChangedEventArgs e) {
            double v;
            if (!Double.TryParse(textBoxGainInDB.Text, out v)) {
                return;
            }

            textBoxGainInAmplitude.TextChanged -= mTextBoxGainInAmplitudeChangedEH;
            textBoxGainInAmplitude.Text = string.Format(CultureInfo.CurrentCulture, "{0}", Math.Pow(10.0, v / 20.0));
            textBoxGainInAmplitude.TextChanged += mTextBoxGainInAmplitudeChangedEH;
        }

        void textBoxGainInAmplitude_TextChanged(object sender, TextChangedEventArgs e) {
            double v;
            if (!Double.TryParse(textBoxGainInAmplitude.Text, out v)) {
                return;
            }
            if (v <= Double.Epsilon) {
                return;
            }

            textBoxGainInDB.TextChanged -= mTextBoxGainInAmplitudeChangedEH;
            textBoxGainInDB.Text = string.Format(CultureInfo.CurrentCulture, "{0}", 20.0 * Math.Log10(v));
            textBoxGainInDB.TextChanged += mTextBoxGainInAmplitudeChangedEH;
        }

        enum UpsamplingFactorType {
            x2,
            x4,
            x8,
            x16,
        };

        private static UpsamplingFactorType UpsamplingFactorToUpsamplingFactorType(int factor) {
            switch (factor) {
            case 2:
                return UpsamplingFactorType.x2;
            case 4:
                return UpsamplingFactorType.x4;
            case 8:
                return UpsamplingFactorType.x8;
            case 16:
                return UpsamplingFactorType.x16;
            default:
                System.Diagnostics.Debug.Assert(false);
                return UpsamplingFactorType.x2;
            }
        }

        private static int UpsamplingFactorTypeToUpsampingfactor(int t) {
            switch (t) {
            case (int)UpsamplingFactorType.x2:
                return 2;
            case (int)UpsamplingFactorType.x4:
                return 4;
            case (int)UpsamplingFactorType.x8:
                return 8;
            case (int)UpsamplingFactorType.x16:
                return 16;
            default:
                System.Diagnostics.Debug.Assert(false);
                return 2;
            }
        }

        enum LpfLenType {
            L255,
            L1023,
            L4095,
            L16383,
            L65535,
        };

        private static LpfLenType LpfLenToLpfLenType(int lpfLen) {
            switch (lpfLen) {
            case 255:
                return LpfLenType.L255;
            case 1023:
                return LpfLenType.L1023;
            case 4095:
                return LpfLenType.L4095;
            case 16383:
                return LpfLenType.L16383;
            case 65535:
            default:
                return LpfLenType.L65535;
            }
        }

        private static int LpfLenTypeToLpfLen(int t) {
            switch (t) {
            case (int)LpfLenType.L255:
                return 255;
            case (int)LpfLenType.L1023:
                return 1023;
            case (int)LpfLenType.L4095:
                return 4095;
            case (int)LpfLenType.L16383:
                return 16383;
            case (int)LpfLenType.L65535:
            default:
                return 65535;
            }
        }

        enum UpsamplerType {
            FFT,
            ZOH,
            InsertZeroes
        };

        enum UpsampleLenType {
            L1024,
            L4096,
            L16384,
            L65536,
            L262144,
        };

        private static UpsampleLenType UpsampleLenToUpsampleLenType(int len) {
            switch (len) {
            case 1024:
                return UpsampleLenType.L1024;
            case 4096:
                return UpsampleLenType.L4096;
            case 16384:
                return UpsampleLenType.L16384;
            case 65536:
                return UpsampleLenType.L65536;
            case 262144:
                return UpsampleLenType.L262144;
            default:
                return UpsampleLenType.L262144;
            }
        }

        private static int UpsampleLenTypeToLpfLen(int t) {
            switch (t) {
            case (int)UpsampleLenType.L1024:
                return 1024;
            case (int)UpsampleLenType.L4096:
                return 4096;
            case (int)UpsampleLenType.L16384:
                return 16384;
            case (int)UpsampleLenType.L65536:
                return 65536;
            case (int)UpsampleLenType.L262144:
                return 262144;
            default:
                return 262144;
            }
        }

        enum NoiseShapingCbItemType {
            NoiseShaping2nd,
            NoiseShaping4th
        };

        ///////////////////////////////////////////////////////////////////////////////////

        private void buttonCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void buttonUseGain_Click(object sender, RoutedEventArgs e) {
            double v;
            if (!Double.TryParse(textBoxGainInAmplitude.Text, out v)) {
                MessageBox.Show(Properties.Resources.ErrorGainValueIsNan);
                return;
            }
            if (v <= Double.Epsilon) {
                MessageBox.Show(Properties.Resources.ErrorGainValueIsTooSmall);
                return;
            }

            mFilter = new GainFilter(v);

            DialogResult = true;
            Close();
        }

        private void buttonUseUpsampler_Click(object sender, RoutedEventArgs e) {
            int factor = UpsamplingFactorTypeToUpsampingfactor(comboBoxUpsamplingFactor.SelectedIndex);
            int len = UpsampleLenTypeToLpfLen(comboBoxUpsampleLen.SelectedIndex);

            switch (comboBoxUpsamplerType.SelectedIndex) {
            case (int)UpsamplerType.ZOH:
                mFilter = new ZeroOrderHoldUpsampler(factor);
                break;
            case (int)UpsamplerType.FFT:
                mFilter = new FftUpsampler(factor, len);
                break;
            case (int)UpsamplerType.InsertZeroes:
                mFilter = new InsertZeroesUpsampler(factor);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                mFilter = new FftUpsampler(factor, len);
                break;
            }

            DialogResult = true;
            Close();
        }

        private void buttonUseLpf_Click(object sender, RoutedEventArgs e) {
            double v;
            if (!Double.TryParse(textBoxLpfCutoff.Text, out v)) {
                MessageBox.Show(Properties.Resources.ErrorLpfCutoffFreqIsNan);
                return;
            }
            if (v <= 0.0) {
                MessageBox.Show(Properties.Resources.ErrorLpfCutoffFreqIsNegative);
                return;
            }

            int slope;
            if (!Int32.TryParse(textBoxLpfSlope.Text, out slope)) {
                MessageBox.Show(Properties.Resources.ErrorLpfSlopeIsNan);
                return;
            }

            if (slope <= 0) {
                MessageBox.Show(Properties.Resources.ErrorLpfSlopeIsTooSmall);
                return;
            }

            int filterLength = LpfLenTypeToLpfLen(comboBoxLpfLen.SelectedIndex);

            mFilter = new LowpassFilter(v, filterLength, slope);
            DialogResult = true;
            Close();
        }

        private void buttonUseNoiseShaping_Click(object sender, RoutedEventArgs e) {
            int nBit;
            if (!Int32.TryParse(textBoxNoiseShapingTargetBit.Text, out nBit)) {
                MessageBox.Show(Properties.Resources.ErrorNoiseShapingBitIsNan);
                return;
            }
            if (nBit < 1 || 23 < nBit) {
                MessageBox.Show(Properties.Resources.ErrorNoiseShapingBitIsOutOfRange);
                return;
            }

            /*
            if (comboBoxNoiseShapingMethod.SelectedIndex == (int)NoiseShapingCbItemType.NoiseShaping4th
                && nBit != 1) {
                MessageBox.Show(Properties.Resources.ErrorNoiseShaping4thBitIsNot1);
                return;
            }
            */

            switch (comboBoxNoiseShapingMethod.SelectedIndex) {
            case (int)NoiseShapingCbItemType.NoiseShaping2nd:
                mFilter = new NoiseShapingFilter(nBit, 2);
                break;
            case (int)NoiseShapingCbItemType.NoiseShaping4th:
                mFilter = new NoiseShaping4thFilter(nBit);
                break;
            }
            DialogResult = true;
            Close();
        }

        private void comboBoxUpsamplerType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            if (comboBoxUpsamplerType.SelectedIndex == (int)UpsamplerType.FFT) {
                comboBoxUpsampleLen.IsEnabled = true;
            } else {
                comboBoxUpsampleLen.IsEnabled = false;
            }
        }

        enum TagEditType {
            Title,
            Album,
            AlbumArtist,
            Artist,
            Genre,
        };

        private void buttonUseTagEdit_Click(object sender, RoutedEventArgs e) {
            TagEditFilter.Type type = TagEditFilter.Type.Title;

            switch (comboBoxTagType.SelectedIndex) {
            case (int)TagEditType.Title:
                type = TagEditFilter.Type.Title;
                break;
            case (int)TagEditType.Album:
                type = TagEditFilter.Type.Album;
                break;
            case (int)TagEditType.AlbumArtist:
                type = TagEditFilter.Type.AlbumArtist;
                break;
            case (int)TagEditType.Artist:
                type = TagEditFilter.Type.Artist;
                break;
            case (int)TagEditType.Genre:
                type = TagEditFilter.Type.Genre;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            mFilter = new TagEditFilter(type, textBoxTagText.Text);
            DialogResult = true;
            Close();
        }

        enum DownsamplerType {
            Down2x,
        };

        private void buttonUseDownsampler_Click(object sender, RoutedEventArgs e) {
            int factor = 0;
            switch (comboBoxDownsampleType.SelectedIndex) {
            case (int)DownsamplerType.Down2x:
                factor = 2;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            int pickSampleIndex = (int)comboBoxDownsampleOption.SelectedIndex;

            mFilter = new Downsampler(factor, pickSampleIndex);
            DialogResult = true;
            Close();
        }

        enum CicFilterType {
            Decimation1stOrder8x,
            Decimation1stOrder8xWithCompensation,
            Interpolation1stOrder4x,
        };

        private void buttonUseCic_Click(object sender, RoutedEventArgs e) {
            int delay;
            if (!Int32.TryParse(textBoxCicDelay.Text, out delay)) {
                MessageBox.Show(Properties.Resources.ErrorCicDelay);
                return;
            }
            if (delay <= 0) {
                MessageBox.Show(Properties.Resources.ErrorCicDelay);
                return;
            }

            mFilter = new CicFilter(CicFilter.CicType.SingleStage, delay);
            DialogResult = true;
            Close();
        }

        private void buttonUseHalfBandFilter_Click(object sender, RoutedEventArgs e) {
            int taps;
            if (!Int32.TryParse(textBoxHalfBandFilterTap.Text, out taps)) {
                MessageBox.Show(Properties.Resources.ErrorHalfbandTaps);
                return;
            }
            if (taps <= 0) {
                MessageBox.Show(Properties.Resources.ErrorHalfbandTaps);
                return;
            }

            mFilter = new HalfbandFilter(taps);
            DialogResult = true;
            Close();
        }

        private void buttonCrossfeedBrowse_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = Properties.Resources.CrossfeedDefaultExt;
            dlg.Filter = Properties.Resources.CrossfeedFileFilter;
            dlg.CheckFileExists = true;
            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxCrossfeedCoefficientFile.Text = dlg.FileName;
        }

        private void buttonUseCrossfeedFilter_Click(object sender, RoutedEventArgs e) {
            if (textBoxCrossfeedCoefficientFile.Text.Length == 0) {
                MessageBox.Show(Properties.Resources.ErrorCrossfeedFile);
                return;
            }

            mFilter = new CrossfeedFilter(textBoxCrossfeedCoefficientFile.Text);
            DialogResult = true;
            Close();
        }
    }
}
