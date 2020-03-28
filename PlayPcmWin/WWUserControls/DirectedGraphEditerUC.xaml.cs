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

        enum Mode {
            ModeAddPointEdge,
            ModeMovePoint,
            ModeDeletePont
        };

        Mode mMode = Mode.ModeAddPointEdge;

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

        private void mRadioButtonAddPointEdge_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeAddPointEdge;
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

        private void UpdatePointSub(bool exec, double posX, double posY) {
            if (posX < 0 || mCanvas.ActualWidth <= posX) {
                return;
            }
            if (posY < 0 || mCanvas.ActualHeight <= posY) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            var pInf = TestHit(posX, posY, pointSz);

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
                    mPoints.Add(new PointInf(el, posX, posY));

                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, posX - pointSz / 2);
                    Canvas.SetTop(el,  posY - pointSz / 2);
                }
            } else {
                // 一時的点表示を更新。
                mEllipseTmp = el;

                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, posX - pointSz / 2);
                Canvas.SetTop(el,  posY - pointSz / 2);
            }
        }

        private void UpdateDeleteSub(bool exec, double posX, double posY) {
            if (posX < 0 || mCanvas.ActualWidth <= posX) {
                return;
            }
            if (posY < 0 || mCanvas.ActualHeight <= posY) {
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

            var pInf = TestHit(posX, posY, pointSz);
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

        private void MovePointSub(MoveProcMode mm, double posX, double posY) {
            if (posX < 0 || mCanvas.ActualWidth <= posX) {
                return;
            }
            if (posY < 0 || mCanvas.ActualHeight <= posY) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            Console.WriteLine("MovePointSub {0} ({1:0.0} {2:0.0}) nPoints={3} {4}", mm, posX, posY, mPoints.Count, mMoveState);

            if (mEllipseTmp != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mEllipseTmp);
            }

            switch (mm) {
            case MoveProcMode.Hover:
                {   // 当たったとき、赤い点を出す。
                    var pInf = TestHit(posX, posY, pointSz);
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
            case MoveProcMode.Select: {
                    var pInf = TestHit(posX, posY, pointSz);
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
                    Canvas.SetLeft(el, posX - pointSz / 2);
                    Canvas.SetTop(el,  posY - pointSz / 2);
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
                        Canvas.SetLeft(el, posX - pointSz / 2);
                        Canvas.SetTop(el, posY - pointSz / 2);
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
                        Canvas.SetLeft(el, posX - pointSz / 2);
                        Canvas.SetTop(el, posY - pointSz / 2);

                        mPoints.Add(new PointInf(el, posX, posY));
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

            var pos = e.GetPosition(mCanvas);
            switch (mMode) {
            case Mode.ModeAddPointEdge:
                UpdatePointSub(false, pos.X, pos.Y);
                break;
            case Mode.ModeDeletePont:
                UpdateDeleteSub(false, pos.X, pos.Y);
                break;
            case Mode.ModeMovePoint:
                MovePointSub(MoveProcMode.Select, pos.X, pos.Y);
                break;
            }
        }

        WWVectorD2 mPrevPos = null;

        private void mCanvas_MouseMove(object sender, MouseEventArgs e) {
            var pos = e.GetPosition(mCanvas);
            if (mPrevPos != null && WWVectorD2.Distance(mPrevPos, new WWVectorD2(pos.X, pos.Y)) < 1.0) {
                return;
            }
            mPrevPos = new WWVectorD2(pos.X, pos.Y);

            if (mMode == Mode.ModeMovePoint) {
                MovePointSub((e.LeftButton == MouseButtonState.Pressed) ? MoveProcMode.Move : MoveProcMode.Hover, pos.X, pos.Y);
                return;
            }

            // Moveではないモードのときは、マウスがホバーしているときだけ処理がある。
            if (e.LeftButton != MouseButtonState.Released) {
                return;
            }

            switch (mMode) {
            case Mode.ModeAddPointEdge:
                UpdatePointSub(false, pos.X, pos.Y);
                break;
            case Mode.ModeDeletePont:
                UpdateDeleteSub(false, pos.X, pos.Y);
                break;
            }
        }

        private void mCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Released) {
                return;
            }

            var pos = e.GetPosition(mCanvas);

            switch (mMode) {
            case Mode.ModeAddPointEdge:
                UpdatePointSub(true, pos.X, pos.Y);
                break;
            case Mode.ModeDeletePont:
                UpdateDeleteSub(true, pos.X, pos.Y);
                break;
            case Mode.ModeMovePoint:
                MovePointSub(MoveProcMode.Exec, pos.X, pos.Y);
                break;
            }
        }

    }
}
