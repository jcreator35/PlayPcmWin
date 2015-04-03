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
        public MainWindow() {
            InitializeComponent();
        }

        enum PoleZeroType {
            Zero,
            Other,
            Pole
        };

        private bool mInitialized = false;

        float [] mDenominators = new float[9];
        float [] mNumerators = new float[9];

        private WWComplex EvalH(WWComplex z) {
            var zRecip = new WWComplex(z).Reciprocal();

            var zRecip2 = new WWComplex(zRecip).Mul(zRecip);
            var zRecip3 = new WWComplex(zRecip2).Mul(zRecip);
            var zRecip4 = new WWComplex(zRecip3).Mul(zRecip);
            var zRecip5 = new WWComplex(zRecip4).Mul(zRecip);
            var zRecip6 = new WWComplex(zRecip5).Mul(zRecip);
            var zRecip7 = new WWComplex(zRecip6).Mul(zRecip);
            var zRecip8 = new WWComplex(zRecip7).Mul(zRecip);

            var hDenom0 = new WWComplex(mDenominators[0], 0.0f);
            var hDenom1 = new WWComplex(mDenominators[1], 0.0f).Mul(zRecip);
            var hDenom2 = new WWComplex(mDenominators[2], 0.0f).Mul(zRecip2);
            var hDenom3 = new WWComplex(mDenominators[3], 0.0f).Mul(zRecip3);
            var hDenom4 = new WWComplex(mDenominators[4], 0.0f).Mul(zRecip4);
            var hDenom5 = new WWComplex(mDenominators[5], 0.0f).Mul(zRecip5);
            var hDenom6 = new WWComplex(mDenominators[6], 0.0f).Mul(zRecip6);
            var hDenom7 = new WWComplex(mDenominators[7], 0.0f).Mul(zRecip7);
            var hDenom8 = new WWComplex(mDenominators[8], 0.0f).Mul(zRecip8);
            var hDenom = new WWComplex(hDenom0).Add(hDenom1).Add(hDenom2).Add(hDenom3).Add(hDenom4).Add(hDenom5).Add(hDenom6).Add(hDenom7).Add(hDenom8).Reciprocal();

            var hNumer0 = new WWComplex(mNumerators[0], 0.0f);
            var hNumer1 = new WWComplex(mNumerators[1], 0.0f).Mul(zRecip);
            var hNumer2 = new WWComplex(mNumerators[2], 0.0f).Mul(zRecip2);
            var hNumer3 = new WWComplex(mNumerators[3], 0.0f).Mul(zRecip3);
            var hNumer4 = new WWComplex(mNumerators[4], 0.0f).Mul(zRecip4);
            var hNumer5 = new WWComplex(mNumerators[5], 0.0f).Mul(zRecip5);
            var hNumer6 = new WWComplex(mNumerators[6], 0.0f).Mul(zRecip6);
            var hNumer7 = new WWComplex(mNumerators[7], 0.0f).Mul(zRecip7);
            var hNumer8 = new WWComplex(mNumerators[8], 0.0f).Mul(zRecip8);
            var hNumer = new WWComplex(hNumer0).Add(hNumer1).Add(hNumer2).Add(hNumer3).Add(hNumer4).Add(hNumer5).Add(hNumer6).Add(hNumer7).Add(hNumer8);
            var h = new WWComplex(hNumer).Mul(hDenom);

            // 孤立特異点や極で起きる異常を適当に除去する
            if (double.IsNaN(h.Magnitude())) {
                return new WWComplex(0.0f, 0.0f);
            }
            return h;
        }

        private void UpdateZ()
        {
            canvasZ.Children.Clear();

            var im = new Image();

            var bm = new WriteableBitmap(
                512,
                512,
                96, 
                96, 
                PixelFormats.Gray32Float, 
                null);
            im.Source = bm;
            im.Stretch = Stretch.None;
            im.HorizontalAlignment = HorizontalAlignment.Left;
            im.VerticalAlignment   = VerticalAlignment.Top;
            
            var px = new float[bm.PixelHeight*bm.PixelWidth];

            int pos = 0;
            for (int yI = 0; yI < bm.PixelHeight; yI++) {
                for (int xI = 0; xI < bm.PixelWidth; xI++) {
                    double y = 2.6666666666666 * (bm.PixelHeight / 2 - yI) / bm.PixelHeight;
                    double x = 2.6666666666666 * (xI - bm.PixelWidth / 2) / bm.PixelHeight;
                    var z = new WWComplex(x, y);

                    var h = EvalH(z);
                    var hM = h.Magnitude();

                    if (hM < 0.1) {
                        hM = 0.1;
                    }
                    float hL = (float)((Math.Log10(hM) + 1.0f) / 5.0f);
                    px[pos] = hL;
                    ++pos;
                }
            }

            bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), px, bm.BackBufferStride, 0);

            canvasZ.Children.Add(im);
        }

        private void LineSetX1Y1X2Y2(Line l, double x1, double y1, double x2, double y2) {
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
        }

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

        private readonly int [] SAMPLE_FREQS = new int [] {
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

        private List<Line> mLineList = new List<Line>();
        private const int FR_LINE_LEFT    = 64;
        private const int FR_LINE_HEIGHT  = 256;
        private const int FR_LINE_NUM     = 512;
        private const int FR_LINE_TOP     = 32;
        private const int FR_LINE_BOTTOM  = FR_LINE_TOP + FR_LINE_HEIGHT;
        private const int FR_LINE_YCENTER = (FR_LINE_TOP + FR_LINE_BOTTOM)/2;

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

        static private float LogSampleFreq(int freq, int idx) {
            switch (freq) {
            case 44100:
            case 48000:
            case 88200:
            case 96000:
            case 176400:
            case 192000:
                return new int[] { 10, 100, 1000, 10000, 20000 } [idx];
            case 2822400:
            case 5644800:
            case 11289600:
                return new int[] { 1000,      10 * 1000, 20*1000,    100 * 1000,  1000 * 1000 }[idx];
            case 22579200:
                return new int[] { 10 * 1000, 20 * 1000, 100 * 1000, 1000 * 1000, 10 * 1000 * 1000 }[idx];
            default:
                System.Diagnostics.Debug.Assert(false);
                return 10;
            }
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

        private void UpdateFR() {
            foreach (var item in mLineList) {
                canvasFR.Children.Remove(item);
            }
            mLineList.Clear();

            // calc frequency response

            double [] frMagnitude = new double[FR_LINE_NUM];
            double [] frPhase     = new double[FR_LINE_NUM];
            double maxMagnitude = 0.0f;

            for (int i=0; i < FR_LINE_NUM; ++i) {
                double theta = AngleFrequency(i);
                var z = new WWComplex(Math.Cos(theta), Math.Sin(theta));
                var h = EvalH(z);
                frMagnitude[i] = h.Magnitude();
                if (maxMagnitude < frMagnitude[i]) {
                    maxMagnitude = frMagnitude[i];
                }
                frPhase[i] = h.Phase();
            }

            if (maxMagnitude < float.Epsilon) {
                maxMagnitude = 1.0f;
            }

            // draw result
            int sampleFrequency = SAMPLE_FREQS[comboBoxSampleFreq.SelectedIndex];

            double phaseShift = 0;

            // 配列に入れてデータ化するといい。
            switch (comboBoxPhaseShift.SelectedIndex) {
            case (int)PhaseShiftType.Zero:
                phaseShift = 0;
                labelPhase180.Content = "180";
                labelPhase90.Content = "90";
                labelPhase0.Content = "0";
                labelPhaseM90.Content = "-90";
                labelPhaseM180.Content = "-180";
                break;
            case (int)PhaseShiftType.P45:
                phaseShift = -1.0 * Math.PI / 4.0;
                labelPhase180.Content = "225";
                labelPhase90.Content = "135";
                labelPhase0.Content = "45";
                labelPhaseM90.Content = "-45";
                labelPhaseM180.Content = "-135";
                break;
            case (int)PhaseShiftType.P90:
                phaseShift = -2.0 * Math.PI / 4.0;
                labelPhase180.Content = "270";
                labelPhase90.Content = "180";
                labelPhase0.Content = "90";
                labelPhaseM90.Content = "00";
                labelPhaseM180.Content = "-90";
                break;
            case (int)PhaseShiftType.P135:
                phaseShift = -3.0 * Math.PI / 4.0;
                labelPhase180.Content = "315";
                labelPhase90.Content = "225";
                labelPhase0.Content = "135";
                labelPhaseM90.Content = "45";
                labelPhaseM180.Content = "-45";
                break;
            case (int)PhaseShiftType.P180:
                phaseShift = -4.0 * Math.PI / 4.0;
                labelPhase180.Content = "360";
                labelPhase90.Content = "270";
                labelPhase0.Content = "180";
                labelPhaseM90.Content = "90";
                labelPhaseM180.Content = "0";
                break;
            case (int)PhaseShiftType.M45:
                phaseShift = 1.0 * Math.PI / 4.0;
                labelPhase180.Content = "135";
                labelPhase90.Content = "45";
                labelPhase0.Content = "-45";
                labelPhaseM90.Content = "-135";
                labelPhaseM180.Content = "-225";
                break;
            case (int)PhaseShiftType.M90:
                phaseShift = 2.0 * Math.PI / 4.0;
                labelPhase180.Content = "90";
                labelPhase90.Content = "0";
                labelPhase0.Content = "-90";
                labelPhaseM90.Content = "-180";
                labelPhaseM180.Content = "-270";
                break;
            case (int)PhaseShiftType.M135:
                phaseShift = 3.0 * Math.PI/4.0;
                labelPhase180.Content = "45";
                labelPhase90.Content = "-45";
                labelPhase0.Content = "-135";
                labelPhaseM90.Content = "-225";
                labelPhaseM180.Content = "-315";
                break;
            case (int)PhaseShiftType.M180:
                phaseShift = Math.PI;
                labelPhase180.Content = "0";
                labelPhase90.Content = "-90";
                labelPhase0.Content = "-180";
                labelPhaseM90.Content = "-270";
                labelPhaseM180.Content = "-360";
                break;
            }

            switch (comboBoxFreqScale.SelectedIndex) {
            case (int)FreqScaleType.Linear:
                labelFRMin.Visibility = Visibility.Visible;
                labelFRMax.Visibility = System.Windows.Visibility.Hidden;
                labelFRMin.Content = SampleFreqString(0);
                labelFR0.Content = SampleFreqString(sampleFrequency * 1.0f / 8);
                labelFR1.Content = SampleFreqString(sampleFrequency * 2.0f / 8);
                labelFR2.Content = SampleFreqString(sampleFrequency * 3.0f / 8);
                labelFR3.Content = SampleFreqString(sampleFrequency * 4.0f / 8);
                lineFR0.X1 = lineFR0.X2 = FR_LINE_LEFT + FR_LINE_NUM * 1 / 4;
                lineFR1.X1 = lineFR1.X2 = FR_LINE_LEFT + FR_LINE_NUM * 2 / 4;
                lineFR2.X1 = lineFR2.X2 = FR_LINE_LEFT + FR_LINE_NUM * 3 / 4;
                lineFR3.Visibility = System.Windows.Visibility.Hidden;
                Canvas.SetLeft(labelFRMin, FR_LINE_LEFT - 10);
                Canvas.SetLeft(labelFR0, lineFR0.X1 - 20);
                Canvas.SetLeft(labelFR1, lineFR1.X1 - 20);
                Canvas.SetLeft(labelFR2, lineFR2.X1 - 20);
                Canvas.SetLeft(labelFR3, FR_LINE_LEFT + FR_LINE_NUM - 20);
                break;
            case (int)FreqScaleType.Logarithmic:
                labelFRMin.Visibility = Visibility.Visible;
                labelFRMax.Visibility = System.Windows.Visibility.Visible;
                labelFRMin.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 0));
                labelFR0.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 1));
                labelFR1.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 2));
                labelFR2.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 3));
                labelFR3.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 4));
                labelFRMax.Content = SampleFreqString(sampleFrequency/2);
                lineFR0.X1 = lineFR0.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 1));
                lineFR1.X1 = lineFR1.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 2));
                lineFR2.X1 = lineFR2.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 3));
                lineFR3.X1 = lineFR3.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 4));
                lineFR3.Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(labelFRMin, FR_LINE_LEFT - 20);
                Canvas.SetLeft(labelFR0, lineFR0.X1 - 20);
                Canvas.SetLeft(labelFR1, lineFR1.X1 - 20);
                Canvas.SetLeft(labelFR2, lineFR2.X1 - 20);
                Canvas.SetLeft(labelFR3, lineFR3.X1 - 25);
                Canvas.SetLeft(labelFRMax, FR_LINE_LEFT + FR_LINE_NUM);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            switch (comboBoxMagScale.SelectedIndex) {
            case (int)MagScaleType.Linear:
                labelMagnitude.Content = "Magnitude";
                labelFRMagMax.Content = string.Format("{0:0.00}", maxMagnitude);
                labelFRMag2.Content   = string.Format("{0:0.00}", maxMagnitude*0.75);
                labelFRMag1.Content   = string.Format("{0:0.00}", maxMagnitude*0.5);
                labelFRMag0.Content   = string.Format("{0:0.00}", maxMagnitude*0.25);
                labelFRMagMin.Content = string.Format("{0:0.00}", 0);

                lineFRMag0.Y1 = lineFRMag0.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT *1 / 4;
                lineFRMag1.Y1 = lineFRMag1.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT *2 / 4;
                lineFRMag2.Y1 = lineFRMag2.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT *3 / 4;
                break;
            case (int)MagScaleType.Logarithmic:
                labelMagnitude.Content = "Magnitude (dB)";
                labelFRMagMax.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude));
                labelFRMag2.Content   = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude / 16));
                labelFRMag1.Content   = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude / 256));
                labelFRMag0.Content   = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude / 4096));
                labelFRMagMin.Content = string.Format("{0:0.00}", 20.0 * Math.Log10(maxMagnitude / 65536));

                lineFRMag0.Y1 = lineFRMag0.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT *1 / 4;
                lineFRMag1.Y1 = lineFRMag1.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT *2 / 4;
                lineFRMag2.Y1 = lineFRMag2.Y2 = FR_LINE_BOTTOM - FR_LINE_HEIGHT *3 / 4;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }


            var lastPosM = new Point();
            var lastPosP = new Point();

            for (int i=0; i < FR_LINE_NUM; ++i) {
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
                        FR_LINE_TOP + FR_LINE_HEIGHT * 20.0 * Math.Log10(frMagnitude[i] / maxMagnitude) / (20.0 * Math.Log10(1.0/65536)) );
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


                    if (bDraw) {
                        var lineM = new Line();
                        lineM.Stroke = Brushes.Blue;
                        LineSetX1Y1X2Y2(lineM, lastPosM.X, lastPosM.Y, posM.X, posM.Y);
                        mLineList.Add(lineM);
                        canvasFR.Children.Add(lineM);
                    }
                }

                if (2 <= i) {
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

            mDenominators[0] = 1.0f;
            mDenominators[1] = float.Parse(textBoxD1.Text);
            mDenominators[2] = float.Parse(textBoxD2.Text);
            mDenominators[3] = float.Parse(textBoxD3.Text);
            mDenominators[4] = float.Parse(textBoxD4.Text);
            mDenominators[5] = float.Parse(textBoxD5.Text);
            mDenominators[6] = float.Parse(textBoxD6.Text);
            mDenominators[7] = float.Parse(textBoxD7.Text);
            mDenominators[8] = float.Parse(textBoxD8.Text);

            mNumerators[0] = float.Parse(textBoxN0.Text);
            mNumerators[1] = float.Parse(textBoxN1.Text);
            mNumerators[2] = float.Parse(textBoxN2.Text);
            mNumerators[3] = float.Parse(textBoxN3.Text);
            mNumerators[4] = float.Parse(textBoxN4.Text);
            mNumerators[5] = float.Parse(textBoxN5.Text);
            mNumerators[6] = float.Parse(textBoxN6.Text);
            mNumerators[7] = float.Parse(textBoxN7.Text);
            mNumerators[8] = float.Parse(textBoxN8.Text);

            UpdateZ();
            UpdateFR();
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
    }
}
