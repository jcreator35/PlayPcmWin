using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static WWShowUSBDeviceTree.WWUsbDeviceTreeCs;

namespace WWShowUSBDeviceTree {
    public class UsbDeviceTreeCanvas {
        Canvas mCanvas;

        List<Path> mCables = new List<Path>();

        List<UsbDevice> mNodeList = new List<UsbDevice>();

        List<List<UsbDevice>> mNodeListByLayer = new List<List<UsbDevice>>();

        public UsbDeviceTreeCanvas(Canvas c) {
            mCanvas = c;
        }

        public void Clear() {
            mCanvas.Children.Clear();
            mCables.Clear();
            mNodeList.Clear();
            mNodeListByLayer.Clear();
        }

        private void AddNode(UsbDevice node) {
            mNodeList.Add(node);
            mCanvas.Children.Add(node.uiElement);
        }

        public void AddNode(WWUsbHostControllerCs hc) {
            string s = string.Format("{0}\n{1}\n{2}",
                    hc.name, hc.vendor, hc.desc);
            var node = new UsbDevice(UsbDevice.NodeType.HostController,
                hc.idx, -1, BusSpeed.RootHub, BusSpeed.RootHub, s);
            AddNode(node);
        }
        public void AddNode(WWUsbHubCs hub) {
            var speed = (WWUsbDeviceTreeCs.BusSpeed)hub.speed;
            var speedStr = (speed == WWUsbDeviceTreeCs.BusSpeed.RootHub) ? "Root hub" :
                string.Format("Max : {0}", WWUsbDeviceTreeCs.WWUsbDeviceBusSpeedToStr(speed));
            var s = string.Format("{0} ports\n{1}", hub.numPorts, speedStr);
            var node = new UsbDevice(UsbDevice.NodeType.Hub, hub.idx, hub.parentIdx, speed, speed, s);
            AddNode(node);
        }
        public void AddNode(WWUsbHubPortCs hp) {
            var nodeType = UsbDevice.NodeType.HubPort;
            if (hp.deviceIsHub != 0) {
                nodeType = UsbDevice.NodeType.HubPortHub;
            }
            var speed = (WWUsbDeviceTreeCs.BusSpeed)hp.speed;
            var version = (WWUsbDeviceTreeCs.BusSpeed)hp.usbVersion;

            var s = string.Format("{0}\n{1}\n{2} {3} {4}\n{5}",
                    hp.name, hp.vendor, hp.ConnectorTypeStr(), hp.VersionStr(), hp.SpeedStr(),
                    hp.PowerStr());

            var node = new UsbDevice(nodeType, hp.idx, hp.parentIdx, speed, version, s);
            AddNode(node);
        }

        public void AddNode(UsbDevice.NodeType nodeType, int idx, int parentIdx,
                WWUsbDeviceTreeCs.BusSpeed speed,
                WWUsbDeviceTreeCs.BusSpeed usbVersion,
                string text) {
            var node = new UsbDevice(nodeType, idx, parentIdx, speed, usbVersion, text);
            mNodeList.Add(node);
            mCanvas.Children.Add(node.uiElement);
        }

        public void Update() {
            // layerを確定する。
            foreach (var v in mNodeList) {
                ResolveLayer(v);
            }

            // 同じlayerのノードを集める。
            mNodeListByLayer.Clear();
            foreach (var v in mNodeList) {
                while (mNodeListByLayer.Count <= v.layer) {
                    mNodeListByLayer.Add(new List<UsbDevice>());
                }
                mNodeListByLayer[v.layer].Add(v);
            }

            ResolveConnections();

            ResolvePositions();
            
            ConnectCables();
        }

        /// <summary>
        /// 親子関係の接続。
        /// </summary>
        private void ResolveConnections() {
            foreach (var n in mNodeList) {
                n.children.Clear();
            }

            foreach (var n in mNodeList) {
                if (n.parentIdx < 0) {
                    continue;
                }

                var p = FindNode(n.parentIdx);
                p.children.Add(n);
                n.parent = p;
            }
        }

        /// <summary>
        /// ノード間の線を引きます。
        /// </summary>
        private void ConnectCables() {
            // すでに引かれている線を消す。
            foreach (var c in mCables) {
                mCanvas.Children.Remove(c);
            }
            mCables.Clear();

            // 線を引いていきます。
            for (int layer = 2; layer < mNodeListByLayer.Count; layer += 2) {
                foreach (var node in mNodeListByLayer[layer]) {
                    var speed = node.speed;
                    var brush = UsbDevice.SpeedToBrush(speed);

                    var parentN = node.parent;
                    
                    var from = new Point(parentN.X + parentN.W, parentN.Y+parentN.H/2);
                    var to = new Point(node.X, node.Y + node.H / 2);

                    var control1 = new Point((from.X + to.X) / 2, from.Y);
                    var control2 = new Point((from.X + to.X) / 2, to.Y);

                    var p = new Path() {
                        Data = new PathGeometry() {
                            Figures = new PathFigureCollection()
                        },
                        Stroke = brush,
                        StrokeThickness = 2,
                    };
                    var pfc = (PathFigureCollection)((PathGeometry)p.Data).Figures;
                    PathFigure pf = new PathFigure() {
                        StartPoint = from,
                        Segments = new PathSegmentCollection()
                    };
                    pf.Segments.Add(new BezierSegment() {
                        Point1 = control1,
                        Point2 = control2,
                        Point3 = to,
                    });
                    pfc.Add(pf);

                    mCanvas.Children.Add(p);
                    mCables.Add(p);
                }
            }

            
        }

        /// <summary>
        /// 表示位置を確定する。
        /// </summary>
        private void ResolvePositions() {

            double canvasW = 0;
            double canvasH = 0;

            double portFitX = 40;
            double spacingX = 50;
            double spacingY = 10;

            double xOffs = spacingY;
            for (int layer=0; layer<mNodeListByLayer.Count(); ++layer) {
                var nodeList = mNodeListByLayer[layer];
                double y = spacingY;

                double maxW = 0;
                foreach (var node in nodeList) {
                    if ((layer & 1) == 1) {

                        // 補足的なポート詳細情報。親に重ねて表示。
                        double x = node.parent.X + node.parent.W - portFitX;
                        y = node.parent.Y + node.parent.H/2 - node.H/2;
                        node.X = x;
                        node.Y = y;
                        Canvas.SetLeft(node.uiElement, x);
                        Canvas.SetTop(node.uiElement, y);
                        Canvas.SetZIndex(node.uiElement, 2);
                    } else {
                        node.X = xOffs;
                        node.Y = y;

                        Canvas.SetLeft(node.uiElement, xOffs);
                        Canvas.SetTop(node.uiElement, y);
                        Canvas.SetZIndex(node.uiElement, 1);

                        // 次(下)のnode位置。
                        y += node.H + spacingY;
                    }

                    if (canvasW < node.X + node.W) {
                        canvasW = node.X + node.W;
                    }
                    if (canvasH < node.Y + node.H) {
                        canvasH = node.Y + node.H;
                    }

                    // 次のlayerの位置計算のための最大幅。
                    if (maxW < node.W) {
                        maxW = node.W;
                    }
                }

                if ((layer & 1) == 0) {
                    xOffs += maxW - portFitX;
                } else {
                    xOffs += maxW + spacingX;
                }
            }
            mCanvas.Width = canvasW + spacingX;
            mCanvas.Height = canvasH + spacingX;
        }

        private UsbDevice FindNode(int idx) {
            foreach (var v in mNodeList) {
                if (v.idx == idx) {
                    return v;
                }
            }

            return null;
        }

        private void ResolveLayer(UsbDevice v) {
            // layerを確定する。
            v.layer = 0;
            UsbDevice c = v;
            while (0 <= c.parentIdx) {
                c = FindNode(c.parentIdx);
                ++v.layer;
            }
        }

    }
}
