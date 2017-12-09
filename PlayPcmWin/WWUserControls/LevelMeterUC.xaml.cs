using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WWUserControls {
    public partial class LevelMeterUC : UserControl {
        private bool mInitialized = false;
        private const long LEVEL_METER_UPDATE_INTERVAL_MS = 66;
        private const double METER_LEFT_X = 50.0;
        private const double METER_WIDTH = 400.0;
        private const double METER_0DB_W = 395.0;
        private const double METER_SMALLEST_DB = -48.0;
        private long mLevelMeterLastDispTick = 0;

        public int YellowLevelDb {
            get;
            set;
        }

        public int PeakHoldSeconds {
            get;
            set;
        }

        public int ReleaseTimeDbPerSec {
            get;
            set;
        }

        public LevelMeterUC() {
            InitializeComponent();
            InitLevelMeter();
            YellowLevelDb = -12;
            PeakHoldSeconds = 3;
            ReleaseTimeDbPerSec = 100;
            ResetLevelMeter();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }

        public void UpdateUITexts() {
            groupBoxPeakHold.Header = Properties.Resources.MainPeakHold;
            groupBoxNominalPeakLevel.Header = Properties.Resources.MainNominalPeakLevel;
            groupBoxLevelMeterOther.Header = Properties.Resources.MainLevelMeterOther;
            textBlockLevelMeterReleaseTime.Text = Properties.Resources.MainLevelMeterReleaseTime;
        }

        public void YellowLevelChangeEnable(bool bEnable) {
            groupBoxNominalPeakLevel.IsEnabled = bEnable;
        }

        private Rectangle[] mRectangleG8chArray;
        private Rectangle[] mRectangleY8chArray;
        private Rectangle[] mRectangleR8chArray;
        private Rectangle[] mRectangleMask8chArray;
        private Rectangle[] mRectanglePeak8chArray;
        private TextBlock[] mTextBlockLevelMeter8chArray;

        private void InitLevelMeter() {
            mRectangleG8chArray = new Rectangle[] {
                rectangleG1,
                rectangleG2,
                rectangleG3,
                rectangleG4,
                rectangleG5,

                rectangleG6,
                rectangleG7,
                rectangleG8,
            };

            mRectangleY8chArray = new Rectangle[] {
                rectangleY1,
                rectangleY2,
                rectangleY3,
                rectangleY4,
                rectangleY5,

                rectangleY6,
                rectangleY7,
                rectangleY8,
            };

            mRectangleR8chArray = new Rectangle[] {
                rectangleR1,
                rectangleR2,
                rectangleR3,
                rectangleR4,
                rectangleR5,

                rectangleR6,
                rectangleR7,
                rectangleR8,
            };
            mRectangleMask8chArray = new Rectangle[] {
                rectangleMask1,
                rectangleMask2,
                rectangleMask3,
                rectangleMask4,
                rectangleMask5,

                rectangleMask6,
                rectangleMask7,
                rectangleMask8,
            };
            mRectanglePeak8chArray = new Rectangle[] {
                rectanglePeak1,
                rectanglePeak2,
                rectanglePeak3,
                rectanglePeak4,
                rectanglePeak5,

                rectanglePeak6,
                rectanglePeak7,
                rectanglePeak8,
            };

            mTextBlockLevelMeter8chArray = new TextBlock[] {
                textBlockLevelMeter1,
                textBlockLevelMeter2,
                textBlockLevelMeter3,
                textBlockLevelMeter4,
                textBlockLevelMeter5,

                textBlockLevelMeter6,
                textBlockLevelMeter7,
                textBlockLevelMeter8,
            };
        }

        public delegate void LevelMeterUCParamChangedDelegate(
                int peakHoldSeconds, int yellowLevelDb, int releaseTimeDbPerSec, bool meterReset);

        private LevelMeterUCParamChangedDelegate mParamChangedCallback = null;

        public void SetParamChangedCallback(LevelMeterUCParamChangedDelegate f) {
            mParamChangedCallback = f;
        }

        /// <summary>
        /// -48dBのとき0
        /// 0dBよりわずかに少ないとき390
        /// 0dB以上の時400
        /// </summary>
        private static double MeterValueDbToW(double db) {
            if (db < METER_SMALLEST_DB) {
                return 0;
            }
            if (0 <= db) {
                return METER_0DB_W;
            }

            return -(db / METER_SMALLEST_DB) * METER_0DB_W + METER_0DB_W;
        }

        private Brush DbToBrush(double dB) {
            if (dB < YellowLevelDb) {
                return new SolidColorBrush(Colors.Lime);
            }
            if (dB < -0.1) {
                return new SolidColorBrush(Colors.Yellow);
            }
            return new SolidColorBrush(Colors.Red);
        }

        /// <summary>
        /// PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSecプロパティの値を更新し、
        /// UIの状態を更新する。
        /// </summary>
        public void PreferenceToUI(int peakHoldSeconds, int yellowLevelDb,
                int releaseTimeDbPerSec) {
            PeakHoldSeconds = peakHoldSeconds;
            YellowLevelDb = yellowLevelDb;
            ReleaseTimeDbPerSec = releaseTimeDbPerSec;

            switch (PeakHoldSeconds) {
            case 1:
            default:
                radioButtonPeakHold1sec.IsChecked = true;
                break;
            case 3:
                radioButtonPeakHold3sec.IsChecked = true;
                break;
            case -1:
                radioButtonPeakHoldInfinity.IsChecked = true;
                break;
            }

            switch (YellowLevelDb) {
            case -6:
                radioButtonNominalPeakM6.IsChecked = true;
                break;
            case -10:
                radioButtonNominalPeakM10.IsChecked = true;
                break;
            case -12:
            default:
                radioButtonNominalPeakM12.IsChecked = true;
                break;
            }

            textBoxLevelMeterReleaseTime.Text = string.Format(
                CultureInfo.InvariantCulture, "{0}",
                ReleaseTimeDbPerSec);

            UpdateLevelMeterScale();
        }

        public void UpdateNumOfChannels(int numChannels) {
            if (numChannels <= 2) {
                canvasLevelMeter2ch.Visibility = Visibility.Visible;
                canvasLevelMeter8ch.Visibility = Visibility.Hidden;
            } else {
                canvasLevelMeter2ch.Visibility = Visibility.Hidden;
                canvasLevelMeter8ch.Visibility = Visibility.Visible;
            }
        }

        public void ResetLevelMeter() {
            Canvas.SetLeft(rectangleMaskL, METER_LEFT_X);
            Canvas.SetLeft(rectangleMaskR, METER_LEFT_X);
            rectangleMaskL.Width = METER_WIDTH;
            rectangleMaskR.Width = METER_WIDTH;
            foreach (var r in mRectangleMask8chArray) {
                Canvas.SetLeft(r, METER_LEFT_X);
                r.Width = METER_WIDTH;
            }

            Canvas.SetLeft(rectanglePeakL, METER_LEFT_X);
            Canvas.SetLeft(rectanglePeakR, METER_LEFT_X);
            rectanglePeakL.Fill = new SolidColorBrush(Colors.Transparent);
            rectanglePeakR.Fill = new SolidColorBrush(Colors.Transparent);
            foreach (var r in mRectanglePeak8chArray) {
                Canvas.SetLeft(r, METER_LEFT_X);
                r.Fill = new SolidColorBrush(Colors.Transparent);
            }

            textBlockLevelMeterL.Text = string.Format(CultureInfo.CurrentCulture, "  L");
            textBlockLevelMeterR.Text = string.Format(CultureInfo.CurrentCulture, "  R");
            for (int ch = 0; ch < mTextBlockLevelMeter8chArray.Length; ++ch) {
                mTextBlockLevelMeter8chArray[ch].Text = string.Format(CultureInfo.CurrentCulture, "  Ch.{0}", ch + 1);
            }
        }

        public void UpdateLevelMeterScale() {
            double greenW = MeterValueDbToW(YellowLevelDb);
            rectangleGL.Width = greenW;
            rectangleGR.Width = greenW;
            foreach (var r in mRectangleG8chArray) {
                r.Width = greenW;
            }

            Canvas.SetLeft(rectangleYL, METER_LEFT_X + greenW);
            Canvas.SetLeft(rectangleYR, METER_LEFT_X + greenW);
            rectangleYL.Width = METER_0DB_W - greenW;
            rectangleYR.Width = METER_0DB_W - greenW;
            foreach (var r in mRectangleY8chArray) {
                Canvas.SetLeft(r, METER_LEFT_X + greenW);
                r.Width = METER_0DB_W - greenW;
            }

            Canvas.SetLeft(rectangleRL, METER_LEFT_X + METER_0DB_W);
            Canvas.SetLeft(rectangleRR, METER_LEFT_X + METER_0DB_W);
            foreach (var r in mRectangleR8chArray) {
                Canvas.SetLeft(r, METER_LEFT_X + METER_0DB_W);
            }

            switch (YellowLevelDb) {
            case -12:
            case -6:
                lineM10dB.Visibility = System.Windows.Visibility.Hidden;
                labelLevelMeterM10dB.Visibility = System.Windows.Visibility.Hidden;
                lineM12dB.Visibility = System.Windows.Visibility.Visible;
                labelLevelMeterM12dB.Visibility = System.Windows.Visibility.Visible;

                lineM10dB8.Visibility = System.Windows.Visibility.Hidden;
                labelLevelMeterM10dB8.Visibility = System.Windows.Visibility.Hidden;
                lineM12dB8.Visibility = System.Windows.Visibility.Visible;
                labelLevelMeterM12dB8.Visibility = System.Windows.Visibility.Visible;
                break;
            case -10:
                lineM10dB.Visibility = System.Windows.Visibility.Visible;
                labelLevelMeterM10dB.Visibility = System.Windows.Visibility.Visible;
                lineM12dB.Visibility = System.Windows.Visibility.Hidden;
                labelLevelMeterM12dB.Visibility = System.Windows.Visibility.Hidden;

                lineM10dB8.Visibility = System.Windows.Visibility.Visible;
                labelLevelMeterM10dB8.Visibility = System.Windows.Visibility.Visible;
                lineM12dB8.Visibility = System.Windows.Visibility.Hidden;
                labelLevelMeterM12dB8.Visibility = System.Windows.Visibility.Hidden;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        // 桁数を右揃えにして表示する。
        private static string DbToString(double db) {
            if (db < -200) {
                return "Low";
            }
            var s = string.Format(CultureInfo.CurrentCulture, "{0:+0.0;-0.0;+0.0}", db);
            return string.Format(CultureInfo.InvariantCulture, "{0,6}", s);
        }

        public void UpdateLevelMeter(double[] peakDb, double[] peakHoldDb) {
            if (DateTime.Now.Ticks - mLevelMeterLastDispTick < LEVEL_METER_UPDATE_INTERVAL_MS * 10000) {
                return;
            }

            mLevelMeterLastDispTick = DateTime.Now.Ticks;

            switch (peakDb.Length) {
            case 2: {
                    double maskLW = METER_WIDTH - MeterValueDbToW(peakDb[0]);
                    double maskRW = METER_WIDTH - MeterValueDbToW(peakDb[1]);

                    Canvas.SetLeft(rectangleMaskL, METER_LEFT_X + (METER_WIDTH - maskLW));
                    Canvas.SetLeft(rectangleMaskR, METER_LEFT_X + (METER_WIDTH - maskRW));
                    rectangleMaskL.Width = maskLW;
                    rectangleMaskR.Width = maskRW;

                    double peakHoldLX = METER_LEFT_X + MeterValueDbToW(peakHoldDb[0]);
                    double peakHoldRX = METER_LEFT_X + MeterValueDbToW(peakHoldDb[1]);
                    Canvas.SetLeft(rectanglePeakL, peakHoldLX);
                    Canvas.SetLeft(rectanglePeakR, peakHoldRX);

                    rectanglePeakL.Fill = DbToBrush(peakHoldDb[0]);
                    rectanglePeakR.Fill = DbToBrush(peakHoldDb[1]);

                    textBlockLevelMeterL.Text = string.Format(CultureInfo.CurrentCulture, "  L\n{0}", DbToString(peakHoldDb[0]));
                    textBlockLevelMeterR.Text = string.Format(CultureInfo.CurrentCulture, "  R\n{0}", DbToString(peakHoldDb[1]));
                }
                break;
            case 8: {
                    for (int ch = 0; ch < 8; ++ch) {
                        double maskW = METER_WIDTH - MeterValueDbToW(peakDb[ch]);
                        Canvas.SetLeft(mRectangleMask8chArray[ch], METER_LEFT_X + (METER_WIDTH - maskW));
                        mRectangleMask8chArray[ch].Width = maskW;

                        double peakHoldX = METER_LEFT_X + MeterValueDbToW(peakHoldDb[ch]);
                        Canvas.SetLeft(mRectanglePeak8chArray[ch], peakHoldX);

                        mRectanglePeak8chArray[ch].Fill = DbToBrush(peakHoldDb[ch]);

                        mTextBlockLevelMeter8chArray[ch].Text = string.Format(CultureInfo.CurrentCulture, "{0}", DbToString(peakHoldDb[ch]));
                    }
                }
                break;
            default:
                break;
            }
        }

        private void radioButtonPeakHold1sec_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            PeakHoldSeconds = 1;
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
            }
        }

        private void radioButtonPeakHold3sec_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            PeakHoldSeconds = 3;
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
            }
        }

        private void radioButtonPeakHoldInfinity_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            PeakHoldSeconds = -1;
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
            }
        }

        private void radioButtonNominalPeakM6_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            YellowLevelDb = -6;
            UpdateLevelMeterScale();
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
            }
        }

        private void radioButtonNominalPeakM10_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            YellowLevelDb = -10;
            UpdateLevelMeterScale();
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
            }
        }

        private void radioButtonNominalPeakM12_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            YellowLevelDb = -12;
            UpdateLevelMeterScale();
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
            }
        }

        private void buttonPeakHoldReset_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            if (null != mParamChangedCallback) {
                mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, true);
            }
        }

        private void textBoxLevelMeterReleaseTime_TextChanged(object sender, TextChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            int v;
            if (Int32.TryParse(textBoxLevelMeterReleaseTime.Text, out v) && 0 <= v) {
                ReleaseTimeDbPerSec = v;
                if (null != mParamChangedCallback) {
                    mParamChangedCallback(PeakHoldSeconds, YellowLevelDb, ReleaseTimeDbPerSec, false);
                }
            } else {
                MessageBox.Show(Properties.Resources.ErrorReleaseTimeMustBePositiveInteger);
            }
        }

    }
}
