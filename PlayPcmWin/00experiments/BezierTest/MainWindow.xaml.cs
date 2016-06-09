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

namespace BezierTest {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void Update() {
            double [] p = new double[3];
            p[0] = slider1.Value;
            p[1] = slider2.Value;
            p[2] = slider3.Value;

            var path = new Path();
            path.Stroke = new SolidColorBrush(Colors.Black);
            var gg = new GeometryGroup();

            double prevX = 0;
            double prevY = p[0];

            double actualWidth = canvas1.ActualWidth;
            double actualHeight = canvas1.ActualHeight;

            for (int i = 0; i < 100; ++i) {
                double t = i / 100.0;
                double y = p[0] * (1-t*t) + p[1] * 2 * (1-t) * t + p[2] * t * t;

                double x = t;

                double dx0 = prevX * actualWidth;
                double dx1 = x * actualWidth;
                double dy0 = (0.5 - prevY* 0.5) * actualHeight;
                double dy1 = (0.5 - y * 0.5) * actualHeight;
                
                var line = new LineGeometry(new Point(dx0, dy0), new Point(dx1, dy1));
                gg.Children.Add(line);

                prevX = x;
                prevY = y;
            }

            for (int i = 0; i < 3; ++i) {
                var point = new EllipseGeometry(new Point(i*actualWidth/2, actualHeight * (0.5 - 0.5 * p[i])), 10, 10);
                gg.Children.Add(point);
            }

            path.Data = gg;
            canvas1.Children.Clear();
            canvas1.Children.Add(path);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Update();
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Update();
        }

        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Update();
        }

        private void slider3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Update();
        }
    }
}
