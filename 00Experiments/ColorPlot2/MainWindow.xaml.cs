// defineするとa*L*平面にプロット。しないとa*b*平面にプロット。
//#define DISP_ASTAR_LSTAR

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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ColorPlot {

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {

        const int COLOR_NUM_PER_SEGMENT = 22;
        const int SEGMENT_NUM = 4;

        byte[] colorTable = {
            // 1～22
            172, 118, 114, 
            163, 115, 86,  
            171, 117, 109, 
            162, 129, 73,  
            164, 114, 92,  

            162, 117, 84,  
            156, 138, 76,  
            161, 125, 72,  
            165, 114, 96,  
            164, 123, 78,

            170, 116, 105,
            159, 132, 74,
            154, 131, 71,
            163, 119, 82,
            149, 137, 76,

            167, 113, 97,
            154, 138, 76,
            162, 123, 73,
            170, 115, 101,
            163, 121, 81,

            164, 114, 88,
            143, 140, 74,

            // 23～44 黄～緑
            143, 140, 74,  
            139, 144, 88,  
            84,  153, 136, 
            136, 145, 92,  
            90, 148, 122,  
            117, 150, 108, 
            96, 152, 119,  
            106, 152, 113, 
            126, 149, 104, 
            133, 147, 96,  
            105, 153, 115, 
            142, 143, 87,  
            89, 151, 132,  
            128, 146, 98,  
            85, 150, 133,  
            105, 154, 119, 
            120, 150, 106, 
            88, 150, 129,  
            91, 150, 126,  
            113, 152, 109, 
            126,149, 102, 
            84, 151, 137,  

            // 45～66
            84, 151, 137,
            107, 141, 166,
            69, 153, 149,
            95, 149, 165,
            120, 138, 168,
            114, 141, 169,
            82, 150, 140,
            73, 152, 146,
            89, 150, 163,
            121, 133, 162,
            75, 153, 151,
            118, 141, 170,
            108, 146, 169,
            102, 149, 164,
            103, 146, 165,
            85, 151, 160,
            84, 150, 162,
            75, 152, 152,
            114, 144, 170,
            80, 151, 143,
            80, 153, 157,
            128, 136, 165,

            // 67～88
            128, 136, 165,
            167, 126, 144,
            170, 119, 132,
            160, 130, 152,
            136, 136, 166,
            152, 132, 156,
            168, 121, 136,
            172, 121, 130,
            157, 124, 145,
            169, 115, 117,
            163, 126, 146,
            143, 136, 164,
            152, 133, 159,
            147, 134, 160,
            138, 136, 164,
            146, 135, 163,
            155, 129, 152,
            132, 137, 166,
            167, 123, 140,
            171, 117, 122,
            172, 117, 126,
            173, 119, 118,
        };

        struct RGB {
            public float r; // 0 to 255
            public float g; // 0 to 255
            public float b; // 0 to 255

            public void Set(float aR, float aG, float aB) {
                r = aR;
                g = aG;
                b = aB;
            }
        };

        struct XYZ {
            public float x;
            public float y;
            public float z;
        };

        struct LabStar {
            public float L;
            public float a;
            public float b;

            public float DistanceSquared(LabStar rhs) {
                return (L - rhs.L) * (L - rhs.L) +
                    (a - rhs.a) * (a - rhs.a) +
                    (b - rhs.b) * (b - rhs.b);
            }

            public float Distance(LabStar rhs) {
                return (float)Math.Sqrt(DistanceSquared(rhs));
            }
        };

        class ColorPatch {
            public int id;
            public RGB rgb;
            public LabStar lab;

            public ColorPatch neighbor;
            public ColorPatch neighbor2;
            public ColorPatch neighbor3;
        };

        // 参照光源はD65光源 CIE 1931 standard observer 2°
        static XYZ RGBtoXYZ(RGB rgb) {
            var r = (rgb.r / 255.0f);
            var g = (rgb.g / 255.0f);
            var b = (rgb.b / 255.0f);

            if (r > 0.04045f) { r = (float)Math.Pow((r + 0.055f) / 1.055f, 2.4f); } else { r /= 12.92f; }
            if (g > 0.04045f) { g = (float)Math.Pow((g + 0.055f) / 1.055f, 2.4f); } else { g /= 12.92f; }
            if (b > 0.04045f) { b = (float)Math.Pow((b + 0.055f) / 1.055f, 2.4f); } else { b /= 12.92f; }

            r *= 100.0f;
            g *= 100.0f;
            b *= 100.0f;

            var xyz = new XYZ();
            xyz.x = r * 0.4124f + g * 0.3576f + b * 0.1805f;
            xyz.y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
            xyz.z = r * 0.0193f + g * 0.1192f + b * 0.9505f;
            return xyz;
        }

        // 参照光源はD65光源 CIE 1931 standard observer 2°
        static LabStar XYZtoLabStar(XYZ xyz) {
            float refX = 95.047f;
            float refY = 100.000f;
            float refZ = 108.883f;

            var x = xyz.x / refX;
            var y = xyz.y / refY;
            var z = xyz.z / refZ;

            if (x > 0.008856f) { x = (float)Math.Pow(x, 1.0f / 3.0f); } else { x = (7.787f * x) + (16.0f / 116.0f); }
            if (y > 0.008856f) { y = (float)Math.Pow(y, 1.0f / 3.0f); } else { y = (7.787f * y) + (16.0f / 116.0f); }
            if (z > 0.008856f) { z = (float)Math.Pow(z, 1.0f / 3.0f); } else { z = (7.787f * z) + (16.0f / 116.0f); }

            var lab = new LabStar();
            lab.L = (116.0f * y) - 16.0f;
            lab.a = 500.0f * (x - y);
            lab.b = 200.0f * (y - z);
            return lab;
        }


        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs eventArgs) {

#if DISP_ASTAR_LSTAR
            // a*L*平面。L軸は0～100なので
            float originOffsetX = 400;
            float originOffsetY = 800;

            labelAxisA.Content = "a*→";
            labelAxisA.Margin = new Thickness(700, 200, 0, 0);
            labelAxisB.Content = "↑L*";
            labelAxisB.Margin = new Thickness(400, 100, 0, 0);
            labelOrigin.Margin = new Thickness(400, 766, 0, 0);
#else
            // a*b*平面
            float originOffsetX = 400;
            float originOffsetY = 400;
            labelAxisA.Content = "a*";
            labelAxisA.Margin = new Thickness(763, 400, 0, 0);
            labelAxisB.Content = "b*";
            labelAxisB.Margin = new Thickness(400, 6, 0, 0);
            labelOrigin.Margin = new Thickness(400, 400, 0, 0);
#endif

            rectangleV.Height = canvas1.Height;
            rectangleV.Margin = new Thickness(originOffsetX, 0, 0, 0);

            rectangleH.Width = canvas1.Width;
            rectangleH.Margin = new Thickness(0, originOffsetY, 0, 0);

            var rgbHash = new HashSet<int>();

            var colors = new List<ColorPatch>[SEGMENT_NUM];
            for (int i = 0; i < SEGMENT_NUM; ++i) {
                colors[i] = new List<ColorPatch>();
            }
            for (int i = 0; i < colorTable.Length / 3; ++i) {
                byte r = colorTable[i * 3];
                byte g = colorTable[i * 3 + 1];
                byte b = colorTable[i * 3 + 2];
                var c = Color.FromRgb(r, g, b);

                RGB rgb = new RGB();
                rgb.Set(r, g, b);
                LabStar lab = XYZtoLabStar(RGBtoXYZ(rgb));

                var cp = new ColorPatch();
                cp.id = i;
                cp.rgb = rgb;
                cp.lab = lab;

                colors[i / COLOR_NUM_PER_SEGMENT].Add(cp);

                Ellipse e = new Ellipse();
                e.Fill = new SolidColorBrush(c);
                e.Height = 16;
                e.Width = 16;

#if DISP_ASTAR_LSTAR
                // a*L*平面
                var pos = new Thickness((lab.a) * 10 + originOffsetX, (-lab.L) * 10 + originOffsetY, 0, 0);
#else
                // a*b*平面
                var pos = new Thickness((lab.a) * 10 + originOffsetX, (-lab.b) * 10 + originOffsetY, 0, 0);
#endif

                e.Margin = pos;
                canvas1.Children.Add(e);
                Canvas.SetZIndex(e, -1);

                bool collision = false;
                int hashValue = r * 65536 + g * 256 + b;
                if (rgbHash.Contains(hashValue)) {
                    collision = true;
                }
                rgbHash.Add(hashValue);

                Label l = new Label();
                l.Content = string.Format("{0}", i + 1);
                l.FontSize = 8;
                l.Foreground = new SolidColorBrush(Colors.White);

                l.Margin = pos;
                if (collision) {
                    l.Margin = new Thickness(pos.Left, pos.Top + l.FontSize, pos.Right, pos.Bottom);
                }

                canvas1.Children.Add(l);
            }

            for (int segment = 0; segment < SEGMENT_NUM; ++segment) {
                for (int i = 0; i < COLOR_NUM_PER_SEGMENT; ++i) {
                    var cpFrom = colors[segment].ElementAt(i);
                    var cpSort = new Dictionary<float, ColorPatch>();
                    for (int j = 0; j < COLOR_NUM_PER_SEGMENT; ++j) {
                        if (i == j) {
                            continue;
                        }
                        var cpTo = colors[segment].ElementAt(j);
                        cpSort.Add(cpFrom.lab.DistanceSquared(cpTo.lab), cpTo);
                    }

                    var sorted = (from entry in cpSort orderby entry.Key ascending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
                    var e = sorted.GetEnumerator();
                    e.MoveNext();
                    cpFrom.neighbor = e.Current.Value;
                    e.MoveNext();
                    cpFrom.neighbor2 = e.Current.Value;
                    e.MoveNext();
                    cpFrom.neighbor3 = e.Current.Value;
                }
            }

            Console.WriteLine("graph g {");
            Console.WriteLine("    graph [bgcolor=\"#484848\"]");
            Console.WriteLine("    node [style=filled, fontsize=32, fontcolor=white]");
            Console.WriteLine("    edge [fontsize=32, fontcolor=white]");
            Console.WriteLine("    rankdir=LR;");

            for (int segment = 0; segment < SEGMENT_NUM; ++segment) {
                Console.WriteLine("    subgraph cluster{0} {{", segment);
                Console.WriteLine("        style=invis;");

                for (int i = 0; i < COLOR_NUM_PER_SEGMENT; ++i) {
                    var cp = colors[segment].ElementAt(i);

                    string shape = "color=\"#484848\", shape=ellipse";
                    if (i == 0 || i == COLOR_NUM_PER_SEGMENT - 1) {
                        shape = ", color=white, shape=doublecircle";
                    }

                    Console.WriteLine("        {4} [fillcolor=\"#{0:x2}{1:x2}{2:x2}\"{3}];",
                        (int)(cp.rgb.r),
                        (int)(cp.rgb.g),
                        (int)(cp.rgb.b),
                        shape,
                        cp.id + 1); //< ★★★★★ 要注意 1足して表示 ★★★★★
                }

                for (int i = 0; i < COLOR_NUM_PER_SEGMENT; ++i) {
                    var cp = colors[segment].ElementAt(i);
                    WriteLink(segment * COLOR_NUM_PER_SEGMENT, colors[segment], cp.id, cp.neighbor.id,  "style=bold,  ");
                    WriteLink(segment * COLOR_NUM_PER_SEGMENT, colors[segment], cp.id, cp.neighbor2.id, "             ");
                    WriteLink(segment * COLOR_NUM_PER_SEGMENT, colors[segment], cp.id, cp.neighbor3.id, "style=dotted,");
                }
                Console.WriteLine("    }");
            }


            Console.WriteLine("}");
        }

        void WriteLink(int offset, List<ColorPatch> colorsInSegment, int id0, int id1, string style) {
            System.Diagnostics.Debug.Assert(id1 - offset < colorsInSegment.Count());

            var c0 = colorsInSegment.ElementAt(id0 - offset);
            var c1 = colorsInSegment.ElementAt(id1 - offset);

            if (!AlreadyHas(new Tuple<int, int>(id0, id1))) {
                Console.WriteLine("        {0} -- {1} [{2} color=white, label=\"{3:0.0}\"];", id0+1, id1+1, style, c0.lab.Distance(c1.lab));
            }
        }

        SortedSet<Tuple<int, int>> mLinkStorage = new SortedSet<Tuple<int, int>>();

        /// <returns> itemが既に出現していたらtrue。itemが初めて出現したらfalse。</returns>
        bool AlreadyHas(Tuple<int, int> item) {
            if (item.Item2 < item.Item1) {
                // swap
                item = new Tuple<int, int>(item.Item2, item.Item1);
            }

            if (0 == mLinkStorage.Where(p => p.Item1 == item.Item1 && p.Item2 == item.Item2).Count()) {
                mLinkStorage.Add(item);
                return false;
            }
            return true;
        }

    }
}
