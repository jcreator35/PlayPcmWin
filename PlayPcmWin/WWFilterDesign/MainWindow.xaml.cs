using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using WWAnalogFilterDesign;
using WWMath;

namespace WWAudioFilter {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool mInitialized = false;

        public MainWindow() {
            InitializeComponent();

            LocalizeUI();
            mInitialized = true;
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
            radioButtonFilterTypeChebyshev.Content = Properties.Resources.Chebyshev;
            radioButtonFilterTypePascal.Content = Properties.Resources.Pascal;
            radioButtonFilterTypeInverseChebyshev.Content = Properties.Resources.InverseChebyshev;
            radioButtonFilterTypeCauer.Content = Properties.Resources.CauerElliptic;
            labelOptimization.Content = Properties.Resources.Optimization + ":";
            buttonUpdate.Content = Properties.Resources.Update;

            comboBoxItemβmax.Content = Properties.Resources.Stopband;
            comboBoxItemβmin.Content = Properties.Resources.Passband;
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e) {
            Update();
        }

        private void AddLog(string s) {
            mTextBoxLog.AppendText(s);
            mTextBoxLog.ScrollToEnd();
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

        private double mG0 = 0;
        private double mGc = 0;
        private double mGs = 0;
        private double mFc = 0;
        private double mFs = 0;
        private ApproximationBase.BetaType mBetaType;
        private AnalogFilterDesign.FilterType mFilterType;
        private double mSamplingFrequency = 176400;

        private bool TryParseNumberWithUnit(string s, out double number) {
            double unit = 1.0;
            number = 0.0;

            var sNumber = TrimUnitString(s, out unit);
            if (!double.TryParse(sNumber, out number)) {
                //MessageBox.Show(string.Format("{0} parse error.",s));
                return false;
            }
            number *= unit;
            return true;
        }

        private bool GetParametersFromUI() {
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

            if (!TryParseNumberWithUnit(textBoxFc.Text, out mFc) || mFc <= 0) {
                MessageBox.Show("Fc parse error. Fc must be greater than 0");
                return false;
            }

            if (!TryParseNumberWithUnit(textBoxFs.Text, out mFs)) {
                MessageBox.Show("Fs parse error. Fs must be number.");
                return false;
            }

            if (mFs <= 0 || mFs <= mFc) {
                MessageBox.Show("Fs parse error. Fs must be greater than Fc and greater than 0");
                return false;
            }

            mBetaType = ApproximationBase.BetaType.BetaMax;
            if (comboBoxOptimization.SelectedItem == comboBoxItemβmin) {
                mBetaType = ButterworthDesign.BetaType.BetaMin;
            }

            mFilterType = AnalogFilterDesign.FilterType.Butterworth;
            if (radioButtonFilterTypeChebyshev.IsChecked == true) {
                mFilterType = AnalogFilterDesign.FilterType.Chebyshev;
            }
            if (radioButtonFilterTypePascal.IsChecked == true) {
                mFilterType = AnalogFilterDesign.FilterType.Pascal;
            }
            if (radioButtonFilterTypeInverseChebyshev.IsChecked == true) {
                mFilterType = AnalogFilterDesign.FilterType.InverseChebyshev;
            }
            if (radioButtonFilterTypeCauer.IsChecked == true) {
                mFilterType = AnalogFilterDesign.FilterType.Cauer;
            }

            if (!TryParseNumberWithUnit(textBoxSamplingFrequency.Text, out mSamplingFrequency) || mSamplingFrequency <= 0) {
                MessageBox.Show("SamplingFrequency parse error. it must be greater than 0");
                return false;
            }

            return true;
        }

        private AnalogFilterDesign mAfd;

        class BackgroundCalcResult {
            public string message;
            public bool success;
        };

        BackgroundWorker mBw = new BackgroundWorker();

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //WWPolynomial.Test();
            //ChebyshevDesign.Test();
            //Functions.Test();

            mBw.DoWork += new DoWorkEventHandler(CalcFilter);
            mBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CalcFilterCompleted);
            Update();
        }

        void CalcFilterCompleted(object sender, RunWorkerCompletedEventArgs e) {
            mMainPanel.IsEnabled = true;
            mTextBlockCalculating.Visibility = System.Windows.Visibility.Collapsed;
            mTextBoxLog.Clear();

            var r = e.Result as BackgroundCalcResult;
            if (!r.success) {
                MessageBox.Show(r.message);
                return;
            }

            AddLog(string.Format("Order={0}\n", mAfd.Order()));

            // 伝達関数の式をログに出力。
            AddLog("Transfer function (factorized, normalized) H(s) = ");
            AddLog(string.Format("{0:G4}", mAfd.NumeratorConstant()));
            for (int i = 0; i < mAfd.NumOfZeroes(); ++i) {
                AddLog(string.Format(" (s + {0})", WWComplex.Minus(mAfd.ZeroNth(i))));
            }
            AddLog(" /");
            for (int i = 0; i < mAfd.NumOfPoles(); ++i) {
                AddLog(string.Format(" (s + {0})", WWComplex.Minus(mAfd.PoleNth(i))));
            }
            AddLog("\n");

            {   // Show expanded Transfer function
                var zeroList = new List<WWComplex>();
                for (int i = 0; i < mAfd.NumOfZeroes(); ++i) {
                    zeroList.Add(mAfd.ZeroNth(i));
                }

                var poleList = new List<WWComplex>();
                for (int i = 0; i < mAfd.NumOfPoles(); ++i) {
                    poleList.Add(mAfd.PoleNth(i));
                }

                var numer = WWPolynomial.RootListToCoeffList(zeroList,
                    new WWComplex(mAfd.NumeratorConstant(), 0));
                var denom = WWPolynomial.RootListToCoeffList(poleList, WWComplex.Unity());

                AddLog("Transfer function (expanded, normalized) H(s) = {");
                AddLog(WWPolynomial.CoeffListToString(numer, "s"));
                AddLog(" } / {");
                AddLog(WWPolynomial.CoeffListToString(denom, "s"));
                AddLog(" }\n");
            }

            AddLog(string.Format("Transfer function with real coefficients H(s) = "));
            for (int i = 0; i < mAfd.RealPolynomialCount(); ++i) {
                AddLog(mAfd.RealPolynomialNth(i).ToString("(s/ωc)", "i"));
                if (i != mAfd.RealPolynomialCount() - 1) {
                    AddLog(" + ");
                }
            }
            AddLog("\n");

            // インパルス応答の式をログに出力。
            var H_s = new List<FirstOrderRationalPolynomial>();
            AddLog(("Impulse Response (frequency normalized): h(t) = "));
            for (int i = 0; i < mAfd.HPfdCount(); ++i) {
                var p = mAfd.HPfdNth(i);
                H_s.Add(p);

                if (!p.N(1).EqualValue(WWComplex.Zero())) {
                    throw new System.NotImplementedException();
                }

                if (p.D(1).EqualValue(WWComplex.Zero())
                        && p.D(0).EqualValue(WWComplex.Unity())) {
                    // 1 → δ(t)
                    AddLog(string.Format("{0:G4} * δ(t)", p.N(0)));
                } else if (p.D(0).EqualValue(WWComplex.Zero())) {
                    // b/s → b*u(t)
                    AddLog(string.Format("{0:G4} * u(t)", p.N(0)));
                } else {
                    // b/(s-a) ⇒ b * exp(t * a)
                    AddLog(string.Format("({0}) * e^ {{ t * ({1}) }}", p.N(0), WWComplex.Minus(p.D(0))));
                }

                if (i != mAfd.HPfdCount() - 1) {
                    AddLog(" + ");
                }
            }
            AddLog("\n");

            // 周波数応答グラフに伝達関数をセット。
            mFrequencyResponseS.TransferFunction = mAfd.TransferFunction;
            mFrequencyResponseS.Update();

            // Pole-Zeroプロットに極と零の位置をセット。
            mPoleZeroPlotS.ClearPoleZero();

            double scale = mAfd.PoleNth(0).Magnitude();
            if (0 < mAfd.NumOfZeroes() && scale < mAfd.ZeroNth(0).Magnitude()) {
                scale = mAfd.ZeroNth(0).Magnitude();
            }
            mPoleZeroPlotS.SetScale(scale);
            for (int i = 0; i < mAfd.NumOfPoles(); ++i) {
                var p = mAfd.PoleNth(i);
                mPoleZeroPlotS.AddPole(p);
            }
            for (int i = 0; i < mAfd.NumOfZeroes(); ++i) {
                var p = mAfd.ZeroNth(i);
                mPoleZeroPlotS.AddZero(p);
            }
            mPoleZeroPlotS.TransferFunction = mAfd.PoleZeroPlotTransferFunction;
            mPoleZeroPlotS.Update();

            // 時間ドメインプロットの更新。
            mTimeDomainPlot.ImpulseResponseFunction = mAfd.ImpulseResponseFunction;
            mTimeDomainPlot.StepResponseFunction = mAfd.UnitStepResponseFunction;
            mTimeDomainPlot.TimeScale = mAfd.TimeDomainFunctionTimeScale;
            mTimeDomainPlot.Update();

            // アナログ回路表示。
            AddLog(string.Format("Analog Filter Stages = {0}\n", mAfd.RealPolynomialCount()));

            mAnalogFilterCircuit.Clear();

            {
                groupBoxAFC.Visibility = System.Windows.Visibility.Visible;
                mAnalogFilterCircuit.CutoffFrequencyHz = mFc;
                for (int i = 0; i < mAfd.RealPolynomialCount(); ++i) {
                    mAnalogFilterCircuit.Add(mAfd.RealPolynomialNth(i));
                }
                mAnalogFilterCircuit.AddFinished();
                mAnalogFilterCircuit.Update();
            }
        }

        void CalcFilter(object sender, DoWorkEventArgs e) {
            var r = new BackgroundCalcResult();
            e.Result = r;

            r.success = false;
            r.message = "unknown error";

            mAfd = new AnalogFilterDesign();
            try {
                mAfd.DesignLowpass(mG0, mGc, mGs, mFc, mFs, mFilterType, mBetaType);
                r.success = true;
            } catch (System.ArgumentOutOfRangeException ex) {
                r.message = string.Format("Design failed! {0}", ex);
                return;
            } catch (System.Exception ex) {
                r.message = string.Format("BUG: should be fixed! {0}", ex);
                return;
            }
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }
            if (!GetParametersFromUI()) {
                return;
            }

            mMainPanel.IsEnabled = false;
            mTextBlockCalculating.Visibility = System.Windows.Visibility.Visible;
            mBw.RunWorkerAsync();
        }

    }
}
