using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WWAudioFilter;
using System.Collections.Generic;
using System.Windows.Shapes;
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

        private double [] mNumer = new double[9];
        private double [] mDenom = new double[9];


        private WWComplex TransferFunction(WWComplex z) {
            var zRecip = new WWComplex(z).Reciprocal();

            var zRecip2 = new WWComplex(zRecip).Mul(zRecip);
            var zRecip3 = new WWComplex(zRecip2).Mul(zRecip);
            var zRecip4 = new WWComplex(zRecip3).Mul(zRecip);
            var zRecip5 = new WWComplex(zRecip4).Mul(zRecip);
            var zRecip6 = new WWComplex(zRecip5).Mul(zRecip);
            var zRecip7 = new WWComplex(zRecip6).Mul(zRecip);
            var zRecip8 = new WWComplex(zRecip7).Mul(zRecip);

            var hDenom0 = new WWComplex(mDenom[0], 0.0f);
            var hDenom1 = new WWComplex(mDenom[1], 0.0f).Mul(zRecip);
            var hDenom2 = new WWComplex(mDenom[2], 0.0f).Mul(zRecip2);
            var hDenom3 = new WWComplex(mDenom[3], 0.0f).Mul(zRecip3);
            var hDenom4 = new WWComplex(mDenom[4], 0.0f).Mul(zRecip4);
            var hDenom5 = new WWComplex(mDenom[5], 0.0f).Mul(zRecip5);
            var hDenom6 = new WWComplex(mDenom[6], 0.0f).Mul(zRecip6);
            var hDenom7 = new WWComplex(mDenom[7], 0.0f).Mul(zRecip7);
            var hDenom8 = new WWComplex(mDenom[8], 0.0f).Mul(zRecip8);
            var hDenom = new WWComplex(hDenom0).Add(hDenom1).Add(hDenom2).Add(hDenom3).Add(hDenom4).Add(hDenom5).Add(hDenom6).Add(hDenom7).Add(hDenom8).Reciprocal();

            var hNumer0 = new WWComplex(mNumer[0], 0.0f);
            var hNumer1 = new WWComplex(mNumer[1], 0.0f).Mul(zRecip);
            var hNumer2 = new WWComplex(mNumer[2], 0.0f).Mul(zRecip2);
            var hNumer3 = new WWComplex(mNumer[3], 0.0f).Mul(zRecip3);
            var hNumer4 = new WWComplex(mNumer[4], 0.0f).Mul(zRecip4);
            var hNumer5 = new WWComplex(mNumer[5], 0.0f).Mul(zRecip5);
            var hNumer6 = new WWComplex(mNumer[6], 0.0f).Mul(zRecip6);
            var hNumer7 = new WWComplex(mNumer[7], 0.0f).Mul(zRecip7);
            var hNumer8 = new WWComplex(mNumer[8], 0.0f).Mul(zRecip8);
            var hNumer = new WWComplex(hNumer0).Add(hNumer1).Add(hNumer2).Add(hNumer3).Add(hNumer4).Add(hNumer5).Add(hNumer6).Add(hNumer7).Add(hNumer8);
            var h = new WWComplex(hNumer).Mul(hDenom);

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
