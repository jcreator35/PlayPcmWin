using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;

namespace WWTaskManagerText {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        const int WIDTH = 8;
        const int HEIGHT = 8;
        const int CHECKBOX_NUM = 64;

        public MainWindow() {
            InitializeComponent();
        }

        BitmapScroll mBMS = null;
        ThreadLoadBitmap mTLB = new ThreadLoadBitmap();
        Timer mTimer = null;

        CheckBox[] mCB = new CheckBox[CHECKBOX_NUM];

        bool mInitialized = false;

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mCB[0] = checkBox0;
            mCB[1] = checkBox1;
            mCB[2] = checkBox2;
            mCB[3] = checkBox3;
            mCB[4] = checkBox4;
            mCB[5] = checkBox5;
            mCB[6] = checkBox6;
            mCB[7] = checkBox7;
            mCB[8] = checkBox8;
            mCB[9] = checkBox9;

            mCB[10] = checkBox10;
            mCB[11] = checkBox11;
            mCB[12] = checkBox12;
            mCB[13] = checkBox13;
            mCB[14] = checkBox14;
            mCB[15] = checkBox15;
            mCB[16] = checkBox16;
            mCB[17] = checkBox17;
            mCB[18] = checkBox18;
            mCB[19] = checkBox19;

            mCB[20] = checkBox20;
            mCB[21] = checkBox21;
            mCB[22] = checkBox22;
            mCB[23] = checkBox23;
            mCB[24] = checkBox24;
            mCB[25] = checkBox25;
            mCB[26] = checkBox26;
            mCB[27] = checkBox27;
            mCB[28] = checkBox28;
            mCB[29] = checkBox29;

            mCB[30] = checkBox30;
            mCB[31] = checkBox31;
            mCB[32] = checkBox32;
            mCB[33] = checkBox33;
            mCB[34] = checkBox34;
            mCB[35] = checkBox35;
            mCB[36] = checkBox36;
            mCB[37] = checkBox37;
            mCB[38] = checkBox38;
            mCB[39] = checkBox39;

            mCB[40] = checkBox40;
            mCB[41] = checkBox41;
            mCB[42] = checkBox42;
            mCB[43] = checkBox43;
            mCB[44] = checkBox44;
            mCB[45] = checkBox45;
            mCB[46] = checkBox46;
            mCB[47] = checkBox47;
            mCB[48] = checkBox48;
            mCB[49] = checkBox49;

            mCB[50] = checkBox50;
            mCB[51] = checkBox51;
            mCB[52] = checkBox52;
            mCB[53] = checkBox53;
            mCB[54] = checkBox54;
            mCB[55] = checkBox55;
            mCB[56] = checkBox56;
            mCB[57] = checkBox57;
            mCB[58] = checkBox58;
            mCB[59] = checkBox59;

            mCB[60] = checkBox60;
            mCB[61] = checkBox61;
            mCB[62] = checkBox62;
            mCB[63] = checkBox63;

            mInitialized = true;
        }

        public static BitmapSource GrayImageDataToImage(byte[] grayImageData, int w, int h) {
            return BitmapSource.Create(w, h, 96, 96, PixelFormats.Gray8, null, grayImageData, w);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            if (radioButtonTextBannerMarquee.IsChecked == true) {
                SetupBanner();
            }
            if (radioButtonStaticBitmapPattern.IsChecked == true) {
                mBMS = null;
                mTimer = null;
                mTLB.Start();
                UpdateStaticBitmap();
            }

            buttonStart.IsEnabled = false;
            buttonStop.IsEnabled = true;
            radioButtonStaticBitmapPattern.IsEnabled = false;
            radioButtonTextBannerMarquee.IsEnabled = false;
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            if (mTimer != null) {
                mTimer.Stop();
            }
            mTLB.Stop();
            buttonStart.IsEnabled = true;
            buttonStop.IsEnabled = false;
            radioButtonStaticBitmapPattern.IsEnabled = true;
            radioButtonTextBannerMarquee.IsEnabled = true;
        }

        void SetupBanner() {
            double interval = 1.0;
            if (!double.TryParse(textBoxInterval.Text, out interval)) {
                MessageBox.Show("Error: Interval should be a number");
                return;
            }

            // 画像を作ります
            var grayImage = TextBitmap.Build(textBoxText.Text);

            int height = HEIGHT;
            int width = grayImage.Length / height;

            image.Source = GrayImageDataToImage(grayImage, width, height);

            mBMS = new BitmapScroll(grayImage, width);
            mTLB.Start();

            mTimer = new Timer();
            mTimer.Interval = interval * 1000;
            mTimer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            mTimer.AutoReset = true;
            mTimer.Enabled = true;
        }

        void TimerElapsed(object sender, ElapsedEventArgs e) {
            var bs = mBMS.Update();
            mTLB.UpdatePattern(bs);

            Console.WriteLine("TimerElapsed()");
            int pos = 0;
            for (int y = 0; y < HEIGHT; ++y) {
                for (int x = 0; x < WIDTH; ++x) {
                    Console.Write("{0}", bs[pos] != 0 ? 'X' : ' ');
                    ++pos;
                }
                Console.WriteLine("");
            }
        }

        private void UpdateStaticBitmap() {
            var bm = new byte[CHECKBOX_NUM];
            for (int i = 0; i < CHECKBOX_NUM; ++i) {
                bm[i] = (byte)((mCB[i].IsChecked == true) ? 0 : 255);
            }
            mTLB.UpdatePattern(bm);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (mTimer != null) {
                mTimer.Stop();
                mTimer.Dispose();
                mTimer = null;
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            UpdateStaticBitmap();
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            UpdateStaticBitmap();
        }

        private void buttonSetAll_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < CHECKBOX_NUM; ++i) {
                mCB[i].IsChecked = true;
            }
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < CHECKBOX_NUM; ++i) {
                mCB[i].IsChecked = false;
            }
        }

        private void radioButtonTextBannerMarquee_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            gbTextBannerMarquee.IsEnabled = true;
            gbStaticBitmapPattern.IsEnabled = false;
        }

        private void radioButtonStaticBitmapPattern_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            gbTextBannerMarquee.IsEnabled = false;
            gbStaticBitmapPattern.IsEnabled = true;
        }

    }
}
