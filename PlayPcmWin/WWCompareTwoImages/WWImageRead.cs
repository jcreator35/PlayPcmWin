using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WWCompareTwoImages
{
    class WWImageRead
    {
        string mColorDir;
        string mMonitorProfileName;

        public enum ColorProfileType
        {
            sRGB,
            AdobeRGB,
            Rec709,
            Monitor,
            NUM
        }

        ColorContext[] mColorCtx = new ColorContext[(int)ColorProfileType.NUM];

        public string MonitorProfileName {
            get { return mMonitorProfileName; }
        }

        public string ColorDir {
            get { return mColorDir; }
        }

        public WWImageRead()
        {
            mColorDir = WWMonitorProfile.GetColorDirectory();
            mMonitorProfileName = WWMonitorProfile.GetMonitorProfile();

            if (mColorDir == null || mMonitorProfileName == null) {
                return;
            }

            mColorDir = mColorDir + "\\";

            SetupColorCtx();
        }

        private void SetupColorCtx()
        {
            mColorCtx[(int)ColorProfileType.sRGB]     = new ColorContext(new Uri(mColorDir + "sRGB Color Space Profile.icm", UriKind.Absolute));
            mColorCtx[(int)ColorProfileType.AdobeRGB] = new ColorContext(new Uri(mColorDir + "AdobeRGB1998.icc", UriKind.Absolute));
            mColorCtx[(int)ColorProfileType.Rec709]   = new ColorContext(new Uri(mColorDir + "ITU-RBT709ReferenceDisplay.icc", UriKind.Absolute));
            mColorCtx[(int)ColorProfileType.Monitor]  = new ColorContext(new Uri(mColorDir + mMonitorProfileName, UriKind.Absolute));
        }

        public BitmapImage SimpleImageRead(string path)
        {
            var bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bmi.CacheOption = BitmapCacheOption.OnLoad;
            bmi.EndInit();
            bmi.Freeze();

            return bmi;
        }

        public BitmapSource ColorConvertedRead(string path, ColorProfileType from)
        {
            var bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bmi.CacheOption = BitmapCacheOption.OnLoad;
            bmi.EndInit();
            bmi.Freeze();

            ColorConvertedBitmap ccb = new ColorConvertedBitmap();
            ccb.BeginInit();
            ccb.Source = bmi;
            ccb.SourceColorContext = mColorCtx[(int)from];
            ccb.DestinationColorContext = mColorCtx[(int)ColorProfileType.Monitor];
            ccb.EndInit();

            return ccb;
        }
    }
}

