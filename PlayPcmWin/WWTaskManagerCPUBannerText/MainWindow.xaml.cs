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
        public MainWindow() {
            InitializeComponent();
        }

        public void Closed() {
        }

        BitmapScroll mBMS = null;
        ThreadLoadBitmap mTLB = new ThreadLoadBitmap();
        Timer mTimer = new Timer();

        public static BitmapSource GrayImageDataToImage(byte[] grayImageData, int w, int h) {
            return BitmapSource.Create(w, h, 96, 96, PixelFormats.Gray8, null, grayImageData, w);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            double interval = 1.0;
            if (!double.TryParse(textBoxInterval.Text, out interval)) {
                MessageBox.Show("Error: Interval should be a number");
                return;
            }

            // 画像を作ります
            var grayImage = TextBitmap.Build(textBoxText.Text);

            int height = 8;
            int width = grayImage.Length / height;

            image.Source = GrayImageDataToImage(grayImage, width, height);

            mBMS = new BitmapScroll(grayImage, width);

            mTLB.Start();

            mTimer.Interval = interval * 1000;
            mTimer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            mTimer.AutoReset = true; 
            mTimer.Enabled = true;

            buttonStart.IsEnabled = false;
            buttonStop.IsEnabled = true;
        }

        void TimerElapsed(object sender, ElapsedEventArgs e) {
            var bs = mBMS.Update();
            mTLB.UpdatePattern(bs);

            Console.WriteLine("TimerElapsed()");
            int pos = 0;
            for (int y = 0; y < 8; ++y) {
                for (int x = 0; x < 8; ++x) {
                    Console.Write("{0}", bs[pos] != 0 ? 'X' : ' ');
                    ++pos;
                }
                Console.WriteLine("");
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            mTimer.Stop();
            mTLB.Stop();
            buttonStart.IsEnabled = true;
            buttonStop.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            mTimer.Stop();
            mTimer.Dispose();
            mTimer = null;
        }
    }
}
