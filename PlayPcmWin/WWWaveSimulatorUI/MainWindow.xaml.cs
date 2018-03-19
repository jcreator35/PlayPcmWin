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
using System.Globalization;

namespace WWWaveSimulatorUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private WaveSim3D mSim;

        private bool mInitialized = false;
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }

        private bool ReadUIParamsAndSetup() {
            int gridW = 0;
            if (!int.TryParse(textBoxGridW.Text, out gridW) || gridW <= 0) {
                MessageBox.Show("Error: Grid Width should be positive integer number");
                return false;
            }
            int gridH = 0;
            if (!int.TryParse(textBoxGridH.Text, out gridH) || gridH <= 0) {
                MessageBox.Show("Error: Grid Height should be positive integer number");
                return false;
            }
            
            int gridD = 0;
            if (!int.TryParse(textBoxGridD.Text, out gridD) || gridD <= 0) {
                MessageBox.Show("Error: Grid Depth should be positive integer number");
                return false;
            }

            mSim = new WaveSim3D(gridW, gridH, gridD);
            return true;
        }

        private void Update() {
            mSim.Update();
            
        }

        private void radioButtonPermit_Checked(object sender, RoutedEventArgs e) {

        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }
            labelHeight.Content = string.Format(CultureInfo.CurrentUICulture, "{0}", (int)slider1.Value);
        }

    }
}
