using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WWMFVideoReaderCs;

namespace WWCompareTwoImages
{
    public class WWImageRead
    {
        static string mColorDir;
        static string mMonitorProfileName;
        static bool mStaticInitSuccess = false;

        static public bool InitSuccess { get { return mStaticInitSuccess;  } }

        public enum ColorProfileType
        {
            sRGB,
            AdobeRGB,
            Rec709,
            Monitor,
            NUM
        }

        static ColorContext[] mColorCtx = new ColorContext[(int)ColorProfileType.NUM];

        static public string MonitorProfileName {
            get { return mMonitorProfileName; }
        }

        static public string ColorProfileName(ColorProfileType t)
        {
            switch (t) {
            case ColorProfileType.sRGB:
                return "sRGB Color Space Profile.icm";
            case ColorProfileType.AdobeRGB:
                return "AdobeRGB1998.icc";
            case ColorProfileType.Rec709:
                return "ITU-RBT709ReferenceDisplay.icc";
            case ColorProfileType.Monitor:
                return MonitorProfileName;
            default:
                System.Diagnostics.Debug.Assert(false);
                return "";
            }
        }

        static public string ColorDir {
            get { return mColorDir; }
        }

        public bool IsVideo { get; set; }

        public WWImageRead() {
        }

        static public bool StaticInit() {
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

            mStaticInitSuccess = SetupColorCtx();
            return mStaticInitSuccess;
        }

        static private bool SetupColorCtx()
        {
            try { 
                mColorCtx[(int)ColorProfileType.sRGB]     = new ColorContext(new Uri(mColorDir + ColorProfileName(ColorProfileType.sRGB), UriKind.Absolute));
            } catch (Exception) {
                MessageBox.Show(string.Format("Error: \"{0}\" Color Profile is not found!", ColorProfileName(ColorProfileType.sRGB)));
                return false;
            }
            try { 
                mColorCtx[(int)ColorProfileType.AdobeRGB] = new ColorContext(new Uri(mColorDir + ColorProfileName(ColorProfileType.AdobeRGB), UriKind.Absolute));
            } catch (Exception) {
                var w = new WWDescriptionWindow(WWDescriptionWindow.LocalPathToUri("desc/InstallAdobeRGB.html"));
                w.ShowDialog();
                return false;
            }
            try {
                mColorCtx[(int)ColorProfileType.Rec709]   = new ColorContext(new Uri(mColorDir + ColorProfileName(ColorProfileType.Rec709), UriKind.Absolute));
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

            IsVideo = false;

            return bmi;
        }

        public BitmapSource ColorConvertedRead(string path, ColorProfileType from)
        {
            BitmapSource r = null;

            var bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bmi.CacheOption = BitmapCacheOption.OnLoad;
            bmi.EndInit();
            bmi.Freeze();

            if (0 == ColorProfileName(from).CompareTo(MonitorProfileName)) {
                // color space is the same.
                r = bmi;
            } else { 
                var ccb = new ColorConvertedBitmap();
                ccb.BeginInit();
                ccb.Source = bmi;
                ccb.SourceColorContext = mColorCtx[(int)from];
                ccb.DestinationColorContext = mColorCtx[(int)ColorProfileType.Monitor];
                ccb.EndInit();
                ccb.Freeze();

                r = ccb;
            }

            IsVideo = false;

            return r;
        }

        private WWMFVideoReader videoRead = new WWMFVideoReader();

        public int VReadStart(string path)
        {
            return videoRead.ReadStart(path);
        }

        public int VReadImage(long posToSeek, ColorProfileType from, out BitmapSource bs, ref long duration, ref long timeStamp)
        {
            int dpi = 96;
            var pf = PixelFormats.Bgr32;
            int hr = videoRead.ReadImage(posToSeek, out WWMFVideoReader.VideoImage vi);

            if (hr < 0) {
                bs = BitmapSource.Create(0, 0, dpi, dpi, pf, null, null, 1);
                return hr;
            }

            var bytesPerPixel = (pf.BitsPerPixel + 7) / 8;
            var stride = bytesPerPixel * vi.w;

            var wb = new WriteableBitmap(vi.w, vi.h, dpi, dpi, pf, null);
            wb.Lock();
            wb.WritePixels(new Int32Rect(0, 0, vi.w, vi.h), vi.img, stride, 0);
            wb.Unlock();
            wb.Freeze();

            if (0 == ColorProfileName(from).CompareTo(MonitorProfileName)) {
                // color space is the same.
                bs = wb;
            } else {
                var ccb = new ColorConvertedBitmap();
                ccb.BeginInit();
                ccb.Source = wb;
                ccb.SourceColorContext = mColorCtx[(int)from];
                ccb.DestinationColorContext = mColorCtx[(int)ColorProfileType.Monitor];
                ccb.EndInit();
                ccb.Freeze();

                bs = ccb;
            }

            duration = vi.duration;
            timeStamp = vi.timeStamp;

            IsVideo = true;
            return hr;
        }

        public void VReadEnd()
        {
            videoRead.ReadEnd();
        }

    }
}

