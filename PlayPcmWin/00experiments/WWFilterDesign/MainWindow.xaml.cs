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
            AddLog(string.Format("order={0}, β={1}\n", bwd.Order(), bwd.Beta()));

            double constant = bwd.TransferFunctionConstant();

            // 伝達関数をログに出力。
            AddLog(string.Format("Transfer function: H(s) = {0} / {{", constant));
            for (int i = 0; i < bwd.Order(); ++i) {
                var a = bwd.PoleNth(i);
                AddLog(string.Format("(s/ωc + {0})", WWComplex.Minus(a)));
            }
            AddLog("} \n");

            // 周波数応答グラフに伝達関数をセット。
            mFrequencyResponse.TransferFunction = (WWComplex s) => {
                WWComplex denominator = new WWComplex(1, 0);
                for (int i = 0; i < bwd.Order(); ++i) {
                    var a = bwd.PoleNth(i);
                    denominator.Mul(WWComplex.Sub(WWComplex.Div(s, ωc), a));
                }
                return WWComplex.Div(new WWComplex(constant, 0), denominator);
            };
            mFrequencyResponse.Update();

            // Pole-Zeroプロットにポールの位置をセット。
            mPoleZeroPlot.ClearPoleZero();
            mPoleZeroPlot.SetScale(bwd.PoleNth(0).Magnitude());
            for (int i = 0; i < bwd.Order(); ++i) {
                var p = bwd.PoleNth(i);
                mPoleZeroPlot.AddPole(p);
            }
            mPoleZeroPlot.TransferFunction = (WWComplex s) => {
                WWComplex denominator = new WWComplex(1, 0);
                for (int i = 0; i < bwd.Order(); ++i) {
                    var a = bwd.PoleNth(i);
                    denominator.Mul(WWComplex.Sub(s, a));
                }
                return WWComplex.Div(new WWComplex(constant, 0), denominator);
            };
            mPoleZeroPlot.Update();

            {
                // 伝達関数を部分分数展開する。
                var numeratorC = new List<WWComplex>();
                numeratorC.Add(new WWComplex(constant, 0));

                var H_Roots = new List<WWComplex>();
                var stepResponseTFRoots = new List<WWComplex>();
                for (int i = 0; i < bwd.Order(); ++i) {
                    var p = bwd.PoleNth(i);
                    H_Roots.Add(p);
                    stepResponseTFRoots.Add(p);
                }
                stepResponseTFRoots.Add(new WWComplex(0, 0));
                var H_PFD = WWPolynomial.PartialFractionDecomposition(numeratorC, H_Roots);
                var stepResponseTFPFD = WWPolynomial.PartialFractionDecomposition(numeratorC, stepResponseTFRoots);

                AddLog("Transfer function (After Partial Fraction Decomposition): H(s) = ");
                for (int i = 0; i < H_PFD.Count(); ++i) {
                    AddLog(H_PFD[i].ToString("s/ωc"));
                    if (i != H_PFD.Count - 1) {
                        AddLog(" + ");
                    }
                }
                AddLog("\n");

                AddLog("Unit Step Function (After Partial Fraction Decomposition): Yγ(s) = ");
                for (int i = 0; i < stepResponseTFPFD.Count(); ++i) {
                    AddLog(stepResponseTFPFD[i].ToString("s/ωc"));
                    if (i != stepResponseTFPFD.Count - 1) {
                        AddLog(" + ");
                    }
                }
                AddLog("\n");

                mTimeDomainPlot.ImpulseResponseFunction = (double t) => {
                    if (t <= 0) {
                        return 0;
                    }

                    // 逆ラプラス変換してインパルス応答関数を得る。
                    WWComplex result = new WWComplex(0,0);

                    foreach (var item in H_PFD) {
                        // numerator * exp(denominator * t)
                        result.Add(WWComplex.Mul(item.NumeratorCoeff(0),
                            new WWComplex(Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Cos(-t * item.DenominatorCoeff(0).imaginary),
                                          Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Sin(-t * item.DenominatorCoeff(0).imaginary))));
                    }

                    return result.real;
                };

                AddLog("Impulse Response (frequency normalized): h(t) = ");
                for (int i = 0; i < H_PFD.Count; ++i) {
                    var item = H_PFD[i];
                    AddLog(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.NumeratorCoeff(0), item.DenominatorCoeff(0)));
                    if (i != H_PFD.Count - 1) {
                        AddLog(" + ");
                    }
                }
                AddLog("\n");

                mTimeDomainPlot.StepResponseFunction = (double t) => {
                    if (t <= 0) {
                        return 0;
                    }

                    // 逆ラプラス変換してインパルス応答関数を得る。
                    WWComplex result = new WWComplex(0, 0);

                    foreach (var item in stepResponseTFPFD) {
                        if (item.DenominatorCoeff(0).EqualValue(new WWComplex(0, 0))) {
                            // b/s → b*u(t)
                            result.Add(item.NumeratorCoeff(0));
                        } else {
                            // b/(s-a) → b * exp(a * t)
                            result.Add(WWComplex.Mul(item.NumeratorCoeff(0),
                                new WWComplex(Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Cos(-t * item.DenominatorCoeff(0).imaginary),
                                              Math.Exp(-t * item.DenominatorCoeff(0).real) * Math.Sin(-t * item.DenominatorCoeff(0).imaginary))));
                        }
                    }

                    return result.real;
                };

                AddLog("Step Response (frequency normalized): y(t) = ");
                for (int i = 0; i < stepResponseTFPFD.Count; ++i) {
                    var item = stepResponseTFPFD[i];
                    if (item.DenominatorCoeff(0).EqualValue(new WWComplex(0, 0))) {
                        // b/s → b*u(t)
                        AddLog(string.Format("({0}) * u(t)", item.NumeratorCoeff(0)));
                    } else {
                        // b/(s-a) → b * exp(a * t)
                        AddLog(string.Format("({0}) * e^ {{ -t * ({1}) }}", item.NumeratorCoeff(0), item.DenominatorCoeff(0)));
                    }
                    if (i != stepResponseTFPFD.Count() - 1) {
                        AddLog(" + ");
                    }
                }
                AddLog("\n");

                mTimeDomainPlot.TimeScale = 1.0 / bwd.CutoffFrequency();
                mTimeDomainPlot.Update();

                // 共役複素数のペアを組み合わせて伝達関数の係数を全て実数にする。
                // s平面のjω軸から遠い項から並べる。
                var H_Real = new List<RationalPolynomial>();
                if ((H_PFD.Count() & 1) == 1) {
                    // 奇数。
                    int center = H_PFD.Count() / 2;
                    H_Real.Add(H_PFD[center]);
                    for (int i = 0; i < H_PFD.Count() / 2; ++i) {
                        H_Real.Add(WWPolynomial.Mul(
                            H_PFD[center - i -1], H_PFD[center + i + 1]));
                    }
                } else {
                    // 偶数。
                    int center = H_PFD.Count() / 2;
                    for (int i = 0; i < H_PFD.Count() / 2; ++i) {
                        H_Real.Add(WWPolynomial.Mul(
                            H_PFD[center - i-1], H_PFD[center + i]));
                    }
                }

                AddLog("Transfer function (real coefficients): H(s) = ");
                for (int i=0; i<H_Real.Count(); ++i) {
                    AddLog(H_Real[i].ToString("s"));
                    if (i != H_Real.Count() - 1) {
                        AddLog(" + ");
                    }
                }
                AddLog("\n");
            }


        }


    }
}
