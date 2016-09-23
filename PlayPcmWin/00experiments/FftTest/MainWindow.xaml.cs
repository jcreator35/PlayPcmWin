using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FftTest {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        class SliderLabel {
            public Slider Slider { get; set; }
            public Label  Label { get; set; }
            public SliderLabel(Slider s, Label l) {
                Slider = s;
                Label = l;
            }

            public double Value {
                get {
                    return Slider.Value;
                }

                set {
                    Slider.Value = value;
                    UpdateLabelValue(value);
                }
            }

            public void UpdateLabelValue(double v) {
                Label.Content = string.Format("{0:0.00}", v);
            }

            public Visibility Visibility {
                set {
                    Slider.Visibility = value;
                    Label.Visibility = value;
                }
            }
        }
        private SliderLabel [] mSLArray;
        private bool mInitialized = false;

        public MainWindow() {
            InitializeComponent();

            mSLArray = new SliderLabel[16];
            mSLArray[0] = new SliderLabel(slider0, labelValue0);
            mSLArray[1] = new SliderLabel(slider1, labelValue1);
            mSLArray[2] = new SliderLabel(slider2, labelValue2);
            mSLArray[3] = new SliderLabel(slider3, labelValue3);
            mSLArray[4] = new SliderLabel(slider4, labelValue4);

            mSLArray[5] = new SliderLabel(slider5, labelValue5);
            mSLArray[6] = new SliderLabel(slider6, labelValue6);
            mSLArray[7] = new SliderLabel(slider7, labelValue7);
            mSLArray[8] = new SliderLabel(slider8, labelValue8);
            mSLArray[9] = new SliderLabel(slider9, labelValue9);

            mSLArray[10] = new SliderLabel(slider10, labelValue10);
            mSLArray[11] = new SliderLabel(slider11, labelValue11);
            mSLArray[12] = new SliderLabel(slider12, labelValue12);
            mSLArray[13] = new SliderLabel(slider13, labelValue13);
            mSLArray[14] = new SliderLabel(slider14, labelValue14);

            mSLArray[15] = new SliderLabel(slider15, labelValue15);

            RestoreUIFromSettings();
        }

        private void RestoreUIFromSettings() {
            mSLArray[0].Value = Properties.Settings.Default.Amplitude0;
            mSLArray[1].Value = Properties.Settings.Default.Amplitude1;
            mSLArray[2].Value = Properties.Settings.Default.Amplitude2;
            mSLArray[3].Value = Properties.Settings.Default.Amplitude3;
            mSLArray[4].Value = Properties.Settings.Default.Amplitude4;

            mSLArray[5].Value = Properties.Settings.Default.Amplitude5;
            mSLArray[6].Value = Properties.Settings.Default.Amplitude6;
            mSLArray[7].Value = Properties.Settings.Default.Amplitude7;
            mSLArray[8].Value = Properties.Settings.Default.Amplitude8;
            mSLArray[9].Value = Properties.Settings.Default.Amplitude9;

            mSLArray[10].Value = Properties.Settings.Default.Amplitude10;
            mSLArray[11].Value = Properties.Settings.Default.Amplitude11;
            mSLArray[12].Value = Properties.Settings.Default.Amplitude12;
            mSLArray[13].Value = Properties.Settings.Default.Amplitude13;
            mSLArray[14].Value = Properties.Settings.Default.Amplitude14;

            mSLArray[15].Value = Properties.Settings.Default.Amplitude15;

            comboBoxSampleCount.SelectedIndex = (int)SampleCountToSampleCountType(Properties.Settings.Default.SampleCount);
            comboBoxUpsample.SelectedIndex = (int)UpsampleMultipleToUpsampleMultipleType(Properties.Settings.Default.UpsampleMultiple);
        }

        private void StoreSettings() {
            Properties.Settings.Default.Amplitude0 = slider0.Value;
            Properties.Settings.Default.Amplitude1 = slider1.Value;
            Properties.Settings.Default.Amplitude2 = slider2.Value;
            Properties.Settings.Default.Amplitude3 = slider3.Value;
            Properties.Settings.Default.Amplitude4 = slider4.Value;

            Properties.Settings.Default.Amplitude5 = slider5.Value;
            Properties.Settings.Default.Amplitude6 = slider6.Value;
            Properties.Settings.Default.Amplitude7 = slider7.Value;
            Properties.Settings.Default.Amplitude8 = slider8.Value;
            Properties.Settings.Default.Amplitude9 = slider9.Value;

            Properties.Settings.Default.Amplitude10 = slider10.Value;
            Properties.Settings.Default.Amplitude11 = slider11.Value;
            Properties.Settings.Default.Amplitude12 = slider12.Value;
            Properties.Settings.Default.Amplitude13 = slider13.Value;
            Properties.Settings.Default.Amplitude14 = slider14.Value;

            Properties.Settings.Default.Amplitude15 = slider15.Value;

            Properties.Settings.Default.SampleCount = SampleCountTypeToSampleCount((SampleCountType)comboBoxSampleCount.SelectedIndex);

            Properties.Settings.Default.Save();
        }

        enum SampleCountType {
            S4,
            S8,
            S16,
        };

        private int SampleCountTypeToSampleCount(SampleCountType t) {
            switch (t) {
            case SampleCountType.S4: return 4;
            case SampleCountType.S8: return 8;
            case SampleCountType.S16: return 16;
            default:
                throw new NotImplementedException();
            }
        }

        private SampleCountType SampleCountToSampleCountType(int sampleCount) {
            switch (sampleCount) {
            case 4: return SampleCountType.S4;
            case 8: return SampleCountType.S8;
            case 16: return SampleCountType.S16;
            default:
                throw new NotImplementedException();
            }
        }

        private void Update() {
            UpdateAmplitudeSliders();
            UpdateWaveFormFrom();
            UpdateMagnitudePhase();
            UpdateUpsampleGraph();
        }

        private void UpdateAmplitudeSliders() {
            for (int i=0; i < mSLArray.Length; ++i) {
                mSLArray[i].Visibility =
                        i < SampleCount()
                        ? Visibility.Visible
                        : Visibility.Hidden;
            }
        }

        private int SampleCount() {
            return SampleCountTypeToSampleCount((SampleCountType)comboBoxSampleCount.SelectedIndex);
        }

        enum UpsampleMultipleType {
            M2,
            M4,
            M8,
        };

        private int UpsampleMultipleTypeToUpsampleMultiple(UpsampleMultipleType multipleType) {
            switch (multipleType) {
            case UpsampleMultipleType.M2:
                return 2;
            case UpsampleMultipleType.M4:
                return 4;
            case UpsampleMultipleType.M8:
                return 8;
            default:
                throw new NotImplementedException();
            }
        }

        private UpsampleMultipleType UpsampleMultipleToUpsampleMultipleType(int multiple) {
            switch (multiple) {
            case 2:
                return UpsampleMultipleType.M2;
            case 4:
                return UpsampleMultipleType.M4;
            case 8:
                return UpsampleMultipleType.M8;
            default:
                throw new NotImplementedException();
            }
        }

        private int UpsampleMultiple() {
            return UpsampleMultipleTypeToUpsampleMultiple((UpsampleMultipleType)comboBoxUpsample.SelectedIndex);
        }

        private const double GRAPH_YAXIS_POS_X = 32;

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

        class WaveFormGraph {
            public List<Tuple<Ellipse,Label, Line>> points = new List<Tuple<Ellipse, Label, Line>>();
        };

        WaveFormGraph mWaveFormFrom = new WaveFormGraph();

        private const int WAVEFORM_SEGMENT_NUM = 8;

        private void UpdateWaveFormFrom() {
            LineSetX1Y1X2Y2(lineWFX,
                    0,                              canvasWaveFormFrom.ActualHeight/2,
                    canvasWaveFormFrom.ActualWidth, canvasWaveFormFrom.ActualHeight/2);

            LineSetX1Y1X2Y2(lineWFY,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X, canvasWaveFormFrom.ActualHeight);

            LineSetX1Y1X2Y2(lineWFTickP1,
                    GRAPH_YAXIS_POS_X - 4, canvasWaveFormFrom.ActualHeight/4,
                    GRAPH_YAXIS_POS_X,     canvasWaveFormFrom.ActualHeight/4);
            LineSetX1Y1X2Y2(lineWFTickM1,
                    GRAPH_YAXIS_POS_X - 4, canvasWaveFormFrom.ActualHeight * 3 / 4,
                    GRAPH_YAXIS_POS_X,     canvasWaveFormFrom.ActualHeight * 3 / 4);

            CanvasSetLeftTop(labelWFP1, 0, canvasWaveFormFrom.ActualHeight / 4 - labelWFP1.ActualHeight/2);
            CanvasSetLeftTop(labelWF0,  0, canvasWaveFormFrom.ActualHeight / 2 - labelFR0.ActualHeight/2);
            CanvasSetLeftTop(labelWFM1, 0, canvasWaveFormFrom.ActualHeight * 3 / 4 - labelWFP1.ActualHeight / 2);

            foreach (var t in mWaveFormFrom.points) {
                canvasWaveFormFrom.Children.Remove(t.Item1);
                canvasWaveFormFrom.Children.Remove(t.Item2);
                canvasWaveFormFrom.Children.Remove(t.Item3);
            }
            mWaveFormFrom.points.Clear();

            double pointIntervalX = (canvasWaveFormFrom.ActualWidth - GRAPH_YAXIS_POS_X) / (SampleCount() + 2);

            for (int i=0; i < SampleCount(); ++i) {
                double x = GRAPH_YAXIS_POS_X + pointIntervalX * (i + 1);
                double y = canvasWaveFormFrom.ActualHeight / 2 - (canvasWaveFormFrom.ActualHeight / 4) * mSLArray[i].Value;

                var el = new Ellipse();
                el.Width = 6;
                el.Height = 6;
                el.Fill = Brushes.Black;
                canvasWaveFormFrom.Children.Add(el);
                CanvasSetLeftTop(el, x-3, y-3);

                var la = new Label();
                la.Content = string.Format("{0:0.00}", mSLArray[i].Value);
                canvasWaveFormFrom.Children.Add(la);
                if (mSLArray[i].Value >= 0) {
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

                mWaveFormFrom.points.Add(new Tuple<Ellipse, Label,Line>(el, la, li));
            }

            polyLineWF.Points.Clear();
            for (int xi=0; xi < (SampleCount() + 3) * WAVEFORM_SEGMENT_NUM; ++xi) {
                double x = ((double)xi / WAVEFORM_SEGMENT_NUM) - 2;

                double y = 0;
                for (int i=0; i < SampleCount()+3+10; ++i) {
                    y += mSLArray[(i-7 + 2 * SampleCount())%SampleCount()].Value * SincPi((i-7) - x);
                }
                polyLineWF.Points.Add(new Point(
                        GRAPH_YAXIS_POS_X + (x + 1) * pointIntervalX,
                        canvasWaveFormFrom.ActualHeight / 2 - (canvasWaveFormFrom.ActualHeight / 4) * y));
            }
        }

        private double SincPi(double x) {
            if (Math.Abs(x) < double.Epsilon) {
                return 1.0;
            }
            return Math.Sin(Math.PI * x) / (Math.PI * x);
        }

        private void UpdateMagnitudePhase() {
            var timeDomain = new WWComplex[SampleCount()];
            for (int i=0; i < timeDomain.Length; ++i) {
                timeDomain[i] = new WWComplex(mSLArray[i].Value, 0);
            }

            var freqDomain = new WWComplex[SampleCount()];
            for (int i=0; i < freqDomain.Length; ++i) {
                freqDomain[i] = new WWComplex();
            }

            var fft = new WWRadix2Fft(SampleCount());
            fft.ForwardFft(timeDomain, freqDomain);

            UpdateMagnitude(freqDomain);
            UpdatePhase(freqDomain);
        }

        class MagnitudeGraph {
            public List<Tuple<Ellipse,Label>> points = new List<Tuple<Ellipse, Label>>();
        };

        MagnitudeGraph mMagnitudeGraph = new MagnitudeGraph();

        private void UpdateMagnitude(WWComplex [] freqDomain) {
            double pointIntervalX = (canvasWaveFormFR.ActualWidth - GRAPH_YAXIS_POS_X) / (SampleCount() / 2 + 1);

            LineSetX1Y1X2Y2(lineFRX,
                GRAPH_YAXIS_POS_X, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X,
                canvasWaveFormFR.ActualWidth, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X);

            LineSetX1Y1X2Y2(lineFRY,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X);

            LineSetX1Y1X2Y2(lineFRTickPHalf,
                    GRAPH_YAXIS_POS_X - 4, (canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X) / 2,
                    GRAPH_YAXIS_POS_X,     (canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X) / 2);

            LineSetX1Y1X2Y2(lineFRTickPMax,
                    GRAPH_YAXIS_POS_X - 4, 0,
                    GRAPH_YAXIS_POS_X,     0);

            LineSetX1Y1X2Y2(lineFRTickXPi,
                    canvasWaveFormFR.ActualWidth - pointIntervalX, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X - 4,
                    canvasWaveFormFR.ActualWidth - pointIntervalX, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X);

            CanvasSetLeftTop(labelFRPMax, 0, -labelFRPMax.ActualHeight / 2);
            CanvasSetLeftTop(labelFRPHalf, 0, (canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X) / 2 - labelFRPHalf.ActualHeight/2);
            CanvasSetLeftTop(labelFR0, 0, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X-labelFR0.ActualHeight/2);
            CanvasSetLeftTop(labelFRXPi,
                    canvasWaveFormFR.ActualWidth - pointIntervalX - labelFRXPi.ActualWidth / 2, canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X);

            labelFRPMax.Content  = string.Format("{0}", SampleCount());
            labelFRPHalf.Content = string.Format("{0}", SampleCount()/2);

            foreach (var t in mMagnitudeGraph.points) {
                canvasWaveFormFR.Children.Remove(t.Item1);
                canvasWaveFormFR.Children.Remove(t.Item2);
            }
            mMagnitudeGraph.points.Clear();

            for (int i=0; i < SampleCount()/2+1; ++i) {
                double x = GRAPH_YAXIS_POS_X + pointIntervalX * i;
                double y = canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X - (canvasWaveFormFR.ActualHeight - GRAPH_YAXIS_POS_X) * freqDomain[i].Magnitude() / SampleCount();

                var el = new Ellipse();
                el.Width = 6;
                el.Height = 6;
                el.Fill = Brushes.Black;
                canvasWaveFormFR.Children.Add(el);
                CanvasSetLeftTop(el, x - 3, y - 3);

                var la = new Label();
                la.Content = string.Format("{0:0.00}", freqDomain[i].Magnitude());
                canvasWaveFormFR.Children.Add(la);
                CanvasSetLeftTop(la, x, y-labelFRPMax.ActualHeight/2);

                mMagnitudeGraph.points.Add(new Tuple<Ellipse, Label>(el, la));
            }
        }

        class PhaseGraph {
            public List<Tuple<Ellipse,Label>> points = new List<Tuple<Ellipse, Label>>();
        };

        PhaseGraph mPhaseGraph = new PhaseGraph();

        private void UpdatePhase(WWComplex[] freqDomain) {
            double pointIntervalX = (canvasWaveFormPhase.ActualWidth - GRAPH_YAXIS_POS_X) / (SampleCount() / 2 + 1);

            LineSetX1Y1X2Y2(linePX,
                GRAPH_YAXIS_POS_X,               canvasWaveFormPhase.ActualHeight / 2,
                canvasWaveFormPhase.ActualWidth, canvasWaveFormPhase.ActualHeight / 2);

            LineSetX1Y1X2Y2(linePY,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X, canvasWaveFormPhase.ActualHeight);

            LineSetX1Y1X2Y2(linePTickPPi,
                    GRAPH_YAXIS_POS_X - 4, 0,
                    GRAPH_YAXIS_POS_X,     0);

            LineSetX1Y1X2Y2(linePTickMPi,
                    GRAPH_YAXIS_POS_X - 4, canvasWaveFormPhase.ActualHeight-1,
                    GRAPH_YAXIS_POS_X,     canvasWaveFormPhase.ActualHeight-1);

            LineSetX1Y1X2Y2(linePTickXPi,
                    canvasWaveFormPhase.ActualWidth - pointIntervalX, canvasWaveFormPhase.ActualHeight / 2 - 4,
                    canvasWaveFormPhase.ActualWidth - pointIntervalX, canvasWaveFormPhase.ActualHeight / 2);

            CanvasSetLeftTop(labelPPPi, 0, 0 - labelPPPi.ActualHeight / 2);
            CanvasSetLeftTop(labelPMPi, 0, canvasWaveFormPhase.ActualHeight - labelPMPi.ActualHeight/2);
            CanvasSetLeftTop(labelP0, 0, canvasWaveFormPhase.ActualHeight / 2 - labelPMPi.ActualHeight / 2);
            CanvasSetLeftTop(labelPXPi,
                    canvasWaveFormPhase.ActualWidth - pointIntervalX - labelPXPi.ActualWidth/2, canvasWaveFormPhase.ActualHeight / 2);

            foreach (var t in mPhaseGraph.points) {
                canvasWaveFormPhase.Children.Remove(t.Item1);
                canvasWaveFormPhase.Children.Remove(t.Item2);
            }
            mPhaseGraph.points.Clear();

            for (int i=0; i < SampleCount() / 2 + 1; ++i) {
                double x = GRAPH_YAXIS_POS_X + pointIntervalX * i;
                double y = canvasWaveFormPhase.ActualHeight/2 - (canvasWaveFormPhase.ActualHeight)/2/Math.PI * freqDomain[i].Phase();

                var el = new Ellipse();
                el.Width = 6;
                el.Height = 6;
                el.Fill = Brushes.Black;
                canvasWaveFormPhase.Children.Add(el);
                CanvasSetLeftTop(el, x - 3, y - 3);

                var la = new Label();
                la.Content = string.Format("{0:0.00}", freqDomain[i].Phase());
                canvasWaveFormPhase.Children.Add(la);
                CanvasSetLeftTop(la, x, y - labelP0.ActualHeight/2);

                mPhaseGraph.points.Add(new Tuple<Ellipse, Label>(el, la));
            }
        }

        class UpsampleGraph {
            public List<Tuple<Ellipse,Label, Line>> points = new List<Tuple<Ellipse, Label, Line>>();
        };

        UpsampleGraph mUpsampleGraph = new UpsampleGraph();

        private void UpdateUpsampleGraph() {
            LineSetX1Y1X2Y2(lineUSX,
                    0, canvasUpsampleGraph.ActualHeight / 2,
                    canvasUpsampleGraph.ActualWidth, canvasUpsampleGraph.ActualHeight / 2);

            LineSetX1Y1X2Y2(lineUSY,
                    GRAPH_YAXIS_POS_X, 0,
                    GRAPH_YAXIS_POS_X, canvasUpsampleGraph.ActualHeight);

            LineSetX1Y1X2Y2(lineUSTickP1,
                    GRAPH_YAXIS_POS_X - 4, canvasUpsampleGraph.ActualHeight / 4,
                    GRAPH_YAXIS_POS_X, canvasUpsampleGraph.ActualHeight / 4);
            LineSetX1Y1X2Y2(lineUSTickM1,
                    GRAPH_YAXIS_POS_X - 4, canvasUpsampleGraph.ActualHeight * 3 / 4,
                    GRAPH_YAXIS_POS_X, canvasUpsampleGraph.ActualHeight * 3 / 4);

            CanvasSetLeftTop(labelUSP1, 0, canvasUpsampleGraph.ActualHeight / 4 - labelUSP1.ActualHeight / 2);
            CanvasSetLeftTop(labelUS0, 0, canvasUpsampleGraph.ActualHeight / 2 - labelUS0.ActualHeight / 2);
            CanvasSetLeftTop(labelUSM1, 0, canvasUpsampleGraph.ActualHeight * 3 / 4 - labelUSM1.ActualHeight / 2);

            foreach (var t in mUpsampleGraph.points) {
                canvasUpsampleGraph.Children.Remove(t.Item1);
                canvasUpsampleGraph.Children.Remove(t.Item2);
                canvasUpsampleGraph.Children.Remove(t.Item3);
            }
            mUpsampleGraph.points.Clear();

            // オリジナルPCMデータtimeDomainOrigをFFTして周波数ドメインデータfreqDomainOrigを得る
            var timeDomainOrig = new WWComplex[SampleCount()];
            for (int i=0; i < timeDomainOrig.Length; ++i) {
                timeDomainOrig[i] = new WWComplex(mSLArray[i].Value, 0);
            }

            var freqDomainOrig = new WWComplex[SampleCount()];

            {
                var fft = new WWRadix2Fft(SampleCount());
                fft.ForwardFft(timeDomainOrig, freqDomainOrig);
            }

            timeDomainOrig = null;

            // 周波数ドメインデータfreqDomainOrigを0で水増ししたデータfreqDomainUpsampledを作って逆FFTする

            int upsampledSampleCount = SampleCount() * UpsampleMultiple();

            var freqDomainUpsampled = new WWComplex[upsampledSampleCount];
            for (int i=0; i < freqDomainUpsampled.Length; ++i) {
                if (i <= freqDomainOrig.Length / 2) {
                    freqDomainUpsampled[i].CopyFrom(freqDomainOrig[i]);
                    if (i == freqDomainOrig.Length / 2) {
                        freqDomainUpsampled[i].Mul(0.5);
                    }
                } else if (freqDomainUpsampled.Length - freqDomainOrig.Length / 2 <= i) {
                    int pos = i + freqDomainOrig.Length - freqDomainUpsampled.Length;
                    freqDomainUpsampled[i].CopyFrom(freqDomainOrig[pos]);
                    if (freqDomainUpsampled.Length - freqDomainOrig.Length / 2 == i) {
                        freqDomainUpsampled[i].Mul(0.5);
                    }
                } else {
                    // do nothing
                }
            }
            freqDomainOrig = null;

            var timeDomainUpsampled = new WWComplex[upsampledSampleCount];
            {
                var fft = new WWRadix2Fft(upsampledSampleCount);
                fft.InverseFft(freqDomainUpsampled, timeDomainUpsampled, 1.0 / SampleCount());
            }

            freqDomainUpsampled = null;


            // アップサンプルされたPCMデータtimeDomainUpsampledをグラフにプロットする
            double pointIntervalX = (canvasUpsampleGraph.ActualWidth - GRAPH_YAXIS_POS_X) / (upsampledSampleCount + 2 * UpsampleMultiple());

            for (int i=0; i < upsampledSampleCount; ++i) {
                double x = GRAPH_YAXIS_POS_X + pointIntervalX * (i + UpsampleMultiple());
                double y = canvasUpsampleGraph.ActualHeight / 2 - (canvasUpsampleGraph.ActualHeight / 4) * timeDomainUpsampled[i].real;

                var el = new Ellipse();
                el.Width = 6;
                el.Height = 6;
                el.Fill = Brushes.Black;
                canvasUpsampleGraph.Children.Add(el);
                CanvasSetLeftTop(el, x - 3, y - 3);

                var la = new Label();
                la.Content = string.Format("{0:0.00}", timeDomainUpsampled[i].real);
                canvasUpsampleGraph.Children.Add(la);
                if (timeDomainUpsampled[i].real >= 0) {
                    CanvasSetLeftTop(la, x - 16, y - labelUS0.ActualHeight);
                } else {
                    CanvasSetLeftTop(la, x - 16, y);
                }

                var li = new Line();
                li.Stroke = Brushes.Black;
                canvasUpsampleGraph.Children.Add(li);
                LineSetX1Y1X2Y2(li,
                    x, y,
                    x, canvasUpsampleGraph.ActualHeight / 2);

                mUpsampleGraph.points.Add(new Tuple<Ellipse, Label, Line>(el, la, li));
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            Update();
        }

        private void comboBoxSampleCount_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            Update();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            foreach (var sl in mSLArray) {
                if (sl.Slider == sender) {
                    sl.UpdateLabelValue(e.NewValue);
                }
            }

            Update();
        }

        private void buttonResetToDefaults_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            Properties.Settings.Default.Reset();
            RestoreUIFromSettings();
            Properties.Settings.Default.Save();
            Update();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            StoreSettings();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            Update();
        }

        private void comboBoxUpsample_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            Update();
        }

    }
}
