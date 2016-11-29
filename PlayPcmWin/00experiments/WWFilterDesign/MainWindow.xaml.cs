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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            mTextBoxLog.Clear();

            double g0 = 0;
            double gc = 0;
            double gs = 0;
            double ωc = 0;
            double ωs = 0;

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

            if (!double.TryParse(textBoxFc.Text, out ωc) || ωc <= 0) {
                MessageBox.Show("Fc parse error. Fc must be greater than 0");
                return;
            }

            if (!double.TryParse(textBoxFs.Text, out ωs) || ωs <= 0 || ωs <= ωc) {
                MessageBox.Show("Fs parse error. Fs must be greater than Fc and greater than 0");
                return;
            }

            var betaType = ButterworthDesign.BetaType.BetaMax;
            if (comboBoxOptimization.SelectedItem == comboBoxItemβmin) {
                betaType = ButterworthDesign.BetaType.BetaMin;
            }

            // Hz → rad/s
            ωc *= 2.0 * Math.PI;
            ωs *= 2.0 * Math.PI;

            double h0 = Math.Pow(10, g0 / 20);
            double hc = Math.Pow(10, gc / 20);
            double hs = Math.Pow(10, gs / 20);

            var bwd = new ButterworthDesign(h0, hc, hs, ωc, ωs, betaType);
            AddLog(string.Format("order={0}\nCoeffs", bwd.Order()));

            double constant = bwd.TransferFunctionConstant();

            mPoleZeroPlot.ClearPoleZero();
            mPoleZeroPlot.SetScale(bwd.PoleNth(0).Magnitude());
            for (int i = 0; i < bwd.Order(); ++i) {
                var p = bwd.PoleNth(i);
                AddLog(string.Format("  {0}\nCoeffs", p));
                mPoleZeroPlot.AddPole(p);
            }

            // 逆ラプラス変換する。
            {
                var nPolynomialCoeffs = new List<WWComplex>();
                nPolynomialCoeffs.Add(new WWComplex(constant, 0));

                var dRoots = new List<WWComplex>();
                for (int i = 0; i < bwd.Order(); ++i) {
                    var p = bwd.PoleNth(i);
                    dRoots.Add(p);
                }
                var polynomialList = InverseLaplaceTransform.PartialFractionDecomposition(nPolynomialCoeffs, dRoots);

                for (int i = 0; i < polynomialList.Count; ++i) {
                    Console.WriteLine(polynomialList[i].ToString("s"));
                    if (i != polynomialList.Count - 1) {
                        Console.WriteLine(" + ");
                    }
                }
                Console.WriteLine("");
            }
        }


    }
}
