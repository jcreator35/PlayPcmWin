using System.Windows;
using WWDirectCompute12;

namespace WWArbitraryResampler {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        WWResampleGpu mResampleGpu = new WWResampleGpu();

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mResampleGpu.Init();

            mComboBoxAdapterList.Items.Clear();
            foreach (var item in mResampleGpu.AdapterList) {
                mComboBoxAdapterList.Items.Add(string.Format("{0} VideoMem={1}MiB SharedMem={2}MiB {3} {4}",
                    item.name,
                    item.videoMemMiB, item.sharedMemMiB,
                    item.remote ? "Remote" : "",
                    item.software ? "Software" : ""));
            }
            if (0 < mComboBoxAdapterList.Items.Count) { 
                mComboBoxAdapterList.SelectedIndex = 0;
            } else {
                MessageBox.Show("Error: No DirectX12 GPU found!",
                    "Error: No DirectX12 GPU Found",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
        }

        private void Window_Closed(object sender, System.EventArgs e) {
            mResampleGpu.Term();
        }
    }
}
