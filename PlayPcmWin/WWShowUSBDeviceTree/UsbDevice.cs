using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WWShowUSBDeviceTree {
    public class UsbDevice {

        public const double PADDING_RIGHT = 30;

        private const double BORDER_THICKNESS = 2;
        private const double PADDING_TEXTBOX = 4;

        private const int ZINDEX_MODULE = 12;
        private const int ZINDEX_PATH = 11;

        private double SPACING_X = 100;
        private double SPACING_Y = 60;


        public enum NodeType {
            HostController,
            Hub,
            HubPort,
            HubPortHub, //< HubPortがハブ。
        };

        public int idx;
        public int parentIdx;
        public UsbDevice left;
        public List<UsbDevice> right = new List<UsbDevice>();

        public UsbDeviceTreeCs.BusSpeed speed;
        public UsbDeviceTreeCs.BusSpeed usbVersion;
        public Brush borderBrush;

        public List<Module> mModules = new List<Module>();
        List<List<Module>> mModuleListByLayer = new List<List<Module>>();
        List<Path> mCables = new List<Path>();
        List<Polygon> mArrows = new List<Polygon>();

        public NodeType nodeType;

        public int layer;
        public string text;
        public UIElement uiElement;
        public double fontSize = 12;
        public double W { get; set; }

        public double H { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public static Brush SpeedToBrush(UsbDeviceTreeCs.BusSpeed speed) {
            switch (speed) {
            case UsbDeviceTreeCs.BusSpeed.HighSpeed:
                return new SolidColorBrush(Colors.White);
            case UsbDeviceTreeCs.BusSpeed.SuperSpeed:
                return new SolidColorBrush(Color.FromRgb(0x40, 0xc0, 0xff));
            case UsbDeviceTreeCs.BusSpeed.SuperSpeedPlus10:
                return new SolidColorBrush(Color.FromRgb(0xff, 0, 0xff));
            case UsbDeviceTreeCs.BusSpeed.SuperSpeedPlus20:
                return new SolidColorBrush(Color.FromRgb(0xff, 0x80, 0xff));
            case UsbDeviceTreeCs.BusSpeed.LowSpeed:
            case UsbDeviceTreeCs.BusSpeed.FullSpeed:
                return new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0));
            default:
                return new SolidColorBrush(Colors.White);
            }
        }

        private void CreateUIElem() {
            var tb = new TextBlock {
                Padding = new Thickness(PADDING_TEXTBOX),
                Text = text,
                FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Color.FromRgb(0x33,0x33,0x37)),
                Foreground = new SolidColorBrush(Colors.White),
            };
            var bd = new Border() {
                BorderThickness = new Thickness(BORDER_THICKNESS),
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
            Canvas.SetLeft(bd, X);
            Canvas.SetTop(bd, Y);

            uiElement = bd;
        }

        public void SpeedUpdated() {
            // bd色を変更する。
            borderBrush = SpeedToBrush(usbVersion);
            var bd = uiElement as Border;
            bd.BorderBrush = borderBrush;
        }

        private void UpdateUIElementWH(double w, double h) {
            var bd = (Border)uiElement;
            var tb = (TextBlock)bd.Child;
            tb.Width = w - BORDER_THICKNESS*2;
            tb.Height = h - BORDER_THICKNESS*2;
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

        public void Add(Module m) {
            mModules.Add(m);
        }

        private Module FindModule(int idx) {
            if (idx < 0) {
                return null;
            }
            foreach (var m in mModules) {
                if (m.idx == idx) {
                    return m;
                }
            }
            return null;
        }

        /// <summary>
        /// モジュールを並べ、このノードのW Hを確定する。
        /// </summary>
        private void ResolveWH() {
            if (mModules.Count == 0) {
                return;
            }

            double thisW = W;
            double thisH = H;

            double xOffs = 20; //< 見た目で調整。
            for (int layer = 0; layer < mModuleListByLayer.Count; ++layer) {
                var moduleList = mModuleListByLayer[layer];

                double y = H;
                double maxW = 0;
                foreach (var m in moduleList) {
                    m.X = xOffs;
                    m.Y = y;
                    Canvas.SetZIndex(m.uiElement, ZINDEX_MODULE);

                    // 次(下)のnode位置。
                    y += m.H + SPACING_Y;

                    if (thisW < m.X + m.W) {
                        thisW = m.X + m.W;
                    }
                    if (thisH < m.Y + m.H) {
                        thisH = m.Y + m.H;
                    }

                    // 次のlayerの位置計算のための最大幅。
                    if (maxW < m.W) {
                        maxW = m.W;
                    }
                }

                // モジュールの総数が多いときはxの間隔を広く開ける。
                int nModules = moduleList.Count;
                if (layer +1 < mModuleListByLayer.Count) {
                    int nNext = mModuleListByLayer[layer + 1].Count;
                    if (nModules < nNext) {
                        nModules = nNext;
                    }
                }

                xOffs += maxW + SPACING_X + 20.0 * nModules;
            }

            W = thisW + PADDING_TEXTBOX*2;
            H = thisH + PADDING_TEXTBOX*2;
            UpdateUIElementWH(W, H);
        }

        public void Resolve() {
            // m.m*Modulesを更新。
            foreach (var m in mModules) {
                m.mLeftModules.Clear();
                m.mRightModules.Clear();
                m.mTopModules.Clear();
                m.mBottomItems.Clear();
            }

            foreach (var m in mModules) {
                foreach (int idx in m.mLeftItems) {
                    m.AddToLeft(FindModule(idx));
                }
                foreach (int idx in m.mRightItems) {
                    m.AddToRight(FindModule(idx));
                }
                foreach (int idx in m.mTopItems) {
                    m.AddToTop(FindModule(idx));
                }
                foreach (int idx in m.mBottomItems) {
                    m.AddToBottom(FindModule(idx));
                }
            }

            // モジュールのlayerを確定する。
            foreach (var m in mModules) {
                m.ResolveLayer();
            }

            // 同じlayerのモジュールを集める。
            mModuleListByLayer.Clear();
            foreach (var v in mModules) {
                while (mModuleListByLayer.Count <= v.layer) {
                    mModuleListByLayer.Add(new List<Module>());
                }
                mModuleListByLayer[v.layer].Add(v);
            }

            ResolveWH();
        }

        private void ResolveModulePositions() {
            foreach (var m in mModules) {
                m.X += X;
                m.Y += Y;
                Canvas.SetLeft(m.uiElement, m.X);
                Canvas.SetTop(m.uiElement, m.Y);
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
            return p;
        }

        private Polygon CreateFilledTriangle(Point p1, Point p2, Point p3, Brush brush) {
            var p = new Polygon() {
                Stroke = brush,
                Fill = brush,
                StrokeThickness = 0,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Points = new PointCollection() { p1, p2, p3 }
            };
            Canvas.SetZIndex(p, ZINDEX_PATH);
            return p;
        }

        private const double ARROW_LENGTH      = 10;
        private const double ARROW_WIDTH_HALF  = 5;

        /// <summary>
        /// ノード間の線を引きます。
        /// </summary>
        private void ConnectCables(Canvas canvas) {
            // すでに引かれている線を消す。
            foreach (var c in mCables) {
                canvas.Children.Remove(c);
            }
            mCables.Clear();

            // 線を引いていきます。
            // 左右の線。
            foreach (var m in mModules) {
                for (int i=0; i<m.mRightModules.Count; ++i) {
                    int count = m.mRightModules.Count;
                    var rightM = m.mRightModules[i];

                    var brush = new SolidColorBrush(Colors.White);
                    if (m.moduleType == Module.ModuleType.ClockRelated) {
                        brush = new SolidColorBrush(Colors.Yellow);
                    }

                    var from = new Point(m.X + m.W, m.Y + m.H/2);
                    var to = new Point(rightM.X - ARROW_LENGTH, rightM.Y + rightM.H / 2);

                    var control1 = new Point((from.X + to.X) / 2, from.Y);
                    var control2 = new Point((from.X + to.X) / 2, to.Y);

                    var p = CreatePath(from, to, control1, control2, brush);
                    canvas.Children.Add(p);
                    mCables.Add(p);

                    var arrow = CreateFilledTriangle(new Point(to.X+ARROW_LENGTH, to.Y), new Point(to.X, to.Y-ARROW_WIDTH_HALF), new Point(to.X, to.Y + ARROW_WIDTH_HALF), brush);
                    canvas.Children.Add(arrow);
                    mArrows.Add(arrow);
                }
            }

            

            // 上下の線。
            foreach (var m in mModules) {
                for (int i=0; i<m.mBottomModules.Count; ++i) {
                    int count = m.mBottomModules.Count;
                    var bottomM = m.mBottomModules[i];

                    var brush = new SolidColorBrush(Colors.Yellow);

                    var from = new Point(m.X + m.W/2, m.Y + m.H);
                    var to = new Point(bottomM.X + bottomM.W/2, bottomM.Y - ARROW_LENGTH);

                    double len = Math.Abs(from.Y - to.Y)/2;
                    if (len < SPACING_Y*0.75) {
                        len = SPACING_Y*0.75;
                    }

                    var control1 = new Point(from.X, from.Y+len);
                    var control2 = new Point(to.X, bottomM.Y-len);

                    var p = CreatePath(from, to, control1, control2, brush);
                    canvas.Children.Add(p);
                    mCables.Add(p);

                    var arrow = CreateFilledTriangle(new Point(to.X, to.Y+ARROW_LENGTH), new Point(to.X-ARROW_WIDTH_HALF, to.Y), new Point(to.X + ARROW_WIDTH_HALF, to.Y), brush);
                    canvas.Children.Add(arrow);
                    mArrows.Add(arrow);
                }
            }
        }

        /// <summary>
        /// 表示場所が確定したら呼ぶ。
        /// </summary>
        public void PositionUpdated(Canvas canvas) {
            ResolveModulePositions();
            ConnectCables(canvas);
        }
    }
}
