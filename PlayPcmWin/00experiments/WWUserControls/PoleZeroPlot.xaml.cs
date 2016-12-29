using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WWMath;

namespace WWUserControls {
    public partial class PoleZeroPlot : UserControl {
        public PoleZeroPlot() {
            InitializeComponent();
            MagnitudeScale = 3.0;
            Mode = ModeType.SPlane;
            mInitialized = true;
        }

        private bool mInitialized = false;

        private List<UIElement> mPoleList = new List<UIElement>();
        private List<UIElement> mZeroList = new List<UIElement>();

        public enum ModeType {
            SPlane,
            ZPlane
        };

        public ModeType Mode { get; set; }

        /* 表示座標系はx+が右、x+が下
         * 原点は(128,128) */
        const double OFFS_X = 128;
        const double OFFS_Y = 128;
        const double SCALE_X = 64;
        const double SCALE_Y = 64;

        /// <summary>
        /// 極の×の大きさ。
        /// </summary>
        const double SCALE_CROSS = 5;

        /// <summary>
        /// 零の直径。
        /// </summary>
        const double DIAMETER_ZERO = 10;

        private double mPoleZeroScale = 1.0;

        public double MagnitudeScale { get; set; }

        public void SetScale(double s) {
            mPoleZeroScale = s;
            mXm1.Text = string.Format("-{0:G4}", s);
            mYm1.Text = string.Format("-{0:G4}j", s);
            mXp1.Text = string.Format("+{0:G4}", s);
            mYp1.Text = string.Format("+{0:G4}j", s);
        }

        public void AddPole(WWComplex pole) {
            double x = OFFS_X + pole.real / mPoleZeroScale * SCALE_X;
            double y = OFFS_Y - pole.imaginary / mPoleZeroScale * SCALE_Y;

            {
                var l = new Line();
                l.X1 = x - SCALE_CROSS;
                l.X2 = x + SCALE_CROSS;
                l.Y1 = y - SCALE_CROSS;
                l.Y2 = y + SCALE_CROSS;
                l.Stroke = new SolidColorBrush(Colors.Black);
                mPoleList.Add(l);
                canvasPoleZero.Children.Add(l);
            }
            {
                var l = new Line();
                l.X1 = x + SCALE_CROSS;
                l.X2 = x - SCALE_CROSS;
                l.Y1 = y - SCALE_CROSS;
                l.Y2 = y + SCALE_CROSS;
                l.Stroke = new SolidColorBrush(Colors.Black);
                mPoleList.Add(l);
                canvasPoleZero.Children.Add(l);
            }
        }

        public void AddZero(WWComplex zero) {
            double x = OFFS_X + zero.real / mPoleZeroScale * SCALE_X;
            double y = OFFS_Y - zero.imaginary / mPoleZeroScale * SCALE_Y;

            {
                var e = new Ellipse();
                e.Width = DIAMETER_ZERO;
                e.Height = DIAMETER_ZERO;
                e.Stroke = new SolidColorBrush(Colors.White);
                mPoleList.Add(e);
                canvasPoleZero.Children.Add(e);
                Canvas.SetLeft(e, x - DIAMETER_ZERO / 2);
                Canvas.SetTop(e, y - DIAMETER_ZERO / 2);
            }
        }

        public void ClearPoleZero() {
            foreach (var pole in mPoleList) {
                canvasPoleZero.Children.Remove(pole);
            }
            foreach (var zero in mZeroList) {
                canvasPoleZero.Children.Remove(zero);
            }

            mPoleList.Clear();
            mZeroList.Clear();
        }

        public void Update() {
            if (!mInitialized) {
                return;
            }

            switch (Mode) {
            case ModeType.SPlane:
                textBlockGraphTitle.Text = "Pole-Zero Plot (S plane)";
                unitCircle.Visibility = System.Windows.Visibility.Collapsed;
                break;
            case ModeType.ZPlane:
                textBlockGraphTitle.Text = "Pole-Zero Plot (Z plane)";
                unitCircle.Visibility = System.Windows.Visibility.Visible;
                break;
            }

            UpdateGradation();
            UpdateGradationSample();
        }

        /// <summary>
        /// HSV to BGRA color conversion (A=255)
        /// </summary>
        /// <param name="h">0 <= h < 360 </param>
        /// <param name="s">0 to 1</param>
        /// <param name="v">0 to 1</param>
        /// <returns></returns>
        private static int HsvToBgra(double h, double s, double v) {
            while (h < 0) {
                h += 360.0;
            }
            while (360 < h) {
                h -= 360;
            }

            // create HSV color then convert it to RGB
            double c = v * s;
            double x = c * (1.0 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double rp = 0;
            double gp = 0;
            double bp = 0;

            if (h < 60) {
                rp = c;
                gp = x;
            } else if (h < 120) {
                rp = x;
                gp = c;
            } else if (h < 180) {
                gp = c;
                bp = x;
            } else if (h < 240) {
                gp = x;
                bp = c;
            } else if (h < 300) {
                rp = x;
                bp = c;
            } else {
                rp = c;
                bp = x;
            }

            byte r = (byte)((rp + m) * 255);
            byte g = (byte)((gp + m) * 255);
            byte b = (byte)((bp + m) * 255);

            return b + (g << 8) + (r << 16) + (0xff << 24);
        }

        private int PhaseToBgra(double phase) {
            return HsvToBgra(-phase * 180.0 / Math.PI + 240.0, 1.0, 1.0);
        }

        public Common.TransferFunctionDelegate TransferFunction = (WWComplex s) => { return new WWComplex(1, 0); };

        private Image mImage = null;
        private Image mImageSample = null;

        /// <summary>
        /// グラデーションサンプル表示。
        /// </summary>
        private void UpdateGradationSample() {
            var im = new Image();

            var bm = new WriteableBitmap(
                (int)canvasGradationSample.Width,
                (int)canvasGradationSample.Height,
                96, 
                96,
                (comboBoxGradationType.SelectedIndex == 0) ? PixelFormats.Gray32Float : PixelFormats.Bgra32,
                null);
            im.Source = bm;
            im.Stretch = Stretch.None;
            im.HorizontalAlignment = HorizontalAlignment.Left;
            im.VerticalAlignment   = VerticalAlignment.Top;
            
            var pxF = new float[bm.PixelHeight * bm.PixelWidth];
            var pxBgra = new int[bm.PixelHeight * bm.PixelWidth];

            for (int x = 0; x < bm.PixelWidth; x++) {
                double hM = MagnitudeScale * x / bm.PixelWidth;
                if (hM < 0.01) {
                    hM = 0.01;
                }
                float hL = (float)((Math.Log10(hM) + 1.0f) / MagnitudeScale);

                double phase = x * 2.0 * Math.PI / bm.PixelWidth;
                int bgra = PhaseToBgra(phase);

                if (comboBoxGradationType.SelectedIndex == 0) {
                    // magnitude
                    for (int y = 0; y < bm.PixelHeight; y++) {
                        pxF[x + y * bm.PixelWidth] = hL;
                    }
                } else {
                    // phase
                    for (int y = 0; y < bm.PixelHeight; y++) {
                        pxBgra[x+y*bm.PixelWidth] = bgra;
                    }
                }
            }
            if (comboBoxGradationType.SelectedIndex == 0) {
                // magnitude
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxF, bm.BackBufferStride, 0);
            } else {
                // phase
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxBgra, bm.BackBufferStride, 0);
            }

            if (mImageSample != null) {
                canvasGradationSample.Children.Remove(mImageSample);
                mImageSample = null;
            }

            canvasGradationSample.Children.Add(im);
            mImageSample = im;

            double graduationScale;
            string unitText;
            if (comboBoxGradationType.SelectedIndex == 0) {
                graduationScale = MagnitudeScale;
                unitText = "";
            } else {
                graduationScale = 360.0;
                unitText = "°";
            }

            label0.Content = string.Format("{0}{1}", 0.0 * graduationScale, unitText);
            label180.Content = string.Format("{0}{1}", 0.5 * graduationScale, unitText);
            label360.Content = string.Format("{0}{1}", 1.0 * graduationScale, unitText);
        }

        /// <summary>
        /// グラデーション画像を作ってポールゼロプロットの裏に貼る。
        /// </summary>
        private void UpdateGradation()
        {
            var im = new Image();
            var bm = new WriteableBitmap(
                256,
                256,
                96, 
                96,
                (comboBoxGradationType.SelectedIndex==0) ? PixelFormats.Gray32Float : PixelFormats.Bgra32,
                null);

            var pxF    = new float[bm.PixelHeight * bm.PixelWidth];
            var pxBgra = new int[bm.PixelHeight * bm.PixelWidth];

            im.Source = bm;
            im.Stretch = Stretch.None;
            im.HorizontalAlignment = HorizontalAlignment.Left;
            im.VerticalAlignment   = VerticalAlignment.Top;

            int pos = 0;
            for (int yI = 0; yI < bm.PixelHeight; yI++) {
                for (int xI = 0; xI < bm.PixelWidth; xI++) {
                    double x = (xI - OFFS_X) * (mPoleZeroScale / SCALE_X);
                    double y = (OFFS_Y - yI) * (mPoleZeroScale / SCALE_Y);
                    var h = TransferFunction(new WWComplex(x, y));
                    if (comboBoxGradationType.SelectedIndex == 0) {
                        // magnitude
                        var hM = h.Magnitude();

                        if (hM < 0.01) {
                            hM = 0.01;
                        }
                        pxF[pos] = (float)((Math.Log10(hM) + 1.0f) / MagnitudeScale);
                    } else {
                        // phase
                        pxBgra[pos] = PhaseToBgra(h.Phase());
                    }
                    ++pos;
                }
            }

            if (comboBoxGradationType.SelectedIndex==0) {
                // magnitude
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxF, bm.BackBufferStride, 0);
            } else {
                // phase
                bm.WritePixels(new Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight), pxBgra, bm.BackBufferStride, 0);
            }

            if (mImage != null) {
                canvasPoleZero.Children.Remove(mImage);
                mImage = null;
            }

            canvasPoleZero.Children.Add(im);
            Canvas.SetZIndex(im, -1);
            mImage = im;

            /*
            // 単位円描画。
            if (mUnitCircle != null) {
                canvasPoleZero.Children.Add(mUnitCircle);
            }
            double circleRadius = 128.0 / scale;
            Ellipse unitCircle = new Ellipse { Width = circleRadius * 2, Height = circleRadius * 2, Stroke = new SolidColorBrush { Color = Colors.Black } };
            unitCircle.Width = circleRadius * 2;
            unitCircle.Height = circleRadius * 2;
            Canvas.SetLeft(unitCircle, 128.0 - circleRadius);
            Canvas.SetTop(unitCircle, 128.0 - circleRadius);
            Canvas.SetZIndex(unitCircle, 1);
            mUnitCircle = unitCircle;
            */
        }

        //private Ellipse mUnitCircle = null;

        private void comboBoxGradationType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
        }
    }
}
