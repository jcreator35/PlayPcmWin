using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QuantizationDemo {
    public partial class MainWindow : Window {
        private WaveForms mWaveForms;

        private const int X_RESOLUTION = 700;
        private const double X_OFFSET = 50;
        private const double Y_OFFSET = 200;
        private const double Y_SCALE = 150;

        private bool mInitialized = true;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
            Update();
        }

        private void ButtonRecalculation_Click(object sender, RoutedEventArgs e) {
            Update();
        }

        private void Update() {
            UpdateData();
            UpdateGraph();
        }


        private bool UpdateData() {
            mWaveForms = null;

            double noiseDb;
            if (!Double.TryParse(mNoiseDb.Text, out noiseDb)) {
                MessageBox.Show(Properties.Resources.ErrorNoiseDb);
                return false;
            }

            int quantizationBit;
            if (!Int32.TryParse(mQuantizationBit.Text, out quantizationBit) || quantizationBit <= 0 || 32 < quantizationBit) {
                MessageBox.Show(Properties.Resources.ErrorQuantizationBit);
                return false;
            }

            double signalAngularFreq;
            if (!Double.TryParse(mSignalAngularFreq.Text, out signalAngularFreq)) {
                MessageBox.Show(Properties.Resources.ErrorAngularFreq);
                return false;
            }
            double signalAngularFreqRad = signalAngularFreq * Math.PI;

            double signalAmplitude;
            if (!Double.TryParse(mSignalAmplitude.Text, out signalAmplitude)) {
                MessageBox.Show(Properties.Resources.ErrorSignalAmplitude);
                return false;
            }

            mWaveForms = new WaveForms();
            mWaveForms.Update(signalAmplitude, noiseDb, quantizationBit, signalAngularFreqRad / X_RESOLUTION, 3 * 44100);

            return true;
        }

        private List<Polyline> mPolyLines = new List<Polyline>();
        private List<Line> mLines = new List<Line>();

        private void AddLinePlot(double[] samples, Brush brush) {
            System.Diagnostics.Debug.Assert(samples.Length == X_RESOLUTION);

            var line = new Polyline();
            line.Stroke = brush;
            for (int x = 0; x < X_RESOLUTION; ++x) {
                double y = samples[x];
                line.Points.Add(new Point(x + X_OFFSET, -y * Y_SCALE + Y_OFFSET));
            }
            mPolyLines.Add(line);
            mCanvas.Children.Add(line);
        }

        private void ShowQuantizerThreshold() {
            if (mWaveForms.QuantizationBit <= 6) {
                int count = 2 << (mWaveForms.QuantizationBit - 1);
                for (int i = 0; i < count+1; ++i) {
                    double y = 2.0 * (-0.5 + (i - 0.5) / count);

                    var l = new Line();
                    l.Stroke = new SolidColorBrush(Colors.LightGray);
                    l.X1 = X_OFFSET;
                    l.X2 = X_OFFSET + X_RESOLUTION;

                    l.Y1 = Y_OFFSET - Y_SCALE * y;
                    l.Y2 = l.Y1;

                    mLines.Add(l);
                    mCanvas.Children.Add(l);
                }
            }
        }

        private bool UpdateGraph() {
            foreach (var p in mPolyLines) {
                mCanvas.Children.Remove(p);
            }
            mPolyLines.Clear();

            foreach (var l in mLines) {
                mCanvas.Children.Remove(l);
            }
            mLines.Clear();

            if (mWaveForms == null) {
                return false;
            }

            if (true == mCbQuantizerThreshold.IsChecked) {
                ShowQuantizerThreshold();
            }

            if (true == mCbOriginal.IsChecked) {
                AddLinePlot(mWaveForms.OriginalSignal, new SolidColorBrush(Colors.Black));
            }
            if (true == mCbNoise.IsChecked) {
                AddLinePlot(mWaveForms.Noise, new SolidColorBrush(Colors.Blue));
            }

            if (true == mCbSignalWithNoise.IsChecked) {
                AddLinePlot(mWaveForms.OriginalSignalPlusNoise, new SolidColorBrush(Colors.Blue));
            }

            if (true == mCbQuantizationNoise.IsChecked) {
                AddLinePlot(mWaveForms.QuantizationNoise, new SolidColorBrush(Colors.Red));
            }

            if (true == mCbOutput.IsChecked) {
                AddLinePlot(mWaveForms.Quantized, new SolidColorBrush(Colors.Red));
            }

            return true;
        }

        private void mCb_CheckedChanged(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            UpdateGraph();
        }

    }
}
