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
            UpdateButterWorth();
        }

        private void UpdateButterWorth() {
            mTextBoxLog.Clear();

            double g0 = 0;
            double gc = 0;
            double gs = 0;
            double fc = 0;
            double fs = 0;

            if (!double.TryParse(textBoxG0.Text, out g0)) {
                MessageBox.Show("G0 parse error.");
                return;
            }
            if (!double.TryParse(textBoxGc.Text, out gc) || g0 <= gc) {
                MessageBox.Show("Gc parse error. gc must be smaller than g0");
                return;
            }
            if (!double.TryParse(textBoxGs.Text, out gs) || gc <= gs) {
                MessageBox.Show("Gs parse error. gs must be smaller than gc");
                return;
            }

            if (!double.TryParse(textBoxFc.Text, out fc) || fc <= 0) {
                MessageBox.Show("Fc parse error. Fc must be greater than 0");
                return;
            }

            if (!double.TryParse(textBoxFs.Text, out fs) || fs <= 0 || fs <= fc) {
                MessageBox.Show("Fs parse error. Fs must be greater than Fc and greater than 0");
                return;
            }

            var betaType = ButterworthDesign.BetaType.BetaMax;
            if (comboBoxOptimization.SelectedItem == comboBoxItemβmin) {
                betaType = ButterworthDesign.BetaType.BetaMin;
            }

            var afd = new AnalogFilterDesign();

            afd.DesignButterworthLowpass(g0, gc, gs, fc, fs, betaType);

            mTextBoxLog.Clear();
            AddLog(string.Format("Order={0}, Beta={1}\n", afd.Order(), afd.Beta()));

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
            for (int i=0; i<afd.RealPolynomialCount(); ++i) {
                mAnalogFilterCircuit.Add(afd.RealPolynomialNth(i));
            }
            mAnalogFilterCircuit.Update();
        }
    }
}
