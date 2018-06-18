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
using System.Windows.Shapes;

namespace WWWaveSimulatorCS {
    public partial class ChooseGPUWindow : Window {
        private bool mInitialized = false;
        
        public ChooseGPUWindow() {
            InitializeComponent();
        }

        public void Add(string desc) {
            listBoxAdapters.Items.Add(desc);
            listBoxAdapters.SelectedIndex = 0;
        }

        public int SelectedAdapterIdx{get;set;}

        private void listBoxAdapters_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            SelectedAdapterIdx = listBoxAdapters.SelectedIndex;
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            Close();
        }

    }
}
