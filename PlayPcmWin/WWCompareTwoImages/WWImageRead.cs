using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WWCompareTwoImages
{
    public class WWImageRead
    {
        string mColorDir;
        string mMonitorProfileName;
        bool mInitSuccess = false;

        public bool InitSuccess { get { return mInitSuccess;  } }

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

        public WWImageRead() {
        }

        public bool Init() {
            mColorDir = WWMonitorProfile.GetColorDirectory();
            mMonitorProfileName = WWMonitorProfile.GetMonitorProfile();

            if (mColorDir == null) {
                MessageBox.Show("Error: Color Profile Directory is not found!");
                return false;
            }

            if (mMonitorProfileName == null) {
                var w = new WWDescriptionWindow(WWDescriptionWindow.LocalPathToUri("desc/SettingMonitorProfile.html"));
                w.ShowDialog();
                return false;
            }

            mColorDir = mColorDir + "\\";

            mInitSuccess = SetupColorCtx();
            return mInitSuccess;
        }

        private bool SetupColorCtx()
        {
            try { 
                mColorCtx[(int)ColorProfileType.sRGB]     = new ColorContext(new Uri(mColorDir + "sRGB Color Space Profile.icm", UriKind.Absolute));
            } catch (Exception) {
                MessageBox.Show("Error: \"sRGB Color Space Profile.icm\" Color Profile is not found!");
                return false;
            }
            try { 
                mColorCtx[(int)ColorProfileType.AdobeRGB] = new ColorContext(new Uri(mColorDir + "AdobeRGB1998.icc", UriKind.Absolute));
            } catch (Exception) {
                var w = new WWDescriptionWindow(WWDescriptionWindow.LocalPathToUri("desc/InstallAdobeRGB.html"));
                w.ShowDialog();
                return false;
            }
            try {
                mColorCtx[(int)ColorProfileType.Rec709]   = new ColorContext(new Uri(mColorDir + "ITU-RBT709ReferenceDisplay.icc", UriKind.Absolute));
            } catch (Exception) {
                var w = new WWDescriptionWindow(WWDescriptionWindow.LocalPathToUri("desc/InstallRec709.html"));
                w.ShowDialog();
                return false;
            }
            try {
                mColorCtx[(int)ColorProfileType.Monitor]  = new ColorContext(new Uri(mColorDir + mMonitorProfileName, UriKind.Absolute));
            } catch (Exception) {
                var w = new WWDescriptionWindow(WWDescriptionWindow.LocalPathToUri("desc/SettingMonitorProfile.html"));
                w.ShowDialog();
                return false;
            }

            return true;
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

