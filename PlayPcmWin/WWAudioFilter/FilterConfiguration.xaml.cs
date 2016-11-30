    using System;
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

            private readonly int[] mConvolutionLengthArray = { 1024, 4096, 16384, 65536 };

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
                cbItemLineDrawUpsampler.Content = Properties.Resources.CbItemLineDrawUpsampler;
                cbItemCubicHermiteSplineUpsampler.Content = Properties.Resources.CbIteCubicHermiteSplineUpsampler;
                cbItemWindowedSincUpsampler.Content = Properties.Resources.cbItemWindowedSincUpsampler;
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
                labelDownsamplerType.Content = Properties.Resources.LabelDownsamplerType;
                labelDownsamplerFactor.Content = Properties.Resources.LabelDownsamplerFactor;
                cbItemDownsamplerOption0.Content = Properties.Resources.CbItemDownsamplerOption0;
                cbItemDownsamplerOption1.Content = Properties.Resources.CbItemDownsamplerOption1;
                labelDownsampleLen.Content = Properties.Resources.LabelUpsamplerLength;
                labelDownsampleLenUnit.Content = Properties.Resources.LabelSamples;
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

                groupBoxNormalize.Header = Properties.Resources.GroupNormalizeFilter;
                labelNormalizeAmplitude.Content = Properties.Resources.LabelNormalizeAmplitude;
                buttonUseNormalize.Content = Properties.Resources.ButtonUseThisFilter;

                groupBoxAddFundamentals.Header = Properties.Resources.GroupAddFundamentals;
                labelAddFundamentalsGain.Content = Properties.Resources.LabelGainInDb;
                buttonUseAddFundamentals.Content = Properties.Resources.ButtonUseThisFilter;

                groupBoxReduceBitDepth.Header = Properties.Resources.GroupReduceBitDepth;
                labelQuantizerBit.Content = Properties.Resources.LabelTargetBitDepth;
                buttonUseReduceBitDepth.Content = Properties.Resources.ButtonUseThisFilter;

                groupBoxFirstOrderAllPassIIR.Header = Properties.Resources.GroupFirstOrderAllPassIIR;
                groupBoxSecondOrderAllPassIIR.Header = Properties.Resources.GroupSecondOrderAllPassIIR;
                labelSecondAllPassIirT.Content = Properties.Resources.LabelSecondAllPassIirT;

                buttonUseCrossfeedFilter.Content = Properties.Resources.ButtonUseThisFilter;
                buttonUseAddJitter.Content = Properties.Resources.ButtonUseThisFilter;
                buttonUseGaussianNoise.Content = Properties.Resources.ButtonUseThisFilter;
                buttonUseDynamicRangeCompression.Content = Properties.Resources.ButtonUseThisFilter;
                buttonUseFirstOrderAllPassIir.Content = Properties.Resources.ButtonUseThisFilter;
                buttonUseSecondOrderAllPassIir.Content = Properties.Resources.ButtonUseThisFilter;

                groupBoxDynamicRangeCompression.Header = Properties.Resources.GroupDynamicRangeCompression;
                groupBoxAddGaussianNoise.Header = Properties.Resources.GroupAddGaussianNoise;
                groupBoxAddJitter.Header = Properties.Resources.GroupAddJitter;

                groupBoxSubsonicFilter.Header = Properties.Resources.GroupSubsonicFilter;
                labelSubsonicFilterCutoffFrequency.Content = Properties.Resources.LabelSubsonicFilterCutoffFrequency;
                buttonUseSubsonicFilter.Content = Properties.Resources.ButtonUseThisFilter;

                groupBoxTimeReversalFilter.Header = Properties.Resources.GroupTimeReversalFilter;
                buttonUseTimeReversalFilter.Content = Properties.Resources.ButtonUseThisFilter;

                groupBoxNosdacCompensationFilter.Header = Properties.Resources.GroupNosdacCompensationFilter;
                labelNosdacCompensationTaps.Content = Properties.Resources.LabelNosdacCompensationTaps;
                buttonUseNosdacCompensationFilter.Content = Properties.Resources.ButtonUseThisFilter;
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
                    comboBoxUpsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(zoh.Factor);
                    comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.ZOH;
                    break;
                case FilterType.LineDrawUpsampler:
                    var ldu = filter as LineDrawUpsampler;
                    comboBoxUpsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(ldu.Factor);
                    comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.LineDraw;
                    break;
                case FilterType.CubicHermiteSplineUpsampler:
                    var chu = filter as CubicHermiteSplineUpsampler;
                    comboBoxUpsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(chu.Factor);
                    comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.CubicHermiteSpline;
                    break;
                case FilterType.LowPassFilter:
                    var lpf = filter as LowpassFilter;
                    textBoxLpfCutoff.Text = string.Format(CultureInfo.CurrentCulture, "{0}", lpf.CutoffFrequency);
                    comboBoxLpfLen.SelectedIndex = (int)LpfLenToLpfLenType(lpf.FilterLength);
                    textBoxLpfSlope.Text = string.Format(CultureInfo.CurrentCulture, "{0}", lpf.FilterSlopeDbOct);
                    break;
                case FilterType.FftUpsampler:
                    var fftu = filter as FftUpsampler;
                    comboBoxUpsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(fftu.Factor);
                    comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.FFT;
                    comboBoxUpsampleLen.SelectedIndex = (int)ResampleLenToUpsampleLenType(fftu.FftLength);
                    comboBoxFftOverlap.SelectedIndex = (int)fftu.Overlap;
                    break;
                case FilterType.WindowedSincUpsampler:
                    var wsu = filter as WindowedSincUpsampler;
                    comboBoxUpsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(wsu.Factor);
                    comboBoxUpsamplerType.SelectedIndex = (int)UpsamplerType.WindowedSinc;
                    comboBoxUpsampleLen.SelectedIndex = (int)ResampleLenToUpsampleLenType(wsu.WindowLength+1);
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
                case FilterType.ReduceBitDepth:
                    var q = filter as ReduceBitDepth;
                    textBoxTargetBitDepth.Text = string.Format(CultureInfo.CurrentCulture, "{0}", q.TargetBitsPerSample);
                    break;
                case FilterType.FirstOrderAllPassIIR:
                    var fomp = filter as FirstOrderAllPassIIRFilter;
                    textBoxFirstOrderAllPassIirA.Text = string.Format(CultureInfo.CurrentCulture, "{0}", fomp.A);
                    break;
                case FilterType.SecondOrderAllPassIIR:
                    var somp = filter as SecondOrderAllPassIIRFilter;
                    textBoxSecondOrderAllPassIirR.Text = string.Format(CultureInfo.CurrentCulture, "{0}", somp.R);
                    textBoxSecondOrderAllPassIirT.Text = string.Format(CultureInfo.CurrentCulture, "{0}", somp.T);
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
                    comboBoxDownsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(ds.Factor);
                    comboBoxDownsampleType.SelectedIndex = ds.PickSampleIndex;
                    break;
                case FilterType.CicFilter:
                    var cic = filter as CicFilter;
                    textBoxCicDelay.Text = string.Format(CultureInfo.CurrentCulture, "{0}", cic.Delay);
                    break;

                case FilterType.InsertZeroesUpsampler:
                    var izu = filter as InsertZeroesUpsampler;
                    comboBoxUpsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(izu.Factor);
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
                case FilterType.JitterAdd:
                    var jaf = filter as JitterAddFilter;
                    break;
                case FilterType.GaussianNoise:
                    var gnf = filter as GaussianNoiseFilter;
                    textBoxGaussianNoiseDb.Text = string.Format(CultureInfo.CurrentCulture, "{0}", gnf.NoiseLevelDb);
                    break;
                case FilterType.DynamicRangeCompression:
                    var drc = filter as DynamicRangeCompressionFilter;
                    textBoxDynamicRangeCompressionLsbScaling.Text = string.Format(CultureInfo.CurrentCulture, "{0}", drc.LsbScalingDb);
                    break;
                case FilterType.UnevenBitDac:
                    var ubd = filter as UnevenBitDacFilter;
                    textBoxUnevenBitDacLsbScaling.Text = string.Format(CultureInfo.CurrentCulture, "{0}", ubd.LsbScalingDb);
                    break;
                case FilterType.AddFundamentals:
                    var af = filter as AddFundamentalsFilter;
                    textBoxAddFundamentalsGain.Text = string.Format(CultureInfo.CurrentCulture, "{0}", 20.0 * Math.Log10(af.Gain));
                    break;
                case FilterType.SubsonicFilter: {
                        var f = filter as SubsonicFilter;
                        textBoxSubsonicFilterCutoffFrequency.Text = string.Format(CultureInfo.CurrentCulture, "{0}", f.CutoffFreq);
                        break;
                    }
                case FilterType.TimeReversal:
                    break;
                case FilterType.ZohNosdacCompensation: {
                        var f = filter as ZohNosdacCompensationFilter;
                        comboBoxNosdacCompensationTaps.SelectedIndex = ZohNosdacTapsToComboBoxIndex(f.Taps);
                    }
                    break;
                case FilterType.WindowedSincDownsampler: {
                        var f = filter as WindowedSincDownsampler;
                        comboBoxDownsamplingFactor.SelectedIndex = (int)ResamplingFactorToResamplingFactorType(f.Factor);
                        comboBoxDownsampleType.SelectedIndex = (int)DownsamplerType.WindowedSinc;
                        comboBoxDownsampleLen.SelectedIndex = (int)ResampleLenToUpsampleLenType(f.WindowLength + 1);
                    }
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

            enum ResamplingFactorType {
                x2,
                x3,
                x4,
                x5,
                x7,

                x8,
                x16,
                x20,
                x40,
                x64,

                x80,
                x147,
                x160,
                x320
            };

            private static readonly int[] ResamplingFactorNumbers = {
                2,
                3,
                4,
                5,
                7,

                8,
                16,
                20,
                40,
                64,

                80,
                147,
                160,
                320
            };

            private static ResamplingFactorType ResamplingFactorToResamplingFactorType(int factor) {
                for (int i = 0; i < ResamplingFactorNumbers.Length; ++i) {
                    if (factor == ResamplingFactorNumbers[i]) {
                        return (ResamplingFactorType)i;
                    }
                }

                // 該当しない。
                return ResamplingFactorType.x2;
            }

            private static int ResamplingFactorTypeToResampingfactor(ResamplingFactorType t) {
                return ResamplingFactorNumbers[(int)t];
            }

            enum LpfLenType {
                L255,
                L1023,
                L4095,
                L16383,
                L65535,

                L1048575,
            };

            private static LpfLenType LpfLenToLpfLenType(int lpfLen) {
                switch (lpfLen) {
                case 255:
                    return LpfLenType.L255;
                case 1023:
                    return LpfLenType.L1023;
                case 4095:
                default:
                    return LpfLenType.L4095;
                case 16383:
                    return LpfLenType.L16383;
                case 65535:
                    return LpfLenType.L65535;

                case 1048575:
                    return LpfLenType.L1048575;
                }
            }

            private static int LpfLenTypeToLpfLen(LpfLenType t) {
                switch (t) {
                case LpfLenType.L255:
                    return 255;
                case LpfLenType.L1023:
                    return 1023;
                case LpfLenType.L4095:
                default:
                    return 4095;
                case LpfLenType.L16383:
                    return 16383;
                case LpfLenType.L65535:
                    return 65535;
                case LpfLenType.L1048575:
                    return 1048575;
                }
            }

            enum UpsamplerType {
                FFT,
                ZOH,
                InsertZeroes,
                LineDraw,
                CubicHermiteSpline,
                WindowedSinc
            };

            enum ResampleLenType {
                L1024,
                L4096,
                L16384,
                L65536,
                L262144,

                L1048576,
            };

            private static ResampleLenType ResampleLenToUpsampleLenType(int len) {
                switch (len) {
                case 1024:
                    return ResampleLenType.L1024;
                case 4096:
                default:
                    return ResampleLenType.L4096;
                case 16384:
                    return ResampleLenType.L16384;
                case 65536:
                    return ResampleLenType.L65536;
                case 262144:
                    return ResampleLenType.L262144;

                case 1048576:
                    return ResampleLenType.L1048576;
                }
            }

            private static int ResampleLenTypeToLpfLen(ResampleLenType t) {
                switch (t) {
                case ResampleLenType.L1024:
                    return 1024;
                case ResampleLenType.L4096:
                default:
                    return 4096;
                case ResampleLenType.L16384:
                    return 16384;
                case ResampleLenType.L65536:
                    return 65536;
                case ResampleLenType.L262144:
                    return 262144;

                case ResampleLenType.L1048576:
                    return 1048576;
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
                int factor = ResamplingFactorTypeToResampingfactor((ResamplingFactorType)comboBoxUpsamplingFactor.SelectedIndex);
                int len = ResampleLenTypeToLpfLen((ResampleLenType)comboBoxUpsampleLen.SelectedIndex);
                var factorType = (UpsamplerType)comboBoxUpsamplerType.SelectedIndex;

                var overlap = (FftUpsampler.OverlapType)comboBoxFftOverlap.SelectedIndex;

                if (!WWAFUtil.IsPowerOfTwo(factor)) {
                    switch (factorType) {
                    case UpsamplerType.FFT:
                        // 2の乗数ではない倍数に対応していない。
                        MessageBox.Show(Properties.Resources.ErrorNotImplementedUpsampler);
                        return;
                    case UpsamplerType.CubicHermiteSpline:
                    case UpsamplerType.InsertZeroes:
                    case UpsamplerType.LineDraw:
                    case UpsamplerType.WindowedSinc:
                    case UpsamplerType.ZOH:
                        // 2の乗数ではない倍数に対応している。
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                    }
                }

                switch (comboBoxUpsamplerType.SelectedIndex) {
                case (int)UpsamplerType.ZOH:
                    mFilter = new ZeroOrderHoldUpsampler(factor);
                    break;
                case (int)UpsamplerType.LineDraw:
                    mFilter = new LineDrawUpsampler(factor);
                    break;
                case (int)UpsamplerType.CubicHermiteSpline:
                    mFilter = new CubicHermiteSplineUpsampler(factor);
                    break;
                case (int)UpsamplerType.FFT:
                    mFilter = new FftUpsampler(factor, len, overlap);
                    break;
                case (int)UpsamplerType.WindowedSinc:
                    mFilter = new WindowedSincUpsampler(factor, len - 1);
                    break;
                case (int)UpsamplerType.InsertZeroes:
                    mFilter = new InsertZeroesUpsampler(factor);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    mFilter = null;
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

                int filterLength = LpfLenTypeToLpfLen((LpfLenType)comboBoxLpfLen.SelectedIndex);

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

                switch (comboBoxUpsamplerType.SelectedIndex) {
                case (int)UpsamplerType.FFT:
                    comboBoxUpsampleLen.IsEnabled = true;
                    comboBoxFftOverlap.IsEnabled = true;
                    break;
                case (int)UpsamplerType.WindowedSinc:
                    comboBoxUpsampleLen.IsEnabled = true;
                    comboBoxFftOverlap.IsEnabled = false;
                    break;
                default:
                    comboBoxUpsampleLen.IsEnabled = false;
                    comboBoxFftOverlap.IsEnabled = false;
                    break;
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
                Pick0,
                Pick1,
                WindowedSinc,
            };

            private void buttonUseDownsampler_Click(object sender, RoutedEventArgs e) {
                var factorType = (ResamplingFactorType)comboBoxDownsamplingFactor.SelectedIndex;
                int factor = ResamplingFactorTypeToResampingfactor(factorType);
                int len = ResampleLenTypeToLpfLen((ResampleLenType)comboBoxDownsampleLen.SelectedIndex);

                DownsamplerType type = (DownsamplerType)comboBoxDownsampleType.SelectedIndex;

                switch (type) {
                case DownsamplerType.Pick0:
                case DownsamplerType.Pick1:
                    mFilter = new Downsampler(factor, (int)type);
                    break;
                case DownsamplerType.WindowedSinc:
                    mFilter = new WindowedSincDownsampler(factor, len - 1);
                    break;
                }

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

            private void buttonUseAddJitter_Click(object sender, RoutedEventArgs e) {
                double sineJitterFreq;
                if (!Double.TryParse(textBoxSinusoidalJitterFreq.Text, out sineJitterFreq)) {
                    MessageBox.Show(Properties.Resources.ErrorSinusolidalJitterFreq);
                    return;
                }
                if (sineJitterFreq < 0) {
                    MessageBox.Show(Properties.Resources.ErrorSinusolidalJitterFreq);
                    return;
                }

                double sineJitterNanosec;
                if (!Double.TryParse(textBoxSinusoidalJitterNanoSeconds.Text, out sineJitterNanosec)) {
                    MessageBox.Show(Properties.Resources.ErrorSinusolidalJitterAmount);
                    return;
                }
                if (sineJitterNanosec < 0) {
                    MessageBox.Show(Properties.Resources.ErrorSinusolidalJitterAmount);
                    return;
                }

                double tpdfJitterNanosec;
                if (!Double.TryParse(textBoxTpdfJitterNanoSeconds.Text, out tpdfJitterNanosec)) {
                    MessageBox.Show(Properties.Resources.ErrorTpdfJitterAmount);
                    return;
                }
                if (tpdfJitterNanosec < 0) {
                    MessageBox.Show(Properties.Resources.ErrorTpdfJitterAmount);
                    return;
                }

                double rpdfJitterNanosec;
                if (!Double.TryParse(textBoxRpdfJitterNanoSeconds.Text, out rpdfJitterNanosec)) {
                    MessageBox.Show(Properties.Resources.ErrorRpdfJitterAmount);
                    return;
                }
                if (rpdfJitterNanosec < 0) {
                    MessageBox.Show(Properties.Resources.ErrorRpdfJitterAmount);
                    return;
                }

                int convolutionN = 1024;
                if (0 <= comboBoxFilterLength.SelectedIndex) {
                    convolutionN = mConvolutionLengthArray[comboBoxFilterLength.SelectedIndex];
                }

                mFilter = new JitterAddFilter(sineJitterFreq, sineJitterNanosec, tpdfJitterNanosec, rpdfJitterNanosec, convolutionN);

                DialogResult = true;
                Close();
            }

            private void buttonUseGaussianNoise_Click(object sender, RoutedEventArgs e) {
                double noiseLevelDb = 0;
                if (!Double.TryParse(textBoxGaussianNoiseDb.Text, out noiseLevelDb)) {
                    MessageBox.Show(Properties.Resources.ErrorGaussianNoiseLevel);
                    return;
                }

                mFilter = new GaussianNoiseFilter(noiseLevelDb);
                DialogResult = true;
                Close();
            }

            private void buttonUseDynamicRangeCompression_Click(object sender, RoutedEventArgs e) {
                double lsbScalingDb = 1;
                if (!Double.TryParse(textBoxDynamicRangeCompressionLsbScaling.Text, out lsbScalingDb)) {
                    MessageBox.Show(Properties.Resources.ErrorDynamicRangeCompressionLsbScaling);
                    return;
                }

                mFilter = new DynamicRangeCompressionFilter(lsbScalingDb);
                DialogResult = true;
                Close();
            }

            private void buttonUseUnevenBitDac_Click(object sender, RoutedEventArgs e) {
                double lsbScalingDb = 1;
                if (!Double.TryParse(textBoxUnevenBitDacLsbScaling.Text, out lsbScalingDb)) {
                    MessageBox.Show(Properties.Resources.ErrorUnevenBitDacLsbScaling);
                    return;
                }

                mFilter = new UnevenBitDacFilter(lsbScalingDb);
                DialogResult = true;
                Close();
            }

            private void buttonUseNormalize_Click(object sender, RoutedEventArgs e) {
                double v;
                if (!Double.TryParse(textBoxNormalizeAmplitude.Text, out v) || 0.0 < v) {
                    MessageBox.Show(Properties.Resources.ErrorNormalizeValue);
                    return;
                }

                mFilter = new NormalizeFilter(Math.Pow(10.0, v / 20.0));

                DialogResult = true;
                Close();
            }

            private void buttonUseAddFundamentals_Click(object sender, RoutedEventArgs e) {
                double v;
                if (!Double.TryParse(textBoxAddFundamentalsGain.Text, out v) || 0.0 < v) {
                    MessageBox.Show(Properties.Resources.ErrorFundamentalsGainValue);
                    return;
                }

                mFilter = new AddFundamentalsFilter(Math.Pow(10.0, v / 20.0));

                DialogResult = true;
                Close();
            }

            private void buttonUseReduceBitDepth_Click(object sender, RoutedEventArgs e) {
                int v;
                if (!Int32.TryParse(textBoxTargetBitDepth.Text, out v) || 24 <= v || v < 1) {
                    MessageBox.Show(Properties.Resources.ErrorTargetBitDepth);
                    return;
                }

                mFilter = new ReduceBitDepth(v);

                DialogResult = true;
                Close();
            }

            private void buttonUseFirstOrderAllPassIir_Click(object sender, RoutedEventArgs e) {
                double a;
                if (!Double.TryParse(textBoxFirstOrderAllPassIirA.Text, out a) || 1.0 < Math.Abs(a) || a == 0) {
                    MessageBox.Show(Properties.Resources.ErrorFirstOrderAllPassIIR);
                    return;
                }

                mFilter = new FirstOrderAllPassIIRFilter(a);

                DialogResult = true;
                Close();
            }

            private void buttonUseSecondOrderAllPassIir_Click(object sender, RoutedEventArgs e) {
                double r;
                if (!Double.TryParse(textBoxSecondOrderAllPassIirR.Text, out r) || 1.0 < Math.Abs(r) || r== 0) {
                    MessageBox.Show(Properties.Resources.ErrorSecondOrderAllPassIirR);
                    return;
                }

                double t;
                if (!Double.TryParse(textBoxSecondOrderAllPassIirT.Text, out t)) {
                    MessageBox.Show(Properties.Resources.ErrorSecondOrderAllPassIirT);
                    return;
                }

                mFilter = new SecondOrderAllPassIIRFilter(r, t);

                DialogResult = true;
                Close();
            }

            private void buttonUseSubsonicFilter_Click(object sender, RoutedEventArgs e) {
                double v;
                if (!Double.TryParse(textBoxSubsonicFilterCutoffFrequency.Text, out v) || v < 1.0) {
                    MessageBox.Show(Properties.Resources.ErrorSubsonicFilterCutoffFrequency);
                    return;
                }

                mFilter = new SubsonicFilter(v);

                DialogResult = true;
                Close();
            }

            private void buttonUseTimeReversalFilter_Click(object sender, RoutedEventArgs e) {
                mFilter = new TimeReversalFilter();

                DialogResult = true;
                Close();
            }

            private static int ZohNosdacTapsToComboBoxIndex(int taps) {
                switch (taps) {
                case 9:
                    return 0;
                case 17:
                    return 1;
                case 33:
                    return 2;
                default:
                    return -1;
                }
            }

            private static int ZohNosdacComboBoxIndexToTaps(int idx) {
                switch (idx) {
                case 0:
                    return 9;
                case 1:
                    return 17;
                case 2:
                default:
                    return 33;
                }
            }

            private void buttonUseNosdacCompensationFilter_Click(object sender, RoutedEventArgs e) {
                mFilter = new ZohNosdacCompensationFilter(ZohNosdacComboBoxIndexToTaps(comboBoxNosdacCompensationTaps.SelectedIndex));

                DialogResult = true;
                Close();
            }

            private void comboBoxDownsampleType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
                if (!mInitialized) {
                    return;
                }

                DownsamplerType type = (DownsamplerType)comboBoxDownsampleType.SelectedIndex;
                switch (type) {
                case DownsamplerType.Pick0:
                case DownsamplerType.Pick1:
                    comboBoxDownsampleLen.IsEnabled = false;
                    break;
                case DownsamplerType.WindowedSinc:
                    comboBoxDownsampleLen.IsEnabled = true;
                    break;
                }
            }
        }
    }
