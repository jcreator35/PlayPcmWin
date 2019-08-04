// 日本語
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text;
using System.Globalization;

namespace WWStringVibration {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private int mNumPoints = 33;
        private bool mInitialized = false;
        private List<double> mPointHeights = new List<double>();

        private double[] mFreqComponent = null;
        private double mTimeNow = 0;
        private double mTimeTick = 0;
        private double mC = 0;
        DispatcherTimer mDispatcherTimer = new DispatcherTimer();

        public MainWindow() {
            InitializeComponent();
            mDispatcherTimer.Tick += DispatcherTimer_Tick;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
            
            Title = string.Format(CultureInfo.CurrentCulture, "WWStringVibration version {0}", AssemblyVersion);

            UpdateNumControlPoints();

            for (int i=0; i<mNumPoints/2; ++i) {
                mPointHeights[i] = Math.Sin(2.0 * Math.PI * i / mNumPoints);
            }

            PointUpdated();
        }

        private void UpdateFreqComponent() {
            double w = mCanvas.ActualWidth;
            double h = mCanvas.ActualHeight;
            double spacing = w / (mNumPoints + 1);
            int N = mNumPoints - 2;

            var p = new double[N];
            for (int i = 0; i < N; ++i) {
                double x = mPointHeights[i + 1];
                p[i] = x;
            }
            var dst = new WWMath.DiscreteSineTransform();
            mFreqComponent = dst.ForwardDST1(p);

            for (int i = 0; i < mFreqComponent.Length; ++i) {
                mFreqComponent[i] /= (N + 1) / 2;
            }
        }

        /// <summary>
        /// 数式の文字表現を表示。
        /// </summary>
        private void DisplayPointDesc() {
            var sb = new StringBuilder();

            sb.Append("y = ");

            int N = mNumPoints - 2;
            for (int i = 0; i < N; ++i) {
                if (0.0001 < Math.Abs(mFreqComponent[i])) {
                    sb.Append(mFreqComponent[i].ToString("+0.0000;- 0.0000;0"));
                    sb.AppendFormat(" sin({0}πx) cos({1}πct) ", i+1, i+1);
                }
            }

            if (sb.Length == 4) {
                sb.Append("0");
            }

            mTextBoxStatus.Text = sb.ToString();
        }

        /// <summary>
        /// 画面に点を表示。
        /// </summary>
        private void RedrawPoints() {
            double w = mCanvas.ActualWidth;
            double h = mCanvas.ActualHeight;
            int n = mPointHeights.Count;
            double spacing = w / (n + 1);
            //Console.WriteLine("w={0} h={1}", w, h);

            for (int i = 0; i < n; ++i) {
                var e = new Ellipse();

                var color = new SolidColorBrush(Colors.Black);
                if (i == 0 || i == n - 1) {
                    color = new SolidColorBrush(Colors.Black);
                }

                double halfSize = 3;

                e.Stroke = color;
                e.Fill = color;
                e.Width = halfSize * 2;
                e.Height = halfSize * 2;

                double pointHeight = h / 2 - (h / 3) * mPointHeights[i];
                //Console.WriteLine("{0} {1}", i, pointHeight);
                Canvas.SetLeft(e, -halfSize + spacing * (i + 1));
                Canvas.SetTop(e, -halfSize + pointHeight);
                mCanvas.Children.Add(e);
            }
        }

        private void RedrawWave() {
            double w = mCanvas.ActualWidth;
            double h = mCanvas.ActualHeight;
            double spacing = w / (mNumPoints + 1);
            int N = mNumPoints - 2;

            int count = (int)((w - spacing * 2) / 3);
            for (int i=0; i<count; ++i) {
                double x1 = spacing + i*3;
                double x2 = spacing + (i+1) * 3;

                double x1M = (double)i / count;
                double x2M = (double)(i+1) / count;

                double y1M = 0;
                double y2M = 0;
                for (int j = 0; j < N; ++j) {
                    y1M += Math.Sin(Math.PI * (j + 1) * i / count) * mFreqComponent[j]
                        *  Math.Cos(Math.PI * (j + 1) * mC * mTimeNow);
                    y2M += Math.Sin(Math.PI * (j + 1) * (i + 1) / count) * mFreqComponent[j]
                        *  Math.Cos(Math.PI * (j + 1) * mC * mTimeNow);
                }

                // 画面サイズに合わせてスケールする。
                double y1 = h / 2 - y1M * (h / 3);
                double y2 = h / 2 - y2M * (h / 3);

                var l = new Line {
                    X1 = x1,
                    X2 = x2,
                    Y1 = y1,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(l,0);
                Canvas.SetTop(l,0);
                mCanvas.Children.Add(l);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

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
            mNumPoints = 1+ (int)Math.Pow(2, (int)mSliderNumControlPoints.Value);
            mLabelNumOfControlPoints.Content = string.Format("Num Of Points ={0}", mNumPoints);

            // control pointの数を調整。
            mPointHeights.Clear();
            while (mPointHeights.Count < mNumPoints) {
                mPointHeights.Add(0.0);
            }

            // シミュレーション用の点列更新。
            PointUpdated();
        }

        private void PointUpdated() {
            mPointHeights[0] = 0;
            mPointHeights[mNumPoints - 1] = 0;

            UpdateFreqComponent();
            DisplayPointDesc();
            RedrawCanvas();
        }

        private void RedrawCanvas() {
            mCanvas.Children.Clear();
            RedrawWave();
            RedrawPoints();
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

            mPointHeights[idx] = (h / 2 - y) / (h/3);
            PointUpdated();
        }

        private void MButtonReset_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mPointHeights.Count; ++i) {
                mPointHeights[i] = 0;
            }
            PointUpdated();
        }

        private void MButtonPreset1_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mNumPoints; ++i) {
                mPointHeights[i] = Math.Sin(Math.PI * i / (mNumPoints-1));
            }
            PointUpdated();
        }
        private void MButtonPreset2_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mNumPoints; ++i) {
                mPointHeights[i] = Math.Sin(2.0 * Math.PI * i / (mNumPoints - 1));
            }
            PointUpdated();
        }
        private void MButtonPreset3_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mNumPoints; ++i) {
                mPointHeights[i] = Math.Sin(4.0 * Math.PI * i / (mNumPoints - 1));
            }
            PointUpdated();
        }

        private void MButtonTriangle1_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mNumPoints/2; ++i) {
                mPointHeights[i]              = (double)i / (mNumPoints / 2);
                mPointHeights[mNumPoints-i-1] = (double)i / (mNumPoints / 2);
            }
            mPointHeights[mNumPoints / 2] = 1;

            PointUpdated();
        }

        private void MButtonSquare2_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mNumPoints; ++i) {
                mPointHeights[i] = (i/((mNumPoints+3)/4) & 1) == 0 ? 1 : -1;
            }

            PointUpdated();
        }

        private void MButtonHump_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < mNumPoints; ++i) {
                mPointHeights[i] = 0;
            }

            for (int i = 0; i < mNumPoints/4; ++i) {
                mPointHeights[i+mNumPoints/4] = Math.Sin(Math.PI * i / (mNumPoints/4));
            }

            PointUpdated();
        }

        private void MButtonStart_Click(object sender, RoutedEventArgs e) {
            if (!double.TryParse(mTextBoxTimeStep.Text, out mTimeTick)) {
                mTextBoxTimeStep.Text = "0.1";
                mTimeTick = 0.1;
            }

            if (!double.TryParse(mTextBoxConstant.Text, out mC)) {
                mTextBoxConstant.Text = "1.0";
                mC = 1.0;
            }

            mTimeNow = 0;

            mDispatcherTimer.Interval = new TimeSpan((long)(mTimeTick * 1000 * 1000 * 10));
            mDispatcherTimer.Start();
        }

        private void MButtonStop_Click(object sender, RoutedEventArgs e) {
            mDispatcherTimer.Stop();
            mTimeNow = 0;
            mLabelTime.Content = "t = 0";
            RedrawCanvas();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            mDispatcherTimer.Stop();
            mDispatcherTimer = null;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e) {
            mTimeNow += mTimeTick;
            mLabelTime.Content = string.Format("t = {0:0.00}", mTimeNow);
            RedrawCanvas();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            RedrawCanvas();
        }

    }
}
