using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Shapes;

namespace TimerResolutionMonitor {
    public sealed partial class MainWindow : Window {
        private bool mLoaded = false;
        private DispatcherTimer mDispatcherTimer;
        private const int LOG_NUM = 180;
        private const double YAXIS_POS_X = 50;
        private const double LINE_TO_TEXT_OFFSET = -14;

        private List<int> mTimerResolutionLog = new List<int>();

        enum UpdateIntervalType {
            Interval500ms,
            Interval1s,
            Interval10s,
        };

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Properties.Settings.Default.Save();
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        const int LINE_NUM            = 18;
        const int MAJOR_LINE_INTERVAL = 6;
        List<Line> mLineList = new List<Line>();

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            var majorLineArgb = new SolidColorBrush(Colors.Gray);
            var minorLineArgb = new SolidColorBrush(Colors.Silver);
            majorLineArgb.Freeze();
            minorLineArgb.Freeze();
            
            for (int i=0; i < LINE_NUM; ++i) {
                var line = new Line();
                if (0 == (i % MAJOR_LINE_INTERVAL)) {
                    line.Stroke = majorLineArgb;
                } else {
                    line.Stroke = minorLineArgb;
                }
                canvas1.Children.Add(line);
                mLineList.Add(line);
            }

            UpdateUI();

            this.Title = string.Format(CultureInfo.CurrentCulture, "TimerResolutionMonitor {0}", AssemblyVersion);

            comboBox1.Items.Clear();
            comboBox1.Items.Add("Graph update interval = 0.5 sec");
            comboBox1.Items.Add("Graph update interval = 1 sec");
            comboBox1.Items.Add("Graph update interval = 10 sec");
            comboBox1.SelectedIndex = Properties.Settings.Default.UpdateInterval;

            DispatcherTimerTick(null, null);

            mDispatcherTimer = new DispatcherTimer();
            mDispatcherTimer.Tick += new EventHandler(DispatcherTimerTick);
            mDispatcherTimer.Interval = GetTimeSpan();
            mDispatcherTimer.Start();

            mLoaded = true;
        }

        private Point CalcVertexPos(int i, int curResolution) {
            return new Point(
                canvas1.ActualWidth-i*(canvas1.ActualWidth-YAXIS_POS_X)/(LOG_NUM-1),
                canvas1.ActualHeight- canvas1.ActualHeight / 5.0 * Math.Log10(curResolution/100.0));
        }

        private void DispatcherTimerTick(object sender, EventArgs e) {
            {
                int minResolution;
                int maxResolution;
                int curResolution;
                int hr = NativeMethods.NtQueryTimerResolution(out minResolution, out maxResolution, out curResolution);
                if (hr != 0) {
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "NtQueryTimerResolution failed {0:X8}", hr));
                    Close();
                }

                mTimerResolutionLog.Insert(0, curResolution);
                while (LOG_NUM < mTimerResolutionLog.Count) {
                    mTimerResolutionLog.RemoveAt(mTimerResolutionLog.Count - 1);
                }

                labelTimerResolution.Content = string.Format(CultureInfo.CurrentCulture, "Current timer resolution = {0:0.0} ms", curResolution * 0.0001);
            }
            {
                PathFigure myPathFigure = new PathFigure();
                myPathFigure.StartPoint = CalcVertexPos(0, mTimerResolutionLog[0]);
                PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();

                for (int i=1; i < mTimerResolutionLog.Count; ++i) {
                    LineSegment myLineSegment = new LineSegment();
                    myLineSegment.Point = CalcVertexPos(i, mTimerResolutionLog[i]);
                    myPathSegmentCollection.Add(myLineSegment);
                }

                myPathFigure.Segments = myPathSegmentCollection;

                PathFigureCollection myPathFigureCollection = new PathFigureCollection();
                myPathFigureCollection.Add(myPathFigure);

                pathGeometry.Figures = myPathFigureCollection;
            }
            UpdateUI();
        }

        private void UpdateUI() {
            {
                int i=0;
                foreach (var line in mLineList) {
                    line.X1 = YAXIS_POS_X + (canvas1.ActualWidth - YAXIS_POS_X) * i / LINE_NUM;
                    line.X2 = YAXIS_POS_X + (canvas1.ActualWidth - YAXIS_POS_X) * i / LINE_NUM;
                    line.Y1 = 0;
                    line.Y2 = canvas1.ActualHeight * 4 / 5;
                    ++i;
                }
            }

            switch (comboBox1.SelectedIndex) {
            case (int)UpdateIntervalType.Interval500ms:
                this.labelX1.Content = "30s";
                this.labelX2.Content = "1min";
                break;
            case (int)UpdateIntervalType.Interval1s:
                this.labelX1.Content = "1min";
                this.labelX2.Content = "2min";
                break;
            case (int)UpdateIntervalType.Interval10s:
                this.labelX1.Content = "10min";
                this.labelX2.Content = "20min";
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
            Canvas.SetLeft(labelX1, (YAXIS_POS_X + (canvas1.ActualWidth - YAXIS_POS_X) * 2 * MAJOR_LINE_INTERVAL / LINE_NUM) - labelX1.ActualWidth / 2);
            Canvas.SetLeft(labelX2, (YAXIS_POS_X + (canvas1.ActualWidth - YAXIS_POS_X) * 1 * MAJOR_LINE_INTERVAL / LINE_NUM) - labelX2.ActualWidth / 2);
            Canvas.SetTop(labelX1, canvas1.ActualHeight * 4 / 5);
            Canvas.SetTop(labelX2, canvas1.ActualHeight * 4 / 5);

            double horizontalInterval = canvas1.ActualHeight / 5;

            this.line100ms.Y1 = horizontalInterval;
            this.line100ms.Y2 = horizontalInterval;
            this.line100ms.X2 = canvas1.ActualWidth;
            Canvas.SetTop(label100ms, line100ms.Y1 + LINE_TO_TEXT_OFFSET);

            this.line10ms.Y1 = horizontalInterval * 2;
            this.line10ms.Y2 = horizontalInterval * 2;
            this.line10ms.X2 = canvas1.ActualWidth;
            Canvas.SetTop(label10ms, line10ms.Y1 + LINE_TO_TEXT_OFFSET);

            this.line1ms.Y1 = horizontalInterval * 3;
            this.line1ms.Y2 = horizontalInterval * 3;
            this.line1ms.X2 = canvas1.ActualWidth;
            Canvas.SetTop(label1ms, line1ms.Y1 + LINE_TO_TEXT_OFFSET);

            this.line100us.Y1 = horizontalInterval * 4;
            this.line100us.Y2 = horizontalInterval * 4;
            this.line100us.X2 = canvas1.ActualWidth;
            Canvas.SetTop(label100us, line100us.Y1 + LINE_TO_TEXT_OFFSET);
        }

        private TimeSpan GetTimeSpan() {
            switch (comboBox1.SelectedIndex) {
            case (int)UpdateIntervalType.Interval500ms:
                return new TimeSpan(0, 0, 0, 0, 500);
            case (int)UpdateIntervalType.Interval10s:
                return new TimeSpan(0, 0, 10);
            case (int)UpdateIntervalType.Interval1s:
            default:
                return new TimeSpan(0, 0, 1);
            }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mLoaded) {
                return;
            }

            Properties.Settings.Default.UpdateInterval = comboBox1.SelectedIndex;

            mTimerResolutionLog.Clear();

            mDispatcherTimer.Interval = GetTimeSpan();
            mDispatcherTimer.Start();
        }

        internal static class NativeMethods {
            [DllImport("ntdll.dll", SetLastError = true)]
            internal static extern int NtQueryTimerResolution(out int minimumResolution, out int maximumResolution, out int currentResolution);
        }
    }
}
