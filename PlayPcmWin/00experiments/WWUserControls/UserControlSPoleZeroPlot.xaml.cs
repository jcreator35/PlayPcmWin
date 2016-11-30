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
    public partial class UserControlSPoleZeroPlot : UserControl {
        public UserControlSPoleZeroPlot() {
            InitializeComponent();
        }

        private List<UIElement> mPoleList = new List<UIElement>();
        private List<UIElement> mZeroList = new List<UIElement>();

        /* 表示座標系はx+が右、y+が下
         * 原点は(128,128) */
        const double OFFS_X = 128;
        const double OFFS_Y = 128;
        const double SCALE_X = 64;
        const double SCALE_Y = 64;

        const double SCALE_CROSS = 5;
        const double DIAMETER_ZERO = 7;

        private double mPoleZeroScale = 1.0;

        public void SetScale(double s) {
            mPoleZeroScale = s;
            mXm1.Text = string.Format("-{0:G4}", s);
            mYm1.Text = string.Format("-{0:G4}j", s);
            mXp1.Text = string.Format("+{0:G4}", s);
            mYp1.Text = string.Format("+{0:G4}j", s);
        }

        public void AddPole(WWComplex pole) {
            double x = OFFS_X + pole.real / mPoleZeroScale * SCALE_X;
            double y = OFFS_Y - pole.imaginary / mPoleZeroScale * SCALE_Y;

            {
                var l = new Line();
                l.X1 = x - SCALE_CROSS;
                l.X2 = x + SCALE_CROSS;
                l.Y1 = y - SCALE_CROSS;
                l.Y2 = y + SCALE_CROSS;
                l.Stroke = new SolidColorBrush(Colors.Black);
                mPoleList.Add(l);
                canvasPoleZero.Children.Add(l);
            }
            {
                var l = new Line();
                l.X1 = x + SCALE_CROSS;
                l.X2 = x - SCALE_CROSS;
                l.Y1 = y - SCALE_CROSS;
                l.Y2 = y + SCALE_CROSS;
                l.Stroke = new SolidColorBrush(Colors.Black);
                mPoleList.Add(l);
                canvasPoleZero.Children.Add(l);
            }
        }

        public void AddZero(WWComplex zero) {
            double x = OFFS_X + zero.real / mPoleZeroScale * SCALE_X;
            double y = OFFS_Y - zero.imaginary / mPoleZeroScale * SCALE_Y;

            {
                var e = new Ellipse();
                e.Width = DIAMETER_ZERO;
                e.Height = DIAMETER_ZERO;
                e.Stroke = new SolidColorBrush(Colors.Black);
                mPoleList.Add(e);
                canvasPoleZero.Children.Add(e);
                Canvas.SetLeft(e, x - DIAMETER_ZERO / 2);
                Canvas.SetTop(e, y - DIAMETER_ZERO / 2);
            }
        }

        public void ClearPoleZero() {
            foreach (var pole in mPoleList) {
                canvasPoleZero.Children.Remove(pole);
            }
            foreach (var zero in mZeroList) {
                canvasPoleZero.Children.Remove(zero);
            }

            mPoleList.Clear();
            mZeroList.Clear();
        }
    }
}
