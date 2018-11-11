using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static WWShowUSBDeviceTree.UsbDeviceTreeCs;

namespace WWShowUSBDeviceTree {
    public class UsbDeviceTreeCanvas {
        private const int ZINDEX_PATH = 0;
        private const int ZINDEX_USBDEVICE = 1;
        private const int ZINDEX_HUB = 2;

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
            var speed = (UsbDeviceTreeCs.BusSpeed)hub.speed;
            var speedStr = (speed == UsbDeviceTreeCs.BusSpeed.RootHub) ? "\nRoot hub" : "";
            var s = string.Format("{0} ports{1}", hub.numPorts, speedStr);
            var node = new UsbDevice(UsbDevice.NodeType.Hub, hub.idx, hub.parentIdx, speed, speed, s);
            AddNode(node);
        }
        public void AddNode(WWUsbHubPortCs hp, bool showDetail) {
            var nodeType = UsbDevice.NodeType.HubPort;
            if (hp.deviceIsHub != 0) {
                nodeType = UsbDevice.NodeType.HubPortHub;
            }
            var speed = (UsbDeviceTreeCs.BusSpeed)hp.speed;
            var version = (UsbDeviceTreeCs.BusSpeed)hp.usbVersion;

            string s = "";

            if (showDetail) {
                var confReader = new UsbConfDescReader();
                var confS = confReader.Read(hp);

                s = string.Format("{0}\n{1}\n{2} {3}, {4}\n{5}",
                        hp.name, hp.vendor, hp.ConnectorTypeStr(), hp.VersionStr(), hp.SpeedStr(),
                        confS);

                var node = new UsbDevice(nodeType, hp.idx, hp.parentIdx, speed, version, s);

                // オーディオのchild nodes。
                foreach (var m in confReader.mModules) {
                    node.Add(m);
                    mCanvas.Children.Add(m.uiElement);
                }
                node.Resolve();

                AddNode(node);

            } else {
                s = string.Format("{0}\n{1}\n{2} {3}, {4}",
                        hp.name, hp.vendor, hp.ConnectorTypeStr(), hp.VersionStr(), hp.SpeedStr());
                var node = new UsbDevice(nodeType, hp.idx, hp.parentIdx, speed, version, s);
                AddNode(node);
            }

        }

        public void AddNode(UsbDevice.NodeType nodeType, int idx, int parentIdx,
                UsbDeviceTreeCs.BusSpeed speed,
                UsbDeviceTreeCs.BusSpeed usbVersion,
                string text) {
            var node = new UsbDevice(nodeType, idx, parentIdx, speed, usbVersion, text);
            mNodeList.Add(node);
            mCanvas.Children.Add(node.uiElement);
        }

        private void FixupHubSpeed() {
            for (int layer=3; layer <mNodeListByLayer.Count; layer += 2) {
                foreach (var n in mNodeListByLayer[layer]) {
                    var parent = FindNode(n.parentIdx);
                    n.speed = parent.speed;
                    n.usbVersion = parent.speed;
                    n.SpeedUpdated();
                    Canvas.SetZIndex(n.uiElement, ZINDEX_HUB);
                }
            }
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

            FixupHubSpeed();

            ResolvePositions();

            ConnectCables();

            foreach (var v in mNodeList) {
                v.PositionUpdated(mCanvas);
            }
        }

        /// <summary>
        /// 親子関係の接続。
        /// </summary>
        private void ResolveConnections() {
            foreach (var n in mNodeList) {
                n.right.Clear();
            }

            foreach (var n in mNodeList) {
                if (n.parentIdx < 0) {
                    continue;
                }

                var p = FindNode(n.parentIdx);
                p.right.Add(n);
                n.left = p;
            }
        }

        private Path CreatePath(Point from, Point to, Point control1, Point control2, Brush brush) {
            var p = new Path() {
                Data = new PathGeometry() {
                    Figures = new PathFigureCollection()
                },
                Stroke = brush,
                StrokeThickness = 2,
            };
            Canvas.SetZIndex(p, ZINDEX_PATH);

            var pfc = ((PathGeometry)p.Data).Figures;
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
            return p;
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
                for (int i=0; i<mNodeListByLayer[layer].Count; ++i) {
                    int count = mNodeListByLayer[layer].Count;
                    var node = mNodeListByLayer[layer][i];

                    var speed = node.speed;
                    var brush = UsbDevice.SpeedToBrush(speed);

                    var parentN = node.left;

                    var from = new Point(parentN.X + parentN.W, parentN.Y + parentN.H / 2);
                    var to = new Point(node.X, node.Y + node.H / 2);

                    var control1 = new Point((from.X + to.X) / 2, from.Y);
                    var control2 = new Point((from.X + to.X) / 2, to.Y);

                    var p = CreatePath(from, to, control1, control2, brush);

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

            double spacingX = 100;
            double spacingY = 10;

            double xOffs = spacingY;
            for (int layer=0; layer<mNodeListByLayer.Count(); ++layer) {
                var nodeList = mNodeListByLayer[layer];
                double y = spacingY;

                double maxW = 0;
                foreach (var node in nodeList) {
                    if ((layer & 1) == 1) {

                        // 補足的なポート詳細情報。親に重ねて表示。
                        double x = node.left.X + node.left.W - UsbDevice.PADDING_RIGHT;
                        y = node.left.Y + node.left.H/2 - node.H/2;
                        node.X = x;
                        node.Y = y;
                        Canvas.SetLeft(node.uiElement, x);
                        Canvas.SetTop(node.uiElement, y);
                        Canvas.SetZIndex(node.uiElement, ZINDEX_HUB);
                    } else {
                        node.X = xOffs;
                        node.Y = y;

                        Canvas.SetLeft(node.uiElement, xOffs);
                        Canvas.SetTop(node.uiElement, y);
                        Canvas.SetZIndex(node.uiElement, ZINDEX_USBDEVICE);

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
                    xOffs += maxW - UsbDevice.PADDING_RIGHT;
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
