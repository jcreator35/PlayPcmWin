using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WWCompareTwoImages
{
    public partial class MainWindow : Window
    {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        class ImgInf
        {
            public string path;
            public BitmapSource img;
            public WWImageRead.ColorProfileType cp;
            public long duration;
            public long timeStamp;
        }

        WWImageRead mImageReadA = new WWImageRead();
        WWImageRead mImageReadB = new WWImageRead();
        ImgInf mImgA = new ImgInf();
        ImgInf mImgB = new ImgInf();
        bool mInitialized = false;
        bool mImageSwap = false;

        public MainWindow()
        {
            InitializeComponent();
            Title = string.Format(CultureInfo.CurrentCulture,
                "WWCompareTwoImages version {0}", AssemblyVersion);
        }

        enum ImgOrientation
        {
            Left,
            Right,
        }

        private void SetCroppedImg(BitmapSource fullImg, Image imgOnScreen, ImgOrientation o)
        {
            imgOnScreen.Source = null;

            double ratio = (double)mSlider.Value / (double)mSlider.Maximum;
            double gridWidth = mGridMain.ColumnDefinitions[1].ActualWidth;
            double imgRenderHeight = fullImg.PixelHeight * (gridWidth / fullImg.PixelWidth);
            imgOnScreen.Height = imgRenderHeight;

            var cropRect = new Int32Rect();
            switch (o) {
            case ImgOrientation.Left:
                cropRect = new Int32Rect(0, 0, (int)(fullImg.PixelWidth * ratio), fullImg.PixelHeight);
                if (cropRect.Width == 0) {
                    cropRect.Width = 1;
                }
                if (fullImg.PixelWidth < cropRect.Width) {
                    cropRect.Width = fullImg.PixelWidth;
                }

                imgOnScreen.Width = gridWidth * ratio;
                break;
            case ImgOrientation.Right:
                cropRect = new Int32Rect((int)(fullImg.PixelWidth * ratio), 0, (int)(fullImg.PixelWidth * (1.0 - ratio)), fullImg.PixelHeight);
                if (cropRect.Width == 0) {
                    cropRect.Width = 1;
                    cropRect.X = fullImg.PixelWidth-1;
                }

                imgOnScreen.Width = gridWidth * (1.0 - ratio);
                break;
            }

            var cropImg = new CroppedBitmap(fullImg, cropRect);
            cropImg.Freeze();

            imgOnScreen.Source = cropImg;
        }

        private BitmapSource ReadFromFile(ImgInf imgInf, long timeStamp, WWImageRead ir)
        {
            var ext = System.IO.Path.GetExtension(imgInf.path);
            string[] imgExt = { ".png", ".jpg", ".jpeg", ".bmp" };
            if (imgExt.Any(s => s.Equals(ext))) {
                // 画像の拡張子の場合。
                imgInf.duration = -1;
                imgInf.timeStamp = -1;
                return ir.ColorConvertedRead(imgInf.path, imgInf.cp);
            }

            ir.VReadEnd();

            int hr = ir.VReadStart(imgInf.path);
            if (hr < 0) {
                if (hr == WWMFVideoReaderCs.WWMFVideoReader.MF_E_INVALIDMEDIATYPE
                    || hr == WWMFVideoReaderCs.WWMFVideoReader.MF_E_UNSUPPORTED_BYTESTREAM_TYPE) {
                    var w = new WWDescriptionWindow(WWDescriptionWindow.LocalPathToUri("desc/UnsupportedMediaType.html"));
                    w.ShowDialog();
                    return null;
                } else { 
                    MessageBox.Show(string.Format("Error: {0:x} while reading {1}", hr, imgInf.path));
                    return null;
                }
            }

            hr = ir.VReadImage(timeStamp, imgInf.cp, out BitmapSource bi, ref imgInf.duration, ref imgInf.timeStamp);
            if (hr < 0) {
                MessageBox.Show(string.Format("Error: {0:x} while reading {1}", hr, imgInf.path));
                return null;
            }
            return bi;
        }

        private BitmapSource NextImage(ImgInf imgInf, WWImageRead ir)
        {
            if (imgInf.duration < 0) {
                return null;
            }

            int hr = ir.VReadImage(-1, imgInf.cp, out BitmapSource bi, ref imgInf.duration, ref imgInf.timeStamp);
            if (hr < 0) {
                MessageBox.Show(string.Format("Error: {0:x} while reading {1}", hr, imgInf.path));
                return null;
            }
            return bi;
        }

        static long SecondToHNS(double timeSec)
        {
            return (long)(timeSec * 1000 * 1000 * 10);
        }

        private void ReadTwoNewImgs(string pathA, double timeSecA, WWImageRead.ColorProfileType cpA, string pathB, double timeSecB, WWImageRead.ColorProfileType cpB)
        {
            mLabelIcc.Content = string.Format("Monitor:{0}, ImageA:{1} ImageB:{2}", WWImageRead.MonitorProfileName, cpA, cpB);

            mImgA.path = pathA;
            mImgB.path = pathB;

            mImgA.cp = cpA;
            mImgB.cp = cpB;

            mImgA.img = ReadFromFile(mImgA, SecondToHNS(timeSecA), mImageReadA);
            mImgB.img = ReadFromFile(mImgB, SecondToHNS(timeSecB), mImageReadB);
        }

        private void ReadTwoNextImgs()
        {
            { 
                var iA = NextImage(mImgA, mImageReadA);
                if (iA != null) {
                    mImgA.img = iA;
                }
            }
            { 
                var iB = NextImage(mImgB, mImageReadB);
                if (iB != null) {
                    mImgB.img = iB;
                }
            }
        }

        private static string HNStoDurationStr(long hns)
        {
            int m = (int)(hns / 60 / 1000 / 1000 / 10);
            int s = (int)(hns / 1000 / 1000 / 10 - m * 60);
            int subH = (int)(hns / 1000 / 100 - (m * 60 + s) * 100);
            return string.Format(CultureInfo.CurrentCulture, "{0:D2}:{1:D2}.{2:D2}", m, s, subH);
        }

        private string LabelStr(ImgInf imgInf)
        {
            string r = System.IO.Path.GetFileName(imgInf.path);
            if (0 <= imgInf.duration) {
                r += " (" + HNStoDurationStr(imgInf.timeStamp) + " / " + HNStoDurationStr(imgInf.duration) + ")";
            }
            return r;
        }

        private void UpdateImgDisp()
        {
            if (mImgA.img == null || mImgB.img == null) {
                return;
            }

            if (mImageSwap) {
                SetCroppedImg(mImgB.img, mImageA, ImgOrientation.Left);
                SetCroppedImg(mImgA.img, mImageB, ImgOrientation.Right);
                mLabelA.Content = LabelStr(mImgB);
                mLabelB.Content = LabelStr(mImgA);
            } else {
                SetCroppedImg(mImgA.img, mImageA, ImgOrientation.Left);
                SetCroppedImg(mImgB.img, mImageB, ImgOrientation.Right);
                mLabelA.Content = LabelStr(mImgA);
                mLabelB.Content = LabelStr(mImgB);
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WWMFVideoReaderCs.WWMFVideoReader.StaticInit();

            if (!WWImageRead.StaticInit()) {
                Close();
                return;
            }

            ReadTwoNewImgs(
                "images\\CC_AdobeRGB_D65_24bit_GT.png", 0, WWImageRead.ColorProfileType.AdobeRGB,
                "images\\CC_sRGB_D65_24bit_GT.png", 0, WWImageRead.ColorProfileType.sRGB);
            mButtonNextFrame.IsEnabled = mImageReadA.IsVideo || mImageReadB.IsVideo;

            UpdateImgDisp();

            mInitialized = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WWMFVideoReaderCs.WWMFVideoReader.StaticTerm();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
            case Key.Escape:
                Close();
                break;
            }
        }

        private void MSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!mInitialized) {
                return;
            }

            //Console.WriteLine("{0}", mSlider.Value);

            UpdateImgDisp();
        }

        private void CheckBoxSwapImg_Checked(object sender, RoutedEventArgs e)
        {
            mImageSwap = true;
            if (!mInitialized) {
                return;
            }

            UpdateImgDisp();
        }

        private void CheckBoxSwapImg_Unchecked(object sender, RoutedEventArgs e)
        {
            mImageSwap = false;
            if (!mInitialized) {
                return;
            }

            UpdateImgDisp();
        }

        private void ButtonReadImages_Clicked(object sender, RoutedEventArgs e)
        {
            var w = new WWOpenTwoImageFilesWindow();

            w.FirstImgPath = mImgA.path;
            w.SecondImgPath = mImgB.path;
            w.FirstImgColorProfile = mImgA.cp;
            w.SecondImgColorProfile = mImgB.cp;

            var r = w.ShowDialog();
            if (r != true) {
                return;
            }

            ReadTwoNewImgs(
                w.FirstImgPath, w.FirstImgTimeSec, w.FirstImgColorProfile,
                w.SecondImgPath, w.SecondImgTimeSec, w.SecondImgColorProfile);
            mButtonNextFrame.IsEnabled = mImageReadA.IsVideo || mImageReadB.IsVideo;

            UpdateImgDisp();

        }

        private void ButtonNextFrame_Clicked(object sender, RoutedEventArgs e)
        {
            ReadTwoNextImgs();
            UpdateImgDisp();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            UpdateImgDisp();
        }
    }
}
