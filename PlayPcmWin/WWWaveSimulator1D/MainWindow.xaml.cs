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
using WWWaveSimulatorCS;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace WWWaveSimulator1D {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private WaveSim1D mSim;

        private int mW = 1024;
        private int mVisualizeStep = 4;
        private int mSleepMillisec = 17;

        private double mCenterY = 127;
        private double mMagnitude = 128;

        private int mTimeStep = 32;

        private DispatcherTimer mDT;

        private object mLock = new object();

        private bool mInitialized = false;
        private float mC0 = 334.0f;             // 334 (m/s)
        private float mΔt = 1.0e-5f;            // 1x10^-5 (s)
        private float mΔx = 334.0f * 1.0e-5f;   // 334 * 10^-5 (m)  (mC0 * mΔtにするとSc=1になる)

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
            if (!float.TryParse(textBoxSoundSpeed.Text, out mC0)) {
                MessageBox.Show("E: parse Sound Speed failed");
                return;
            }
            float Δt_ms;
            if (!float.TryParse(textBoxTimeStep.Text, out Δt_ms)) {
                MessageBox.Show("E: parsing Time step failed");
                return;
            }
            mΔt = Δt_ms * 0.001f;

            mΔx = mC0 * mΔt;

            textBlockHalf.Text = string.Format("{0}", mΔx * 512);
            textBlockFull.Text = string.Format("{0}", mΔx * 1024);

            lock (mLock) {
                if (mSim != null) {
                    mSim.Term();
                    mSim = null;
                }

                mSim = new WaveSim1D(mW, mC0, mΔt, mΔx);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            lock (mLock) {
                int nStimuli = mSim.Update(mTimeStep);
                //Console.Write("{0} ", nStimuli);

                UpdateUI();
            }

            // Forcing the CommandManager to raise the RequerySuggested event
            //CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateUI() {
            mPolylineP.Points.Clear();

            var P = mSim.P();
            for (int i = 0; i < P.Length; i+=mVisualizeStep) {
                double y = mCenterY - mMagnitude * P[i];
                mPolylineP.Points.Add(new Point(i, y));
            }

            /*
            var V = mSim.V();
            for (int i = 0; i < V.Length; ++i) {
                double y = mCenterY - mMagnitude * V[i];
                mPolylineV.Points.Add(new Point(i * mScale, y));
            }
            */

            labelSec.Content = string.Format("{0:F4}", mSim.ElapsedTime());
            labelMagnitude.Content = string.Format("Magnitude: {0:F4}", mSim.Magnitude());
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

            float magnitude;
            if (!float.TryParse(textBoxMagnitude.Text, out magnitude)) {
                MessageBox.Show("Parse error : Stimulus magnitude");
                return;
            }

            mSim.AddStimulus(t, (float)(p.X), freq, magnitude);
        }

        private void buttonRewind_Click(object sender, RoutedEventArgs e) {
            mSim.Reset();
        }


        private void buttonUpdateSimulator_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            UpdateSimulator();
        }

        private void sliderFreq_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }
        }

        private void buttonFastForward_Click(object sender, RoutedEventArgs e) {
            lock (mLock) {
                mSim.Update(mTimeStep*10);
            }
        }

        private void buttonFastForward10_Click(object sender, RoutedEventArgs e) {
            lock (mLock) {
                mSim.Update(mTimeStep*100);
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

        private void Window_Closed(object sender, EventArgs e) {
            mSim.Term();
            mSim = null;
        }

        private void sliderTimeStep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            mTimeStep = (int)Math.Pow(2.0, e.NewValue);
            labelTimeStepNumber.Content = string.Format("{0}", mTimeStep);
        }
    
    }
}
