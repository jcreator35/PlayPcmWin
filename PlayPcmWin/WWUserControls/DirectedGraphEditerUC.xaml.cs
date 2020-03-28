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

        private bool mInitialized = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            int gridSz = int.Parse(mTextBoxGridSize.Text);

            RedrawGrid(gridSz);
            UpdateDescription();
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
            ModeDeletePont,
            ModeAddEdge,
            ModeDeleteEdge,
        };

        Mode mMode = Mode.ModeAddPoint;


        private void UpdateDescription() {
            if (!mInitialized) {
                return;
            }

            switch (mMode) {
            case Mode.ModeAddPoint:
                mLabelDescription.Content = "Left click to add new point.";
                break;
            case Mode.ModeMovePoint:
                mLabelDescription.Content = "Press left mouse button and drag existing point.";
                break;
            case Mode.ModeDeletePont:
                mLabelDescription.Content = "Left click existing point to delete.";
                break;
            case Mode.ModeAddEdge:
                mLabelDescription.Content = "Left click and connect existing point. Right Click to cancel.";
                break;
            case Mode.ModeDeleteEdge:
                mLabelDescription.Content = "Left click edge to delete.";
                break;
            }
        }

        private void UpdateGraphStatus() {
            mLabelNumOfPoints.Content = string.Format("Num of Points = {0}", mPointList.Count);
            mLabelNumOfEdges.Content = string.Format("Num of Edges = {0}", mEdgeList.Count);
        }

        Brush mGridBrush = new SolidColorBrush(Colors.LightGray);
        Brush mPointBrush = new SolidColorBrush(Colors.Black);
        Brush mTmpBrush = new SolidColorBrush(Colors.Blue);
        Brush mErrBrush = new SolidColorBrush(Colors.Red);

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Pointの処理。

        PointInf mTmpDrawPoint = null;

        class PointInf {
            private int idx;
            public int Idx {
                get {
                    return idx;
                }
            }
            public Ellipse ellipse;
            public WWVectorD2 xy;
            private static int mNextPointIdx = 100;

            public PointInf(Ellipse e, double x, double y) {
                idx = mNextPointIdx++;
                ellipse = e;
                xy = new WWVectorD2(x, y);
            }
        }

        List<PointInf> mPointList = new List<PointInf>();

        class Edge {
            public int fromPointIdx;
            public int toPointIdx;
            public Line line;
            public Edge(Line aLine, int from, int to) {
                line = aLine;
                fromPointIdx = from;
                toPointIdx = to;
            }
        };

        List<Edge> mEdgeList = new List<Edge>();

        /// <summary>
        /// ポイントやエッジを追加/削除するコマンド。
        /// </summary>
        class Command {
            public PointInf point;
            public Edge edge;
            public int beforeIdx;
            public int afterIdx;

            public enum CommandType {
                AddPoint,
                DeletePoint,
                AddEdge,
                DeleteEdge,
                ChangePointIdx,
            };
            public CommandType cmd;

            public Command(CommandType c, PointInf p, Edge e) {
                cmd = c;
                point = p;
                edge = e;
            }
        };

        /// <summary>
        /// アンドゥー単位となるコマンド列。
        /// </summary>
        class CommandAtomic {
            public List<Command> commandList = new List<Command>();
            public CommandAtomic(Command c) {
                commandList = new List<Command>();
                commandList.Add(c);
            }

            public CommandAtomic() {
            }
        };

        /// <summary>
        /// アンドゥー用リスト。
        /// </summary>
        List<CommandAtomic> mCommandList = new List<CommandAtomic>();

        private void AddCmdToUndoList(CommandAtomic ca) {
            mCommandList.Add(ca);
            mButtonUndo.IsEnabled = true;
        }


        private Edge FindEdge(int idxFrom, int idxTo) {
            foreach (var e in mEdgeList) {
                if (e.fromPointIdx == idxFrom
                    && e.toPointIdx == idxTo) {
                    return e;
                }
            }

            return null;
        }

        private PointInf FindPointByIdx(int idx) {
            foreach (var p in mPointList) {
                if (p.Idx == idx) {
                    return p;
                }
            }
            return null;
        }

        private Edge FindEdge(WWVectorD2 pos) {
            foreach (var e in mEdgeList) {
                var p1 = FindPointByIdx(e.fromPointIdx);
                var p2 = FindPointByIdx(e.toPointIdx);

                if (WWVectorD2.Distance(pos, p1.xy) < 1) {
                    return e;
                }
                if (WWVectorD2.Distance(pos, p2.xy) < 1) {
                    return e;
                }
            }

            return null;
        }

        CommandAtomic mCommandAtomic = null;

        private void DeleteEdgesByPointIdx(int pIdx) {
            System.Diagnostics.Debug.Assert(mCommandAtomic != null);

            var delEdgeList = new List<Edge>();

            foreach (var e in mEdgeList) {
                if (e.fromPointIdx == pIdx
                        || e.toPointIdx == pIdx) {
                    delEdgeList.Add(e);
                }
            }

            foreach (var e in delEdgeList) {
                // アンドゥー用リストに追加。
                var cmd = new Command(Command.CommandType.DeleteEdge, null, e);
                CommandDo(cmd, 0);
                mCommandAtomic.commandList.Add(cmd);
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

        private void mRadioButtonAddPoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeAddPoint;
            Cursor = Cursors.Cross;
            UpdateDescription();
            CancelAddEdge();
        }

        private void mRadioButtonMovePoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeMovePoint;
            Cursor = Cursors.Cross;
            UpdateDescription();
            CancelAddEdge();
        }

        private void mRadioButtonDeletePoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeDeletePont;
            Cursor = Cursors.Cross;
            UpdateDescription();
            CancelAddEdge();
        }

        private void mRadioButtonAddEdge_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeAddEdge;
            Cursor = Cursors.Cross;
            UpdateDescription();
            CancelAddEdge();
        }

        private void mRadioButtonDeleteEdge_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeDeleteEdge;
            Cursor = Cursors.Cross;
            UpdateDescription();
            CancelAddEdge();
        }

        /// <returns>既存の点と当たったら既存点のPointInfを戻す。</returns>
        private PointInf TestHit(double x, double y, double threshold) {
            var xy = new WWVectorD2(x, y);

            foreach (var p in mPointList) {
                if (WWVectorD2.Distance(p.xy, xy) < threshold) {
                    return p;
                }
            }

            return null;
        }

        private void UpdatePointSub(bool exec, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X
                    || pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                if (mTmpDrawPoint != null) {
                    // 既存のtmp点を消す。
                    mCanvas.Children.Remove(mTmpDrawPoint.ellipse);
                }
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            var pInf = TestHit(pos.X, pos.Y, pointSz);

            if (mTmpDrawPoint != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mTmpDrawPoint.ellipse);
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
                    var point = new PointInf(el, pos.X, pos.Y);
                    var cmd = new Command(Command.CommandType.AddPoint, point, null);
                    CommandDo(cmd, pointSz);
                    AddCmdToUndoList(new CommandAtomic(cmd));
                    UpdateGraphStatus();
                }
            } else {
                // 一時的点表示を更新。
                var point = new PointInf(el, pos.X, pos.Y);
                mTmpDrawPoint = point;

                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, pos.X - pointSz / 2);
                Canvas.SetTop(el,  pos.Y - pointSz / 2);
            }
        }

        private void UpdateDeleteSub(bool exec, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X
                    || pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            if (mTmpDrawPoint != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mTmpDrawPoint.ellipse);
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
                System.Diagnostics.Debug.Assert(mCommandAtomic == null);
                mCommandAtomic = new CommandAtomic();

                // 消えた点のpInf.idx番号を参照しているエッジをすべて削除。
                DeleteEdgesByPointIdx(pInf.Idx);

                // 削除コマンドを完成してアンドゥー用リストに追加。
                var cmd = new Command(Command.CommandType.DeletePoint, pInf, null);
                CommandDo(cmd, pointSz);
                mCommandAtomic.commandList.Add(cmd);

                AddCmdToUndoList(mCommandAtomic);
                mCommandAtomic = null;

                UpdateGraphStatus();

            } else {
                // 一時的点表示を更新。
                var point = new PointInf(el, pInf.xy.X, pInf.xy.Y);
                mTmpDrawPoint = point;

                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
                Canvas.SetTop(el,  pInf.xy.Y - pointSz / 2);
            }
        }

        enum MovePointState {
            Init,
            Selected,
        };

        MovePointState mMoveState = MovePointState.Init;

        enum MovePointMode {
            Hover,
            Select,
            Move,
            Exec,
        };

        private PointInf mMoveBeforePoint = null;

        /// <summary>
        /// 一時的ポイント移動を反映しエッジ描画更新。
        /// </summary>
        private void RedrawEdge(PointInf removedPoint, PointInf addedPoint) {
            foreach (var e in mEdgeList) {
                PointInf p1 = null;
                if (e.fromPointIdx == removedPoint.Idx) {
                    p1 = addedPoint;
                } else {
                    p1 = FindPointByIdx(e.fromPointIdx);
                }

                PointInf p2 = null;
                if (e.toPointIdx == removedPoint.Idx) {
                    p2 = addedPoint;
                } else {
                    p2 = FindPointByIdx(e.toPointIdx);
                }

                mCanvas.Children.Remove(e.line);
                e.line = null;

                var l = new Line();
                l.X1 = p1.xy.X;
                l.Y1 = p1.xy.Y;
                l.X2 = p2.xy.X;
                l.Y2 = p2.xy.Y;
                l.Stroke = mPointBrush;
                e.line = l;
                mCanvas.Children.Add(l);
            }
        }

        /// <summary>
        /// 点のidxが変わったとき、edgeの点idxを更新する。
        /// </summary>
        private void EdgeListReplacePointIdx(int beforeIdx, int afterIdx) {
            foreach (var e in mEdgeList) {
                if (e.fromPointIdx == beforeIdx) {
                    e.fromPointIdx = afterIdx;
                }
                if (e.toPointIdx == beforeIdx) {
                    e.toPointIdx = afterIdx;
                }
            }
        }

        private void RedrawEdge() {
            foreach (var e in mEdgeList) {
                var p1 = FindPointByIdx(e.fromPointIdx);
                var p2 = FindPointByIdx(e.toPointIdx);

                mCanvas.Children.Remove(e.line);
                e.line = null;

                var l = new Line();
                l.X1 = p1.xy.X;
                l.Y1 = p1.xy.Y;
                l.X2 = p2.xy.X;
                l.Y2 = p2.xy.Y;
                l.Stroke = mPointBrush;
                e.line = l;
                mCanvas.Children.Add(l);
            }
        }

        private void MovePointSub(MovePointMode mm, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X
                    || pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                return;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            Console.WriteLine("MovePointSub {0} ({1:0.0} {2:0.0}) nPoints={3} {4}", mm, pos.X, pos.Y, mPointList.Count, mMoveState);

            if (mTmpDrawPoint != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mTmpDrawPoint.ellipse);
            }

            switch (mm) {
            case MovePointMode.Hover:
                {   // 当たったとき、赤い点を出す。
                    var pInf = TestHit(pos.X, pos.Y, pointSz);
                    if (pInf == null) {
                        return;
                    }

                    var el = new Ellipse();
                    el.Width = pointSz;
                    el.Height = pointSz;
                    el.Fill = mErrBrush;

                    var point = new PointInf(el, pInf.xy.X, pInf.xy.Y);
                    mTmpDrawPoint = point;

                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
                    Canvas.SetTop(el,  pInf.xy.Y - pointSz / 2);
                }
                break;
            case MovePointMode.Select:
                {
                    var pInf = TestHit(pos.X, pos.Y, pointSz);
                    if (pInf == null) {
                        mMoveState = MovePointState.Init;
                        return;
                    }

                    mMoveState = MovePointState.Selected;

                    // 当たった点を消す。
                    mMoveBeforePoint = pInf;
                    var cmd = new Command(Command.CommandType.DeletePoint, pInf, null);
                    CommandDo(cmd, 0);

                    // 移動コマンドの前半(点削除)作成。
                    System.Diagnostics.Debug.Assert(mCommandAtomic == null);
                    mCommandAtomic = new CommandAtomic(cmd);

                    // マウスポインター位置に赤い点を出す。
                    var el = new Ellipse();
                    el.Width = pointSz;
                    el.Height = pointSz;
                    el.Fill = mErrBrush;

                    var point = new PointInf(el, pos.X, pos.Y);
                    mTmpDrawPoint = point;

                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, pos.X - pointSz / 2);
                    Canvas.SetTop(el,  pos.Y - pointSz / 2);
                }
                break;
            case MovePointMode.Move:
                {
                    if (mMoveState == MovePointState.Selected) {
                        // マウスポインター位置に赤い点を出す。
                        var el = new Ellipse();
                        el.Width = pointSz;
                        el.Height = pointSz;
                        el.Fill = mErrBrush;

                        var point = new PointInf(el, pos.X, pos.Y);
                        
                        mTmpDrawPoint = point;

                        mCanvas.Children.Add(el);
                        Canvas.SetLeft(el, pos.X - pointSz / 2);
                        Canvas.SetTop(el, pos.Y - pointSz / 2);

                        RedrawEdge(mMoveBeforePoint, point);
                    }
                }
                break;
            case MovePointMode.Exec:
                {
                    if (mMoveState == MovePointState.Selected) {
                        // マウスポインター位置の点を追加。
                        var el = new Ellipse();
                        el.Width = pointSz;
                        el.Height = pointSz;
                        el.Fill = mPointBrush;
                        var point = new PointInf(el, pos.X, pos.Y);

                        {
                            var cmd = new Command(Command.CommandType.AddPoint, point, null);
                            CommandDo(cmd, pointSz);

                            // 移動コマンドを完成してアンドゥー用リストに追加。
                            System.Diagnostics.Debug.Assert(mCommandAtomic != null);
                            mCommandAtomic.commandList.Add(cmd);
                        }

                        {
                            // 点の移動とともにエッジも移動する。
                            // 点が更新されたので、エッジの点idxを更新する。
                            EdgeListReplacePointIdx(mMoveBeforePoint.Idx, point.Idx);
                            var cmd = new Command(Command.CommandType.ChangePointIdx, null, null);
                            cmd.beforeIdx = mMoveBeforePoint.Idx;
                            cmd.afterIdx = point.Idx;

                            CommandDo(cmd, 0);
                            mCommandAtomic.commandList.Add(cmd);
                        }

                        AddCmdToUndoList(mCommandAtomic);
                        mCommandAtomic = null;

                        RedrawEdge();

                        mMoveState = MovePointState.Init;
                    }
                }
                break;
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Edgeの処理。

        Ellipse mTmpEdgeP1 = null;
        Ellipse mTmpEdgeP2 = null;

        PointInf mEdgeFirstPos = null;
        Line mTmpEdge = null;

        private void AddEdgeMouseDown(WWVectorD2 pos) {
            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            var pInf = TestHit(pos.X, pos.Y, pointSz);

            Console.WriteLine("AddEdgeMouseDown ({0:0.0} {0:0.0})", pos.X, pos.Y);

            if (mEdgeFirstPos != null) {
                // 既に1点目が選択済み。
                return;
            }

            // 1個目の頂点の選択。
            if (pInf == null) {
                return;
            }

            mEdgeFirstPos = pInf;

            // マウスポインター位置に青い点を出す。
            if (mTmpEdgeP1 != null) {
                mCanvas.Children.Remove(mTmpEdgeP1);
                mTmpEdgeP1 = null;
            }
            var el = new Ellipse();
            el.Width = pointSz;
            el.Height = pointSz;
            el.Fill = mTmpBrush;
            mTmpEdgeP1 = el;
            mCanvas.Children.Add(el);
            Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
            Canvas.SetTop(el, pInf.xy.Y - pointSz / 2);
        }

        private void AddEdgeMouseMove(WWVectorD2 pos) {
            Console.WriteLine("AddEdgeMouseMove ({0:0.0} {0:0.0})", pos.X, pos.Y);

            if (mTmpEdgeP2 != null) {
                mCanvas.Children.Remove(mTmpEdgeP2);
                mTmpEdgeP2 = null;
            }

            if (mTmpEdge != null) {
                mCanvas.Children.Remove(mTmpEdge);
                mTmpEdge = null;
            }

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            var pInf = TestHit(pos.X, pos.Y, pointSz);

            if (mEdgeFirstPos == null) {
                // 1点目の候補 pInf。
                if (pInf == null) {
                    return;
                } else {
                    // 1点目の候補をハイライト表示する。
                    if (mTmpEdgeP1 != null) {
                        mCanvas.Children.Remove(mTmpEdgeP1);
                        mTmpEdgeP1 = null;
                    }
                    var el = new Ellipse();
                    el.Width = pointSz;
                    el.Height = pointSz;
                    el.Fill = mTmpBrush;
                    mTmpEdgeP1 = el;
                    mCanvas.Children.Add(el);
                    Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
                    Canvas.SetTop(el, pInf.xy.Y - pointSz / 2);
                }
                return;
            }

            // 2点目の候補 pInf。

            if (pInf == null) {
                // 点を指していない。
                // 赤い線を引く。
                var l = new Line();
                l.X1 = mEdgeFirstPos.xy.X;
                l.Y1 = mEdgeFirstPos.xy.Y;
                l.X2 = pos.X;
                l.Y2 = pos.Y;
                l.Stroke = mErrBrush;
                mTmpEdge = l;
                mCanvas.Children.Add(l);
                return;
            }

            if (mEdgeFirstPos == pInf) {
                // 線が引けない。
                return;
            }

            {   // 線を引くことができる。
                var l = new Line();
                l.X1 = mEdgeFirstPos.xy.X;
                l.Y1 = mEdgeFirstPos.xy.Y;
                l.X2 = pInf.xy.X;
                l.Y2 = pInf.xy.Y;
                l.Stroke = mTmpBrush;
                mTmpEdge = l;
                mCanvas.Children.Add(l);

                // 点を出す。
                var el = new Ellipse();
                el.Width = pointSz;
                el.Height = pointSz;
                el.Fill = mTmpBrush;
                mTmpEdgeP2 = el;
                mCanvas.Children.Add(el);
                Canvas.SetLeft(el, pInf.xy.X - pointSz / 2);
                Canvas.SetTop(el, pInf.xy.Y - pointSz / 2);
            }
        }

        private void AddEdgeMouseUp(WWVectorD2 pos) {
            Console.WriteLine("AddEdgeMouseUp ({0:0.0} {0:0.0})", pos.X, pos.Y);

            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            if (mTmpEdge != null) {
                mCanvas.Children.Remove(mTmpEdge);
                mTmpEdge = null;
            }

            var pInf = TestHit(pos.X, pos.Y, pointSz);

            if (pInf == null) {
                // 点を指していない。
                return;
            }

            if (mEdgeFirstPos == pInf) {
                // 線が引けない。
                return;
            }

            // 終点がある == pInf。

            if (mTmpEdgeP1 != null) {
                mCanvas.Children.Remove(mTmpEdgeP1);
                mTmpEdgeP1 = null;
            }
            if (mTmpEdgeP2 != null) {
                mCanvas.Children.Remove(mTmpEdgeP2);
                mTmpEdgeP2 = null;
            }

            if (mEdgeFirstPos == null) {
                return;
            }

            if (null != FindEdge(mEdgeFirstPos.Idx, pInf.Idx)) {
                // 既に有るので足さない。
                CancelAddEdge();
            }

            // 線を引く。
            var l = new Line();
            l.X1 = mEdgeFirstPos.xy.X;
            l.Y1 = mEdgeFirstPos.xy.Y;
            l.X2 = pInf.xy.X;
            l.Y2 = pInf.xy.Y;
            l.Stroke = mPointBrush;

            var edge = new Edge(l, mEdgeFirstPos.Idx, pInf.Idx);
            var cmd = new Command(Command.CommandType.AddEdge, null, edge);
            CommandDo(cmd, 0);

            // アンドゥー用リストに追加。
            AddCmdToUndoList(new CommandAtomic(cmd));

            UpdateGraphStatus();

            mEdgeFirstPos = null;
        }

        private Edge FindNearestEdge(WWVectorD2 pos) {
            Edge nearestEdge = null;
            double nearestDistance = double.MaxValue;
            double margin = 1.0;

            foreach (var e in mEdgeList) {
                var p1 = FindPointByIdx(e.fromPointIdx);
                var p2 = FindPointByIdx(e.toPointIdx);

                // 直線の方程式 ax + by + c = 0
                double dx = p2.xy.X - p1.xy.X;
                double dy = p2.xy.Y - p1.xy.Y;
                System.Diagnostics.Debug.Assert(dx != 0 || dy != 0);

                double a = dy;
                double b = -dx;
                double c = p2.xy.X * p1.xy.Y - p2.xy.Y * p1.xy.X;

                // 直線と点の距離。
                double distance = Math.Abs(a * pos.X + b * pos.Y + c) / Math.Sqrt(a * a + b * b);
                double nearestPointOnLineX = (b * (+b * pos.X - a * pos.Y) - a * c) / (a * a + b * b);
                double nearestPointOnLineY = (a * (-b * pos.X + a * pos.Y) - b * c) / (a * a + b * b);
                if ((margin + nearestPointOnLineX < p1.xy.X && margin + nearestPointOnLineX < p2.xy.X)
                        || (margin + p1.xy.X < nearestPointOnLineX && margin + p2.xy.X < nearestPointOnLineX)
                        || (margin + nearestPointOnLineY < p1.xy.Y && margin + nearestPointOnLineY < p2.xy.Y)
                        || (margin + p1.xy.Y < nearestPointOnLineY && margin + p2.xy.Y < nearestPointOnLineY)) {
                    // 点と最短距離の直線上の点が線分の範囲外。
                    continue;
                }

                if (distance < nearestDistance) {
                    // 現時点で最も距離が近いエッジ。
                    nearestDistance = distance;
                    nearestEdge = e;
                }
            }

            return nearestEdge;
        }

        private void DeleteEdgeMouseDown(WWVectorD2 pos) {
            if (mTmpEdge != null) {
                mCanvas.Children.Remove(mTmpEdge);
                mTmpEdge = null;
            }

            var e = FindNearestEdge(pos);
            if (e == null) {
                return;
            }

            var cmd = new Command(Command.CommandType.DeleteEdge, null, e);
            CommandDo(cmd, 0);

            // アンドゥー用リストに追加。
            AddCmdToUndoList(new CommandAtomic(cmd));

            UpdateGraphStatus();
        }

        private void DeleteEdgeMouseMove(WWVectorD2 pos) {
            if (mTmpEdge != null) {
                mCanvas.Children.Remove(mTmpEdge);
                mTmpEdge = null;
            }

            var e = FindNearestEdge(pos);
            if (e == null) {
                return;
            }

            var p1 = FindPointByIdx(e.fromPointIdx);
            var p2 = FindPointByIdx(e.toPointIdx);

            // エッジを強調表示する。
            var l = new Line();
            l.X1 = p1.xy.X;
            l.Y1 = p1.xy.Y;
            l.X2 = p2.xy.X;
            l.Y2 = p2.xy.Y;
            l.Stroke = mErrBrush;
            mTmpEdge = l;
            mCanvas.Children.Add(l);
        }

        private void CanvasMouseDownLeft(MouseButtonEventArgs e) {
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
                MovePointSub(MovePointMode.Select, pos);
                break;
            case Mode.ModeAddEdge:
                AddEdgeMouseDown(pos);
                break;
            case Mode.ModeDeleteEdge:
                DeleteEdgeMouseDown(new WWVectorD2(posExact.X, posExact.Y));
                break;
            }
        }

        private void CancelAddEdge() {
            if (mTmpEdgeP1 != null) {
                mCanvas.Children.Remove(mTmpEdgeP1);
                mTmpEdgeP1 = null;
            }
            if (mTmpEdgeP2 != null) {
                mCanvas.Children.Remove(mTmpEdgeP2);
                mTmpEdgeP2 = null;
            }
            if (mTmpEdge != null) {
                mCanvas.Children.Remove(mTmpEdge);
                mTmpEdge = null;
            }
            mEdgeFirstPos = null;
        }

        private void CanvasMouseDownRight(MouseButtonEventArgs e) {
            switch (mMode) {
            case Mode.ModeAddEdge:
                CancelAddEdge();
                break;
            }
        }

        private void mCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                CanvasMouseDownLeft(e);
                return;
            }
            if (e.RightButton == MouseButtonState.Pressed) {
                CanvasMouseDownRight(e);
                return;
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
                MovePointSub((e.LeftButton == MouseButtonState.Pressed) ? MovePointMode.Move : MovePointMode.Hover,
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
            case Mode.ModeAddEdge:
                AddEdgeMouseMove(pos);
                break;
            case Mode.ModeDeleteEdge:
                DeleteEdgeMouseMove(new WWVectorD2(posExact.X, posExact.Y));
                break;
            }
        }

        private void CanvasMouseUpLeft(MouseButtonEventArgs e) {
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
                MovePointSub(MovePointMode.Exec, pos);
                break;
            case Mode.ModeAddEdge:
                AddEdgeMouseUp(pos);
                break;
            }
        }

        private void mCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Released) {
                CanvasMouseUpLeft(e);
                return;
            }
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

            foreach (var p in mPointList) {
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

            mPointList = newPoints;
        }

        private void mCanvas_MouseLeave(object sender, MouseEventArgs e) {
            if (mTmpDrawPoint != null) {
                // tmp点を消す。
                mCanvas.Children.Remove(mTmpDrawPoint.ellipse);
                mTmpDrawPoint = null;
            }
        }

        private void CommandDo(Command c, int pointSz) {
            switch (c.cmd) {
            case Command.CommandType.DeleteEdge:
                // エッジを削除。
                mCanvas.Children.Remove(c.edge.line);
                mEdgeList.Remove(c.edge);
                break;
            case Command.CommandType.AddEdge:
                // エッジを追加。
                mCanvas.Children.Add(c.edge.line);
                mEdgeList.Add(c.edge);
                break;
            case Command.CommandType.DeletePoint:
                // 点を削除。
                mCanvas.Children.Remove(c.point.ellipse);
                mPointList.Remove(c.point);
                break;
            case Command.CommandType.AddPoint:
                // 点を追加。
                mCanvas.Children.Add(c.point.ellipse);
                Canvas.SetLeft(c.point.ellipse, c.point.xy.X - pointSz / 2);
                Canvas.SetTop(c.point.ellipse, c.point.xy.Y - pointSz / 2);
                mPointList.Add(c.point);
                break;
            case Command.CommandType.ChangePointIdx:
                // エッジリストに入っている点番号を更新する。
                foreach (var e in mEdgeList) {
                    if (e.fromPointIdx == c.beforeIdx) {
                        e.fromPointIdx = c.afterIdx;
                    }
                    if (e.toPointIdx == c.beforeIdx) {
                        e.toPointIdx = c.afterIdx;
                    }
                }
                break;
            }
        }

        private void CommandUndo(Command c, int pointSz) {
            switch (c.cmd) {
            case Command.CommandType.AddEdge:
                // エッジを削除。
                mCanvas.Children.Remove(c.edge.line);
                mEdgeList.Remove(c.edge);
                break;
            case Command.CommandType.DeleteEdge:
                // エッジを追加。
                mCanvas.Children.Add(c.edge.line);
                mEdgeList.Add(c.edge);
                break;
            case Command.CommandType.AddPoint:
                // 点を削除。
                mCanvas.Children.Remove(c.point.ellipse);
                mPointList.Remove(c.point);
                break;
            case Command.CommandType.DeletePoint:
                // 点を追加。
                mCanvas.Children.Add(c.point.ellipse);
                Canvas.SetLeft(c.point.ellipse, c.point.xy.X - pointSz / 2);
                Canvas.SetTop(c.point.ellipse, c.point.xy.Y - pointSz / 2);
                mPointList.Add(c.point);
                break;
            case Command.CommandType.ChangePointIdx:
                // エッジリストに入っている点番号を更新する。
                foreach (var e in mEdgeList) {
                    if (e.fromPointIdx == c.afterIdx) {
                        e.fromPointIdx = c.beforeIdx;
                    }
                    if (e.toPointIdx == c.afterIdx) {
                        e.toPointIdx = c.beforeIdx;
                    }
                }
                break;
            }
        }

        private void mButtonUndo_Click(object sender, RoutedEventArgs e) {
            int pointSz = 6;
            if (!int.TryParse(mTextBoxPointSize.Text, out pointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }

            CancelAddEdge();

            // アンドゥー用リストから1個コマンドを取り出し
            // アンドゥーする。
            var ca = mCommandList[mCommandList.Count - 1];
            mCommandList.RemoveAt(mCommandList.Count - 1);

            foreach (var c in ca.commandList.Reverse<Command>()) {
                CommandUndo(c, pointSz);
            }

            RedrawEdge();

            mButtonUndo.IsEnabled = 0 < mCommandList.Count;

        }

    }
}
