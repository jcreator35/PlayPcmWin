using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TimeResolutionTest {
    public partial class MainWindow : Window {
        private DrawGraph mDG;

        private int BitDepth = 2;

        /// <summary>
        /// 作成する画像の枚数。
        /// </summary>
        private int NumImages;

        /// <summary>
        /// サンプルレート。
        /// </summary>
        private const double SampleRate = 44100.0;

        private const int SubsampleDivide = 100;

        /// <summary>
        /// 時間の刻み。
        /// </summary>
        private const double TimeTick = (1.0 / SubsampleDivide) / SampleRate;

        /// <summary>
        /// 矩形波周波数。
        /// </summary>
        private const double Freq = 1000.0;

        /// <summary>
        /// PCMデータの総サンプル数。
        /// </summary>
        private const int NumSamples = 256;

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        /// <summary>
        /// オリジナル関数f(x)。
        /// </summary>
        /// <param name="x">0～1の値。</param>
        /// <returns>f(x)を戻す。</returns>
        private double SquareWave(double x) {
            double y = 0;

            // 時間(秒) tSec
            double tSec = mTime + x * mDG.NumGridX / SampleRate;

            double freq = Freq;

            int order = 1;
            while (freq < SampleRate / 2) {
                double θ = 2.0 * Math.PI * tSec * freq;
                double v = (4.0 / Math.PI / order) * Math.Sin(θ);

                y += v;

                //矩形波なので奇数次高調波が出る。
                order += 2;
                freq = Freq * order;
            };

            //Console.WriteLine("SW, {0},{1},{2}", x, tSec, y);

            return y;
        }

        // 中心付近をプロットする。
        private double[] mSampleArray = new double[NumSamples];

        private static double Sinc(double x) {
            if (Math.Abs(x) < double.Epsilon) {
                return 1.0;
            }

            return Math.Sin(Math.PI * x) / (Math.PI * x);
        }

        /// <summary>
        /// オリジナル関数を量子化してサンプル列にする。
        /// </summary>
        private void CreateSampleArray(DrawGraph.FunctionToPlot f) {
            for (int i = 0; i < NumSamples; ++i) {
                int iS = i - NumSamples / 2;

                double x = (double)iS / mDG.NumGridX;
                double y = f(x);


                // ここでyを量子化する。
                // yの範囲は大体 ±1だが、
                // -2 ≦ y < 2の範囲を表現できるビット列に収容。

                int numYSteps = (int)Math.Pow(2, BitDepth);
                double scale = numYSteps / 4;

                double q = Math.Floor(y * scale + 0.5);

                mSampleArray[i] = (double)q / scale;

                //Console.WriteLine("CSA, {0}, {1}, {2}", x, y, mSampleArray[i]);
            }
            // Console.WriteLine("");
        }

        /// <summary>
        /// サンプル列からアナログ波形を生成。
        /// </summary>
        /// <param name="x">0～1の値。</param>
        /// <returns>f(x)を戻す。</returns>
        private double QuantizedWaveform(double x) {
            double y = 0;

            for (int i = 0; i < NumSamples; ++i) {
                int iS = i - NumSamples / 2;
                y += mSampleArray[i] * Sinc(x * mDG.NumGridX - iS);
            }

            //Console.WriteLine("QW, {0}, {1}", x, y);

            return y;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void Setup() {
            System.IO.Directory.CreateDirectory(string.Format("{0}bit", BitDepth));
            mTime = (double)(-mDG.NumGridX) / SampleRate;
            mCounter = 0;
            Redraw();
        }

        public void Redraw() {
            CreateSampleArray(SquareWave);
            mDG.Redraw(QuantizedWaveform, string.Format("44.1kHz {0}bit PCM", BitDepth));

            mTime += TimeTick;
        }

        private int mCounter = 0;
        private double mTime;

        public MainWindow() {
            InitializeComponent();

            mDG = new DrawGraph(mCanvas0);
            NumImages = SubsampleDivide * (mDG.NumGridX + 1);
        }

        private void SaveCanvasToPng() {
            SaveCanvasToPng1(string.Format("{0}bit/{1:D8}.png", BitDepth, mCounter++));
        }

        private void SaveCanvasToPng1(string path) {
            var bounds = VisualTreeHelper.GetDescendantBounds(mCanvas0);
            double dpi = 96d;

            var rtb = new RenderTargetBitmap(
                (int)bounds.Width, (int)bounds.Height, dpi, dpi,
                System.Windows.Media.PixelFormats.Default);

            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen()) {
                VisualBrush vb = new VisualBrush(mCanvas0);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);

            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            try {
                var ms = new System.IO.MemoryStream();
                pngEncoder.Save(ms);
                ms.Close();
                System.IO.File.WriteAllBytes(path, ms.ToArray());
            } catch (Exception err) {
                MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        BackgroundWorker mBW = new BackgroundWorker();

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Setup();

            mBW.WorkerReportsProgress = true;
            mBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBW_RunWorkerCompleted);
            mBW.DoWork += new DoWorkEventHandler(mBW_DoWork);
            mBW.ProgressChanged += new ProgressChangedEventHandler(mBW_ProgressChanged);

            mBW.RunWorkerAsync();
        }

        void mBW_DoWork(object sender, DoWorkEventArgs e) {
            for (int i = 0; i < NumImages; ++i) {
                System.Threading.Thread.Sleep(500);
                mBW.ReportProgress(1);
            }
            System.Threading.Thread.Sleep(500);
        }

        void mBW_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            SaveCanvasToPng();
            Redraw();
        }

        void mBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            SaveCanvasToPng();

            Console.WriteLine("{0}bit Finished.",BitDepth);

            ++BitDepth;
            if (8 < BitDepth) {
                return;
            }

            Setup();

            mBW.RunWorkerAsync();
        }

    }
}
