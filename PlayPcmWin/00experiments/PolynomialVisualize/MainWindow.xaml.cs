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

        private bool mInitialized = false;

        float [] mDenominators = new float[9];
        float [] mNumerators = new float[9];

        private WWComplex EvalH(WWComplex z) {
            var zRecip = new WWComplex(z).Reciprocal();
#if true
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
#else
            int D = 2205;
            var zRecipArray = new WWComplex[D*4+1];
            zRecipArray[0] = new WWComplex(1, 0);
            for (int i = 1; i < zRecipArray.Length; ++i) {
                zRecipArray[i] = new WWComplex(zRecip).Mul(zRecipArray[i - 1]);
            }
            var ma = new WWComplex(1,0).Sub(zRecipArray[D]).Div(new WWComplex(1,0).Sub(zRecipArray[1]).Mul(D));
            var ma2 = ma.Mul(ma);
            var ma4 = ma2.Mul(ma2);
            var h = new WWComplex(zRecipArray[2 * D - 2]).Sub(ma4);
#endif
            // 孤立特異点や極で起きる異常を適当に除去する
            if (double.IsNaN(h.Magnitude())) {
                return new WWComplex(0.0f, 0.0f);
            }
            return h;
        }

        private int PhaseToBgra(double phase) {
            return Util.HsvToBgra(-phase * 180.0 / Math.PI + 240.0, 1.0, 1.0);
        }

        private void UpdateZ()
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

                    var h = EvalH(z);
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
                return new int[] { 1, 10, 100, 1000, 10000, 20000 } [idx];
            case 2822400:
            case 5644800:
            case 11289600:
                return new int[] { 100, 1000,      10 * 1000, 20*1000,    100 * 1000,  1000 * 1000 }[idx];
            case 22579200:
                return new int[] { 1000, 10 * 1000, 20 * 1000, 100 * 1000, 1000 * 1000, 10 * 1000 * 1000 }[idx];
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

        private static double[] mMagnitudeRange = new double[] {
            1.0/16,
            0.97162795157710617416734286616884
        };

        private void UpdateFR() {
            foreach (var item in mLineList) {
                canvasFR.Children.Remove(item);
            }
            mLineList.Clear();

            // calc frequency response

            double[] frMagnitude = new double[FR_LINE_NUM];
            double[] frPhase = new double[FR_LINE_NUM];
            double maxMagnitude = 0.0f;

            for (int i = 0; i < FR_LINE_NUM; ++i) {
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

            PhaseGraduation pg = mPhaseGraduationArray[comboBoxPhaseShift.SelectedIndex];
            double phaseShift = pg.phaseShiftRad;
            labelPhase180.Content = string.Format("{0}", 180 + pg.phaseShiftDeg);
            labelPhase90.Content = string.Format("{0}", 90 + pg.phaseShiftDeg);
            labelPhase0.Content = string.Format("{0}", 0 + pg.phaseShiftDeg);
            labelPhaseM90.Content = string.Format("{0}", -90 + pg.phaseShiftDeg);
            labelPhaseM180.Content = string.Format("{0}", -180 + pg.phaseShiftDeg);

            switch (comboBoxFreqScale.SelectedIndex) {
            case (int)FreqScaleType.Linear:
                labelFR10.Visibility = Visibility.Visible;
                labelFRMax.Visibility = System.Windows.Visibility.Hidden;
                labelFR1.Content = SampleFreqString(0);
                labelFR10.Content = SampleFreqString(sampleFrequency * 1.0f / 10);
                labelFR100.Content = SampleFreqString(sampleFrequency * 2.0f / 10);
                labelFR1k.Content = SampleFreqString(sampleFrequency * 3.0f / 10);
                labelFR10k.Content = SampleFreqString(sampleFrequency * 4.0f / 10);
                labelFR20k.Content = SampleFreqString(sampleFrequency * 5.0f / 10);

                lineFR10.X1 = lineFR10.X2 = FR_LINE_LEFT + FR_LINE_NUM * 1 / 5;
                lineFR100.X1 = lineFR100.X2 = FR_LINE_LEFT + FR_LINE_NUM * 2 / 5;
                lineFR1k.X1 = lineFR1k.X2 = FR_LINE_LEFT + FR_LINE_NUM * 3 / 5;
                lineFR10k.X1 = lineFR10k.X2 = FR_LINE_LEFT + FR_LINE_NUM * 4 / 5;
                lineFR20k.Visibility = System.Windows.Visibility.Hidden;

                Canvas.SetLeft(labelFR1, FR_LINE_LEFT - 10);
                Canvas.SetLeft(labelFR10, lineFR10.X1 - 20);
                Canvas.SetLeft(labelFR100, lineFR100.X1 - 20);
                Canvas.SetLeft(labelFR1k, lineFR1k.X1 - 20);
                Canvas.SetLeft(labelFR10k, lineFR10k.X1 - 20);
                Canvas.SetLeft(labelFR20k, FR_LINE_LEFT + FR_LINE_NUM - 20);
                break;
            case (int)FreqScaleType.Logarithmic:
                labelFR10.Visibility = Visibility.Visible;
                labelFRMax.Visibility = System.Windows.Visibility.Visible;
                labelFR1.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 0));
                labelFR10.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 1));
                labelFR100.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 2));
                labelFR1k.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 3));
                labelFR10k.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 4));
                labelFR20k.Content = SampleFreqString(LogSampleFreq(sampleFrequency, 5));
                labelFRMax.Content = SampleFreqString(sampleFrequency / 2);
                lineFR10.X1 = lineFR10.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 1));
                lineFR100.X1 = lineFR100.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 2));
                lineFR1k.X1 = lineFR1k.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 3));
                lineFR10k.X1 = lineFR10k.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 4));
                lineFR20k.X1 = lineFR20k.X2 = FR_LINE_LEFT + FR_LINE_NUM * LogFrequencyX(LogSampleFreq(sampleFrequency, 5));
                lineFR20k.Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(labelFR1, FR_LINE_LEFT - 20);
                Canvas.SetLeft(labelFR10, lineFR10.X1 - 20);
                Canvas.SetLeft(labelFR100, lineFR100.X1 - 20);
                Canvas.SetLeft(labelFR1k, lineFR1k.X1 - 20);
                Canvas.SetLeft(labelFR10k, lineFR10k.X1 - 20);
                Canvas.SetLeft(labelFR20k, lineFR20k.X1 - 25);
                Canvas.SetLeft(labelFRMax, FR_LINE_LEFT + FR_LINE_NUM);
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
            UpdateGradation();
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
    }
}
