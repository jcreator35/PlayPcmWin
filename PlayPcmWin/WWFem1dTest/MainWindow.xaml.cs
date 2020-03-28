using System;
using System.Windows;
using WWMath;

namespace WWFem1dTest {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void Calc() {
            int N = 0;
            if (!int.TryParse(textBoxNumElems.Text, out N) || N <= 0) {
                MessageBox.Show("Error: Num of Elems should be integer larger than 0");
                return;
            }

            // 試験関数Vkとf(x)との積分Fk=∫f(x)Vk dxを求める。
            var f = new double[N];
            for (int i = 0; i < N; ++i) {
                double x = (double)(i + 1) / N;
                double y = mGraphFx.Sample(x);

                // 最後の区間は半区間。
                double Vk = (i == N - 1) ? (1.0 / N / 2) : (1.0 / N);

                f[i] = y * Vk;
            }

            // 合成行列K
            // Kij = ∫c(x)dVi/dx dVj/dx dx
            // i=j=0のとき、dVi/dxは、
            //   0≦x＜1/N  の区間でVi'=Vj'= +N → Vi'*Vj' = N^2
            //   1/N≦x＜2/Nの区間でVi'=Vj'= -N → Vi'*Vj' = N^2
            //                            区間    値
            //   ∫c(x)dVi/dx dVj/dx dx = (2/N) * N^2
            // i=0,j=1のとき
            //   0≦x＜1/N  の区間でVi'=+N, Vj'= 0 → Vi'Vj' = 0
            //   1/N≦x＜2/Nの区間でVi'=-N, Vj'= N → Vi'Vj' = -N^2
            //   2/N≦x＜3/Nの区間でVi'= 0, Vj'=-N → Vi'Vj' = 0
            //                            区間     値
            //   ∫c(x)dVi/dx dVj/dx dx = (1/N) * (-N^2)
            // i=N-1, j=N-1のとき
            //   N-1/N≦x＜1の区間でVi'=Vj'=N → Vi'Vj' = N^2
            //                            区間     値
            //   ∫c(x)dVi/dx dVj/dx dx = (1/N) * (N^2)
            var Kij = new double[N * N];
            for (int i = 0; i < N; ++i) {
                for (int j = 0; j < N; ++j) {
                    int pos = i * N + j;
                    double x = (double)(i + 1) / N;
                    double c = mGraphCx.Sample(x);

                    if (i == N - 1 && j == N - 1) {
                        Kij[pos] = c * (double)(1.0 / N) * N * N;
                    } else if (i == j) {
                        Kij[pos] = c * (double)(2.0 / N) * N * N;
                    } else if (Math.Abs(i - j) == 1) {
                        Kij[pos] = c * (double)(1.0 / N) * (-N * N);
                    } else {
                        Kij[pos] = 0;
                    }
                }
            }

            var K = new WWMatrix(N, N, Kij);

            // Ku=fを解いてUを求める。
            var u = WWLinearEquation.SolveKu_eq_f(K, f);

            {   // 求まったUをグラフにする。
                mGraphUx.SetArbitraryFunctionStart();

                // U(0) = 0 : 境界条件。
                mGraphUx.SetArbitraryFunctionPoint(0, 0);

                for (int i = 0; i < u.Length; ++i) {
                    double x = (double)(i+1)/N;
                    double y = u[i];
                    mGraphUx.SetArbitraryFunctionPoint(x, y);
                }

                mGraphUx.SetArbitraryFunctionEnd();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Calc();
        }

        private void buttonCalcUx_Click(object sender, RoutedEventArgs e) {
            Calc();
        }

    }
}
