// 日本語
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WWStringVibration {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private int mNumPoints = 32;
        private bool mInitialized = false;
        private List<double> mPointHeights = new List<double>();

        public MainWindow() {
            InitializeComponent();
        }

        private void CalcWave() {
            /*
            var p = new WWMath.WWComplex[mNumPoints];
            for (int i=0; i<mNumPoints; ++i) {
                p[i] = new WWMath.WWComplex(mPointHeights[i],0);
            }
            var fft = new WWMath.WWRadix2Fft(mNumPoints);
            var f = fft.ForwardFft(p);
            {
                int i = 0;
                foreach (var item in f) {
                    if (item != null) { 
                        Console.WriteLine("{0} {1} {2} {3}", i++, item.real, item.imaginary, item.Magnitude());
                    }
                }
            }
            */
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            UpdateNumControlPoints();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            UpdateNumControlPoints();
        }

        class SliderTag {
            public Label l;
            public int idx;
            public SliderTag(Label lbl, int aIdx) {
                l = lbl;
                idx = aIdx;
            }
        }

        private void UpdateNumControlPoints() {
            mNumPoints = (int)Math.Pow(2, (int)mSliderNumControlPoints.Value);
            mLabelNumOfControlPoints.Content = string.Format("Num Of Points ={0}", mNumPoints);

            // control pointの数を調整。
            while (mPointHeights.Count < mNumPoints) {
                mPointHeights.Add(0.0);
            }
            while (mNumPoints < mPointHeights.Count) {
                mPointHeights.RemoveAt(mPointHeights.Count - 1);
            }

            PointUpdated();

            // シミュレーション用の点列更新。
            UpdateCanvas();
        }

        private void UpdatePointPrint() {
            mStackPanel.Children.Clear();
            for (int i = 0; i < mNumPoints; ++i) {
                Label l = new Label();
                l.Content = string.Format("Point{0} height={1:g4}",
                    i + 1, mPointHeights[i]);
                mStackPanel.Children.Add(l);
            }
        }

        private void PointUpdated() {
            UpdatePointPrint();
            CalcWave();
        }

        private void UpdateCanvas() {
            double w = mCanvas.ActualWidth;
            double h = mCanvas.ActualHeight;
            int n = mPointHeights.Count;
            double spacing = w / (n + 1);
            //Console.WriteLine("w={0} h={1}", w, h);

            mCanvas.Children.Clear();
            for (int i = 0; i < n; ++i) {
                var e = new Ellipse();

                var color = new SolidColorBrush(Colors.Black);
                if (i == 0 || i == n - 1) {
                    color = new SolidColorBrush(Colors.Red);
                }
                e.Stroke = color;
                e.Fill = color;
                e.Width = 5;
                e.Height = 5;

                double pointHeight = h / 2 - mPointHeights[i];
                //Console.WriteLine("{0} {1}", i, pointHeight);
                Canvas.SetLeft(e, spacing * (i + 1));
                Canvas.SetTop(e, pointHeight);
                mCanvas.Children.Add(e);
            }
        }

        private void MCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Released) {
                return;
            }

            double w = mCanvas.ActualWidth;
            double h = mCanvas.ActualHeight;
            int n = mPointHeights.Count;
            double x = e.GetPosition(mCanvas).X;
            double y = e.GetPosition(mCanvas).Y;

            double spacing = (w / (n + 1));

            // 最も近い点の番号
            double xO = x - spacing / 2;

            int idx = (int)(xO / spacing);

            if (idx < 1) {
                idx = 1;
            }

            if (n-2 < idx) {
                idx = n - 2;
            }

            mPointHeights[idx] = h / 2 - y;
            PointUpdated();
            UpdateCanvas();
        }

        private void MButtonReset_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mPointHeights.Count; ++i) {
                mPointHeights[i] = 0;
            }
            PointUpdated();
            UpdateCanvas();
        }

        private void MButtonPreset1_Click(object sender, RoutedEventArgs e) {
            double h = mCanvas.ActualHeight;
            double h2 = h / 3;
            for (int i = 0; i < mNumPoints; ++i) {
                mPointHeights[i] = h2 * Math.Sin(2.0 * Math.PI * i / mNumPoints);
            }
            mPointHeights[0] = 0;
            mPointHeights[mNumPoints - 1] = 0;
            PointUpdated();
            UpdateCanvas();
        }
    }
}
