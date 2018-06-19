using System.Windows;
using System.Windows.Controls;

namespace WWDirectComputeCS {
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

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }

    }
}
