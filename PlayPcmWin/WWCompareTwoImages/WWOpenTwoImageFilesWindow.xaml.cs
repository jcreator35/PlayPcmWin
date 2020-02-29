using Microsoft.Win32;
using System.Windows;

namespace WWCompareTwoImages
{
    public partial class WWOpenTwoImageFilesWindow : Window
    {
        private bool mInitialized = false;

        public string FirstImgPath {
            get { return mFirstImgTextBox.Text; }
            set { mFirstImgTextBox.Text = value; }
        }

        public string SecondImgPath {
            get { return mSecondImgTextBox.Text; }
            set { mSecondImgTextBox.Text = value; }
        }

        private double mTimeASec = 0;
        private double mTimeBSec = 0;
        public double FirstImgTimeSec {
            get { return mTimeASec; }
        }

        public double SecondImgTimeSec {
            get { return mTimeBSec; }
        }

        /// <summary>
        /// this should be matched to xaml IsDefault
        /// </summary>
        private WWImageRead.ColorProfileType mFirstImgColorProfile = WWImageRead.ColorProfileType.sRGB;

        /// <summary>
        /// this should be matched to xaml IsDefault
        /// </summary>
        private WWImageRead.ColorProfileType mSecondImgColorProfile = WWImageRead.ColorProfileType.sRGB;


        public WWImageRead.ColorProfileType FirstImgColorProfile {
            get {
                return mFirstImgColorProfile;
            }
            set {
                switch (value) {
                case WWImageRead.ColorProfileType.sRGB:
                    mRadioButtonFS.IsChecked = true;
                    break;
                case WWImageRead.ColorProfileType.AdobeRGB:
                    mRadioButtonFA.IsChecked = true;
                    break;
                case WWImageRead.ColorProfileType.Rec709:
                    mRadioButtonFR.IsChecked = true;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
            }
        }

        public WWImageRead.ColorProfileType SecondImgColorProfile {
            get {
                return mSecondImgColorProfile;
            }
            set {
                switch (value) {
                case WWImageRead.ColorProfileType.sRGB:
                    mRadioButtonSS.IsChecked = true;
                    break;
                case WWImageRead.ColorProfileType.AdobeRGB:
                    mRadioButtonSA.IsChecked = true;
                    break;
                case WWImageRead.ColorProfileType.Rec709:
                    mRadioButtonSR.IsChecked = true;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
            }
        }


        private void RadioButtonFirstImg_sRGB_Checked(object sender, RoutedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            mFirstImgColorProfile = WWImageRead.ColorProfileType.sRGB;
        }

        private void RadioButtonFirstImg_AdobeRGB_Checked(object sender, RoutedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            mFirstImgColorProfile = WWImageRead.ColorProfileType.AdobeRGB;
        }

        private void RadioButtonFirstImg_Rec709_Checked(object sender, RoutedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            mFirstImgColorProfile = WWImageRead.ColorProfileType.Rec709;
        }

        private void RadioButtonSecondImg_sRGB_Checked(object sender, RoutedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            mSecondImgColorProfile = WWImageRead.ColorProfileType.sRGB;
        }

        private void RadioButtonSecondImg_AdobeRGB_Checked(object sender, RoutedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            mSecondImgColorProfile = WWImageRead.ColorProfileType.AdobeRGB;
        }

        private void RadioButtonSecondImg_Rec709_Checked(object sender, RoutedEventArgs e)
        {
            if (!mInitialized) {
                return;
            }
            mSecondImgColorProfile = WWImageRead.ColorProfileType.Rec709;
        }

        public WWOpenTwoImageFilesWindow()
        {
            InitializeComponent();
            mInitialized = true;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(FirstImgPath)) {
                MessageBox.Show("Error: First image specified does not exist.");
                return;
            }
            if (!System.IO.File.Exists(SecondImgPath)) {
                MessageBox.Show("Error: Second image specified does not exist.");
                return;
            }

            if (!double.TryParse(mTimeATextBox.Text, out mTimeASec)) {
                MessageBox.Show("Error: First image time is not number.");
                return;
            }

           if (!double.TryParse(mTimeBTextBox.Text, out mTimeBSec)) {
                MessageBox.Show("Error: Second image time is not number.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonFirstImageBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Title = "Please choose First image file";
            var r = ofd.ShowDialog();
            if (r != true) {
                return;
            }

            mFirstImgTextBox.Text = ofd.FileName;
        }

        private void ButtonSecondImageBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Title = "Please choose Second image file";
            var r = ofd.ShowDialog();
            if (r != true) {
                return;
            }

            mSecondImgTextBox.Text = ofd.FileName;
        }

    }
}
