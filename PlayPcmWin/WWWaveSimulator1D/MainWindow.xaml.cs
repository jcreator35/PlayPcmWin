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

        private int mW = 256;
        private int mScale = 4;
        private int mSleepMillisec = 17;

        private double mCenterY = 127;
        private double mMagnitude = 64;

        DispatcherTimer mDT;

        public MainWindow() {
            InitializeComponent();
        }

        private bool mInitialized = false;

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            mSim = new WaveSim1D(mW);

            mDT = new System.Windows.Threading.DispatcherTimer();
            mDT.Tick += new EventHandler(dispatcherTimer_Tick);
            mDT.Interval = new TimeSpan(0, 0, 0, 0, mSleepMillisec);
            mDT.Start();

            buttonPlay.IsEnabled = false;
            buttonStepForward.IsEnabled = false;
            buttonPause.IsEnabled = true;
            buttonRewind.IsEnabled = true;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            mSim.Update();

            UpdateUI();

            // Forcing the CommandManager to raise the RequerySuggested event
            //CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateUI() {
            mPolylineP.Points.Clear();

            var P = mSim.P();
            for (int i = 0; i < P.Length; ++i) {
                double y = mCenterY - mMagnitude * P[i];
                mPolylineP.Points.Add(new Point(i * mScale, y));
            }

            /*
            var V = mSim.V();
            for (int i = 0; i < V.Length; ++i) {
                double y = mCenterY - mMagnitude * V[i];
                mPolylineV.Points.Add(new Point(i * mScale, y));
            }
            */

            labelSec.Content = string.Format("{0:F4}", mSim.ElapsedTime());
        }

        private void canvasP_MouseUp(object sender, MouseButtonEventArgs e) {
            Point p = Mouse.GetPosition(canvasP);
            mSim.AddStimula((float)(p.X / mScale));
        }

        private void buttonRewind_Click(object sender, RoutedEventArgs e) {
            mSim.Reset();
        }
    }
}
