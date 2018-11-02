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
        private WWUsbDeviceTreeCs mUDT = new WWUsbDeviceTreeCs();
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
        }

        private void Refresh() {
            mUDT.Refresh();

            mUSBCanvas.Clear();
            int nHC = mUDT.HCs.Count;
            for (int i = 0; i < nHC; ++i) {
                var hc = mUDT.HCs[i];
                mUSBCanvas.AddNode(UsbDevice.NodeType.HostController, hc.idx, -1,
                    WWUsbDeviceTreeCs.BusSpeed.RootHub,
                    WWUsbDeviceTreeCs.BusSpeed.RootHub,
                    string.Format("{0}\n{1}\n{2}",
                    hc.name, hc.vendor, hc.desc));
            }

            int nHub = mUDT.Hubs.Count;
            for (int i=0; i< mUDT.Hubs.Count; ++i) {
                var hub = mUDT.Hubs[i];
                var speed = (WWUsbDeviceTreeCs.BusSpeed)hub.speed;
                var speedStr = (speed == WWUsbDeviceTreeCs.BusSpeed.RootHub) ? "Root hub" :
                    string.Format("Max : {0}", WWUsbDeviceTreeCs.WWUsbDeviceBusSpeedToStr(speed));
                mUSBCanvas.AddNode(UsbDevice.NodeType.Hub, hub.idx, hub.parentIdx,
                    speed, speed, string.Format("{0} ports\n{1}",
                    hub.numPorts, speedStr));
            }

            foreach (var hp in mUDT.HPs) {
                var nodeType = UsbDevice.NodeType.HubPort;
                if (hp.deviceIsHub != 0) {
                    nodeType = UsbDevice.NodeType.HubPortHub;
                }
                var speed = (WWUsbDeviceTreeCs.BusSpeed)hp.speed;
                var version = (WWUsbDeviceTreeCs.BusSpeed)hp.usbVersion;
                mUSBCanvas.AddNode(nodeType, hp.idx, hp.parentIdx,
                    speed, version, string.Format("{0}\n{1}\n{2} {3} {4}\n{5}",
                        hp.name, hp.vendor, hp.ConnectorTypeStr(), hp.VersionStr(), hp.SpeedStr(),
                        hp.PowerStr()));
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
    }
}
