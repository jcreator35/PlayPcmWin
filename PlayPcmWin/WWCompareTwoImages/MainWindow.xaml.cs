using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WWCompareTwoImages
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class ImgInf
        {
            public string path;
            public BitmapSource img;
        }

        WWImageRead mImageRead;
        ImgInf mImgA = new ImgInf();
        ImgInf mImgB = new ImgInf();
        bool mInitialized = false;
        bool mImageSwap = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        enum ImgOrientation
        {
            Left,
            Right,
        }

        private void SetCroppedImg(BitmapSource fullImg, Image imgOnScreen, ImgOrientation o)
        {
            double ratio = (double)mSlider.Value / (double)mSlider.Maximum;
            double gridWidth = mGridMain.ColumnDefinitions[1].ActualWidth;

            var cropRect = new Int32Rect();
            switch (o) {
            case ImgOrientation.Left:
                cropRect = new Int32Rect(0, 0, (int)(fullImg.PixelWidth * ratio), fullImg.PixelHeight);
                if (cropRect.Width == 0) {
                    cropRect.Width = 1;
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

            imgOnScreen.Source = cropImg;
        }

        private void ReadTwoImgs(string pathA, WWImageRead.ColorProfileType cpA, string pathB, WWImageRead.ColorProfileType cpB)
        {
            mImgA.path = pathA;
            mImgB.path = pathB;
            mImgA.img = mImageRead.ColorConvertedRead(pathA, cpA);
            mImgB.img = mImageRead.ColorConvertedRead(pathB, cpB);
        }

        private void UpdateImgDisp()
        {
            if (mImgA.img == null || mImgB.img == null) {
                return;
            }

            if (mImageSwap) {
                SetCroppedImg(mImgB.img, mImageA, ImgOrientation.Left);
                SetCroppedImg(mImgA.img, mImageB, ImgOrientation.Right);
                mLabelA.Content = System.IO.Path.GetFileName(mImgB.path);
                mLabelB.Content = System.IO.Path.GetFileName(mImgA.path);
            } else {
                SetCroppedImg(mImgA.img, mImageA, ImgOrientation.Left);
                SetCroppedImg(mImgB.img, mImageB, ImgOrientation.Right);
                mLabelA.Content = System.IO.Path.GetFileName(mImgA.path);
                mLabelB.Content = System.IO.Path.GetFileName(mImgB.path);
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mImageRead = new WWImageRead();

            //ReadTwoImgs("images\\ColorChart_AdobeRGB.png", WWImageRead.ColorProfileType.AdobeRGB, "images\\ColorChart_sRGB.png", WWImageRead.ColorProfileType.sRGB);
            ReadTwoImgs("images\\CC_AdobeRGB_D65_24bit_GT.png", WWImageRead.ColorProfileType.AdobeRGB, "images\\CC_sRGB_D65_24bit_GT.png", WWImageRead.ColorProfileType.sRGB);

            UpdateImgDisp();

            mInitialized = true;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_ReadImages(object sender, RoutedEventArgs e)
        {

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
    }
}
