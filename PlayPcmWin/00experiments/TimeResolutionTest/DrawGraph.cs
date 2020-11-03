using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace TimeResolutionTest {
    class DrawGraph {
        private Canvas mCanvas;

        public void SetCanvas(Canvas c) {
            mCanvas = c;
        }

        /// <summary>
        /// グラフのスケーリング係数。
        /// </summary>
        public double mScale = 0.5;

        /// <summary>
        /// グリッド線 縦棒
        /// </summary>
        public int NumGridX = 8;

        /// <summary>
        /// グリッド線 横棒
        /// </summary>
        public int NumGridY = 4;
        
        public DrawGraph(Canvas canvas) {
            mCanvas = canvas;
        }

        public delegate double FunctionToPlot(double x);

        List<UIElement> mElements = new List<UIElement>();

        private void AddLine(Vector p0, Vector p1, Brush b) {
            var l = new Line();
            l.X1 = p0.X;
            l.Y1 = p0.Y;
            l.X2 = p1.X;
            l.Y2 = p1.Y;
            l.Stroke = b;

            mCanvas.Children.Add(l);
            mElements.Add(l);
        }

        private TextBlock AddTextBlock(Vector xy, double fontSize, Brush brush, string text) {
            var tb = new TextBlock();
            tb.Foreground = brush;
            tb.FontSize = fontSize;
            tb.Text = text;
            mCanvas.Children.Add(tb);
            Canvas.SetLeft(tb, xy.X);
            Canvas.SetTop(tb, xy.Y);
            mElements.Add(tb);

            return tb;
        }

        private void AddTextBlockV(Vector xy, double fontSize, Brush brush, string text) {
            var tb = AddTextBlock(xy, fontSize, brush, text);
            var rt = new RotateTransform(270);
            tb.RenderTransform = rt;
        }

        private void AddCircle(Vector center, double diameter, Brush brush) {
            var e = new Ellipse();
            e.Width = diameter;
            e.Height = diameter;
            e.Stroke = brush;
            e.Fill = brush;

            mCanvas.Children.Add(e);
            Canvas.SetLeft(e, center.X - diameter / 2);
            Canvas.SetTop(e, center.Y - diameter / 2);
            mElements.Add(e);
        }

        public void Redraw(FunctionToPlot fp, string title) {
            var WH = new Size(mCanvas.ActualWidth, mCanvas.ActualHeight);

            mCanvas.Children.Clear();
            mElements.Clear();

            for (int i = 1; i < NumGridX; ++i) {
                AddLine(new Vector(i * WH.Width / NumGridX, 0), new Vector(i * WH.Width / NumGridX, WH.Height), Brushes.LightGray);
            }
            for (int i = 1; i < NumGridY; ++i) {
                AddLine(new Vector(0, i * WH.Height / NumGridY), new Vector(WH.Width, i * WH.Height / NumGridY), Brushes.LightGray);
            }

            AddLine(new Vector(0, WH.Height/2), new Vector(WH.Width, WH.Height/2), Brushes.Gray);
            AddLine(new Vector(0, 0), new Vector(0, WH.Height), Brushes.Gray);

            RedrawFunction(fp);

            AddTextBlock(new Vector(0, WH.Height/2), 10, Brushes.Black, "0");
            AddTextBlock(new Vector(WH.Width / NumGridX-20, WH.Height / 2), 10, Brushes.Black, "1/44100");
            AddTextBlock(new Vector(2*WH.Width / NumGridX - 20, WH.Height / 2), 10, Brushes.Black, "2/44100");
            AddTextBlock(new Vector(3 * WH.Width / NumGridX - 20, WH.Height / 2), 10, Brushes.Black, "3/44100");
            AddTextBlock(new Vector(4 * WH.Width / NumGridX - 20, WH.Height / 2), 10, Brushes.Black, "4/44100");
            AddTextBlock(new Vector(5 * WH.Width / NumGridX - 20, WH.Height / 2), 10, Brushes.Black, "5/44100");
            AddTextBlock(new Vector(6 * WH.Width / NumGridX - 20, WH.Height / 2), 10, Brushes.Black, "6/44100");

            AddTextBlock(new Vector(WH.Width-55, WH.Height/2), 10, Brushes.Black, "Time(sec) →");
            AddTextBlockV(new Vector(0, 60), 10, Brushes.Black, "Amplitude →");
            AddTextBlock(new Vector(20, 12), 16, Brushes.Black, title);
        }



        private void RedrawFunction(FunctionToPlot fp) {
            var canvasWH = new Size(mCanvas.ActualWidth, mCanvas.ActualHeight);

            {
                // 線を引きます。
                var p = new Polyline();
                p.Stroke = Brushes.DarkOrange;
                p.StrokeThickness = 1;
                p.FillRule = FillRule.EvenOdd;

                var pc = new PointCollection();

                for (int i = 0; i <= canvasWH.Width; ++i) {
                    double x = (double)i / canvasWH.Width;
                    double y = fp(x);
                    double yDisp = canvasWH.Height / 2 - mScale * y * canvasWH.Height / 2;

                    pc.Add(new System.Windows.Point(i, yDisp));
                }

                p.Points = pc;
                mCanvas.Children.Add(p);
                mElements.Add(p);
            }

            {
                // 点を描きます。
                for (int i = 0; i <= NumGridX; ++i) {
                    double x = (double)i / NumGridX;
                    double y = fp(x);
                    double xDisp = x * canvasWH.Width;
                    double yDisp = canvasWH.Height / 2 - mScale * y * canvasWH.Height / 2;

                    AddCircle(new Vector(xDisp, yDisp), 5.0, Brushes.Black);
                }
            }

        }


    }
}
