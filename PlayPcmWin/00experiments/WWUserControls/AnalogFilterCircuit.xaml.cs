using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        public enum ResistorValueType {
            RV_1kΩ,
            RV_1_5kΩ,
            RV_2_2kΩ,
            RV_3_3kΩ,
            RV_4_7kΩ,
            RV_6_8kΩ,
            RV_10kΩ,
            RV_15kΩ,
            RV_22kΩ,
            RV_33kΩ,
            RV_47kΩ,
            RV_68kΩ,
            RV_100kΩ,
            NUM
        };

        private double[] mResistorValues = new double[] {
            1000,
            1500,
            2200,
            3300,
            4700,
            6800,
            10000,
            15000,
            22000,
            33000,
            47000,
            68000,
            100000
        };

        private string[] mResistorStrings = new string[] {
            "1.0kΩ",
            "1.5kΩ",
            "2.2kΩ",
            "3.3kΩ",
            "4.7kΩ",
            "6.8kΩ",
            "10kΩ",
            "15kΩ",
            "22kΩ",
            "33kΩ",
            "47kΩ",
            "68kΩ",
            "100kΩ",
        };

        public enum CapacitorValueType {
            CV_100pF,
            CV_150pF,
            CV_220pF,
            CV_330pF,
            CV_470pF,
            CV_680pF,

            CV_1nF,
            CV_1_5nF,
            CV_2_2nF,
            CV_3_3nF,
            CV_4_7nf,
            CV_6_8nF,

            CV_10nF,
            CV_15nF,
            CV_22nF,
            CV_33nF,
            CV_47nF,
            CV_68nF,

            CV_0_1uF,
            CV_0_15uF,
            CV_0_22uF,
            CV_0_33uF,
            CV_0_47uF,
            CV_0_68uF,

            CV_1uF,
            NUM
        };

        private double[] mCapacitorValues = new double[] {
            0.0000000001, // 100p
            0.0000000015, // 150p
            0.0000000022, // 220p
            0.0000000033, // 330p
            0.0000000047, // 470p
            0.0000000068, // 680p

            0.000000001, // 1n
            0.000000015, // 1.5n
            0.000000022, // 2.2n
            0.000000033, // 3.3n
            0.000000047, // 4.7n
            0.000000068, // 6.8n

            0.00000001, // 10n
            0.00000015, // 15n
            0.00000022, // 22n
            0.00000033, // 33n
            0.00000047, // 47n
            0.00000068, // 68n

            0.0000001, // 0.1u
            0.0000015, // 0.15u
            0.0000022, // 0.22u
            0.0000033, // 0.33u
            0.0000047, // 0.47u
            0.0000068, // 0.68u

            0.000001 // 1u
        };

        private string[] mCapacitorStrings = new string[] {
            "100pF",
            "150pF",
            "220pF",
            "330pF",
            "470pF",
            "680pF",

            "1.0nF",
            "1.5nF",
            "2.2nF",
            "3.3nF",
            "4.7nF",
            "6.8nF",

            "10nF",
            "15nF",
            "22nF",
            "33nF",
            "47nF",
            "68nF",

            "0.1μF",
            "0.15μF",
            "0.22μF",
            "0.33μF",
            "0.47μF",
            "0.68μF",

            "1μF",
        };

        public double ResistorValueTypeToValue(ResistorValueType t) {
            return mResistorValues[(int)t];
        }

        public double CapacitorValueTypeToValue(CapacitorValueType t) {
            return mCapacitorValues[(int)t];
        }

        public void Clear() {
            canvas1.Children.Clear();
            mRealPolynomialList.Clear();
            stackPanelResistor.Children.Clear();
            mX = 0;
        }

        public void Add(RationalPolynomial realPolynomial) {
            mRealPolynomialList.Add(realPolynomial);
        }

        public int FilterStages { get { return mRealPolynomialList.Count(); } }

        private ComboBox CreateResistorCombobox(int stage) {
            var cb = new ComboBox();
            var values = Enum.GetValues(typeof(ResistorValueType));
            for (int j = 0; j < (int)ResistorValueType.NUM; ++j) {
                var item = (ResistorValueType)j;
                var cbi = new ComboBoxItem();
                cbi.Content = string.Format(Properties.Resources.ResistorValueDescription, stage + 1, mResistorStrings[j]);
                cb.Items.Add(cbi);
            }
            cb.SelectedIndex = (int)ResistorValueType.RV_10kΩ;
            cb.SelectionChanged += new SelectionChangedEventHandler(ComboBoxResistorValueSelectionChanged);
            return cb;
        }

        private ComboBox CreateCapacitorCombobox(int stage) {
            var cb = new ComboBox();
            var values = Enum.GetValues(typeof(CapacitorValueType));
            for (int j = 0; j < (int)CapacitorValueType.NUM; ++j) {
                var item = (CapacitorValueType)j;
                var cbi = new ComboBoxItem();
                cbi.Content = string.Format(Properties.Resources.CapacitorValueDescription, stage + 1, mCapacitorStrings[j]);
                cb.Items.Add(cbi);
            }
            cb.SelectedIndex = (int)CapacitorValueType.CV_1nF;
            cb.SelectionChanged += new SelectionChangedEventHandler(ComboBoxResistorValueSelectionChanged);
            return cb;
        }

        public void AddFinished() {
            stackPanelResistor.Children.Clear();

            for (int i=0; i<FilterStages; ++i) {
                var p = mRealPolynomialList[i];

                if (p.Order() == 2 && p.N(2).EqualValue(WWComplex.Unity())) {
                    var grid = new Grid();
                    {
                        var cd = new ColumnDefinition();
                        cd.Width = new GridLength(1, GridUnitType.Star);
                        grid.ColumnDefinitions.Add(cd);
                    }
                    {
                        var cd = new ColumnDefinition();
                        cd.Width = new GridLength(6, GridUnitType.Pixel);
                        grid.ColumnDefinitions.Add(cd);
                    }
                    {
                        var cd = new ColumnDefinition();
                        cd.Width = new GridLength(1, GridUnitType.Star);
                        grid.ColumnDefinitions.Add(cd);
                    }

                    var cbR = CreateResistorCombobox(i);
                    Grid.SetColumn(cbR, 0);
                    grid.Children.Add(cbR);

                    var cbC = CreateCapacitorCombobox(i);
                    Grid.SetColumn(cbC, 2);
                    grid.Children.Add(cbC);

                    grid.Margin = new Thickness(3);
                    stackPanelResistor.Children.Add(grid);
                } else {
                    var cb = CreateResistorCombobox(i);
                    cb.Margin = new Thickness(3);
                    stackPanelResistor.Children.Add(cb);
                }
            }
        }

        void ComboBoxResistorValueSelectionChanged(object sender, SelectionChangedEventArgs e) {
            Update();
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
        private void AddVoltageInverter(double gain, double resistorValue) {
            if (0 < gain) {
                throw new ArgumentOutOfRangeException("gain");
            }

            double rIn = resistorValue;
            double rF = -gain * rIn;

            // 入力抵抗Rin
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H, ResistorValueString(rIn));

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
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H - 30, ResistorValueString(rF));

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
        private void AddResistorH(double x, double y, string text = "") {
            if (text.Length != 0) {
                AddText(x-10, y - 23, text);
            }

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
        /// 左端座標を指定して横キャパシターを描画。高さはCAPACITOR_THICKNESS
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void AddCapacitorH(double x, double y, string text = "") {
            if (text.Length != 0) {
                AddText(x - 10, y - 23, text);
            }

            const double CAPACITOR_WIDTH = 16;

            AddLine(x, y - CAPACITOR_WIDTH / 2, x, y + CAPACITOR_WIDTH / 2, mBrush);
            x += CAPACITOR_THICKNESS;

            AddLine(x, y - CAPACITOR_WIDTH / 2, x, y + CAPACITOR_WIDTH / 2, mBrush);
        }

        /// <summary>
        /// 上端座標を指定して縦抵抗を描画。幅はRESISTOR_LENGTH
        /// </summary>
        private void AddResistorV(double x, double y, string text = "") {
            if (text.Length != 0) {
                AddText(x - 40, y-16, text);
            }
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
        private void AddCapacitorV(double x, double y, string text = "") {
            if (text.Length != 0) {
                AddText(x - 40, y - 16, text);
            }
            const double CAPACITOR_WIDTH = 20;

            AddLine(x - CAPACITOR_WIDTH / 2, y, x + CAPACITOR_WIDTH / 2, y, mBrush);
            y += CAPACITOR_THICKNESS;

            AddLine(x - CAPACITOR_WIDTH / 2, y, x + CAPACITOR_WIDTH / 2, y, mBrush);
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

        /// <summary>
        /// Analog Electronic Filters pp.58
        /// </summary>
        private void DrawFirstOrderFilter(int nStage, FirstOrderRationalPolynomial pf, double resistorValue) {
            /* a == 1/R0C0
             * C0 = R0/a
             */
            double r0 = 1;
            double c0 = r0 / pf.D(0).real;

            // 周波数スケーリング。キャパシタの値をωcで割る。
            double ωc = CutoffFrequencyHz * 2.0 * Math.PI;
            c0 /= ωc;

            // 最後に抵抗値を全て10 * 1000倍、キャパシターの容量を10*1000分の1にする。
            r0 *= resistorValue;
            c0 /= resistorValue;

            textBoxParameters.Text += string.Format("Stage {0} is 1st order Passive LPF. Fc={1}Hz\n", nStage + 1, ValueString(CutoffFrequencyHz));

            // 抵抗R0
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H, ResistorValueString(r0));

            // 抵抗の右から出る横線。右にアンプがある。
            AddLine(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H,
                    mX + RESISTOR_LENGTH + 30, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += RESISTOR_LENGTH + 10;

            // 抵抗R0とキャパシタC0をつなげる縦線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H,
                    mX, CIRCUIT_INPUT_LINE_H + 30, mBrush);

            // キャパシターC0
            AddCapacitorV(mX, CIRCUIT_INPUT_LINE_H + 30, CapacitorValueString(c0));

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
                    mX + 50, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += 50;
        }

        /// <summary>
        /// 2nd order state variable lowpass notch or highpass notch filter
        /// 今田悟 and 深谷武彦,実用アナログフィルタ設計法,CQ出版,1989
        /// p.63 and p.58 Figure 2.15(a)
        /// </summary>
        private void DrawStateVariableNotchSecondOrderFilter(int nStage, SecondOrderRationalPolynomial ps, double r, double c) {
            /* 
             * 有理多項式は周波数が正規化されているので、まず係数を周波数スケーリングする。
             * 
             * H(s) = { (s/ωc)^2 + n0 } { (s/ωc)^2 + d1*(s/ωc) + d0 }
             * 
             *      ⇩  分子と分母にωc^2を掛ける。
             *      
             * H(s) = { s^2 + ωc^2 n0 } { s^2 + ωc * d1 + ωc^2 d0 }
             *            ___
             * ωp = ωc * √d0 
             *            ___
             * ωn = ωc * √n0 
             * 
             * Q = ωp / { d1 * ωc }
             * 
             * ωp^2 = R2/{R3R6R7C1C2}
             * 
             * ωn^2 = R8ωp^2/R9
             * 
             * Q=(1+R4/R5)(1/{1/R1+1/R2+1/R3})sqrt(R6C1/R2R3R7C2)
             * 
             * 設計法18
             * 
             * R1=R2=R3=R4=R9=R10=R
             * 
             * C1=C2=C
             * 
             * Rf=1 / { ωp*Cf }
             * 
             * R6=R7=Rf
             * 
             * R5=R/{3Q-1}
             * 
             * R8={ωn^2/ωp^2}R
             * 
             */

            double ωc = CutoffFrequencyHz * 2.0 * Math.PI;
            double ωp = ωc * Math.Sqrt(ps.D(0).real);
            double ωn = ωc * Math.Sqrt(ps.N(0).real);
            double Q = ωp / ps.D(1).real / ωc;

            /* Test p64 計算例18
            Q = 5;
            ωp = 2.0 * Math.PI * 1000;
            ωn = 2.0 * Math.PI * 2000;
            */

            double rf = 1.0 / ωp / c;
            double r5 = r / (3.0 * Q - 1.0);
            double r8 = ωn * ωn * r / ωp / ωp;

            /*
            Console.WriteLine("R={0} C={1} ωc={2} ωp={3} ωn={4} Q={5} Rf={6} R5={7} r8={8}",
                r, c, ωc, ωp, ωn, Q, rf, r5, r8);
            */

            textBoxParameters.Text += string.Format("Stage {0} is 2nd order State variable lowpass notch filter. Fp={1}Hz Fn={2}Hz Q={3:G3}\n",
                nStage + 1, ValueString(ωp / 2 / Math.PI), ValueString(ωn / 2 / Math.PI), Q);

            // 抵抗R1
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H, ResistorValueString(r));

            {
                // R1の右から出る横線。オペアンプA1の入力につながる。
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 30, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 30, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + RESISTOR_LENGTH + 30 + 10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            mX += RESISTOR_LENGTH + 20;

            {
                // R1の右から出て、R2につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H - 30));
                AddLineStrip(p, mBrush);
            }
            // 抵抗R2
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H - 30, ResistorValueString(r));

            {
                // R2の左から出て、R3につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - 60));
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H - 60));
                AddLineStrip(p, mBrush);
            }
            // 抵抗R3
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H - 60, ResistorValueString(r));

            {
                // オペアンプA1の左下から出て、R5につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX + 20, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + 50));
                AddLineStrip(p, mBrush);
            }
            // 抵抗R5
            AddResistorV(mX, CIRCUIT_INPUT_LINE_H + 50, ResistorValueString(r5));

            // R5からGNDに接続する縦線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H + 50 + RESISTOR_LENGTH,
                    mX, CIRCUIT_INPUT_LINE_H + 50 + RESISTOR_LENGTH + 20, mBrush);
            AddGnd(mX, CIRCUIT_INPUT_LINE_H + 50 + RESISTOR_LENGTH + 20);

            // R5の上からR4の左に接続する線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H + 40,
                    mX + 20, CIRCUIT_INPUT_LINE_H + 40, mBrush);
            // 抵抗R4
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H + 40, ResistorValueString(r));

            mX += 20;

            // オペアンプA1
            AddOpamp(mX, CIRCUIT_INPUT_LINE_H, PlusPosition.Bottom);

            {
                // 抵抗R2からオペアンプA1の出力につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            mX += OPAMP_WIDTH + 10;

            {
                // オペアンプA1出力 → R8入力

                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + 40+30));
                p.Add(new Point(mX+20, CIRCUIT_INPUT_LINE_H + 40 + 30));
                AddLineStrip(p, mBrush);
            }
            // 抵抗R8
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H +40+30, ResistorValueString(r8));

            // R2の右からR6の左に接続する線。
            AddLine(mX, CIRCUIT_INPUT_LINE_H - 30,
                    mX + 20, CIRCUIT_INPUT_LINE_H - 30, mBrush);
            // 抵抗R6
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H - 30, ResistorValueString(rf));

            // R6の右からC1の左に接続する線。
            AddLine(mX + 20 + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H - 30,
                    mX + 20 + RESISTOR_LENGTH + 40, CIRCUIT_INPUT_LINE_H - 30, mBrush);

            mX += 20 + RESISTOR_LENGTH + 20;

            // C1
            AddCapacitorH(mX + 20, CIRCUIT_INPUT_LINE_H - 30, CapacitorValueString(c));

            {
                // C1からオペアンプA2の出力につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX + 20 + CAPACITOR_THICKNESS, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 10 + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 10 + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 10 + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            {
                // R6からオペアンプA2の左上につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            {
                // オペアンプA2の左下からGNDにつながる線。
                var p = new List<Point>();
                p.Add(new Point(mX + 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX, CIRCUIT_INPUT_LINE_H + 20));
                AddLineStrip(p, mBrush);
            }

            // オペアンプA2
            AddOpamp(mX + 10, CIRCUIT_INPUT_LINE_H, PlusPosition.Bottom);
            AddGnd(mX, CIRCUIT_INPUT_LINE_H + 20);

            mX += 10 + OPAMP_WIDTH;

            {
                // R4の右からオペアンプA2の出力につながる線。
                var p = new List<Point>();
                p.Add(new Point(mX - OPAMP_WIDTH - 10 - 20 - RESISTOR_LENGTH - 20 - 10 - OPAMP_WIDTH + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H + 40));
                p.Add(new Point(mX + 10, CIRCUIT_INPUT_LINE_H + 40));
                p.Add(new Point(mX + 10, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            mX += 10;

            // オペアンプの出力からR7
            AddLine(mX, CIRCUIT_INPUT_LINE_H - 30,
                    mX + 20, CIRCUIT_INPUT_LINE_H - 30, mBrush);

            // 抵抗R7
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H - 30, ResistorValueString(rf));

            // R7 → C2
            AddLine(mX + 20 + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H - 30,
                    mX + 20 + RESISTOR_LENGTH + 40, CIRCUIT_INPUT_LINE_H - 30, mBrush);

            // C2
            AddCapacitorH(mX + 20 + RESISTOR_LENGTH + 40, CIRCUIT_INPUT_LINE_H - 30, CapacitorValueString(c));

            {
                // R7 → オペアンプA3の左上
                var p = new List<Point>();
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 30, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }
            {
                // オペアンプA3の左下 → GND
                var p = new List<Point>();
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 30, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H + 20));
                AddLineStrip(p, mBrush);
            }

            mX += 20 + RESISTOR_LENGTH + 30;

            // オペアンプA3
            AddOpamp(mX, CIRCUIT_INPUT_LINE_H, PlusPosition.Bottom);
            AddGnd(mX - 10, CIRCUIT_INPUT_LINE_H + 20);

            {
                // C2の出力 → オペアンプA3の出力
                var p = new List<Point>();
                p.Add(new Point(mX + 10 + CAPACITOR_THICKNESS, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            {
                // R3の出力 → オペアンプA3の出力
                var p = new List<Point>();
                p.Add(new Point(mX - 10 - 20 - RESISTOR_LENGTH - 20 - 10 - OPAMP_WIDTH - 10 - 20 - RESISTOR_LENGTH - 20 - 10 - OPAMP_WIDTH - 20 + 20 + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H - 60));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 60));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                AddLineStrip(p, mBrush);
            }

            mX += OPAMP_WIDTH + 10;

            // オペアンプA3 → R9
            AddLine(mX, CIRCUIT_INPUT_LINE_H,
                    mX + 20, CIRCUIT_INPUT_LINE_H, mBrush);
            // 抵抗R9
            AddResistorH(mX + 20, CIRCUIT_INPUT_LINE_H, ResistorValueString(r));

            {
                // R9の出力 → R10入力
                var p = new List<Point>();
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 20, CIRCUIT_INPUT_LINE_H - 30));
                AddLineStrip(p, mBrush);
            }
            {
                // R9の出力 → オペアンプA4左上
                var p = new List<Point>();
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 10, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 20, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }
            {
                // オペアンプA4左下 → GND
                var p = new List<Point>();
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 20, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 10, CIRCUIT_INPUT_LINE_H + OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + 20 + RESISTOR_LENGTH + 20 + 10, CIRCUIT_INPUT_LINE_H + 20));
                AddLineStrip(p, mBrush);
            }

            // 抵抗R10
            AddResistorH(mX + 20 + RESISTOR_LENGTH + 20 + 20, CIRCUIT_INPUT_LINE_H - 30, ResistorValueString(r));

            mX += 20 + RESISTOR_LENGTH + 40;

            // オペアンプA4
            AddOpamp(mX, CIRCUIT_INPUT_LINE_H, PlusPosition.Bottom);
            AddGnd(mX - 10, CIRCUIT_INPUT_LINE_H + 20);

            {
                // R8の出力 → オペアンプA4左上入力
                var p = new List<Point>();
                p.Add(new Point(mX -30, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX - 30, CIRCUIT_INPUT_LINE_H + 40 + 30));
                p.Add(new Point(mX - 40-RESISTOR_LENGTH-20-10-OPAMP_WIDTH-10-20-RESISTOR_LENGTH-20-10-OPAMP_WIDTH-10-20, CIRCUIT_INPUT_LINE_H + 40 + 30));
                AddLineStrip(p, mBrush);
            }

            {
                // R10出力 → オペアンプA4出力
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + OPAMP_WIDTH, CIRCUIT_INPUT_LINE_H));
                AddLineStrip(p, mBrush);
            }

            // オペアンプA4出力 → 出力
            AddLine(mX + OPAMP_WIDTH + 10, CIRCUIT_INPUT_LINE_H,
                    mX + OPAMP_WIDTH + 70, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += OPAMP_WIDTH + 70;
        }

        /// <summary>
        /// Sallen-Key Minimum Sensitivity 2nd order Lowpass filter
        /// Analog Electronic Filters pp.470
        /// </summary>
        private void DrawSallenKeyLowpassSecondOrderFilter(int nStage, SecondOrderRationalPolynomial ps, double resistorValue) {
            /* k=1
             * R1=R2=1
             * とする。
             * 
             * C1=2Q/ω0
             * 
             * C2=1/2ω0Q
             * 
             *       ___
             * ω0 = √d0
             * Q = ω0/d1
             */
            double ω0 = Math.Sqrt(ps.D(0).real);
            double Q = ω0 / ps.D(1).real;
            double r1 = 1;
            double r2 = 1;
            double c1 = 2.0 * Q / ω0;
            double c2 = 1.0 / 2.0 / ω0 / Q;

            // 周波数スケーリング。キャパシタの値をωcで割る。
            double ωc = CutoffFrequencyHz * 2.0 * Math.PI;
            c1 /= ωc;
            c2 /= ωc;

            // 最後に抵抗値を全て10 * 1000倍、キャパシターの容量を10*1000分の一する。
            r1 *= resistorValue;
            r2 *= resistorValue;
            c1 /= resistorValue;
            c2 /= resistorValue;

            string message = "";
            if (4.0 <= Q) {
                message = " : HighQ and unstable";
            }
            textBoxParameters.Text += string.Format("Stage {0} is Sallen-Key LPF. ω0={1:G3} Q={2:G3}{3}\n", nStage + 1,
                ω0, Q, message);

            // 抵抗R1
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H, ResistorValueString(r1));

            // R1の右から出る横線。R2の入力につながる。
            AddLine(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H,
                    mX + RESISTOR_LENGTH + 40, CIRCUIT_INPUT_LINE_H, mBrush);

            mX += RESISTOR_LENGTH + 40;

            // 抵抗R2
            AddResistorH(mX, CIRCUIT_INPUT_LINE_H, ResistorValueString(r2));

            {
                // 抵抗R2の右から出る横線。オペアンプの＋入力につながる。
                var p = new List<Point>();
                p.Add(new Point(mX + RESISTOR_LENGTH, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 20 + 40, CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX + RESISTOR_LENGTH + 20 + 40, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                p.Add(new Point(mX + RESISTOR_LENGTH + 20 + 40 + 10, CIRCUIT_INPUT_LINE_H - OPAMP_INPUT_H_OFFS));
                AddLineStrip(p, mBrush);
            }

            {
                // 抵抗R1-R2とキャパシタC1をつなげる縦線。
                var p = new List<Point>();
                p.Add(new Point(mX -20,                    CIRCUIT_INPUT_LINE_H));
                p.Add(new Point(mX -20,                    CIRCUIT_INPUT_LINE_H - 30));
                p.Add(new Point(mX + RESISTOR_LENGTH + 20, CIRCUIT_INPUT_LINE_H - 30));
                AddLineStrip(p, mBrush);
            }

            mX += RESISTOR_LENGTH + 20;

            // キャパシターC1
            AddCapacitorH(mX, CIRCUIT_INPUT_LINE_H - 30, CapacitorValueString(c1));

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
            AddCapacitorV(mX, CIRCUIT_INPUT_LINE_H + 30, CapacitorValueString(c2));

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
            if (10.0 * 1000.0 * 1000 <= v) {
                // 10MΩ~99MΩ
                return string.Format("{0:F1}M", v / (1000.0 * 1000));
            }
            if (1000.0 * 1000 <= v) {
                // 1MΩ~9MΩ
                return string.Format("{0:F2}M", v / (1000.0 * 1000));
            }
            if (100.0 * 1000.0 <= v) {
                // 100kΩ~999kΩ
                return string.Format("{0:F0}k", v / (1000.0));
            }
            if (10.0 * 1000.0 <= v) {
                // 10kΩ~99kΩ
                return string.Format("{0:F1}k", v / (1000.0));
            }
            if (1000.0 <= v) {
                // 1kΩ~9kΩ
                return string.Format("{0:F2}k", v / (1000.0));
            }
            if (100.0 <= v) {
                // 100Ω~999Ω
                return string.Format("{0:F0}k", v);
            }
            if (10.0 <= v) {
                // 10Ω~99Ω
                return string.Format("{0:F1}k", v);
            }

            if (v < 0.01 * 0.001 * 0.001 * 0.001) {
                // 1pF~9pF
                return string.Format("{0:F2}p", v * (1000.0 * 1000.0 * 1000.0 * 1000.0));
            }
            if (v < 0.1 * 0.001 * 0.001 * 0.001) {
                // 10pF~99pF
                return string.Format("{0:F1}p", v * (1000.0 * 1000.0 * 1000.0 * 1000.0));
            }
            if (v < 0.001 * 0.001 * 0.001) {
                // 100pF~999pF
                return string.Format("{0:F0}p", v * (1000.0 * 1000.0 * 1000.0 * 1000.0));
            }
            if (v < 0.01 * 0.001 * 0.001) {
                // 1nF~9nF
                return string.Format("{0:F2}n", v * (1000.0 * 1000.0 * 1000.0));
            }
            if (v < 0.1 * 0.001 * 0.001) {
                // 10nF~99nF
                return string.Format("{0:F1}n", v * (1000.0 * 1000.0 * 1000.0));
            }
            if (v < 0.001 * 0.001) {
                // 100nF~999nF
                return string.Format("{0:F0}n", v * (1000.0 * 1000.0 * 1000.0));
            }
            if (v < 0.01 * 0.001) {
                // 1μF~9μF
                return string.Format("{0:F2}μ", v * (1000.0 * 1000.0));
            }
            if (v < 0.1 * 0.001) {
                // 10μF~99μF
                return string.Format("{0:F1}μ", v * (1000.0 * 1000.0));
            }
            if (v < 0.001) {
                // 100μF~999μF
                return string.Format("{0:F0}μ", v * (1000.0 * 1000.0));
            }

            return string.Format("{0:F1}", v);
        }

        private static string ResistorValueString(double v) {
            return string.Format("{0}Ω", ValueString(v));
        }

        private static string CapacitorValueString(double v) {
            return string.Format("{0}F", ValueString(v));
        }

        private double GetResistorValueOfStage(int stage) {
            var cb = stackPanelResistor.Children[stage] as ComboBox;
            if (cb == null) {
                var grid = stackPanelResistor.Children[stage] as Grid;
                cb = grid.Children[0] as ComboBox;
            }
            ResistorValueType rvt = (ResistorValueType)cb.SelectedIndex;
            return ResistorValueTypeToValue(rvt);
        }

        private double GetCapacitorValueOfStage(int stage) {
            var grid = stackPanelResistor.Children[stage] as Grid;
            var cb = grid.Children[1] as ComboBox;
            CapacitorValueType cvt = (CapacitorValueType)cb.SelectedIndex;
            return CapacitorValueTypeToValue(cvt);
        }

        public void Update() {
            canvas1.Children.Clear();
            mX = 0;
            textBoxParameters.Clear();

            DrawInput();

            int order = 0;
            int nStage = 0;
            foreach (var p in mRealPolynomialList) {
                order += p.Order();
                if (p.Order() == 1) {
                    // 1次多項式。

                    var pf = p as FirstOrderRationalPolynomial;
                    double rf = GetResistorValueOfStage(nStage);
                    DrawFirstOrderFilter(nStage, pf, rf);
                } else {
                    // 2次多項式。

                    var ps = p as SecondOrderRationalPolynomial;
                    if (ps.N(2).EqualValue(WWComplex.Unity())) {
                        double rf = GetResistorValueOfStage(nStage);
                        double cf = GetCapacitorValueOfStage(nStage);
                        DrawStateVariableNotchSecondOrderFilter(nStage, ps, rf, cf);
                    } else {
                        double rf = GetResistorValueOfStage(nStage);
                        DrawSallenKeyLowpassSecondOrderFilter(nStage, ps, rf);
                    }
                }

                ++nStage;
            }

            DrawOutput();
            canvas1.Width = mX;
            canvas1.Height = CIRCUIT_INPUT_LINE_H * 3;

            textBoxParameters.Text += string.Format("Order = {0}, Stage = {1}\n", order, mRealPolynomialList.Count());
        }
    }
}
