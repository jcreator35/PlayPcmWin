using System.Windows.Controls;
using System.Collections.Generic;
using WWMath;

namespace WWUserControls {
    public partial class Function1dGraph : UserControl {
        private List<WWVectorD2> mPoints = new List<WWVectorD2>();

        private bool mInitialized = false;

        public Function1dGraph() {
            InitializeComponent();
            Epsilon = 1e-7;
            mInitialized = true;
        }

        public string Title {
            get { return (string)mLabelTitle.Content; }
            set { mLabelTitle.Content = value; }
        }

        public string YAxis {
            get { return (string)mLabelY.Content; }
            set { mLabelY.Content = value; }
        }

        public string XAxis {
            get { return (string)mLabelX.Content; }
            set { mLabelX.Content = value; }
        }

        public double Epsilon {
            get;
            set;
        }

        public enum FunctionEnum {
            YequalsOne,
            YequalsX,
            Yequals1minusX,
            YequalsXsquared,
            ArbitraryFunction,
        };

        public FunctionEnum FunctionType {
            get { return (FunctionEnum)mComboBoxFuncType.SelectedIndex; }
            set { mComboBoxFuncType.SelectedIndex = (int)value; }
        }

        public bool IsEnabledFunctionType {
            set { mComboBoxFuncType.IsEnabled = value; }
        }

        private void Redraw() {
            if (!mInitialized) {
                return;
            }

            var fe = (FunctionEnum)mComboBoxFuncType.SelectedIndex;

            double W = mCanvas.ActualWidth;
            double H = mCanvas.ActualHeight;

            double xMin = 0;
            double xMax = 1;
            double yMin = 0;
            double yMax = 1;

            mPolyLine.Points.Clear();
            switch (fe) {
            case FunctionEnum.YequalsOne:
                mPolyLine.Points.Add(new System.Windows.Point(0, 0));
                mPolyLine.Points.Add(new System.Windows.Point(W, 0));
                break;
            case FunctionEnum.Yequals1minusX:
                mPolyLine.Points.Add(new System.Windows.Point(0, 0));
                mPolyLine.Points.Add(new System.Windows.Point(W, H));
                break;
            case FunctionEnum.YequalsX:
                mPolyLine.Points.Add(new System.Windows.Point(W, 0));
                mPolyLine.Points.Add(new System.Windows.Point(0, H));
                break;
            case FunctionEnum.YequalsXsquared:
                for (int i = 0; i < W; ++i) {
                    double x = (double)i / (W - 1);
                    double y = x * x;

                    mPolyLine.Points.Add(new System.Windows.Point(W * x, H * (1.0 - y)));
                }
                break;
            case FunctionEnum.ArbitraryFunction: {
                    // 最大値、最小値を調べる。
                    xMax = double.MinValue;
                    xMin = double.MaxValue;
                    yMax = double.MinValue;
                    yMin = double.MaxValue;
                    for (int i = 0; i < mPoints.Count; ++i) {
                        {
                            double x = mPoints[i].X;
                            if (xMax < x) {
                                xMax = x;
                            }
                            if (x < xMin) {
                                xMin = x;
                            }
                        }
                        {
                            double y = mPoints[i].Y;
                            if (yMax < y) {
                                yMax = y;
                            }
                            if (y < yMin) {
                                yMin = y;
                            }
                        }
                    }

                    // x,yの範囲。
                    double xRange = xMax - xMin;
                    if (xRange < Epsilon) {
                        xRange = 1.0;
                    }
                    double yRange = yMax - yMin;
                    if ((yRange / xRange) < Epsilon) {
                        yRange = 1.0;
                    }

                    for (int i = 0; i < mPoints.Count; ++i) {
                        var xy = mPoints[i];
                        mPolyLine.Points.Add(new System.Windows.Point(
                            W * (        xy.X - xMin) / xRange,
                            H * (1.0 - ((xy.Y - yMin) / yRange))));
                    }

                    // xの最小値、最大値
                }
                break;
            }

            // 軸最大値、最小値表示更新。
            labelXmin.Content = string.Format("{0:0.00}", xMin);
            labelXmax.Content = string.Format("{0:0.00}", xMax);
            labelYmin.Content = string.Format("{0:0.00}", yMin);
            labelYmax.Content = string.Format("{0:0.00}", yMax);
        }

        /// <summary>
        /// f(x)を戻す。
        /// </summary>
        /// <param name="x">入力値x</param>
        /// <returns>出力値f(x)</returns>
        public double Sample(double x) {
            var fe = (FunctionEnum)mComboBoxFuncType.SelectedIndex;

            switch (fe) {
            case FunctionEnum.YequalsOne:
                return 1.0;
            case FunctionEnum.YequalsX:
                return x;
            case FunctionEnum.Yequals1minusX:
                return 1.0 - x;
            case FunctionEnum.YequalsXsquared:
                return x * x;
            case FunctionEnum.ArbitraryFunction: {
                    double prevX = 0;
                    for (int i = 0; i < mPoints.Count; ++i) {
                        double curX = mPoints[i].X;
                        if (prevX <= x && x <= curX) {
                            if (i == 0) {
                                // 最左値をホールドする。
                                return mPoints[i].Y;
                            } else {
                                double prevY = mPoints[i - 1].Y;
                                double curY = mPoints[i].Y;

                                // 線形補間。
                                double y = prevY + (curY - prevY) * (x - prevX) / (curX - prevX);
                                return y;
                            }
                        }

                        prevX = curX;
                    }

                    // 最右値をホールドする。
                    return mPoints[mPoints.Count - 1].Y;
                }
            }
            return 0;
        }

        public void SetArbitraryFunctionStart() {
            mPoints.Clear();
        }

        public void SetArbitraryFunctionPoint(double x, double y) {
            mPoints.Add(new WWVectorD2(x, y));
        }

        /// <summary>
        /// UIスレッドから呼んで下さい。
        /// </summary>
        public void SetArbitraryFunctionEnd() {
            Redraw();
        }

        private void comboBoxFuncType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Redraw();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) {
            Redraw();
        }

    }
}
