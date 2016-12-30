using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WWUserControls {
    /// <summary>
    /// Interaction logic for TimeDomainPlot.xaml
    /// </summary>
    public partial class TimeDomainPlot : UserControl {
        public TimeDomainPlot() {
            InitializeComponent();

            TimeRange = 20.0;
            TimeScale = 0.01;
            mInitialized = true;
        }

        private bool mInitialized = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            Update();
        }

        private double[] mTimeScales = {
            1.0,
            2.0,
            5.0};

        private const int FR_LINE_LEFT = 64;
        private const int FR_LINE_HEIGHT = 256;
        private const int FR_LINE_WIDTH = 500;
        private const int FR_LINE_TOP = 32;
        private const int FR_LINE_BOTTOM = FR_LINE_TOP + FR_LINE_HEIGHT;
        private const int FR_LINE_YCENTER = (FR_LINE_TOP + FR_LINE_BOTTOM) / 2;

        private const int TIME_LABEL_NUM = 10;
        
        public double TimeRange { get; set; }

        public double TimeScale { get; set; }

        private const double MINUS_TIME_RATIO = 0.2;

        public Common.TimeDomainResponseFunctionDelegate ImpulseResponseFunction = (double t) => { return 0; };
        public Common.TimeDomainResponseFunctionDelegate StepResponseFunction = (double t) => { if (t <= 0) { return 0; } return 1; };

        public enum FunctionType {
            ImpulseResponse,
            StepResponse,
        };

        private double PlotXToTime(int idx) {
            return TimeRange * ((double)idx / FR_LINE_WIDTH - MINUS_TIME_RATIO);
        }

        private int TimeToPlotX(double t) {
            return (int)((t / TimeRange + MINUS_TIME_RATIO) * FR_LINE_WIDTH);
        }

        private void LineSetPos(Line l, double x1, double y1, double x2, double y2) {
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
        }

        private List<Line> mLineList = new List<Line>();

        private List<Label> mLabelList = new List<Label>();

        public void Update() {
            if (!mInitialized) {
                return;
            }

            double scale = mTimeScales[comboBoxTimeScale.SelectedIndex];
            TimeScale = 0.01 * scale;
            TimeRange = 20.0 * scale;

            foreach (var item in mLineList) {
                canvasTD.Children.Remove(item);
            }
            mLineList.Clear();

            foreach (var item in mLabelList) {
                canvasTD.Children.Remove(item);
            }
            mLabelList.Clear();

            double[] sampled = new double[FR_LINE_WIDTH];

            double maxMagnitude = 0.01;

            for (int idx = 0; idx < FR_LINE_WIDTH; ++idx) {
                double t = PlotXToTime(idx);
                double y = 0;
                switch ((FunctionType)comboBoxFunction.SelectedIndex) {
                case FunctionType.ImpulseResponse:
                    y = ImpulseResponseFunction(t*2*Math.PI);
                    break;
                case FunctionType.StepResponse:
                    y = StepResponseFunction(t * 2 * Math.PI);
                    break;
                }

                if (maxMagnitude < Math.Abs(y)) {
                    maxMagnitude = Math.Abs(y);
                }

                sampled[idx] = y;
            }

            // 時間表示と時間の縦線を引く。
            for (double idx = 0; idx <= FR_LINE_WIDTH; idx += (double)FR_LINE_WIDTH / TIME_LABEL_NUM) {
                double t = PlotXToTime((int)idx);

                {
                    var l = new Line();
                    l.Stroke = new SolidColorBrush(Colors.LightGray);
                    LineSetPos(l, FR_LINE_LEFT + idx, FR_LINE_TOP, FR_LINE_LEFT + idx, FR_LINE_BOTTOM);
                    canvasTD.Children.Add(l);
                    mLineList.Add(l);
                }

                {
                    var label = new Label();
                    label.Content = Common.UnitNumberString(t * TimeScale);
                    canvasTD.Children.Add(label);
                    Canvas.SetLeft(label, FR_LINE_LEFT + idx - 10);
                    Canvas.SetTop(label, FR_LINE_BOTTOM);
                    mLabelList.Add(label);
                }
            }

            // Amplitudeのラベル表示更新。
            Label[] mAmplitudeLabels = new Label[] {
                labelFRAmpM100,
                labelFRAmpM075,
                labelFRAmpM050,
                labelFRAmpM025,
                labelFRAmp000,
                labelFRAmp025,
                labelFRAmp050,
                labelFRAmp075,
                labelFRAmp100};
            double amp = -1.0;
            foreach (var item in mAmplitudeLabels) {
                item.Content = string.Format("{0:0.000}", amp * maxMagnitude);
                amp += 0.25;
            }

            {
                // グラフ描画。
                double lastY = FR_LINE_TOP + FR_LINE_HEIGHT / 2.0 - (FR_LINE_HEIGHT / 2.0) * sampled[0] / maxMagnitude;
                for (int idx = 0; idx < FR_LINE_WIDTH; ++idx) {
                    double y = FR_LINE_TOP + FR_LINE_HEIGHT / 2.0 - (FR_LINE_HEIGHT / 2.0) * sampled[idx] / maxMagnitude;

                    if (idx != 0) {
                        var l = new Line();
                        l.Stroke = new SolidColorBrush(Colors.Black);
                        LineSetPos(l, FR_LINE_LEFT + idx - 1, lastY, FR_LINE_LEFT + idx, y);
                        canvasTD.Children.Add(l);
                        mLineList.Add(l);
                    }

                    lastY = y;
                }
            }

        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

    }
}
