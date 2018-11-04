using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WWShowUSBDeviceTree {
    public partial class MainWindow : Window {
        private bool mInitialized = false;
        private UsbDeviceTreeCs mUDT = new UsbDeviceTreeCs();
        private UsbDeviceTreeCanvas mUSBCanvas;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mUSBCanvas = new UsbDeviceTreeCanvas(mCanvas);

            mUDT.Init();
            Refresh();

            mTextBoxDescription.Text = Properties.Resources.DescriptionText;

            mInitialized = true;
        }

        private void mButtonRefresh_Click(object sender, RoutedEventArgs e) {
            Refresh();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            mUDT.Term();

            e.Cancel = false;
        }

        private void Refresh() {
            mUDT.Refresh();

            mUSBCanvas.Clear();

            foreach (var hc in mUDT.HCs) {
                mUSBCanvas.AddNode(hc);
            }

            foreach (var hub in mUDT.Hubs) {
                mUSBCanvas.AddNode(hub);
            }

            foreach (var hp in mUDT.HPs) {
                mUSBCanvas.AddNode(hp);
            }

            mUSBCanvas.Update();
        }

        private void mCBShowDesc_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mTextBoxDescription.Visibility = Visibility.Visible;
        }

        private void mCBShowDesc_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mTextBoxDescription.Visibility = Visibility.Collapsed;
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            // 原因を調べていないが、ここには来ない。PreviewMouseWheel
            // には来る。
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            var scaling = sliderWindowScaling.Value;
            if (e.Delta < 0) {
                // 1.25の4乗根 = 1.0573712634406
                scaling /= 1.0573712634406;
            } else {
                scaling *= 1.0573712634406;
            }
            sliderWindowScaling.Value = scaling;
        }
    }
}
