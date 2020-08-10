using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows.Media;
using System;

namespace WWUserControls {
    /// <summary>
    /// Interaction logic for TimeDomainPositiveValue1dUC.xaml
    /// </summary>
    public partial class TimeDomainPositiveValue1dUC : UserControl {
        private double mXMax = 10;
        private double mYMax = double.MinValue;
        private double mYMin = double.MaxValue;
        private bool mInitialized = false;
        private List<Line> mLineList = new List<Line>();
        private List<TimeVal> mTimeValList = new List<TimeVal>();

        private class TimeVal {
            public double timeSec;
            public double val;

            public TimeVal(double aTimeSec, double aVal) {
                timeSec = aTimeSec;
                val = aVal;
            }
        };

        /// <summary>
        /// グラフタイトルをセットする。
        /// </summary>
        public string Title {
            get { return labelTitle.Content as string; }
            set { labelTitle.Content = value; }
        }

        /// <summary>
        /// X軸の説明。
        /// </summary>
        public string XLabel {
            get { return labelX.Content as string; }
            set { labelX.Content = value; }
        }

        /// <summary>
        /// Y軸の説明。
        /// </summary>
        public string YLabel {
            get { return labelY.Content as string; }
            set { labelY.Content = value; }
        }

        public TimeDomainPositiveValue1dUC() {
            InitializeComponent();

            mInitialized = true;
        }

        /// <summary>
        /// 貯めこんだグラフ描画データを消す。
        /// </summary>
        public void Reset() {
            mTimeValList.Clear();
            mXMax = 10;

            mYMax = double.MinValue;
            mYMin = double.MaxValue;

            Update();
        }

        /// <summary>
        /// 描画スレッドで呼ぶ。
        /// </summary>
        public void UpdateLatestValue(double timeSec, double val) {
            // 最大値、最小値更新。
            if (mYMax < val) {
                mYMax = val;
            }
            if (val < mYMin) {
                mYMin = val;
            }
            if (mYMax == mYMin) {
                mYMax = mYMin + 0.001;
            }

            if (mXMax < timeSec) {
                mXMax = timeSec;
            }

            var timeVal = new TimeVal(timeSec, val);
            mTimeValList.Add(timeVal);

            Update();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            Update();
        }

        private const int ORIGINAL_W = 600;
        private const int ORIGINAL_H = 350;
        private const int GRAPH_LEFT = 64;
        private const int GRAPH_HEIGHT = 256;
        private const int GRAPH_WIDTH = 500;
        private const int GRAPH_TOP = 32;
        private const int GRAPH_BOTTOM = GRAPH_TOP + GRAPH_HEIGHT;

        private static void LineSetPos(Line l, double x1, double y1, double x2, double y2) {
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            if (0 < mTimeValList.Count) {
                // update label values
                labelX25.Content = string.Format("{0:0.0}", mXMax * 0.25);
                labelX50.Content = string.Format("{0:0.0}", mXMax * 0.50);
                labelX75.Content = string.Format("{0:0.0}", mXMax * 0.75);
                labelX100.Content = string.Format("{0:0.0}", mXMax * 1.00);
            }

            // clear existing line
            foreach (var item in mLineList) {
                canvasTD.Children.Remove(item);
            }
            mLineList.Clear();

            if (2 <= mTimeValList.Count) {
                labelY0.Content = string.Format("{0:0.0}", mYMin);
                labelY25.Content = string.Format("{0:0.0}", mYMin + (mYMax - mYMin) * 0.25);
                labelY50.Content = string.Format("{0:0.0}", mYMin + (mYMax - mYMin) * 0.50);
                labelY75.Content = string.Format("{0:0.0}", mYMin + (mYMax - mYMin) * 0.75);
                labelY100.Content = string.Format("{0:0.0}", mYMax);

                // draw line

                var firstXY = mTimeValList[0];
                double lastX = GRAPH_LEFT + GRAPH_WIDTH * firstXY.timeSec / mXMax;
                double lastY = GRAPH_BOTTOM - GRAPH_HEIGHT * (firstXY.val - mYMin) / (mYMax - mYMin);

                if (mTimeValList.Count < GRAPH_WIDTH) {
                    // データの数が少ない場合。すべての値をプロット。

                    for (int i = 1; i < mTimeValList.Count; ++i) {
                        var xy = mTimeValList[i];
                        double x = GRAPH_LEFT + GRAPH_WIDTH * xy.timeSec / mXMax;
                        double y = GRAPH_BOTTOM - GRAPH_HEIGHT * (xy.val - mYMin) / (mYMax - mYMin);

                        var l = new Line();
                        l.Stroke = new SolidColorBrush(Colors.Black);
                        LineSetPos(l, lastX, lastY, x, y);
                        canvasTD.Children.Add(l);
                        mLineList.Add(l);

                        lastX = x;
                        lastY = y;
                    }
                } else {
                    // データの数が多すぎる場合。間欠的に値を拾って線を引く。

                    for (int i = 1; i < GRAPH_WIDTH; i+=1) {
                        int idx = (int)(((double)i/GRAPH_WIDTH) * mTimeValList.Count);
                        if (mTimeValList.Count <= idx) {
                            idx = mTimeValList.Count - 1;
                        }
                        //Console.Write("{0} ", idx);
                        var xy = mTimeValList[idx];
                        double x = GRAPH_LEFT + GRAPH_WIDTH * xy.timeSec / mXMax;
                        double y = GRAPH_BOTTOM - GRAPH_HEIGHT * (xy.val - mYMin) / (mYMax - mYMin);

                        var l = new Line();
                        l.Stroke = new SolidColorBrush(Colors.Black);
                        LineSetPos(l, lastX, lastY, x, y);
                        canvasTD.Children.Add(l);
                        mLineList.Add(l);

                        lastX = x;
                        lastY = y;
                    }

                    //Console.WriteLine("");
                }
            }
        }
    }
}
