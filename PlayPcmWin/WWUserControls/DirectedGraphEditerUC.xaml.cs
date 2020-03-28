// 日本語。
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
    /// Interaction logic for DirectedGraphEditerUC.xaml
    /// </summary>
    public partial class DirectedGraphEditerUC : UserControl {
        public DirectedGraphEditerUC() {

            InitializeComponent();

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            int gridSz = int.Parse(mTextBoxGridSize.Text);

            RedrawGrid(gridSz);
        }

        List<Line> mGridLines = new List<Line>();

        private void RedrawGrid(int gridSz) {
            foreach (var p in mGridLines) {
                mCanvas.Children.Remove(p);
            }
            mGridLines.Clear();

            for (int x = 0; x < mCanvas.ActualWidth; x += gridSz) {
                var p = new Line();
                p.X1 = x;
                p.X2 = x;
                p.Y1 = 0;
                p.Y2 = mCanvas.ActualHeight;
                p.Stroke = mGridBrush;

                mCanvas.Children.Add(p);

                mGridLines.Add(p);
            }

            for (int y = 0; y < mCanvas.ActualHeight; y += gridSz) {
                var p = new Line();
                p.X1 = 0;
                p.X2 = mCanvas.ActualWidth;
                p.Y1 = y;
                p.Y2 = y;
                p.Stroke = mGridBrush;

                mCanvas.Children.Add(p);

                mGridLines.Add(p);
            }
        }



        enum Mode {
            ModeAddPoint,
            ModeMovePoint,
            ModeDeletePont
        };

        Mode mMode = Mode.ModeAddPoint;

        Brush mGridBrush = new SolidColorBrush(Colors.LightGray);
        Brush mPointBrush = new SolidColorBrush(Colors.Black);
        Brush mTmpBrush = new SolidColorBrush(Colors.Blue);
        Brush mErrBrush = new SolidColorBrush(Colors.Red);

        Ellipse mEllipseTmp = null;

        class PointInf {
            public Ellipse ellipse;
            public WWVectorD2 xy;
            public PointInf(Ellipse e, double x, double y) {
                ellipse = e;
                xy = new WWVectorD2(x, y);
            }
        }

        List<PointInf> mPoints = new List<PointInf>();

        private void mRadioButtonAddPoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeAddPoint;
            Cursor = Cursors.Cross;
        }

        private void mRadioButtonMovePoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeMovePoint;
            Cursor = Cursors.Cross;
        }

        private void mRadioButtonDeletePoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeDeletePont;
            Cursor = Cursors.Cross;
        }

        /// <returns>既存の点と当たったら既存点のPointInfを戻す。</returns>
        private PointInf TestHit(double x, double y, double threshold) {
            var xy = new WWVectorD2(x, y);
            foreach (var p in mPoints) {
                if (WWVectorD2.Distance(p.xy, xy) < threshold) {
                    return p;
                }
            }

            return null;
        }

        private void UpdatePointSub(bool exec, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X) {
                return;
            }
            if (pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            var pInf = TestHit(pos.X, pos.Y, pointSz);

            if (mEllipseTmp != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mEllipseTmp);
            }

            var el = new Ellipse();
            el.Width = pointSz;
            el.Height = pointSz;

            var color = exec ? mPointBrush : mTmpBrush;
            if (pInf != null) {
                color = mErrBrush;
            }
            el.Fill = color;

            if (exec) {
                // 点を追加する。
                if (pInf != null) {
                    // 追加不可能。
                } else {
                    // 追加可能。
                    mPoints.Add(new PointInf(el, pos.X, pos.Y));

                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, pos.X - pointSz / 2);
                    Canvas.SetTop(el,  pos.Y - pointSz / 2);
                }
            } else {
                // 一時的点表示を更新。
                mEllipseTmp = el;

                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, pos.X - pointSz / 2);
                Canvas.SetTop(el,  pos.Y - pointSz / 2);
            }
        }

        private void UpdateDeleteSub(bool exec, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X) {
                return;
            }
            if (pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            if (mEllipseTmp != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mEllipseTmp);
            }

            var pInf = TestHit(pos.X, pos.Y, pointSz);
            if (pInf == null) {
                return;
            }
    
            var el = new Ellipse();
            el.Width = pointSz;
            el.Height = pointSz;
            el.Fill = mErrBrush;

            if (exec) {
                mCanvas.Children.Remove(pInf.ellipse);
                mPoints.Remove(pInf);
            } else {
                // 一時的点表示を更新。
                mEllipseTmp = el;

                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
                Canvas.SetTop(el,  pInf.xy.Y - pointSz / 2);
            }
        }


        enum MoveState {
            Init,
            Selected,
        };

        MoveState mMoveState = MoveState.Init;

        enum MoveProcMode {
            Hover,
            Select,
            Move,
            Exec
        };

        private void MovePointSub(MoveProcMode mm, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X) {
                return;
            }
            if (pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            Console.WriteLine("MovePointSub {0} ({1:0.0} {2:0.0}) nPoints={3} {4}", mm, pos.X, pos.Y, mPoints.Count, mMoveState);

            if (mEllipseTmp != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mEllipseTmp);
            }

            switch (mm) {
            case MoveProcMode.Hover:
                {   // 当たったとき、赤い点を出す。
                    var pInf = TestHit(pos.X, pos.Y, pointSz);
                    if (pInf == null) {
                        return;
                    }

                    var el = new Ellipse();
                    el.Width = pointSz;
                    el.Height = pointSz;
                    el.Fill = mErrBrush;

                    mEllipseTmp = el;

                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
                    Canvas.SetTop(el,  pInf.xy.Y - pointSz / 2);
                }
                break;
            case MoveProcMode.Select:
                {
                    var pInf = TestHit(pos.X, pos.Y, pointSz);
                    if (pInf == null) {
                        mMoveState = MoveState.Init;
                        return;
                    }

                    mMoveState = MoveState.Selected;

                    // 当たった点を消す。
                    mCanvas.Children.Remove(pInf.ellipse);
                    mPoints.Remove(pInf);

                    // マウスポインター位置に赤い点を出す。
                    var el = new Ellipse();
                    el.Width = pointSz;
                    el.Height = pointSz;
                    el.Fill = mErrBrush;

                    mEllipseTmp = el;

                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, pos.X - pointSz / 2);
                    Canvas.SetTop(el,  pos.Y - pointSz / 2);
                }
                break;
            case MoveProcMode.Move:
                {
                    if (mMoveState == MoveState.Selected) {
                        // マウスポインター位置に赤い点を出す。
                        var el = new Ellipse();
                        el.Width = pointSz;
                        el.Height = pointSz;
                        el.Fill = mErrBrush;

                        mEllipseTmp = el;

                        mCanvas.Children.Add(el);
                        Canvas.SetLeft(el, pos.X - pointSz / 2);
                        Canvas.SetTop(el, pos.Y - pointSz / 2);
                    }
                }
                break;
            case MoveProcMode.Exec:
                {
                    if (mMoveState == MoveState.Selected) {
                        // マウスポインター位置の点を追加。
                        var el = new Ellipse();
                        el.Width = pointSz;
                        el.Height = pointSz;
                        el.Fill = mPointBrush;

                        mCanvas.Children.Add(el);
                        Canvas.SetLeft(el, pos.X - pointSz / 2);
                        Canvas.SetTop(el, pos.Y - pointSz / 2);

                        mPoints.Add(new PointInf(el, pos.X, pos.Y));
                        mMoveState = MoveState.Init;
                    }
                }
                break;
            }
        }

        private void mCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) {
                return;
            }
            
            int gridSz = 4;
            if (!int.TryParse(mTextBoxGridSize.Text, out gridSz)) {
                MessageBox.Show("Error: Grid Size parse error");
                return;
            }

            var posExact = e.GetPosition(mCanvas);

            var pos = SnapToGrid(posExact.X, posExact.Y, gridSz);

            switch (mMode) {
            case Mode.ModeAddPoint:
                UpdatePointSub(false, pos);
                break;
            case Mode.ModeDeletePont:
                UpdateDeleteSub(false, pos);
                break;
            case Mode.ModeMovePoint:
                MovePointSub(MoveProcMode.Select, pos);
                break;
            }
        }

        WWVectorD2 mPrevPos = null;

        private void mCanvas_MouseMove(object sender, MouseEventArgs e) {
            int gridSz = 4;
            if (!int.TryParse(mTextBoxGridSize.Text, out gridSz)) {
                MessageBox.Show("Error: Grid Size parse error");
                return;
            }

            var posExact = e.GetPosition(mCanvas);
            if (mPrevPos != null && WWVectorD2.Distance(mPrevPos, new WWVectorD2(posExact.X, posExact.Y)) < 1.0) {
                return;
            }
            mPrevPos = new WWVectorD2(posExact.X, posExact.Y);

            var pos = SnapToGrid(posExact.X, posExact.Y, gridSz);

            if (mMode == Mode.ModeMovePoint) {
                MovePointSub((e.LeftButton == MouseButtonState.Pressed) ? MoveProcMode.Move : MoveProcMode.Hover,
                    pos);
                return;
            }

            // Moveではないモードのときは、マウスがホバーしているときだけ処理がある。
            if (e.LeftButton != MouseButtonState.Released) {
                return;
            }

            switch (mMode) {
            case Mode.ModeAddPoint:
                UpdatePointSub(false, pos);
                break;
            case Mode.ModeDeletePont:
                UpdateDeleteSub(false, pos);
                break;
            }
        }

        private void mCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Released) {
                return;
            }

            int gridSz = 4;
            if (!int.TryParse(mTextBoxGridSize.Text, out gridSz)) {
                MessageBox.Show("Error: Grid Size parse error");
                return;
            } 
            
            var posExact = e.GetPosition(mCanvas);
            var pos = SnapToGrid(posExact.X, posExact.Y, gridSz);

            switch (mMode) {
            case Mode.ModeAddPoint:
                UpdatePointSub(true, pos);
                break;
            case Mode.ModeDeletePont:
                UpdateDeleteSub(true, pos);
                break;
            case Mode.ModeMovePoint:
                MovePointSub(MoveProcMode.Exec, pos);
                break;
            }
        }

        private bool PointExists(List<PointInf> points, WWVectorD2 xy) {
            foreach (var p in points) {
                if (WWVectorD2.Distance(p.xy, xy) < 1) {
                    return true;
                }
            }

            return false;
        }

        private WWVectorD2 SnapToGrid(double xD, double yD, int gridSz) {
            int x = (int)xD;
            x = ((x + gridSz / 2) / gridSz) * gridSz;

            int y = (int)yD;
            y = ((y + gridSz / 2) / gridSz) * gridSz;

            return new WWVectorD2(x, y);
        }

        private void mButtonSnapToGrid_Click(object sender, RoutedEventArgs e) {
            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            int gridSz = 4;
            if (!int.TryParse(mTextBoxGridSize.Text, out gridSz)) {
                MessageBox.Show("Error: Grid Size parse error");
                return;
            }

            RedrawGrid(gridSz);

            var newPoints = new List<PointInf>();

            foreach (var p in mPoints) {
                mCanvas.Children.Remove(p.ellipse);

                var xy = SnapToGrid(p.xy.X, p.xy.Y, gridSz);
                if (PointExists(newPoints, xy)) {
                    // 同じ位置に点が重なっている。
                    // この点は削除する。
                    continue;
                }

                var el = new Ellipse();
                el.Width = pointSz;
                el.Height = pointSz;
                el.Fill = mPointBrush;

                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, xy.X - pointSz / 2);
                Canvas.SetTop(el, xy.Y - pointSz / 2);

                newPoints.Add(new PointInf(el, xy.X, xy.Y));
            }

            mPoints = newPoints;
        }

    }
}
