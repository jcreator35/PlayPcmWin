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

namespace WaveFormDisp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        struct Params {
            public double amplitude;
            public double angularVelocity;
            public double phase;
            public double constant;
            public int sampleCount;
        };

        private Params mParams;

        List<double> mSampleValueList = new List<double>();

        private const double GRAPH_YAXIS_POS_X = 32;
        
        private const int WAVEFORM_SEGMENT_NUM = 8;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            UpdateParams();
            UpdateSampleValueList();
            UpdateGraph();
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e) {
            UpdateParams();
            UpdateSampleValueList();
            UpdateGraph();
        }

        private bool UpdateParams() {
            bool rv = true;

            if (!Double.TryParse(textBoxAmplitude.Text, out mParams.amplitude)) {
                MessageBox.Show("amplitude read error");
                rv = false;
            }

            if (!Double.TryParse(textBoxAngularVelo.Text, out mParams.angularVelocity)) {
                MessageBox.Show("angularVelocity read error");
                rv = false;
            }

            if (!Double.TryParse(textBoxPhase.Text, out mParams.phase)) {
                MessageBox.Show("phase read error");
                rv = false;
            }

            if (!Double.TryParse(textBoxConstant.Text, out mParams.constant)) {
                MessageBox.Show("constant read error");
                rv = false;
            }

            if (!Int32.TryParse(textBoxSampleCount.Text, out mParams.sampleCount)) {
                MessageBox.Show("sample count read error");
                rv = false;
            }

            return rv;
        }

        private void UpdateSampleValueList() {
            mSampleValueList = new List<double>();

            for (int i = 0; i < mParams.sampleCount; ++i) {
                double y = mParams.amplitude * Math.Sin(
                    mParams.angularVelocity * Math.PI * i +
                    mParams.phase * Math.PI) + mParams.constant;
                mSampleValueList.Add(y);
            }
        }

        private void CanvasSetLeftTop(UIElement e, double x, double y) {
            Canvas.SetLeft(e, x);
            Canvas.SetTop(e, y);
        }

        private void LineSetX1Y1X2Y2(Line l, double x1, double y1, double x2, double y2) {
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
        }

        private double SincPi(double x) {
            if (Math.Abs(x) < double.Epsilon) {
                return 1.0;
            }
            return Math.Sin(Math.PI * x) / (Math.PI * x);
        }
        
        class WaveFormGraph {
            public List<Tuple<Ellipse, Label, Line>> points = new List<Tuple<Ellipse, Label, Line>>();
        };

        private WaveFormGraph mWaveFormFrom = new WaveFormGraph();

        private void UpdateGraph() {
            LineSetX1Y1X2Y2(lineWFX,
                    0, canvasWaveFormFrom.ActualHeight / 2,
                    canvasWaveFormFrom.ActualWidth, canvasWaveFormFrom.ActualHeight / 2);

            LineSetX1Y1X2Y2(lineWFY,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X, canvasWaveFormFrom.ActualHeight);

            LineSetX1Y1X2Y2(lineWFTickP1,
                    GRAPH_YAXIS_POS_X - 4, canvasWaveFormFrom.ActualHeight / 4,
                    GRAPH_YAXIS_POS_X, canvasWaveFormFrom.ActualHeight / 4);
            LineSetX1Y1X2Y2(lineWFTickM1,
                    GRAPH_YAXIS_POS_X - 4, canvasWaveFormFrom.ActualHeight * 3 / 4,
                    GRAPH_YAXIS_POS_X, canvasWaveFormFrom.ActualHeight * 3 / 4);

            LineSetX1Y1X2Y2(lineWFArrowYL,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X - 5, 10);

            LineSetX1Y1X2Y2(lineWFArrowYR,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X + 5, 10);

            CanvasSetLeftTop(labelWFP1, GRAPH_YAXIS_POS_X - 32, canvasWaveFormFrom.ActualHeight / 4 - labelWFP1.ActualHeight / 2);
            CanvasSetLeftTop(labelWF0,  GRAPH_YAXIS_POS_X - 32, canvasWaveFormFrom.ActualHeight / 2 - labelWFP1.ActualHeight / 2);
            CanvasSetLeftTop(labelWFM1, GRAPH_YAXIS_POS_X - 32, canvasWaveFormFrom.ActualHeight * 3 / 4 - labelWFP1.ActualHeight / 2);

            foreach (var t in mWaveFormFrom.points) {
                canvasWaveFormFrom.Children.Remove(t.Item1);
                canvasWaveFormFrom.Children.Remove(t.Item2);
                canvasWaveFormFrom.Children.Remove(t.Item3);
            }
            mWaveFormFrom.points.Clear();

            int sampleCount = mSampleValueList.Count;
            double pointIntervalX = canvasWaveFormFrom.ActualWidth / sampleCount;

            // plot discrete sample 
            for (int i = 0; i < sampleCount; ++i) {
                var v = mSampleValueList[i];
                double x = GRAPH_YAXIS_POS_X + pointIntervalX * i;
                double y = canvasWaveFormFrom.ActualHeight / 2 - (canvasWaveFormFrom.ActualHeight / 4) * v;

                var el = new Ellipse();
                el.Width = 6;
                el.Height = 6;
                el.Fill = Brushes.Black;
                canvasWaveFormFrom.Children.Add(el);
                CanvasSetLeftTop(el, x - 3, y - 3);

                var la = new Label();
                la.Content = string.Format("{0:0.00}", v);
                canvasWaveFormFrom.Children.Add(la);
                if (v >= 0) {
                    CanvasSetLeftTop(la, x - 16, y - labelWF0.ActualHeight);
                } else {
                    CanvasSetLeftTop(la, x - 16, y);
                }

                var li = new Line();
                li.Stroke = Brushes.Black;
                canvasWaveFormFrom.Children.Add(li);
                LineSetX1Y1X2Y2(li,
                    x, y,
                    x, canvasWaveFormFrom.ActualHeight / 2);

                mWaveFormFrom.points.Add(new Tuple<Ellipse, Label, Line>(el, la, li));
            }

            // plot waveform
            polyLineWF.Points.Clear();
            for (int xi = 0; xi < (sampleCount + 3) * WAVEFORM_SEGMENT_NUM; ++xi) {
                double x = ((double)xi / WAVEFORM_SEGMENT_NUM) - 1;

                double y = 0;
                for (int i = 0; i < sampleCount + 3 + 10; ++i) {
                    y += mSampleValueList[(i - 7 + 2 * sampleCount) % sampleCount] * SincPi((i - 7) - x);
                }
                polyLineWF.Points.Add(new Point(
                        GRAPH_YAXIS_POS_X + x * pointIntervalX,
                        canvasWaveFormFrom.ActualHeight / 2 - (canvasWaveFormFrom.ActualHeight / 4) * y));
            }

        }

    }
}
