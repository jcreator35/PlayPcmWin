using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WWMath;

namespace WWAudioFilter {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            LocalizeUI();
        }

        private void LocalizeUI() {
            groupBoxSpecification.Header = Properties.Resources.AnalogFilterSpecification;
            groupBoxFR.Header = Properties.Resources.FrequencyResponse;
            groupBoxTD.Header = Properties.Resources.TimeDomainPlot;
            groupBoxPoleZero.Header = Properties.Resources.PoleZeroPlot;
            groupBoxAFC.Header = Properties.Resources.AnalogFilterCircuit;
            groupBoxDesignParameters.Header = Properties.Resources.DesignParameters;

            textBlockGain.Text = "↑" + Properties.Resources.Gain;
            textblockFrequency.Text = Properties.Resources.Frequency;
            groupBoxFilterType.Header = Properties.Resources.FilterType;
            radioButtonFilterTypeButterworth.Content = Properties.Resources.Butterworth;
            labelOptimization.Content = Properties.Resources.Optimization + ":";
            buttonUpdate.Content = Properties.Resources.Update;

            comboBoxItemβmax.Content = Properties.Resources.Stopband;
            comboBoxItemβmin.Content = Properties.Resources.Passband;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Update();
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e) {
            Update();
        }

        private void AddLog(string s) {
            mTextBoxLog.AppendText(s);
            mTextBoxLog.ScrollToEnd();
        }

        private void Update() {
            if (radioButtonFilterTypeButterworth.IsChecked == true) {
                UpdateButterWorth();
            }
            if (radioButtonFilterTypeChebyshev.IsChecked == true) {
                UpdateChebyshev();
            }
        }

        struct Unit {
            public char unit;
            public double unitValue;
            public Unit(char aUnit, double aUnitValue) {
                unit = aUnit;
                unitValue = aUnitValue;
            }
        };

        private Unit[] mUnits = new Unit[] { new Unit('k', 1000.0), new Unit('M', 1000.0 * 1000) };

        private string TrimUnitString(string s, out double unit) {
            unit = 1.0;

            s = s.Trim();
            if (s.Length == 0) {
                return s;
            }

            foreach (var item in mUnits) {
                if (s[s.Length - 1] == item.unit) {
                    s = s.TrimEnd(item.unit);
                    unit = item.unitValue;
                    return s;
                }
            }

            return s;
        }

        double mG0 = 0;
        double mGc = 0;
        double mGs = 0;
        double mFc = 0;
        double mFs = 0;
        ApproximationBase.BetaType mBetaType;

        private bool GetParametersFromUI() {
            double unit = 1.0;

            if (!double.TryParse(textBoxG0.Text, out mG0)) {
                MessageBox.Show("G0 parse error.");
                return false;
            }
            if (!double.TryParse(textBoxGc.Text, out mGc) || mG0 <= mGc) {
                MessageBox.Show("Gc parse error. mGc must be smaller than mG0");
                return false;
            }
            if (!double.TryParse(textBoxGs.Text, out mGs) || mGc <= mGs) {
                MessageBox.Show("Gs parse error. mGs must be smaller than mGc");
                return false;
            }

            string fcS = textBoxFc.Text;
            fcS = TrimUnitString(fcS, out unit);
            if (!double.TryParse(fcS, out mFc) || mFc <= 0) {
                MessageBox.Show("Fc parse error. Fc must be greater than 0");
                return false;
            }
            mFc *= unit;

            string fsS = textBoxFs.Text;
            fsS = TrimUnitString(fsS, out unit);
            if (!double.TryParse(fsS, out mFs)) {
                MessageBox.Show("Fs parse error. Fs must be number.");
                return false;
            }
            mFs *= unit;
            if (mFs <= 0 || mFs <= mFc) {
                MessageBox.Show("Fs parse error. Fs must be greater than Fc and greater than 0");
                return false;
            }

            mBetaType = ApproximationBase.BetaType.BetaMax;
            if (comboBoxOptimization.SelectedItem == comboBoxItemβmin) {
                mBetaType = ButterworthDesign.BetaType.BetaMin;
            }
            return true;
        }

        private void UpdateChebyshev() {
            mTextBoxLog.Clear();
            if (!GetParametersFromUI()) {
                return;
            }

            // Hz → rad/s
            double ωc = mFc * 2.0 * Math.PI;
            double ωs = mFs * 2.0 * Math.PI;

            // dB → linear
            double h0 = Math.Pow(10, mG0 / 20);
            double hc = Math.Pow(10, mGc / 20);
            double hs = Math.Pow(10, mGs / 20);

            var cd = new ChebyshevDesign(h0, hc, hs, ωc, ωs, mBetaType);
        }

        private void UpdateButterWorth() {
            mTextBoxLog.Clear();
            if (!GetParametersFromUI()) {
                return;
            }

            var afd = new AnalogFilterDesign();

            afd.DesignButterworthLowpass(mG0, mGc, mGs, mFc, mFs, mBetaType);

            mTextBoxLog.Clear();
            AddLog(string.Format("Order={0}, Beta={1}\n", afd.Order(), afd.Beta()));

            // 伝達関数の式をログに出力。
            AddLog(string.Format("Transfer function H(s) = "));
            for (int i = 0; i < afd.RealPolynomialCount(); ++i) {
                AddLog(string.Format("{0}", afd.RealPolynomialNth(i).ToString("s")));
                if (i != afd.RealPolynomialCount() -1) {
                    AddLog(" + ");
                }
            }
            AddLog("\n");

            // インパルス応答の式をログに出力。
            AddLog(("Impulse Response (frequency normalized): h(t) = "));
            for (int i = 0; i < afd.HPfdCount(); ++i) {
                var item = afd.HPfdNth(i);
                AddLog(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.N(0), item.D(0)));
                if (i != afd.HPfdCount() - 1) {
                    AddLog(" + ");
                }
            }
            AddLog("\n");

            // 周波数応答グラフに伝達関数をセット。
            mFrequencyResponse.TransferFunction = afd.TransferFunction;
            mFrequencyResponse.Update();

            // Pole-Zeroプロットにポールの位置をセット。
            mPoleZeroPlot.ClearPoleZero();
            mPoleZeroPlot.SetScale(afd.PoleNth(0).Magnitude());
            for (int i = 0; i < afd.Order(); ++i) {
                var p = afd.PoleNth(i);
                mPoleZeroPlot.AddPole(p);
            }
            mPoleZeroPlot.TransferFunction = afd.PoleZeroPlotTransferFunction;
            mPoleZeroPlot.Update();

            // 時間ドメインプロットの更新。
            mTimeDomainPlot.ImpulseResponseFunction = afd.ImpulseResponseFunction;
            mTimeDomainPlot.StepResponseFunction = afd.UnitStepResponseFunction;
            mTimeDomainPlot.TimeScale = afd.TimeDomainFunctionTimeScale;
            mTimeDomainPlot.Update();

            // アナログ回路表示。
            AddLog(string.Format("Analog Filter Stages = {0}\n", afd.RealPolynomialCount()));

            mAnalogFilterCircuit.Clear();
            mAnalogFilterCircuit.CutoffFrequencyHz = mFc;
            for (int i=0; i<afd.RealPolynomialCount(); ++i) {
                mAnalogFilterCircuit.Add(afd.RealPolynomialNth(i));
            }
            mAnalogFilterCircuit.AddFinished();
            mAnalogFilterCircuit.Update();
        }

    }
}
