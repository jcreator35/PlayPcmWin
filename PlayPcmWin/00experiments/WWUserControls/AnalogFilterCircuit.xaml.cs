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
            CutoffFrequencyHz = 1.0;
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

        /// <summary>
        /// カットオフ周波数 (Hz)
        /// </summary>
        public double CutoffFrequencyHz { get; set; }

        /// <summary>
        /// 描画する位置。
        /// </summary>
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

        private void AddFilledCircle(double x, double y, double diameter, SolidColorBrush brush) {
            var c = new Ellipse();
            c.Stroke = brush;
            c.Fill = brush;
            c.Width = diameter;
            c.Height = diameter;
            canvas1.Children.Add(c);
            Canvas.SetLeft(c, x - diameter / 2);
            Canvas.SetTop(c, y - diameter / 2);
        }

        private const double CIRCUIT_INPUT_LINE_H = 80;
        private const double INPUT_CIRCLE_WH = 6;
        private const double INPUT_LINE_W = 30;
        private const double OUTPUT_LINE_W = 10;

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
        /// ボルテージフォロアー。
        /// </summary>
        private void AddVoltageFollower() {
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

            //AddFilledCircle(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H, 5, mBrush);

            mX += OPAMP_WIDTH + 20;
        }
        
        /// <summary>
        /// 反転アンプ。
        /// </summary>
        /// <param name="gain">ゲイン。反転なので負の値を入れること。</param>
        private void AddVoltageInverter(double gain) {
            if (0 < gain) {
                throw new ArgumentOutOfRangeException("gain");
            }

            double rIn = mResistorMultiplier;
            double rF = -gain * rIn;

            // 入力抵抗Rin
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H);
            AddText(mX, CIRCUIT_INPUT_LINE_H - 23, string.Format("R{0}", mR.Count()));
            mR.Add(rIn);

            mX += RESISTOR_LENGTH;

            {   // 入力抵抗Rinからオペアンプの-入力に接続する線。
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 30, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            {
                // オペアンプの＋入力からGNDに接続する線。
                var p = new List<Point>();
                p.Add(new Point(mX + 30, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 30));
                AddLineStrip(p, mBrush);
            }

            // GND
            AddGnd(mX + 20, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS + 30);

            mX += 30;

            AddOpamp(mX, CIRCUIT_INPUT_LINE_H, PlusPosition.Bottom);

            {
                // オペアンプのマイナス入力からRfの入力への線。
                var p = new List<Point>();
                p.Add(new Point(mX - 20, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX - 20, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - 30));
                AddLineStrip(p, mBrush);
            }

            // 抵抗Rf
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H - 30);
            AddText(mX, CIRCUIT_INPUT_LINE_H - 30 - 23, string.Format("R{0}", mR.Count()));
            mR.Add(rF);

            {
                // 抵抗Rfの出力からオペアンプの出力への線。
                var p = new List<Point>();
                p.Add(new Point(mX +RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            AddLine(mX + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H,
                mX + OPAMP_WIDTH + 20, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += OPAMP_WIDTH + 20;
        }

        private enum PlusPosition {
            Top,
            Bottom
        }

        /// <summary>
        /// 左座標を指定してオペアンプを描画。
        /// </summary>
        private void AddOpamp(double x, double y, PlusPosition pp = PlusPosition.Top) {
            var p = new List<Point>();
            p.Add(new Point(x, y - 15));
            p.Add(new Point(x+OPAMP_WIDTH, y));
            p.Add(new Point(x, y+15));
            p.Add(new Point(x, y - 15));
            AddLineStrip(p, mBrush);

            const double PLUS_LEN = 6;

            switch (pp) {
            case PlusPosition.Top:
                AddLine(x + 5, y - OPAMP_INPUT_H_OFFS - PLUS_LEN / 2, x + 5, y - OPAMP_INPUT_H_OFFS + PLUS_LEN / 2, mBrush);
                AddLine(x + 5 - PLUS_LEN / 2, y - OPAMP_INPUT_H_OFFS, x + 5 + PLUS_LEN / 2, y - OPAMP_INPUT_H_OFFS, mBrush);
                break;
            case PlusPosition.Bottom:
                AddLine(x + 5, y + OPAMP_INPUT_H_OFFS - PLUS_LEN / 2, x + 5, y + OPAMP_INPUT_H_OFFS + PLUS_LEN / 2, mBrush);
                AddLine(x + 5 - PLUS_LEN / 2, y + OPAMP_INPUT_H_OFFS, x + 5 + PLUS_LEN / 2, y + OPAMP_INPUT_H_OFFS, mBrush);
                break;
            }
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
            AddLine(mX, CIRCUIT_INPUT_LINE_H, mX + OUTPUT_LINE_W, CIRCUIT_INPUT_LINE_H, mBrush);
            mX += OUTPUT_LINE_W + INPUT_CIRCLE_WH/2;

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

        private List<double> mR = new List<double>();
        private List<double> mC = new List<double>();

        private double mResistorMultiplier = 10 * 1000;

        /// <summary>
        /// Analog Electronic Filters pp.58
        /// </summary>
        private void DrawFirstOrderFilter(FirstOrderRationalPolynomial pf) {
            /* a == 1/R0C0
             * C0 = aR0
             */
            double r0 = 1;
            double c0 = pf.D(0).real * r0;

            // 周波数スケーリング。キャパシタの値をωcで割る。
            double ωc = CutoffFrequencyHz * 2.0 * Math.PI;
            c0 /= ωc;

            // 最後に抵抗値を全て10 * 1000倍、キャパシターの容量を10*1000分の1にする。
            r0 *= mResistorMultiplier;
            c0 /= mResistorMultiplier;

            // 抵抗R0
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H);
            AddText(mX, CIRCUIT_INPUT_LINE_H-23, string.Format("R{0}", mR.Count()));
            mR.Add(r0);

            // 抵抗の右から出る横線。右にアンプがある。
            AddLine(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H,
                    mX + RESISTOR_LENGTH + 30, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += RESISTOR_LENGTH + 10;

            // 抵抗R0とキャパシタC0をつなげる縦線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H,
                    mX, CIRCUIT_INPUT_LINE_H + 30, mBrush);

            // キャパシターC0
            AddCapacitorV(mX, CIRCUIT_INPUT_LINE_H + 30);
            AddText(      mX - 28, CIRCUIT_INPUT_LINE_H + 25, string.Format("C{0}", mC.Count()));
            mC.Add(c0);

            // キャパシタC0からGNDに接続する線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS,
                    mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS+ RESISTOR_LENGTH + 10, mBrush);

            // GND
            AddGnd(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 10);

            mX += 20;

            // ボルテージフォロアー。
            AddVoltageFollower();

            // オペアンプの出力線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H,
                    mX + 30, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += 30;
        }

        /// <summary>
        /// Sallen-Key Minimum Sensitivity 2nd order Lowpass filter
        /// Analog Electronic Filters pp.470
        /// </summary>
        private void DrawSecondOrderFilter(SecondOrderRationalPolynomial ps) {
            /*       ___
             * ω0 = √d0
             * Q = ω0/d1
             * k = 4/3
             * c2 = 1F
             */
            double ω0 = Math.Sqrt(ps.D(0).real);
            double Q = ω0 / ps.D(1).real;
            double k = 4.0 / 3.0;
            double c2 = 1.0;

            double c1 = Math.Sqrt(3.0) * Q * c2;
            double r1 = 1.0 / (ω0 * Q * c2);
            double r2 = 1.0 / (Math.Sqrt(3.0) * ω0 * c2);

            // 周波数スケーリング。キャパシタの値をωcで割る。
            double ωc = CutoffFrequencyHz * 2.0 * Math.PI;
            c1 /= ωc;
            c2 /= ωc;

            // 最後に抵抗値を全て10 * 1000倍、キャパシターの容量を10*1000分の一する。
            r1 *= mResistorMultiplier;
            r2 *= mResistorMultiplier;
            c1 /= mResistorMultiplier;
            c2 /= mResistorMultiplier;

            // 抵抗R1
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H);
            AddText(mX, CIRCUIT_INPUT_LINE_H - 23, string.Format("R{0}", mR.Count()));
            mR.Add(r1);

            // R1の右から出る横線。R2の入力につながる。
            AddLine(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H,
                    mX + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += RESISTOR_LENGTH + 20;

            // 抵抗R2
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H);
            AddText(mX, CIRCUIT_INPUT_LINE_H - 23, string.Format("R{0}", mR.Count()));
            mR.Add(r2);

            {
                // 抵抗R2の右から出る横線。オペアンプの＋入力につながる。
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10 + 40 + 10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            {
                // 抵抗R1-R2とキャパシタC1をつなげる縦線。
                var p = new List<Point>();
                p.Add(new Point(mX -10,                    CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX -10,                    CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + RESISTOR_LENGTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                AddLineStrip(p, mBrush);
            }

            mX += RESISTOR_LENGTH + 10;

            // キャパシターC1
            AddCapacitorH(mX, CIRCUIT_INPUT_LINE_H - 30);
            AddText(mX-5, CIRCUIT_INPUT_LINE_H -58, string.Format("C{0}", mC.Count()));
            mC.Add(c1);

            {
                // キャパシターC1からオペアンプのー入力。
                var p = new List<Point>();
                p.Add(new Point(mX + CAPACITOR_THICKNESS, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 40 + 10 + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 40 + 10 + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H + 30));
                p.Add(new Point(mX + 40, CIRCUIT_INPUT_LINE_H + 30));
                p.Add(new Point(mX + 40, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 40 + 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }


            // R2からC2に接続する縦線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H,
                    mX, CIRCUIT_INPUT_LINE_H + 30, mBrush);

            // キャパシターC2
            AddCapacitorV(mX, CIRCUIT_INPUT_LINE_H + 30);
            AddText(mX - 32, CIRCUIT_INPUT_LINE_H + 25, string.Format("C{0}", mC.Count()));
            mC.Add(c2);

            // C2からGNDに接続する縦線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS,
                    mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 10, mBrush);

            AddGnd(mX, CIRCUIT_INPUT_LINE_H + 30 + CAPACITOR_THICKNESS + RESISTOR_LENGTH + 10);

            mX += 40 + 10;

            AddOpamp(mX, CIRCUIT_INPUT_LINE_H);

            // オペアンプの出力線。
            AddLine(mX + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H,
                    mX + OPAMP_WIDTH + 50, CIRCUIT_INPUT_LINE_H, mBrush);

            AddFilledCircle(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H, 5, mBrush);

            mX += OPAMP_WIDTH + 50;

        }

        private static string ValueString(double v) {
            if (1000.0 * 1000 * 1000 <= v) {
                return string.Format("{0:F}G", v / (1000.0 * 1000 * 1000));
            }
            if (1000.0 * 1000 <= v) {
                return string.Format("{0:F}M", v / (1000.0 * 1000));
            }

            if (1000.0 <= v) {
                return string.Format("{0:F}k", v / (1000.0));
            }

            if (v <= 0.001 * 0.001 * 0.001) {
                return string.Format("{0:F}p", v * (1000.0 * 1000.0 * 1000.0 * 1000.0));
            }

            if (v <= 0.001 * 0.001) {
                return string.Format("{0:F}n", v * (1000.0 * 1000.0 * 1000.0));
            }

            if (v <= 0.001) {
                return string.Format("{0:F}μ", v * (1000.0 * 1000.0));
            }

            if (v <= 1.0) {
                return string.Format("{0:F}m", v * (1000.0));
            }

            return string.Format("{0:F}", v);
        }

        private static string ResistorValueString(double v) {
            return string.Format("{0}Ω", ValueString(v));
        }

        private static string CapacitorValueString(double v) {
            return string.Format("{0}F", ValueString(v));
        }

        public void Update() {
            canvas1.Children.Clear();
            mX = 0;
            mR.Clear();
            mC.Clear();
            textBoxParameters.Clear();

            DrawInput();

            int order = 0;

            foreach (var p in mRealPolynomialList) {
                order += p.Order();
                if (p.Order() == 1) {
                    var pf = p as FirstOrderRationalPolynomial;
                    // 1次多項式。
                    DrawFirstOrderFilter(pf);
                } else {
                    var ps = p as SecondOrderRationalPolynomial;
                    // 2次多項式。
                    DrawSecondOrderFilter(ps);
                }
            }

            DrawOutput();
            canvas1.Width = mX;
            canvas1.Height = CIRCUIT_INPUT_LINE_H * 3;

            textBoxParameters.Clear();
            textBoxParameters.Text += string.Format("Order = {0}, Stage = {1}\n", order, mRealPolynomialList.Count());
            for (int i = 0; i < mR.Count(); ++i) {
                textBoxParameters.Text += string.Format("R{0}={1}\n", i, ResistorValueString(mR[i]));
            }
            for (int i = 0; i < mC.Count(); ++i) {
                textBoxParameters.Text += string.Format("C{0}={1}\n", i, CapacitorValueString(mC[i]));
            }
        }
    }
}
