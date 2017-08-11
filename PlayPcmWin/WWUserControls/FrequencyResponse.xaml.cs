using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WWMath;

namespace WWUserControls {
    /// <summary>
    /// Interaction logic for FrequencyResponse.xaml
    /// </summary>
    public partial class FrequencyResponse : UserControl {
        public FrequencyResponse() {
            InitializeComponent();
            SamplingFrequency = 44100;
            mInitialized = true;
        }

        private bool mInitialized = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            Update();
        }

        public enum ModeType {
            SPlane,
            ZPlane
        };

        private ModeType mMode = ModeType.SPlane;

        public ModeType Mode {
            get { return mMode; }
            set { mMode = value; ModeChanged(); }
        }

        private const int FR_LINE_LEFT = 64;
        private const int FR_LINE_HEIGHT = 256;
        private const int FR_LINE_WIDTH = 512;
        private const int FR_LINE_TOP = 32;
        private const int FR_LINE_BOTTOM = FR_LINE_TOP + FR_LINE_HEIGHT;
        private const int FR_LINE_YCENTER = (FR_LINE_TOP + FR_LINE_BOTTOM) / 2;

        private const double DISP_MAG_THRESHOLD = 0.001 * 0.001 * 0.001;

        private bool mShowGain = true;
        private bool mShowPhase = true;
        private bool mShowGroupDelay = true;

        public bool ShowGain {
            get {
                return mShowGain;
            }
            set {
                mShowGain = value;
                checkBoxShowGain.IsChecked = mShowGain;
            }
        }
        public bool ShowPhase {
            get {
                return mShowPhase;
            }
            set {
                mShowPhase = value;
                checkBoxShowPhase.IsChecked = mShowPhase;
            }
        }
        public bool ShowGroupDelay {
            get {
                return mShowGroupDelay;
            }
            set {
                mShowGroupDelay = value;
                checkBoxShowGroupDelay.IsChecked = mShowGroupDelay;
            }
        }

        public void UpdateMagnitudeRange(MagnitudeRangeType t) {
            comboBoxMagRange.SelectedIndex = (int)t;
        }

        public enum FreqScaleType {
            Linear,
            Logarithmic,
        };

        public double SamplingFrequency { get; set; }

        public enum FreqRangeType {
            SF_0_0001HzTo1Hz,
            SF_0_001HzTo10Hz,
            SF_0_01HzTo100Hz,
            SF_0_1HzTo1kHz,
            SF_1HzTo10kHz,
            SF_10HzTo100kHz,
            SF_10HzTo1MHz,
            SF_10HzTo10MHz,
        };

        public enum MagScaleType {
            Linear,
            Logarithmic,
        };

        public enum MagnitudeRangeType {
            M0_1dB,
            M0_3dB,
            M1dB,
            M3dB,
            M6dB,
            M12dB,
            M24dB,
            M48dB,
            M72dB,
            M96dB,
            M120dB,
            M140dB,
            M160dB,
            M180dB
        }

        private Tuple<double, double> FreqStartEnd() {
            switch ((FreqScaleType)comboBoxFreqScale.SelectedIndex) {
            case FreqScaleType.Logarithmic:
                // 対数の時、開始周波数は0.1, 1, 10, 100, …でなければならない
                switch ((FreqRangeType)comboBoxFreqRange.SelectedIndex) {
                case FreqRangeType.SF_0_0001HzTo1Hz:
                    return new Tuple<double, double>(0.0001, 1);
                case FreqRangeType.SF_0_001HzTo10Hz:
                    return new Tuple<double, double>(0.001, 10);
                case FreqRangeType.SF_0_01HzTo100Hz:
                    return new Tuple<double, double>(0.01, 100);
                case FreqRangeType.SF_0_1HzTo1kHz:
                    return new Tuple<double, double>(0.1, 1000);
                case FreqRangeType.SF_1HzTo10kHz:
                    return new Tuple<double, double>(1, 10 * 1000);
                case FreqRangeType.SF_10HzTo100kHz:
                    return new Tuple<double, double>(10, 100 * 1000);
                case FreqRangeType.SF_10HzTo1MHz:
                    return new Tuple<double, double>(10, 1000 * 1000);
                case FreqRangeType.SF_10HzTo10MHz:
                    return new Tuple<double, double>(10, 10 * 1000 * 1000);
                }
                System.Diagnostics.Debug.Assert(false);
                break;
            case FreqScaleType.Linear:
                switch ((FreqRangeType)comboBoxFreqRange.SelectedIndex) {
                case FreqRangeType.SF_0_0001HzTo1Hz:
                    return new Tuple<double, double>(0, 1);
                case FreqRangeType.SF_0_001HzTo10Hz:
                    return new Tuple<double, double>(0, 10);
                case FreqRangeType.SF_0_01HzTo100Hz:
                    return new Tuple<double, double>(0, 100);
                case FreqRangeType.SF_0_1HzTo1kHz:
                    return new Tuple<double, double>(0, 1000);
                case FreqRangeType.SF_1HzTo10kHz:
                    return new Tuple<double, double>(0, 10 * 1000);
                case FreqRangeType.SF_10HzTo100kHz:
                    return new Tuple<double, double>(0, 100 * 1000);
                case FreqRangeType.SF_10HzTo1MHz:
                    return new Tuple<double, double>(0, 1000 * 1000);
                case FreqRangeType.SF_10HzTo10MHz:
                    return new Tuple<double, double>(0, 10 * 1000 * 1000);
                }
                break;
            }

            System.Diagnostics.Debug.Assert(false);
            return new Tuple<double, double>(0, 100 * 1000);
        }

        /// <summary>
        /// Magnitude ScaleNumeratorCoeffs(対数軸)の8乗根を戻す。目盛が8つあるので。
        /// </summary>
        private double MagnitudeRangeValue() {
            double[] mRange = new double[] {
                Math.Pow(Math.Pow(10.0,-0.1/20.0), 1.0/8.0), // -0.1dBの8乗根
                Math.Pow(Math.Pow(10.0,-0.3/20.0), 1.0/8.0), // -0.3dBの8乗根
                Math.Pow(Math.Pow(10.0, -1.0/20.0), 1.0/8.0), // -1dBの8乗根
                Math.Pow(1.0/Math.Sqrt(2.0), 1.0/8.0), // -3dBの8乗根
                Math.Pow(1.0/2.0, 1.0/8.0), // -6dBの8乗根
                Math.Pow(1.0/4.0, 1.0/8.0), // -12dBの8乗根
                Math.Pow(1.0/16.0, 1.0/8.0), // -24dBの8乗根
                Math.Pow(1.0/256.0, 1.0/8.0), // -48dBの8乗根
                Math.Pow(1.0/4096.0, 1.0/8.0), // -72dBの8乗根
                Math.Pow(1.0/65536, 1.0/8.0), // -96dBの8乗根
                Math.Pow(Math.Pow(10.0,-120.0/20.0), 1.0/8.0), // -120dBの8乗根。
                Math.Pow(Math.Pow(10.0,-140.0/20.0), 1.0/8.0),
                Math.Pow(Math.Pow(10.0,-160.0/20.0), 1.0/8.0),
                Math.Pow(Math.Pow(10.0,-180.0/20.0), 1.0/8.0),
            };

            return mRange[(int)(MagnitudeRangeType)comboBoxMagRange.SelectedIndex];
        }

        /// <summary>
        /// 最大減衰値。
        /// </summary>
        private double MagRangeMax() {
            return Math.Pow(MagnitudeRangeValue(), 8);
        }

        private List<Line> mLineList = new List<Line>();
        private List<Label> mLabelList = new List<Label>();

        /// <summary>
        /// プロット座標x → 周波数
        /// </summary>
        /// <param name="idx">0 &lt;= idx &lt; FR_LINE_WIDTH</param>
        /// <returns>周波数 Hz</returns>
        private double PlotXToFrequency(int idx) {
            switch ((FreqScaleType)comboBoxFreqScale.SelectedIndex) {
            case FreqScaleType.Linear:
                return FreqStartEnd().Item2 * idx / FR_LINE_WIDTH;
            case FreqScaleType.Logarithmic: {
                    var se = FreqStartEnd();

                    double startLog = Math.Log10(se.Item1);
                    double endLog = Math.Log10(se.Item2);
                    double freqLog = startLog + (endLog - startLog) * idx / FR_LINE_WIDTH;
                    return Math.Pow(10, freqLog);
                }
            }

            System.Diagnostics.Debug.Assert(false);
            return 0;
        }

        /// <summary>
        /// 周波数 → プロット座標x
        /// </summary>
        /// <param name="n">周波数 Hz</param>
        /// <returns>プロット座標x</returns>
        private double FrequencyToPlotX(double freq) {
            switch ((FreqScaleType)comboBoxFreqScale.SelectedIndex) {
            case FreqScaleType.Linear:
                return (freq / FreqStartEnd().Item2) * FR_LINE_WIDTH;
            case FreqScaleType.Logarithmic: {
                    var se = FreqStartEnd();
                    double startLog = Math.Log10(se.Item1);
                    double endLog = Math.Log10(se.Item2);
                    double freqLog = Math.Log10(freq);

                    return ((freqLog - startLog) / (endLog - startLog)) * FR_LINE_WIDTH;
                }
            }

            System.Diagnostics.Debug.Assert(false);
            return 0;
        }

        private enum FreqListType {
            ForLine,
            ForLabel
        };

        private List<double> GenerateVGridFreqListLinear(FreqListType t) {
            var result = new List<double>();
            double count = 10;
            double interval = FreqStartEnd().Item2 / count;

            double freq = 0;
            for (int i = 0; i < count; ++i) {
                result.Add(freq);
                freq += interval;
            }

            return result;
        }

        private List<double> GenerateVGridFreqListLogarithmic(FreqListType t) {
            var result = new List<double>();
            var itemInDecade = new double[] { 1, 2, 5 };
            if (t == FreqListType.ForLine) {
                itemInDecade = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            }

            var freqStartEnd = FreqStartEnd();
            double freq = freqStartEnd.Item1;

            do {
                foreach (var item in itemInDecade) {
                    var f = freq;
                    f *= item;

                    if (freqStartEnd.Item2 < f) {
                        break;
                    }
                    result.Add(f);
                }

                freq *= 10;
            } while (freq <= freqStartEnd.Item2);

            return result;
        }

        /// <summary>
        /// グリッド縦棒の周波数のリストを生成。
        /// </summary>
        /// <returns></returns>
        private List<double> GenerateVerticalGridFreqList(FreqListType t) {
            switch ((FreqScaleType)comboBoxFreqScale.SelectedIndex) {
            case FreqScaleType.Linear:
                return GenerateVGridFreqListLinear(t);
            case FreqScaleType.Logarithmic:
                return GenerateVGridFreqListLogarithmic(t);
            }

            System.Diagnostics.Debug.Assert(false);
            return null;
        }

        private void LineSetPos(Line l, double x1, double y1, double x2, double y2) {
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
        }

        public WWMath.Functions.TransferFunctionDelegate TransferFunction = (WWComplex s) => { return new WWComplex(1, 0); };

        public void ModeChanged() {
            if (!mInitialized) {
                return;
            }

            // 特にすることは無い。
        }

        public void Update() {
            if (!mInitialized) {
                return;
            }

            foreach (var item in mLineList) {
                canvasFR.Children.Remove(item);
            }
            mLineList.Clear();

            foreach (var item in mLabelList) {
                canvasFR.Children.Remove(item);
            }
            mLabelList.Clear();
            
            // F特の計算。

            var frMagnitude = new double[FR_LINE_WIDTH];

            // frequency - phase
            var frPhase = new double[FR_LINE_WIDTH];

            // angle frequency of idx
            var frω = new double[FR_LINE_WIDTH];

            var frGroupDelay = new double[FR_LINE_WIDTH];

            double maxMagnitude = 0.0;
            double minPhase = 0.0;
            double maxGroupDelay = 0.0;

            for (int i = 0; i < FR_LINE_WIDTH; ++i) {
                double ω = 2.0 * Math.PI * PlotXToFrequency(i);

                WWComplex h;
                switch (Mode) {
                case ModeType.SPlane:
                    h = TransferFunction(new WWComplex(0, ω));
                    break;
                case ModeType.ZPlane: {
                        double θ = ω / SamplingFrequency;
                        h = TransferFunction(new WWComplex(Math.Cos(θ), Math.Sin(θ)));
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    h = WWComplex.Unity();
                    break;
                }
                double magnitude = h.Magnitude();
                if (maxMagnitude < magnitude) {
                    maxMagnitude = magnitude;
                }

                frω[i] = ω;
                frMagnitude[i] = h.Magnitude();

                if (i == 0) {
                    frPhase[i] = h.Phase();
                } else {
                    frPhase[i] = h.Phase();
                    while (frPhase[i - 1] < frPhase[i]) {
                        frPhase[i] -= 2.0 * Math.PI;
                    }
                }
                if (frPhase[i] < minPhase) {
                    minPhase = frPhase[i];
                }

                if (1 <= i) {
                    double phaseDiff = frPhase[i] - frPhase[i - 1];
                    frGroupDelay[i] = -phaseDiff / (frω[i] - frω[i - 1]);
                    if (maxGroupDelay < frGroupDelay[i]) {
                        maxGroupDelay = frGroupDelay[i];
                    }
                }
                /*
                Console.WriteLine("{0}Hz: {1:g4}dB {2:g4} deg", ω / (2.0 * Math.PI),
                    20.0 * Math.Log10(frMagnitude[i]),
                    frPhase[i]*180.0/Math.PI);
                */
            }

            if (maxMagnitude < float.Epsilon) {
                maxMagnitude = 1.0f;
            }
            if (-float.Epsilon < minPhase) {
                // 30°
                minPhase = -Math.PI/6;
            }

            if (maxGroupDelay < 0.0001) {
                maxGroupDelay = 0.0001;
            }

            double minDegree = minPhase * 180.0 / Math.PI;

            labelPhase180.Content  = string.Format("{0:g4}", 0);
            labelPhase90.Content   = string.Format("{0:g4}", minDegree * (1.0/4.0));
            labelPhase0.Content    = string.Format("{0:g4}", minDegree * (2.0 / 4.0));
            labelPhaseM90.Content  = string.Format("{0:g4}", minDegree * (3.0 / 4.0));
            labelPhaseM180.Content = string.Format("{0:g4}", minDegree * (4.0 / 4.0));

            labelGroupDelay0.Content = Common.UnitNumberString(maxGroupDelay * (0.0 / 4.0));
            labelGroupDelay1.Content = Common.UnitNumberString(maxGroupDelay * (1.0 / 4.0));
            labelGroupDelay2.Content = Common.UnitNumberString(maxGroupDelay * (2.0 / 4.0));
            labelGroupDelay3.Content = Common.UnitNumberString(maxGroupDelay * (3.0 / 4.0));
            labelGroupDelay4.Content = Common.UnitNumberString(maxGroupDelay * (4.0 / 4.0));


            var vGridFreqList = GenerateVerticalGridFreqList(FreqListType.ForLine);
            foreach (var freq in vGridFreqList) {
                double x = FrequencyToPlotX(freq);
                var l = new Line();
                LineSetPos(l, FR_LINE_LEFT + x, FR_LINE_TOP, FR_LINE_LEFT + x, FR_LINE_BOTTOM);
                l.Stroke = new SolidColorBrush(Colors.LightGray);
                canvasFR.Children.Add(l);
                mLineList.Add(l);
            }

            vGridFreqList = GenerateVerticalGridFreqList(FreqListType.ForLabel);
            foreach (var freq in vGridFreqList) {
                double x = FrequencyToPlotX(freq);

                var t = new Label();
                t.Content = Common.UnitNumberString(freq);
                canvasFR.Children.Add(t);
                Canvas.SetLeft(t, FR_LINE_LEFT + x - 10);
                Canvas.SetTop(t, FR_LINE_BOTTOM);
                mLabelList.Add(t);
                //Console.WriteLine("{0} {1}Hz", p, n);
            }

            double magRange = MagnitudeRangeValue();

            Label[] labels = new Label[] {
                labelFRMag0000,
                labelFRMag0125,
                labelFRMag0250,
                labelFRMag0375,
                labelFRMag0500,
                labelFRMag0625,
                labelFRMag0750,
                labelFRMag0875,
                labelFRMag1000,
            };

            switch ((MagScaleType)comboBoxMagScale.SelectedIndex) {
            case MagScaleType.Linear:
                labelMagnitude.Content = "Magnitude";
                for (int i = 0; i < labels.Length; ++i) {
                    labels[i].Content = string.Format("{0:0.000}", i * maxMagnitude * 0.125);
                }
                break;
            case MagScaleType.Logarithmic:
                labelMagnitude.Content = "Magnitude (dB)";

                double magnitude = maxMagnitude;
                for (int i = 0; i < labels.Length; ++i) {
                    labels[labels.Length - 1 - i].Content = string.Format("{0:0.00}", 20.0 * Math.Log10(magnitude));
                    //Console.WriteLine("{0} {1}", magnitude, 20.0 * Math.Log10(magnitude));
                    magnitude *= magRange;
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            {
                var visibility = ShowGain ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                labelMagnitude.Visibility = visibility;
                for (int i = 0; i < labels.Length; ++i) {
                    labels[i].Visibility = visibility;
                }
            }

            {
                var visibility = ShowPhase ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                labelPhase.Visibility = visibility;
                labelPhase0.Visibility = visibility;
                labelPhase90.Visibility = visibility;
                labelPhase180.Visibility = visibility;
                labelPhaseM180.Visibility = visibility;
                labelPhaseM90.Visibility = visibility;
            }

            {
                var visibility = ShowGroupDelay ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                labelGroupDelay.Visibility = visibility;
                labelGroupDelay0.Visibility = visibility;
                labelGroupDelay1.Visibility = visibility;
                labelGroupDelay2.Visibility = visibility;
                labelGroupDelay3.Visibility = visibility;
                labelGroupDelay4.Visibility = visibility;
            }

            // 周波数応答の折れ線を作る。

            double magRangeMax = MagRangeMax();
            var lastPosM = new Point();
            var lastPosP = new Point();
            var lastPosG = new Point();

            for (int i = 0; i < FR_LINE_WIDTH; ++i) {
                if (mMode == ModeType.ZPlane && SamplingFrequency/2 < frω[i] / 2 / Math.PI) {
                    break;
                }

                Point posM = new Point();
                Point posP = new Point();
                Point posG = new Point();

                double phase = frPhase[i];
                /*
                while (phase <= -Math.PI) {
                    phase += 2.0 * Math.PI;
                }
                while (Math.PI < phase) {
                    phase -= 2.0f * Math.PI;
                }
                */

                switch ((MagScaleType)comboBoxMagScale.SelectedIndex) {
                case MagScaleType.Linear:
                    posM = new Point(FR_LINE_LEFT + i, FR_LINE_BOTTOM - FR_LINE_HEIGHT * frMagnitude[i] / maxMagnitude);
                    break;
                case MagScaleType.Logarithmic:
                    posM = new Point(FR_LINE_LEFT + i,
                        FR_LINE_TOP 
                        + FR_LINE_HEIGHT * 20.0 * Math.Log10(frMagnitude[i] / maxMagnitude)
                          / (20.0 * Math.Log10(magRangeMax)));
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }

                bool bDraw = (1 <= i);
                switch ((MagScaleType)comboBoxMagScale.SelectedIndex) {
                case MagScaleType.Logarithmic:
                    if (FR_LINE_BOTTOM < posM.Y || FR_LINE_BOTTOM < lastPosM.Y) {
                        bDraw = false;
                    }
                    break;
                }

                if (ShowGain && bDraw) {
                    if (DISP_MAG_THRESHOLD < frMagnitude[i]) {
                        var lineM = new Line();
                        lineM.Stroke = Brushes.Blue;
                        LineSetPos(lineM, lastPosM.X, lastPosM.Y, posM.X, posM.Y);
                        mLineList.Add(lineM);
                        canvasFR.Children.Add(lineM);
                    }
                }

                if (ShowPhase) {
                    // phase plot
                    // 振幅が小さいと回転の精度が低いので表示しない。
                    if (DISP_MAG_THRESHOLD < frMagnitude[i]) {
                        posP = new Point(FR_LINE_LEFT + i, FR_LINE_TOP + FR_LINE_HEIGHT * phase / minPhase);
                        if (1 <= i && ( posP.X - lastPosP.X ) < 2) {
                            var lineP = new Line();
                            lineP.Stroke = Brushes.Red;
                            LineSetPos(lineP, lastPosP.X, lastPosP.Y, posP.X, posP.Y);
                            mLineList.Add(lineP);
                            canvasFR.Children.Add(lineP);
                        }
                    }
                }

                if (ShowGroupDelay) {
                    // group delay plot.
                    // 振幅が小さいと回転の精度が低いので表示しない。
                    if (1 <= i && DISP_MAG_THRESHOLD < frMagnitude[i]) {
                        double phaseDiff = frPhase[i] - frPhase[i - 1];
                        double groupDelay = -phaseDiff / ( frω[i] - frω[i - 1] );
                        //Console.WriteLine("{0} {1}", i, groupDelay);

                        posG = new Point(FR_LINE_LEFT + i, FR_LINE_BOTTOM - FR_LINE_HEIGHT * groupDelay / maxGroupDelay);

                        if (2 <= i && ( posG.X - lastPosG.X ) < 2) {
                            var lineG = new Line();
                            lineG.Stroke = Brushes.Gray;
                            LineSetPos(lineG, lastPosG.X, lastPosG.Y, posG.X, posG.Y);
                            mLineList.Add(lineG);
                            canvasFR.Children.Add(lineG);
                        }
                    }
                }

                lastPosP = posP;
                lastPosM = posM;
                lastPosG = posG;
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }

        private void checkBoxShowGain_Changed(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mShowGain = checkBoxShowGain.IsChecked == true;
            Update();
        }

        private void checkBoxShowPhase_Changed(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mShowPhase = checkBoxShowPhase.IsChecked == true;
            Update();
        }

        private void checkBoxShowGroupDelay_Changed(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mShowGroupDelay = checkBoxShowGroupDelay.IsChecked == true;
            Update();
        }
    }
}
