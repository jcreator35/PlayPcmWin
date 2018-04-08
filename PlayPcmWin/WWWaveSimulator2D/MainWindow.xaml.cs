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
        private WaveSim2D mSim;
        private object mLock = new object();
        private int mW = 1024;
        private int mH = 1024;
        private int mSleepMillisec = 100;
        private float mC0 = 334; // 334m/s
        private float mΔt = 0.01f * 0.001f; // 0.01ms
        private float mΔx;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            UpdateSimulator();

            mDT = new System.Windows.Threading.DispatcherTimer();
            mDT.Tick += new EventHandler(dispatcherTimer_Tick);
            mDT.Interval = new TimeSpan(0, 0, 0, 0, mSleepMillisec);
            mDT.Start();

            buttonRewind.IsEnabled = true;
            buttonFastForward.IsEnabled = true;

            labelFreq.IsEnabled = false;
            textBoxFreq.IsEnabled = false;
        }

        private void UpdateSimulator() {
            mΔx = mC0 * mΔt;

            textBlockHalf.Text = string.Format("{0}", mΔx * 512);
            textBlockFull.Text = string.Format("{0}", mΔx * 1024);

            lock (mLock) {
                mSim = new WaveSim2D(mW, mH, mC0, mΔt, mΔx);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            lock (mLock) {
                int nStimuli = mSim.Update();
                //Console.Write("{0} ", nStimuli);

                UpdateUI();
            }
        }

        private void UpdateUI() {

            var Pshow = mSim.Pshow();
            var bitmap = BitmapSource.Create(mW, mH, 96,96, PixelFormats.Gray32Float, null, Pshow, mW*4);
            bitmap.Freeze();
            mImage.Source = bitmap;

            labelSec.Content = string.Format("{0:F4}", mSim.ElapsedTime());
            labelMagnitude.Content = string.Format("Magnitude: {0:F4}", mSim.Magnitude());
        }

        private void buttonRewind_Click(object sender, RoutedEventArgs e) {
            mSim.Reset();
        }

        private void buttonFastForward_Click(object sender, RoutedEventArgs e) {
            lock (mLock) {
                for (int i = 0; i < 1000; ++i) {
                    mSim.Update();
                }
            }
        }

        private void buttonFastForward10_Click(object sender, RoutedEventArgs e) {
            lock (mLock) {
                for (int i = 0; i < 10000; ++i) {
                    mSim.Update();
                }
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

            mSim.AddStimulus(t, (float)(p.X), (float)(p.Y), freq, magnitude);
        }
    }
}
