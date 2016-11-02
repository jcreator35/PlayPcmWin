using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WWAudioFilter;
using System.Collections.Generic;
using System.Windows.Shapes;

namespace PolynomialVisualize {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        private List<Line> mLineList = new List<Line>();
        private const int FR_LINE_LEFT = 64;
        private const int FR_LINE_HEIGHT = 256;
        private const int FR_LINE_NUM = 512;
        private const int FR_LINE_TOP = 32;
        private const int FR_LINE_BOTTOM = FR_LINE_TOP + FR_LINE_HEIGHT;
        private const int FR_LINE_YCENTER = (FR_LINE_TOP + FR_LINE_BOTTOM) / 2;

        enum PoleZeroType {
            Zero,
            Other,
            Pole
        };

        enum PoleZeroScaleType {
            Scale1x,
            Scale0_5x,
            Scale0_2x,
            Scale0_1x,
        };
        static double[] mPoleZeroScale = new double[] {
            1.0,
            0.5,
            0.2,
            0.1
        };

        enum PoleZeroDispMode {
            Magnitude,
            Phase
        };

        enum MagScaleType {
            Linear,
            Logarithmic,
        };

        enum FreqScaleType {
            Linear,
            Logarithmic,
        };

        enum PhaseShiftType {
            Zero,
            P45,
            P90,
            P135,
            P180,
            M45,
            M90,
            M135,
            M180,
            NUM
        };

        enum SampleFreqType {
            SF44100,
            SF48000,
            SF88200,
            SF96000,
            SF176400,

            SF192000,
            SF2822400,
            SF5644800,
            SF11289600,
            SF22579200,
        };

        private enum StartFrequencyType {
            SF_1Hz,
            SF_10Hz,
        };

        private StartFrequencyType StartFrequency() {
            var sf = StartFrequencyType.SF_1Hz;
            if (comboBoxItemStartFreq10Hz.IsSelected) {
                sf = StartFrequencyType.SF_10Hz;
            }
            return sf;
        }

        private readonly int[] SAMPLE_FREQS = new int[] {
            44100,
            48000,
            88200,
            96000,
            176400,

            192000,
            2822400,
            5644800,
            11289600,
            22579200,
        };

        private float LogSampleFreq(int freq, int idx) {
            switch (freq) {
            case 44100:
            case 48000:
            case 88200:
            case 96000:
            case 176400:
            case 192000:
                switch (StartFrequency()) {
                case StartFrequencyType.SF_1Hz:
                default:
                    if (idx == 14) {
                        return freq / 2;
                    } else {
                        return new int[] { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 22050 }[idx];
                    }
                case StartFrequencyType.SF_10Hz:
                    if (idx == 11) {
                        return freq / 2;
                    } else {
                        return new int[] { 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 22050, 0, 0, 0 }[idx];
                    }
                }

            case 2822400:
            case 5644800:
            case 11289600:
            case 22579200:
                switch (StartFrequency()) {
                case StartFrequencyType.SF_1Hz:
                case StartFrequencyType.SF_10Hz:
                default:
                    if (idx == 14) {
                        return freq / 2;
                    } else {
                        return new int[] { 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10 * 1000, 20 * 1000, 50 * 1000, 100 * 1000, 200 * 1000, 220 * 1000 }[idx];
                    }
                }
            default:
                System.Diagnostics.Debug.Assert(false);
                return 10;
            }
        }

        private static double[] mMagnitudeRange = new double[] {
            0.06309573444801932494343601366223, // -96dBの4乗根
            0.97162795157710617416734286616884, // -1dBの4乗根
            0.91727593538977958470087508027281, // -3dBの4乗根
            0.70794578438413791080221494218931, // -12dBの4乗根
            0.25118864315095801110850320677993, // -48dBの4乗根
        };

        private enum XLineType {
            X1,
            X2,
            X5,
            X10,
            X20,
            X50,
            X100,
            X200,
            X500,
            X1k,
            X2k,
            X5k,
            X10k,
            X20k,
            XMax,
            NUM
        };
        private Label[] mXLabelList;
        private Line[] mXLineList;

        public MainWindow() {
            InitializeComponent();

            mXLabelList = new Label[] {
                labelFR1,
                labelFR2,
                labelFR5,
                labelFR10,
                labelFR20,
                labelFR50,
                labelFR100,
                labelFR200,
                labelFR500,
                labelFR1k,
                labelFR2k,
                labelFR5k,
                labelFR10k,
                labelFR20k,
                labelFRMax,
            };

            mXLineList = new Line[] {
                lineFR1,
                lineFR2,
                lineFR5,
                lineFR10,
                lineFR20,
                lineFR50,
                lineFR100,
                lineFR200,
                lineFR500,
                lineFR1k,
                lineFR2k,
                lineFR5k,
                lineFR10k,
                lineFR20k,
                lineFRMax,
            };
        }

        private bool mInitialized = false;

        private int PhaseToBgra(double phase) {
            return Util.HsvToBgra(-phase * 180.0 / Math.PI + 240.0, 1.0, 1.0);
        }

        private void UpdateZ(double[] numerators, double[] denominators)
        {
            if (checkBoxShowPoleZero.IsChecked == false) {
                canvasZ.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            double scale = mPoleZeroScale[comboBoxPoleZeroScale.SelectedIndex];
            PoleZeroDispMode dispMode = (PoleZeroDispMode)comboBoxPoleZeroDispMode.SelectedIndex;

            var im = new Image();

            var bm = new WriteableBitmap(
                512,
                512,
                96, 
                96, 
                dispMode == PoleZeroDispMode.Magnitude ? PixelFormats.Gray32Float : PixelFormats.Bgra32,
                null);
            var pxF = new float[bm.PixelHeight * bm.PixelWidth];
            var pxBgra = new int[bm.PixelHeight * bm.PixelWidth];

            im.Source = bm;
            im.Stretch = Stretch.None;
            im.HorizontalAlignment = HorizontalAlignment.Left;
            im.VerticalAlignment   = VerticalAlignment.Top;

            int pos = 0;
            for (int yI = 0; yI < bm.PixelHeight; yI++) {
                for (int xI = 0; xI < bm.PixelWidth; xI++) {
                    double y = 2.6666666666666 / scale * (bm.PixelHeight / 2 - yI) / bm.PixelHeight;
                    double x = 2.6666666666666 / scale * (xI - bm.PixelWidth / 2) / bm.PixelHeight;
                    var z = new WWComplex(x, y);

                    var h = TransferFunction.EvalH(numerators, denominators, z);
                    var hM = h.Magnitude();

                    if (hM < 0.1) {
                        hM = 0.1;
                    }
                    float hL = (float)((Math.Log10(hM) + 1.0f) / 5.0f);
                    pxF[pos] = hL;
                    pxBgra[pos] = PhaseToBgra(h.Phase());
                    ++pos;
                }
            }

            switch (dispMode) {
            case PoleZeroDispMode.Magnitude:
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxF, bm.BackBufferStride, 0);
                break;
            case PoleZeroDispMode.Phase:
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxBgra, bm.BackBufferStride, 0);
                break;
            }

            canvasZ.Children.Clear();
            canvasZ.Children.Add(im);

            double circleRadius = 192.0 * scale;
            Ellipse unitCircle = new Ellipse { Width = circleRadius * 2, Height = circleRadius * 2, Stroke = new SolidColorBrush {Color = Colors.Black} };
            unitCircle.Width = circleRadius * 2;
            unitCircle.Height = circleRadius * 2;
            canvasZ.Children.Add(unitCircle);
            Canvas.SetLeft(unitCircle, 256.0 - circleRadius);
            Canvas.SetTop(unitCircle, 256.0 - circleRadius);
            Canvas.SetZIndex(unitCircle, 1);
            canvasZ.Visibility = System.Windows.Visibility.Visible;

        }

        /// <summary>
        /// グラデーションサンプル表示。
        /// </summary>
        private void UpdateGradation() {
            PoleZeroDispMode dispMode = (PoleZeroDispMode)comboBoxPoleZeroDispMode.SelectedIndex;
            float maxMagnitude = 5.0f;

            var im = new Image();

            var bm = new WriteableBitmap(
                (int)canvasGradation.Width,
                (int)canvasGradation.Height,
                96, 
                96,
                dispMode == PoleZeroDispMode.Magnitude ? PixelFormats.Gray32Float : PixelFormats.Bgra32,
                null);
            im.Source = bm;
            im.Stretch = Stretch.None;
            im.HorizontalAlignment = HorizontalAlignment.Left;
            im.VerticalAlignment   = VerticalAlignment.Top;
            
            var pxF = new float[bm.PixelHeight * bm.PixelWidth];
            var pxBgra = new int[bm.PixelHeight * bm.PixelWidth];

            for (int yI = 0; yI < bm.PixelHeight; yI++) {
                double hM = maxMagnitude * yI / bm.PixelHeight;
                if (hM < 0.1) {
                    hM = 0.1;
                }
                float hL = (float)((Math.Log10(hM) + 1.0f) / 5.0f);

                double phase = yI * 2.0 * Math.PI / bm.PixelHeight;
                int bgra = PhaseToBgra(phase);

                for (int xI = 0; xI < bm.PixelWidth; xI++) {
                    // 下から上に塗る。
                    pxF[xI + (bm.PixelHeight - 1 - yI) * bm.PixelWidth] = hL;
                    pxBgra[xI + (bm.PixelHeight - 1 - yI) * bm.PixelWidth] = bgra;
                }
            }
            if (dispMode == PoleZeroDispMode.Magnitude) {
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxF, bm.BackBufferStride, 0);
            } else {
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxBgra, bm.BackBufferStride, 0);
            }
            
            canvasGradation.Children.Clear();
            canvasGradation.Children.Add(im);

            double graduationScale;
            string unitText;
            if (dispMode == PoleZeroDispMode.Magnitude) {
                groupBoxPoleZeroLegend.Header = "Magnitude";
                graduationScale = 1.0;
                unitText = "";
            } else {
                groupBoxPoleZeroLegend.Header = "Phase";
                graduationScale = 60.0;
                unitText = "°";
            }

            labelGradation0.Content = string.Format("{0}{1}", 0.0 * graduationScale, unitText);
            labelGradation1.Content = string.Format("{0}{1}", 1.0 * graduationScale, unitText);
            labelGradation2.Content = string.Format("{0}{1}", 2.0 * graduationScale, unitText);
            labelGradation3.Content = string.Format("{0}{1}", 3.0 * graduationScale, unitText);
            labelGradation4.Content = string.Format("{0}{1}", 4.0 * graduationScale, unitText);
            labelGradation5.Content = string.Format("{0}{1}", 5.0 * graduationScale, unitText);
            labelGradation6.Content = string.Format("{0}{1}", 6.0 * graduationScale, unitText);
        }

        private void LineSetX1Y1X2Y2(Line l, double x1, double y1, double x2, double y2) {
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
        }



        static private string SampleFreqString(float freq) {
            if (freq < 1000) {
                return string.Format("{0}", freq);
            }
            if (freq < 1000 * 10) {
                return string.Format("{0:0.00}k", freq/1000);
            }
            if (freq < 1000 * 100) {
                if (freq / 100 == (int)(freq / 100)) {
                    // 10.00kとか20.00kは10k,20kと書く
                    return string.Format("{0:0}k", freq / 1000);
                }
                return string.Format("{0:0.0}k", freq / 1000);
            }
            if (freq < 1000 * 1000) {
                return string.Format("{0:0}k", freq / 1000);
            }
            if (freq < 1000 * 1000 * 10) {
                if (freq / 10000 == (int)(freq / 10000)) {
                    // 1.00Mとか2.00Mは1M,2Mと書く
                    return string.Format("{0:0}M", freq / 1000 / 1000);
                }
                return string.Format("{0:0.00}M", freq / 1000 / 1000);
            }
            if (freq < 1000 * 1000 * 100) {
                if (freq / 100000 == (int)(freq / 100000)) {
                    // 10.0Mとか20.0Mは10M,20Mと書く
                    return string.Format("{0:0}M", freq / 1000 / 1000);
                }
                return string.Format("{0:0.0}M", freq / 1000 / 1000);
            }
            if (freq < 1000 * 1000 * 1000) {
                return string.Format("{0:0}M", freq / 1000 / 1000);
            }

            return string.Format("{0.00}G", freq / 1000 / 1000 / 1000);
        }

        private double AngleFrequency(int idx) {
            switch (comboBoxFreqScale.SelectedIndex) {
            case (int)FreqScaleType.Linear:
                return Math.PI * idx / FR_LINE_NUM;
            case (int)FreqScaleType.Logarithmic: {
                int sampleFrequency = SAMPLE_FREQS[comboBoxSampleFreq.SelectedIndex];
                double freq0 = LogSampleFreq(sampleFrequency, 0);
                double startLog = Math.Log10(freq0);
                double endLog = Math.Log10(sampleFrequency / 2);
                double freqLog = startLog + (endLog - startLog) * idx / FR_LINE_NUM;
                return Math.PI * (Math.Pow(10, freqLog) / (sampleFrequency / 2));
            }
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }
        }

        private double LogFrequencyX(float freq) {
            int sampleFrequency = SAMPLE_FREQS[comboBoxSampleFreq.SelectedIndex];
            double freq0 = LogSampleFreq(sampleFrequency, 0);
            double startLog = Math.Log10(freq0);
            double endLog = Math.Log10(sampleFrequency / 2);
            double freqLog = Math.Log10(freq);

            return (freqLog - startLog) / (endLog - startLog);
        }

        /// <summary>
        /// 位相目盛りの値
        /// </summary>
        struct PhaseGraduation {
            public double phaseShiftRad;
            public int phaseShiftDeg;

            public PhaseGraduation(double rad, int deg) {
                phaseShiftRad = rad;
                phaseShiftDeg = deg;
            }
        };

        private static PhaseGraduation[] mPhaseGraduationArray = new PhaseGraduation[] {
            new PhaseGraduation(0, 0),
            new PhaseGraduation(-1.0 * Math.PI / 4.0, 45),
            new PhaseGraduation(-2.0 * Math.PI / 4.0, 90),
            new PhaseGraduation(-3.0 * Math.PI / 4.0, 135),
            new PhaseGraduation(-4.0 * Math.PI / 4.0, 180),
            new PhaseGraduation(1.0 * Math.PI / 4.0, -45),
            new PhaseGraduation(2.0 * Math.PI / 4.0, -90),
            new PhaseGraduation(3.0 * Math.PI / 4.0, -135),
            new PhaseGraduation(4.0 * Math.PI / 4.0, -180),
        };

        private void UpdateFR(double[] numerators, double[] denominators) {

            foreach (var item in mLineList) {
                canvasFR.Children.Remove(item);
            }
            mLineList.Clear();
            
            int sampleFrequency = SAMPLE_FREQS[comboBoxSampleFreq.SelectedIndex];

            // calc frequency response

            double[] frMagnitude = new double[FR_LINE_NUM];
            double[] frPhase = new double[FR_LINE_NUM];
            double maxMagnitude = 0.0f;

            for (int i = 0; i < FR_LINE_NUM; ++i) {
                double theta = AngleFrequency(i);
                var z = new WWComplex(Math.Cos(theta), Math.Sin(theta));
                var h = TransferFunction.EvalH(numerators, denominators, z);
                frMagnitude[i] = h.Magnitude();
                if (maxMagnitude < frMagnitude[i]) {
                    maxMagnitude = frMagnitude[i];
                }

                //Console.WriteLine("{0}Hz: {1}dB", sampleFrequency * theta / 2 / Math.PI, 20.0 * Math.Log10(frMagnitude[i]));

                frPhase[i] = h.Phase();
            }

            if (maxMagnitude < float.Epsilon) {
                maxMagnitude = 1.0f;
            }

            // draw result
            PhaseGraduation pg = mPhaseGraduationArray[comboBoxPhaseShift.SelectedIndex];
            double phaseShift = pg.phaseShiftRad;
            labelPhase180.Content = string.Format("{0}", 180 + pg.phaseShiftDeg);
            labelPhase90.Content = string.Format("{0}", 90 + pg.phaseShiftDeg);
            labelPhase0.Content = string.Format("{0}", 0 + pg.phaseShiftDeg);
            labelPhaseM90.Content = string.Format("{0}", -90 + pg.phaseShiftDeg);
            labelPhaseM180.Content = string.Format("{0}", -180 + pg.phaseShiftDeg);

            switch (comboBoxFreqScale.SelectedIndex) {
            case (int)FreqScaleType.Linear:
                for (int i=0; i<mXLabelList.Length; ++i) {
                    mXLineList[i].X1 = mXLineList[i].X2 = FR_LINE_LEFT + FR_LINE_NUM * i / 6;
                    mXLineList[i].Visibility = (1 <= i && i <= 5) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

                    mXLabelList[i].Content = SampleFreqString(sampleFrequency * i / 12.0f);
                    mXLabelList[i].Visibility = (i <= 6) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    Canvas.SetLeft(mXLabelList[i], mXLineList[i].X1 - 10);
                }
                break;
            case (int)FreqScaleType.Logarithmic:
                for (int i = 0; i < mXLabelList.Length; ++i) {
                    if (LogSampleFreq(sampleFrequency, i) == 0) {
                        mXLineList[i].Visibility = System.Windows.Visibility.Collapsed;
                        mXLabelList[i].Visibility = System.Windows.Visibility.Collapsed;
                    } else {
                        mXLineList[i].X1 = mXLineList[i].X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, i));
                        mXLineList[i].Visibility = (1 <= i && i < (int)XLineType.XMax) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

                        mXLabelList[i].Content = SampleFreqString(LogSampleFreq(sampleFrequency, i));
                        if (0 < i && (mXLineList[i].X1 - mXLineList[i - 1].X1) < 20) {
                            // 1個前の数字表示との間隔が狭すぎるので間隔をあける。
                            Canvas.SetLeft(mXLabelList[i], mXLineList[i-1].X1 + 10);
                        } else {
                            Canvas.SetLeft(mXLabelList[i], mXLineList[i].X1 - 10);
                        }
                        mXLabelList[i].Visibility = System.Windows.Visibility.Visible;
                    }
                }
                
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            double magRange = mMagnitudeRange[comboBoxMagnitudeRange.SelectedIndex];

            switch (comboBoxMagScale.SelectedIndex) {
            case (int)MagScaleType.Linear:
                labelMagnitude.Content = "Magnitude";
                labelFRMagMax.Content = string.Format("{0:0.00}", maxMagnitude);
                labelFRMag2.Content = string.Format("{0:0.00}", maxMagnitude * 0.75);
                labelFRMag1.Content = string.Format("{0:0.00}", maxMagnitude * 0.5);
                labelFRMag0.Content = string.Format("{0:0.00}", maxMagnitude * 0.25);
                labelFRMagMin.Content = string.Format("{0:0.00}", 0);

                lineFRMag0.Y1 = lineFRMag0.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT * 1 / 4;
                lineFRMag1.Y1 = lineFRMag1.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT * 2 / 4;
                lineFRMag2.Y1 = lineFRMag2.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT * 3 / 4;
                break;
            case (int)MagScaleType.Logarithmic:
                labelMagnitude.Content = "Magnitude (dB)";
                labelFRMagMax.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude));
                labelFRMag2.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude * magRange));
                labelFRMag1.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude * magRange * magRange));
                labelFRMag0.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude * magRange * magRange * magRange));
                labelFRMagMin.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude * magRange * magRange * magRange * magRange));

                lineFRMag0.Y1 = lineFRMag0.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT * 1 / 4;
                lineFRMag1.Y1 = lineFRMag1.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT * 2 / 4;
                lineFRMag2.Y1 = lineFRMag2.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT * 3 / 4;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            {
                var visibility = (checkBoxShowGain.IsChecked == true) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                labelFRMag0.Visibility = visibility;
                labelFRMag1.Visibility = visibility;
                labelFRMag2.Visibility = visibility;
                labelFRMagMax.Visibility = visibility;
                labelFRMagMin.Visibility = visibility;
                labelMagnitude.Visibility = visibility;
            }

            {
                var visibility = (checkBoxShowPhase.IsChecked == true) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                labelPhase.Visibility = visibility;
                labelPhase0.Visibility = visibility;
                labelPhase90.Visibility = visibility;
                labelPhase180.Visibility = visibility;
                labelPhaseM180.Visibility = visibility;
                labelPhaseM90.Visibility = visibility;
            }

            var lastPosM = new Point();
            var lastPosP = new Point();

            for (int i = 0; i < FR_LINE_NUM; ++i) {
                Point posM = new Point();
                Point posP = new Point();

                double phase = frPhase[i] + phaseShift;
                while (phase <= -Math.PI) {
                    phase += 2.0 * Math.PI;
                }
                while (Math.PI < phase) {
                    phase -= 2.0f * Math.PI;
                }

                posP = new Point(FR_LINE_LEFT + i, FR_LINE_YCENTER - FR_LINE_HEIGHT * phase / (2.0f * Math.PI));

                switch (comboBoxMagScale.SelectedIndex) {
                case (int)MagScaleType.Linear:
                    posM = new Point(FR_LINE_LEFT + i, FR_LINE_BOTTOM - FR_LINE_HEIGHT * frMagnitude[i] / maxMagnitude);
                    break;
                case (int)MagScaleType.Logarithmic:
                    posM = new Point(FR_LINE_LEFT + i,
                        FR_LINE_TOP + FR_LINE_HEIGHT * 20.0 * Math.Log10(frMagnitude[i] / maxMagnitude) / (20.0 * Math.Log10(magRange * magRange * magRange * magRange)));
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }

                if (1 <= i) {
                    bool bDraw = true;
                    switch (comboBoxMagScale.SelectedIndex) {
                    case (int)MagScaleType.Logarithmic:
                        if (FR_LINE_BOTTOM < posM.Y || FR_LINE_BOTTOM < lastPosM.Y) {
                            bDraw = false;
                        }
                        break;
                    }


                    if (bDraw && (checkBoxShowGain.IsChecked == true)) {
                        var lineM = new Line();
                        lineM.Stroke = Brushes.Blue;
                        LineSetX1Y1X2Y2(lineM, lastPosM.X, lastPosM.Y, posM.X, posM.Y);
                        mLineList.Add(lineM);
                        canvasFR.Children.Add(lineM);
                    }
                }

                if (2 <= i && (checkBoxShowPhase.IsChecked == true)) {
                    var lineP = new Line();
                    lineP.Stroke = Brushes.Red;
                    LineSetX1Y1X2Y2(lineP, lastPosP.X, lastPosP.Y, posP.X, posP.Y);
                    mLineList.Add(lineP);
                    canvasFR.Children.Add(lineP);
                }
                lastPosP = posP;
                lastPosM = posM;
            }
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            var numerators = new double[9];
            var denominators = new double[9];

            numerators[0] = double.Parse(textBoxN0.Text);
            numerators[1] = double.Parse(textBoxN1.Text);
            numerators[2] = double.Parse(textBoxN2.Text);
            numerators[3] = double.Parse(textBoxN3.Text);
            numerators[4] = double.Parse(textBoxN4.Text);
            numerators[5] = double.Parse(textBoxN5.Text);
            numerators[6] = double.Parse(textBoxN6.Text);
            numerators[7] = double.Parse(textBoxN7.Text);
            numerators[8] = double.Parse(textBoxN8.Text);

            denominators[0] = 1.0;
            denominators[1] = double.Parse(textBoxD1.Text);
            denominators[2] = double.Parse(textBoxD2.Text);
            denominators[3] = double.Parse(textBoxD3.Text);
            denominators[4] = double.Parse(textBoxD4.Text);
            denominators[5] = double.Parse(textBoxD5.Text);
            denominators[6] = double.Parse(textBoxD6.Text);
            denominators[7] = double.Parse(textBoxD7.Text);
            denominators[8] = double.Parse(textBoxD8.Text);

            UpdateZ(numerators, denominators);
            UpdateGradation();
            UpdateFR(numerators, denominators);
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

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
            Reset();
        }

        private void comboBoxFreqScale_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void comboBoxMagScale_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void comboBoxSampleFreq_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e) {
            Reset();
        }

        private void comboBoxPhaseShift_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void comboBoxPoleZeroScale_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void comboBoxPoleZeroDispMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void checkBoxShowPhase_Checked(object sender, RoutedEventArgs e) {
            Update();
        }

        private void checkBoxShowGain_Checked(object sender, RoutedEventArgs e) {
            Update();
        }

        private void checkBoxShowGain_Unchecked(object sender, RoutedEventArgs e) {
            Update();
        }

        private void checkBoxShowPhase_Unchecked(object sender, RoutedEventArgs e) {
            Update();
        }

        private void checkBoxShowPoleZero_Checked(object sender, RoutedEventArgs e) {
            Update();
        }

        private void checkBoxShowPoleZero_Unchecked(object sender, RoutedEventArgs e) {
            Update();
        }

        private void comboBoxMagnitudeRange_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void comboBoxStartFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void buttonPlotFromImage_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "image files|*.png";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            var itfr = new ImageToFreqResponse();
            var freqResponse = itfr.Run(dlg.FileName);

            Update();
            UpdateFRFrom(freqResponse, 2);
        }

        private void UpdateFRFrom(WWComplex[] fr, int period) {
            PhaseGraduation pg = mPhaseGraduationArray[comboBoxPhaseShift.SelectedIndex];
            double phaseShift = pg.phaseShiftRad;

            foreach (var item in mLineList) {
                canvasFR.Children.Remove(item);
            }
            mLineList.Clear();

            double maxMagnitude = 0.0;
            for (int i = 0; i < fr.Length; ++i) {
                if (maxMagnitude < fr[i].Magnitude()) {
                    maxMagnitude = fr[i].Magnitude();
                }
            }

            double magRange = mMagnitudeRange[comboBoxMagnitudeRange.SelectedIndex];

            var lastPosM = new Point();
            var lastPosP = new Point();

            lastPosP.X = -1;

            int N = fr.Length / 2;
            double MAGNITUDE_THRESHOLD = 0.2;

            for (int i = 0; i < N; ++i) {
                Point posM = new Point();
                Point posP = new Point();

                double phase = fr[i].Phase() + phaseShift;
                while (phase <= -Math.PI) {
                    phase += 2.0 * Math.PI;
                }
                while (Math.PI < phase) {
                    phase -= 2.0f * Math.PI;
                }

                int x = FR_LINE_NUM * i / N;

                posP = new Point(FR_LINE_LEFT + x,
                    FR_LINE_YCENTER - FR_LINE_HEIGHT * phase / (2.0f * Math.PI));

                switch (comboBoxMagScale.SelectedIndex) {
                case (int)MagScaleType.Linear:
                    posM = new Point(FR_LINE_LEFT + x, FR_LINE_BOTTOM - FR_LINE_HEIGHT * fr[i].Magnitude() / maxMagnitude);
                    break;
                case (int)MagScaleType.Logarithmic:
                    posM = new Point(FR_LINE_LEFT + x,
                        FR_LINE_TOP + FR_LINE_HEIGHT * 20.0 * Math.Log10(fr[i].Magnitude() / maxMagnitude)
                            / (20.0 * Math.Log10(magRange * magRange * magRange * magRange)));
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }

                if (1 <= i) {
                    bool bDraw = true;
                    switch (comboBoxMagScale.SelectedIndex) {
                    case (int)MagScaleType.Logarithmic:
                        if (FR_LINE_BOTTOM < posM.Y || FR_LINE_BOTTOM < lastPosM.Y) {
                            bDraw = false;
                        }
                        break;
                    }


                    if (bDraw && (checkBoxShowGain.IsChecked == true)) {
                        var lineM = new Line();
                        lineM.Stroke = Brushes.Blue;
                        LineSetX1Y1X2Y2(lineM, lastPosM.X, lastPosM.Y, posM.X, posM.Y);
                        mLineList.Add(lineM);
                        canvasFR.Children.Add(lineM);
                    }
                }

                if (0 <= lastPosP.X && 2 <= i && (checkBoxShowPhase.IsChecked == true)
                        && MAGNITUDE_THRESHOLD < fr[i].Magnitude() / maxMagnitude) {
                    var lineP = new Line();
                    lineP.Stroke = Brushes.Red;
                    LineSetX1Y1X2Y2(lineP, lastPosP.X, lastPosP.Y, posP.X, posP.Y);
                    mLineList.Add(lineP);
                    canvasFR.Children.Add(lineP);
                }

                if (MAGNITUDE_THRESHOLD < fr[i].Magnitude() / maxMagnitude) {
                    lastPosP = posP;
                }
                lastPosM = posM;
            }
        }

    }
}
