using System;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace WWDistortionNoise {
    /// <summary>
    /// DistortionNoiseFilterConfiguration.xaml の相互作用ロジック
    /// </summary>
    public partial class DistortionNoiseFilterConfiguration : Window {
        FilterBase mFilter;
        public FilterBase Filter {
            get { return mFilter; }
        }

        private TextChangedEventHandler mTextBoxGainInDbChangedEH;
        private TextChangedEventHandler mTextBoxGainInAmplitudeChangedEH;

        private readonly int [] mConvolutionLengthArray = { 1024, 4096, 16384, 65536 };
        
        public DistortionNoiseFilterConfiguration(FilterBase filter) {
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
        }
        
        private void SetLocalizedTextToUI() {
            groupBoxGain.Header = Properties.Resources.GroupGain;
            labelGainInAmplitude.Content = Properties.Resources.LabelGainInAmplitude;
            labelGainInDB.Content = Properties.Resources.LabelGainInDb;
            labelGainAmplitudeUnit.Content = Properties.Resources.LabelX;
            buttonUseGain.Content = Properties.Resources.ButtonUseThisFilter;
        }

        private void InitializeUIbyFilter(FilterBase filter) {
            switch (filter.FilterType) {
            case FilterType.Gain:
                textBoxGainInDB.TextChanged -= mTextBoxGainInDbChangedEH;
                textBoxGainInAmplitude.TextChanged -= mTextBoxGainInAmplitudeChangedEH;

                var gain = filter as GainFilter;
                textBoxGainInDB.Text = string.Format(CultureInfo.CurrentCulture, "{0}", 20.0 * Math.Log10(gain.Amplitude));
                textBoxGainInAmplitude.Text = string.Format(CultureInfo.CurrentCulture, "{0}", gain.Amplitude);

                textBoxGainInDB.TextChanged += mTextBoxGainInDbChangedEH;
                textBoxGainInAmplitude.TextChanged += mTextBoxGainInAmplitudeChangedEH;
                break;
            case FilterType.JitterAdd:
                var jitter = filter as JitterAddFilter;
                textBoxSinusoidalJitterFreq.Text = string.Format(CultureInfo.CurrentCulture, "{0}", jitter.SineJitterFreq);
                textBoxSinusoidalJitterNanoSeconds.Text = string.Format(CultureInfo.CurrentCulture, "{0}", jitter.SineJitterNanosec);
                textBoxTpdfJitterNanoSeconds.Text = string.Format(CultureInfo.CurrentCulture, "{0}", jitter.TpdfJitterNanosec);
                textBoxRpdfJitterNanoSeconds.Text = string.Format(CultureInfo.CurrentCulture, "{0}", jitter.RpdfJitterNanosec);
                for (int i=0; i < mConvolutionLengthArray.Length; ++i) {
                    if (mConvolutionLengthArray[i] == jitter.ConvolutionLengthMinus1) {
                        comboBoxFilterLength.SelectedIndex = i;
                    }
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
    }
}
