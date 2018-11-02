using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWShowUSBDeviceTree {
    public class UsbDevice {
        public enum NodeType {
            HostController,
            Hub,
            HubPort,
            HubPortHub, //< HubPortがハブ。
        };

        public int idx;
        public int parentIdx;

        public WWUsbDeviceTreeCs.BusSpeed speed;
        public WWUsbDeviceTreeCs.BusSpeed usbVersion;
        public Brush borderBrush;

        public UsbDevice parent;
        public List<UsbDevice> children = new List<UsbDevice>();

        public NodeType nodeType;

        public int layer;
        public string text;
        public UIElement uiElement;
        public double fontSize = 12;
        public double W {
            get { return width; }
        }

        public double H {
            get { return height; }
        }

        public double X { get; set; }
        public double Y { get; set; }

        private double width;
        private double height;

        private void CreateUIElem() {
            // UI elementを作ります
            FontFamily fontFamily = new FontFamily("Segoe UI");

            var tb = new TextBlock {
                Padding = new Thickness(4),
                Text = text,
                FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Color.FromRgb(0x33,0x33,0x37)),
                Foreground = new SolidColorBrush(Colors.White),
            };
            var bd = new Border() {
                BorderThickness = new Thickness(2),
                BorderBrush = borderBrush,
                Child = tb,
            };

            if (nodeType == NodeType.HostController
                || nodeType == NodeType.HubPortHub) {
                tb.Padding = new Thickness(4, 4, 50, 4);
            }

            // UI Elementのサイズを確定します。ActualWidthとActualHeightで取得できる。
            bd.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            bd.Arrange(new Rect(bd.DesiredSize));
            width = bd.ActualWidth;
            height = bd.ActualHeight;

            uiElement = bd;
        }

        public static Brush SpeedToBrush(WWUsbDeviceTreeCs.BusSpeed speed) {
            switch (speed) {
            case WWUsbDeviceTreeCs.BusSpeed.HighSpeed:
                return new SolidColorBrush(Colors.White);
            case WWUsbDeviceTreeCs.BusSpeed.SuperSpeed:
                return new SolidColorBrush(Color.FromRgb(0x40, 0xc0, 0xff));
            case WWUsbDeviceTreeCs.BusSpeed.SuperSpeedPlus:
                return new SolidColorBrush(Color.FromRgb(0xff, 0, 0xff));
            case WWUsbDeviceTreeCs.BusSpeed.LowSpeed:
            case WWUsbDeviceTreeCs.BusSpeed.FullSpeed:
                return new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0));
            default:
                return new SolidColorBrush(Colors.White);
            }
        }

        public UsbDevice(NodeType nodeType, int idx, int parentIdx,
            WWUsbDeviceTreeCs.BusSpeed speed,
            WWUsbDeviceTreeCs.BusSpeed usbVersion,
            string text) {
            this.nodeType = nodeType;
            this.idx = idx;
            this.parentIdx = parentIdx;
            this.text = text;
            this.speed = speed;
            this.usbVersion = usbVersion;
            this.borderBrush = SpeedToBrush(usbVersion);

            CreateUIElem();
        }
    }
}
