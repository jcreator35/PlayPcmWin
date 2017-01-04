using System.Windows;
using WWMath;

namespace PolynomialVisualize {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
            mFrequencyResponse.Mode = WWUserControls.FrequencyResponse.ModeType.ZPlane;
            mPoleZeroPlot.Mode = WWUserControls.PoleZeroPlot.ModeType.ZPlane;
            Reset();
        }

        private bool mInitialized = false;

        private const int ORDER_PLUS_1 = 9;

        private double [] mNumer = new double[ORDER_PLUS_1];
        private double [] mDenom = new double[ORDER_PLUS_1];

        private WWComplex TransferFunction(WWComplex z) {
            var zR = WWComplex.Reciprocal(z);
            var zRecip = new WWComplex[ORDER_PLUS_1];
            zRecip[0] = WWComplex.Unity();
            for (int i=1; i < zRecip.Length; ++i) {
                zRecip[i] = WWComplex.Mul(zRecip[i - 1], zR);
            }

            var denom = WWComplex.Zero();
            for (int i=0; i < zRecip.Length; ++i) {
                denom = WWComplex.Add(denom, WWComplex.Mul(new WWComplex(mDenom[i], 0), zRecip[i]));
            }
            var numer = WWComplex.Zero();
            for (int i=0; i < zRecip.Length; ++i) {
                numer = WWComplex.Add(numer, WWComplex.Mul(new WWComplex(mNumer[i], 0), zRecip[i]));
            }

            var h = WWComplex.Div(numer, denom);

            // 孤立特異点や極で起きる異常を適当に除去する
            if (double.IsNaN(h.Magnitude())) {
                return new WWComplex(0.0f, 0.0f);
            }
            return h;
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            mNumer[0] = double.Parse(textBoxN0.Text);
            mNumer[1] = double.Parse(textBoxN1.Text);
            mNumer[2] = double.Parse(textBoxN2.Text);
            mNumer[3] = double.Parse(textBoxN3.Text);
            mNumer[4] = double.Parse(textBoxN4.Text);
            mNumer[5] = double.Parse(textBoxN5.Text);
            mNumer[6] = double.Parse(textBoxN6.Text);
            mNumer[7] = double.Parse(textBoxN7.Text);
            mNumer[8] = double.Parse(textBoxN8.Text);

            mDenom[0] = 1.0;
            mDenom[1] = double.Parse(textBoxD1.Text);
            mDenom[2] = double.Parse(textBoxD2.Text);
            mDenom[3] = double.Parse(textBoxD3.Text);
            mDenom[4] = double.Parse(textBoxD4.Text);
            mDenom[5] = double.Parse(textBoxD5.Text);
            mDenom[6] = double.Parse(textBoxD6.Text);
            mDenom[7] = double.Parse(textBoxD7.Text);
            mDenom[8] = double.Parse(textBoxD8.Text);

            mFrequencyResponse.TransferFunction = TransferFunction;
            mFrequencyResponse.NyquistFrequency = double.Parse(textBoxNyquistFrequency.Text);
            mFrequencyResponse.Update();

            mPoleZeroPlot.TransferFunction = TransferFunction;
            mPoleZeroPlot.Update();
        }

        private void Reset() {
            textBoxN0.Text = "1";
            textBoxN1.Text = "0";
            textBoxN2.Text = "0";
            textBoxN3.Text = "0";
            textBoxN4.Text = "0";
            textBoxN5.Text = "0";
            textBoxN6.Text = "0";
            textBoxN7.Text = "0";
            textBoxN8.Text = "0";
            textBoxD1.Text = "-0.9";
            textBoxD2.Text = "0";
            textBoxD3.Text = "0";
            textBoxD4.Text = "0";
            textBoxD5.Text = "0";
            textBoxD6.Text = "0";
            textBoxD7.Text = "0";
            textBoxD8.Text = "0";
            Update();
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e) {
            Update();
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e) {
            Reset();
        }
    }
}
