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
using WWMath;

namespace WWUserControls {
    /// <summary>
    /// Interaction logic for AnalogFilterCircuit.xaml
    /// </summary>
    public partial class AnalogFilterCircuit : UserControl {
        public AnalogFilterCircuit() {
            InitializeComponent();
        }

        private List<RationalPolynomial> mRealPolynomialList = new List<RationalPolynomial>();

        public void Clear() {
            canvas1.Children.Clear();
            mRealPolynomialList.Clear();
            mX = 0;
        }

        public void Add(RationalPolynomial realPolynomial) {
            mRealPolynomialList.Add(realPolynomial);
        }

        private double mX = 0;

        private void AddLine(double x1, double y1, double x2, double y2, SolidColorBrush brush) {
            var l = new Line();
            l.Stroke = brush;
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
            canvas1.Children.Add(l);
        }

        private void AddLineStrip(List<Point> pointList, SolidColorBrush brush) {
            var prevPoint = pointList[0];

            for (int i = 1; i < pointList.Count(); ++i) {
                var curPoint = pointList[i];
                AddLine(prevPoint.X, prevPoint.Y, curPoint.X, curPoint.Y, brush);
                prevPoint = curPoint;
            }
        }

        /// <param name="x">中心座標</param>
        /// <param name="y">中心座標</param>
        /// <param name="diameter">直径</param>
        /// <param name="brush"></param>
        private void AddCircle(double x, double y, double diameter, SolidColorBrush brush) {
            var c = new Ellipse();
            c.Stroke = brush;
            c.Width = diameter;
            c.Height = diameter;
            canvas1.Children.Add(c);
            Canvas.SetLeft(c, x - diameter / 2);
            Canvas.SetTop(c, y - diameter / 2);
        }

        private const double CIRCUIT_INPUT_LINE_H = 100;
        private const double INPUT_CIRCLE_WH = 6;
        private const double INPUT_LINE_W = 30;

        private const double RESISTOR_LENGTH = 3 * 6;

        private const double CAPACITOR_THICKNESS = 5;

        private const double OPAMP_INPUT_H_OFFS = 8;
        private const double OPAMP_WIDTH = 30;

        private SolidColorBrush mBrush = new SolidColorBrush(Colors.Black);

        private void DrawInput() {
            mX += 50;
            
            AddCircle(mX, CIRCUIT_INPUT_LINE_H, INPUT_CIRCLE_WH, mBrush);
            mX += INPUT_CIRCLE_WH/2;

            AddLine(mX, CIRCUIT_INPUT_LINE_H, mX + INPUT_LINE_W, CIRCUIT_INPUT_LINE_H, mBrush);
            mX += INPUT_LINE_W;
        }

        /// <summary>
        /// 左座標を指定してオペアンプを描画。
        /// </summary>
        private void AddOpamp(double x, double y) {
            var p = new List<Point>();
            p.Add(new Point(x, y - 15));
            p.Add(new Point(x+OPAMP_WIDTH, y));
            p.Add(new Point(x, y+15));
            p.Add(new Point(x, y - 15));
            AddLineStrip(p, mBrush);

            const double PLUS_LEN = 6;
            AddLine(x + 5, y - OPAMP_INPUT_H_OFFS - PLUS_LEN / 2, x + 5, y - OPAMP_INPUT_H_OFFS + PLUS_LEN / 2, mBrush);
            AddLine(x + 5 - PLUS_LEN / 2, y - OPAMP_INPUT_H_OFFS, x + 5 + PLUS_LEN / 2, y - OPAMP_INPUT_H_OFFS, mBrush);
        }

        /// <summary>
        /// 上端座標を指定してGNDを描画。
        /// </summary>
        private void AddGnd(double x, double y) {
            double GND_WIDTH = 20;
            const double GND_LINE_INTERVAL = 4;

            AddLine(x - GND_WIDTH / 2, y, x + GND_WIDTH / 2, y, mBrush);
            y += GND_LINE_INTERVAL;
            GND_WIDTH -= 5;

            AddLine(x - GND_WIDTH / 2, y, x + GND_WIDTH / 2, y, mBrush);
            y += GND_LINE_INTERVAL;
            GND_WIDTH -= 5;

            AddLine(x - GND_WIDTH / 2, y, x + GND_WIDTH / 2, y, mBrush);
            y += GND_LINE_INTERVAL;
            GND_WIDTH -= 5;
        }

        /// <summary>
        /// 左端座標を指定して横抵抗を描画。幅はRESISTOR_LENGTH
        /// </summary>
        private void AddResistorH(double x, double y) {
            const double RESISTOR_H2 = 5;
            const double RESISTOR_W  = 3;

            var p = new List<Point>();
            p.Add(new Point(x, y));
            x += RESISTOR_W / 2;
            p.Add(new Point(x, y - RESISTOR_H2));
            x += RESISTOR_W;
            p.Add(new Point(x, y + RESISTOR_H2));
            x += RESISTOR_W;
            p.Add(new Point(x, y - RESISTOR_H2));
            x += RESISTOR_W;
            p.Add(new Point(x, y + RESISTOR_H2));
            x += RESISTOR_W;
            p.Add(new Point(x, y - RESISTOR_H2));
            x += RESISTOR_W;
            p.Add(new Point(x, y + RESISTOR_H2));
            x += RESISTOR_W/2;
            p.Add(new Point(x, y));
            AddLineStrip(p, mBrush);
        }

        /// <summary>
        /// 上端座標を指定して縦抵抗を描画。幅はRESISTOR_LENGTH
        /// </summary>
        private void AddResistorV(double x, double y) {
            const double RESISTOR_H2 = 5;
            const double RESISTOR_W = 3;

            var p = new List<Point>();
            p.Add(new Point(x, y));
            y += RESISTOR_W / 2;
            p.Add(new Point(x+RESISTOR_H2, y));
            y += RESISTOR_W;
            p.Add(new Point(x - RESISTOR_H2, y));
            y += RESISTOR_W;
            p.Add(new Point(x + RESISTOR_H2, y));
            y += RESISTOR_W;
            p.Add(new Point(x - RESISTOR_H2, y));
            y += RESISTOR_W;
            p.Add(new Point(x + RESISTOR_H2, y));
            y += RESISTOR_W;
            p.Add(new Point(x - RESISTOR_H2, y));
            y += RESISTOR_W/2;
            p.Add(new Point(x, y));
            AddLineStrip(p, mBrush);
        }

        /// <summary>
        /// 上端座標を指定して縦キャパシターを描画。高さはCAPACITOR_THICKNESS
        /// </summary>
        private void AddCapacitorV(double x, double y) {
            const double CAPACITOR_WIDTH = 20;

            AddLine(x-CAPACITOR_WIDTH/2, y, x + CAPACITOR_WIDTH / 2, y, mBrush);
            y += CAPACITOR_THICKNESS;

            AddLine(x - CAPACITOR_WIDTH / 2, y, x + CAPACITOR_WIDTH / 2, y, mBrush);
        }

        /// <summary>
        /// 左端座標を指定して横キャパシターを描画。高さはCAPACITOR_THICKNESS
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void AddCapacitorH(double x, double y) {
            const double CAPACITOR_WIDTH = 20;

            AddLine(x, y - CAPACITOR_WIDTH / 2, x, y + CAPACITOR_WIDTH / 2, mBrush);
            x += CAPACITOR_THICKNESS;

            AddLine(x, y - CAPACITOR_WIDTH / 2, x, y + CAPACITOR_WIDTH / 2, mBrush);
        }

        private void DrawOutput() {
            AddLine(mX, CIRCUIT_INPUT_LINE_H, mX + INPUT_LINE_W, CIRCUIT_INPUT_LINE_H, mBrush);
            mX += INPUT_LINE_W + INPUT_CIRCLE_WH/2;

            AddCircle(mX, CIRCUIT_INPUT_LINE_H, INPUT_CIRCLE_WH, mBrush);
            mX += INPUT_CIRCLE_WH/2;

            mX += 50;
        }

        private void AddText(double x, double y, string s) {
            TextBlock tb = new TextBlock();
            tb.Text = s;
            canvas1.Children.Add(tb);
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
        }

        private int mR = 0;
        private int mC = 0;

        private void DrawFirstOrderFilter(FirstOrderRationalPolynomial pf) {
            // ボルテージフォロアー。
            {
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }
            mX += 10;

            AddOpamp(mX, CIRCUIT_INPUT_LINE_H);

            {
                // フィードバックの線。
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX - 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX - 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            AddLine(mX + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H, mX + OPAMP_WIDTH + 20, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += OPAMP_WIDTH + 20;

            // 抵抗R0
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H);
            AddText(mX, CIRCUIT_INPUT_LINE_H-23, string.Format("R{0}", mR));
            ++mR;

            {
                // 抵抗の右から出る横線。オペアンプの＋入力につながる。
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40, CIRCUIT_INPUT_LINE_H-OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40 +10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            // 抵抗R0とキャパシタC0をつなげる縦線。
            AddLine(mX + RESISTOR_LENGTH + 10, CIRCUIT_INPUT_LINE_H,
                    mX + RESISTOR_LENGTH + 10, CIRCUIT_INPUT_LINE_H + 30, mBrush);

            // キャパシターC0
            AddCapacitorV(mX + RESISTOR_LENGTH + 10, CIRCUIT_INPUT_LINE_H + 30);
            AddText(      mX + RESISTOR_LENGTH + 10 - 28, CIRCUIT_INPUT_LINE_H + 25, string.Format("C{0}", mC));
            ++mC;

            {
                // キャパシタC0から非反転アンプのフィードバックのGNDに接続する線。
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH + 10, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 10));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 10));
                AddLineStrip(p, mBrush);
            }

            mX += RESISTOR_LENGTH + 10 + 40;

            // 抵抗Rx
            AddResistorV(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS);
            AddText(mX-20, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS, string.Format("R{0}", mR));
            ++mR;

            // 抵抗Rxの下からGNDにつながる縦線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH,
                    mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 20, mBrush);

            // GND
            AddGnd(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 20);

            {
                // 抵抗Rx からオペアンプのマイナス入力への線。
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            mX += 10;

            AddOpamp(mX, CIRCUIT_INPUT_LINE_H);

            // オペアンプのマイナス入力とフィードバック抵抗。
            AddLine(mX - 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20,
                    mX, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20, mBrush);

            // フィードバック抵抗。
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20);
            AddText(mX + 5, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 25, string.Format("R{0}", mR));
            ++mR;

            {
                // フィードバック抵抗からオペアンプの線。
                var p = new List<Point>();
                p.Add(new Point(mX+RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 20));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            // オペアンプの出力線。
            AddLine(mX +OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H,
                    mX + OPAMP_WIDTH+20, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += OPAMP_WIDTH+20;
        }

        public void Update() {
            canvas1.Children.Clear();
            mX = 0;
            mR = 0;
            mC = 0;

            DrawInput();

            foreach (var p in mRealPolynomialList) {
                if (p.Order() == 1) {
                    var pf = p as FirstOrderRationalPolynomial;
                    // 1次多項式。
                    DrawFirstOrderFilter(pf);
                } else {
                    // 2次多項式。
                }
            }

            DrawOutput();
            canvas1.Width = mX;
            canvas1.Height = CIRCUIT_INPUT_LINE_H * 3;
        }
    }
}
