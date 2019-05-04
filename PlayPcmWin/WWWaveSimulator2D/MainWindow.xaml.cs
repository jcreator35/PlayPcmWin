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
using System.Windows.Threading;
using WWWaveSimulatorCS;

namespace WWWaveSimulator2D {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool mInitialized;
        private DispatcherTimer mDT;
        private WaveSimFdtd2D mSim;
        private object mLock = new object();
        private int mW = 512;
        private int mH = 512;
        private int mSleepMillisec = 50;
        private float mC0 = 334; // 334m/s
        private float mΔt = 0.01f * 0.001f; // 0.01ms
        private float mΔx;
        private int mSimRepeatCount = 16;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            CreateSimulator();

            mDT = new System.Windows.Threading.DispatcherTimer();
            mDT.Tick += new EventHandler(dispatcherTimer_Tick);
            mDT.Interval = new TimeSpan(0, 0, 0, 0, mSleepMillisec);
            mDT.Start();

            buttonRewind.IsEnabled = true;
            buttonFastForward.IsEnabled = true;

            labelFreq.IsEnabled = false;
            textBoxFreq.IsEnabled = false;

            sliderStep.Value = 4;
        }

        private void DestroySimulator() {
            lock (mLock) {
                if (mSim != null) {
                    mSim.Term();
                    mSim = null;
                }
            }
        }

        private void CreateSimulator() {
            if (!float.TryParse(mTextBoxC0.Text, out mC0) || mC0 <= 0) {
                MessageBox.Show("C0 should be number larger than 0");
                return;
            }
            if (!float.TryParse(mTextBoxΔt.Text, out mΔt) || mΔt <= 0) {
                MessageBox.Show("Δt should be number larger than 0");
                return;
            }
            mΔt = mΔt * 0.001f; //< msで入力、秒に変換。

            lock (mLock) {
                mSim = new WaveSimFdtd2D(mW, mH, mC0, mΔt);
            }

            mΔx = mSim.GetΔx();
            Console.WriteLine("C0={0} Δt={1} Δx={2}", mC0, mΔt, mΔx);

            textBlockHalf.Text = string.Format("{0:0.00} m", mΔx * 512);
            textBlockFull.Text = string.Format("{0:0.00} m", mΔx * 1024);

            {
                var bitmap = BitmapSource.Create(mW, mH, 96, 96, PixelFormats.Gray32Float, null, mSim.LossShow(), mW * 4);
                bitmap.Freeze();
                mImageLoss.Source = bitmap;
            }
            {
                var bitmap = BitmapSource.Create(mW, mH, 96, 96, PixelFormats.Gray32Float, null, mSim.CrShow(), mW * 4);
                bitmap.Freeze();
                mImageCr.Source = bitmap;
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            lock (mLock) {
                if (mSim == null) {
                    return;
                }

                if (0 < mSimRepeatCount) {
                    int nStimuli = mSim.Update(mSimRepeatCount);
                    //Console.Write("{0} ", nStimuli);

                    UpdateUI();
                }
            }
        }

        private void UpdateUI() {

            var Pshow = mSim.Pshow();
            var bitmap = BitmapSource.Create(mW, mH, 96,96, PixelFormats.Gray32Float, null, Pshow, mW*4);
            bitmap.Freeze();
            mImagePressure.Source = bitmap;

            labelSec.Content = string.Format("{0:F4}", mSim.ElapsedTime());
            labelMagnitude.Content = string.Format("Magnitude: {0:F4}", mSim.Magnitude());
            labelIteration.Content = string.Format("{0}", mSim.TimeTick());
        }

        private void buttonRewind_Click(object sender, RoutedEventArgs e) {
            mSim.Reset();
        }

        private void buttonFastForward_Click(object sender, RoutedEventArgs e) {
            lock (mLock) {
                mSim.Update(1000);
            }
        }

        private void buttonFastForward10_Click(object sender, RoutedEventArgs e) {
            lock (mLock) {
                mSim.Update(10000);
            }
        }

        private void comboBoxSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            WaveEvent.EventType t =
                (WaveEvent.EventType)comboBoxSourceType.SelectedIndex;

            switch (t) {
            case WaveEvent.EventType.Gaussian:
            case WaveEvent.EventType.Pulse:
                labelFreq.IsEnabled = false;
                textBoxFreq.IsEnabled = false;
                break;
            case WaveEvent.EventType.Sine:
                labelFreq.IsEnabled = true;
                textBoxFreq.IsEnabled = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        private void canvasP_MouseUp(object sender, MouseButtonEventArgs e) {
            Point p = Mouse.GetPosition(canvasP);

            {
                var l1 = new Line();
                l1.X1 = p.X - 3;
                l1.Y1 = p.Y - 3;
                l1.X2 = p.X + 3;
                l1.Y2 = p.Y + 3;
                l1.Stroke = Brushes.Yellow;
                l1.StrokeThickness = 1;
                canvasP.Children.Add(l1);

                var l2 = new Line();
                l2.X1 = p.X - 3;
                l2.Y1 = p.Y + 3;
                l2.X2 = p.X + 3;
                l2.Y2 = p.Y - 3;
                l2.Stroke = Brushes.Yellow;
                l2.StrokeThickness = 1;
                canvasP.Children.Add(l2);
            }

            int canvasW = (int)canvasP.ActualWidth;
            int canvasH = (int)canvasP.ActualHeight;

            float canvasToSimX = (float)mW / canvasW;
            float canvasToSimY = (float)mH / canvasH;

            WaveEvent.EventType t =
                (WaveEvent.EventType)comboBoxSourceType.SelectedIndex;

            float freq;
            if (!float.TryParse(textBoxFreq.Text, out freq)) {
                MessageBox.Show("Parse error : Frequency");
                return;
            }

            float magnitude = 0;
            if (!float.TryParse(textBoxStimulationMagnitude.Text, out magnitude)) {
                MessageBox.Show("Error: Stimulation magnitude parse error");
                return;
            }

            mSim.AddStimulus(t, (int)(p.X * canvasToSimX), (int)(p.Y * canvasToSimY), freq, magnitude);
        }

        private void sliderStep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            int v = (int)e.NewValue;
            if (v == -1) {
                mSimRepeatCount = 0;
            } else {
                mSimRepeatCount = (int)Math.Pow(2, v);
            }

            labelStepNum.Content = string.Format("{0}", mSimRepeatCount);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            mDT.Stop();
            lock(mLock) {
                DestroySimulator();
            }
        }

        private void radioButtonShowPressure_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mSim.VisualizeMode = WaveSimFdtd2D.VisualizeModeType.VM_Linear;

            mImageCr.Visibility = System.Windows.Visibility.Hidden;
            mImageLoss.Visibility = System.Windows.Visibility.Hidden;
            mImagePressure.Visibility = System.Windows.Visibility.Visible;
        }

        private void radioButtonShowPressureLog_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mSim.VisualizeMode = WaveSimFdtd2D.VisualizeModeType.VM_Log;

            mImageCr.Visibility = System.Windows.Visibility.Hidden;
            mImageLoss.Visibility = System.Windows.Visibility.Hidden;
            mImagePressure.Visibility = System.Windows.Visibility.Visible;
        }

        private void radioButtonShowLoss_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mImageCr.Visibility = System.Windows.Visibility.Hidden;
            mImageLoss.Visibility = System.Windows.Visibility.Visible;
            mImagePressure.Visibility = System.Windows.Visibility.Hidden;
        }

        private void radioButtonShowCr_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mImageCr.Visibility = System.Windows.Visibility.Visible;
            mImageLoss.Visibility = System.Windows.Visibility.Hidden;
            mImagePressure.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mDT.Stop();
            DestroySimulator();
            CreateSimulator();
            mDT.Start();
        }

    }
}
