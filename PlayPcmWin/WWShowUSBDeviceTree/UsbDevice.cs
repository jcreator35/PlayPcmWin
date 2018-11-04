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

        public const double PADDING_RIGHT = 50;
        public enum NodeType {
            HostController,
            Hub,
            HubPort,
            HubPortHub, //< HubPortがハブ。
        };

        public int idx;
        public int parentIdx;

        public UsbDeviceTreeCs.BusSpeed speed;
        public UsbDeviceTreeCs.BusSpeed usbVersion;
        public Brush borderBrush;

        public UsbDevice parent;
        public List<UsbDevice> children = new List<UsbDevice>();

        public NodeType nodeType;

        public int layer;
        public string text;
        public UIElement uiElement;
        public double fontSize = 12;
        public double W { get; private set; }

        public double H { get; private set; }

        public double X { get; set; }
        public double Y { get; set; }

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
                tb.Padding = new Thickness(4, 4, PADDING_RIGHT+4, 4);
            }

            // UI Elementのサイズを確定します。ActualWidthとActualHeightで取得できる。
            bd.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            bd.Arrange(new Rect(bd.DesiredSize));
            W = bd.ActualWidth;
            H = bd.ActualHeight;

            uiElement = bd;
        }

        public static Brush SpeedToBrush(UsbDeviceTreeCs.BusSpeed speed) {
            switch (speed) {
            case UsbDeviceTreeCs.BusSpeed.HighSpeed:
                return new SolidColorBrush(Colors.White);
            case UsbDeviceTreeCs.BusSpeed.SuperSpeed:
                return new SolidColorBrush(Color.FromRgb(0x40, 0xc0, 0xff));
            case UsbDeviceTreeCs.BusSpeed.SuperSpeedPlus:
                return new SolidColorBrush(Color.FromRgb(0xff, 0, 0xff));
            case UsbDeviceTreeCs.BusSpeed.LowSpeed:
            case UsbDeviceTreeCs.BusSpeed.FullSpeed:
                return new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0));
            default:
                return new SolidColorBrush(Colors.White);
            }
        }

        public UsbDevice(NodeType nodeType, int idx, int parentIdx,
            UsbDeviceTreeCs.BusSpeed speed,
            UsbDeviceTreeCs.BusSpeed usbVersion,
            string text) {
            this.nodeType = nodeType;
            this.idx = idx;
            this.parentIdx = parentIdx;
            this.text = text;
            this.speed = speed;
            this.usbVersion = usbVersion;
            borderBrush = SpeedToBrush(usbVersion);

            CreateUIElem();
        }
    }
}
