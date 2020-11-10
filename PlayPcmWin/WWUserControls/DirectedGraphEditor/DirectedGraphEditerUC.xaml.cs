// 日本語。
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using WWMath;

namespace WWUserControls {
    public partial class DirectedGraphEditerUC : UserControl {
        public DirectedGraphEditerUC() {
            InitializeComponent();
        }

        private bool mInitialized = false;

        private DrawParams mDP;
        private PointProc mPP;
        private EdgeProc mEP;
        private DataGridPointProc mDataGridPointProc;
        private DataGridEdgeProc mDataGridEdgeProc;

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            mDP = new DrawParams();
            mDP.mCanvas = mCanvas;
            mDP.mGridSz = int.Parse(mTextBoxGridSize.Text);
            mDP.mPointSz = int.Parse(mTextBoxPointSize.Text);
            mDP.mArrowSz = int.Parse(mTextBoxArrowSize.Text);

            mPP = new PointProc(mDP);
            mEP = new EdgeProc(mDP, mPP);

            mDataGridPointProc = new DataGridPointProc(mDataGridP, mPP.PointParamChanged);
            mDataGridEdgeProc = new DataGridEdgeProc(mDataGridE, mEP.EdgeParamChanged);

            RedrawGrid();
            UpdateDescription();
        }

        /// <summary>
        /// 頂点のリストを戻す。
        /// </summary>
        public List<PointInf> PointList() {
            return mPP.mPointList;
        }

        /// <summary>
        /// エッジのリストを戻す。
        /// </summary>
        public List<Edge> EdgeList() {
            return mEP.mEdgeList;
        }

        /// <summary>
        /// アースされている点。
        /// idxに-1が指定されていたらどこもアースしない。
        /// </summary>
        public PointInf EarthedPoint() {
            PointInf p = null;

            int idx = 0;
            if (!int.TryParse(mTextBoxEarthPointIdx.Text, out idx)) {
                // 文字ではない物が入力された。
                MessageBox.Show(
                    string.Format("Error: Earth Point idx parse error."));
            } else {
                if (idx == -1) {
                    // pIdx == -1が指定された：意図的にアース無し。
                } else {
                    p = mPP.FindPointByIdx(idx, PointProc.FindPointMode.FindFromPointList);
                    if (p == null) {
                        // 指定された番号のpが存在しない。
                        MessageBox.Show(
                            string.Format("Error: Earth Point idx specified not found. idx={0}", idx));
                    }
                }
            }

            mPP.UpdateEarthedPoint(p);

            return p;
        }

        List<Line> mGridLines = new List<Line>();

        private void RedrawGrid() {
            foreach (var p in mGridLines) {
                mCanvas.Children.Remove(p);
            }
            mGridLines.Clear();

            for (int x = 0; x < mCanvas.ActualWidth; x += mDP.mGridSz) {
                var p = DrawUtil.NewLine(new WWVectorD2(x, 0), new WWVectorD2(x, mCanvas.ActualHeight), mDP.mGridBrush);
                Canvas.SetZIndex(p, mDP.Z_Grid);
                mCanvas.Children.Add(p);
                mGridLines.Add(p);
            }

            for (int y = 0; y < mCanvas.ActualHeight; y += mDP.mGridSz) {
                var p = DrawUtil.NewLine(new WWVectorD2(0, y), new WWVectorD2(mCanvas.ActualWidth, y), mDP.mGridBrush);
                Canvas.SetZIndex(p, mDP.Z_Grid);
                mCanvas.Children.Add(p);
                mGridLines.Add(p);
            }
        }

        enum Mode {
            ModeSetFirstPoint,
            ModeAddEdge,
            ModeDeletePointEdge,
        };

        Mode mMode = Mode.ModeSetFirstPoint;

        private void UpdateDescription() {
            if (!mInitialized) {
                return;
            }

            switch (mMode) {
            case Mode.ModeSetFirstPoint:
                mLabelDescription.Content = "Left click to set start point of edge.";
                break;
            case Mode.ModeAddEdge:
                mLabelDescription.Content = "Left click to add new point with edge. Right click to stop adding edge.";
                break;
            case Mode.ModeDeletePointEdge:
                mLabelDescription.Content = "Left click existing point / edge to delete it.";
                break;
            }
        }

        private void UpdateGraphStatus() {
            mLabelNumOfPoints.Content = string.Format("Num of Points = {0}", mPP.mPointList.Count);
            mLabelNumOfEdges.Content = string.Format("Num of Edges = {0}", mEP.mEdgeList.Count);
        }

        private void DeletePointEdgeMouseMove(WWVectorD2 pos) {
            // 選択状態色を一旦通常色に戻します。
            DeletePointEdgeCancel();

            var e = mEP.FindNearestEdge(pos);
            var p = mPP.TestHit(pos, mDP.mPointSz);

            if (e != null) {
                if (p != null) {
                    // 近いほうがヒット。
                    if (mEP.EdgeDistanceFromPos(e, pos, 1.0, PointProc.FindPointMode.FindFromPointList)
                            < WWVectorD2.Distance(p.xy, pos)) {
                        // eのほうが近い。
                        mEP.EdgeChangeColor(e, mDP.mBrightBrush);
                        mEP.mTmpEdge = e;
                    } else {
                        // pのほうが近い。
                        mPP.PointChangeColor(p, mDP.mBrightBrush);
                        mPP.mFromPoint = p;
                    }
                } else {
                    // エッジ。
                    mEP.EdgeChangeColor(e, mDP.mBrightBrush);
                    mEP.mTmpEdge = e;
                }
            } else if (p != null) {
                // p。
                mPP.PointChangeColor(p, mDP.mBrightBrush);
                mPP.mFromPoint = p;
            }
        }

        /// <summary>
        /// 始点が存在し、マウスがホバーしているとき、一時的エッジの描画位置を更新する。
        /// </summary>
        private void TmpEdgeRedrawMouseMove(WWVectorD2 pos) {
            mPP.MouseMoveUpdateTmpPoint(pos);
            mEP.MouseMoveUpdateTmpEdge(pos);
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // コマンド。(アンドゥー用)

#region Command

        /// <summary>
        /// ポイントやエッジを追加/削除するコマンド。
        /// </summary>
        class Command {
            public PointInf point;
            public Edge edge;
            //public int beforeIdx;
            //public int afterIdx;

            public enum CommandType {
                AddPoint,
                DeletePoint,
                AddEdge,
                DeleteEdge,
                ChangePointIdx,
            };
            public CommandType cmd;

            /// <summary>
            /// 点の追加、削除コマンド。
            /// CommandDo()で実行、CommandUndo()でアンドゥーする。
            /// AddPoint: pは、描画物=nullで、xyを指定して作成し渡す。e=null。
            /// AddEdge: p=null、eは描画物=nullで始点番号、終点番号を指定して作成し渡す。
            /// </summary>
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

            public void Add(Command c) {
                commandList.Add(c);
            }
        };

        void CommandAtomicDo(CommandAtomic ca) {
            foreach (var c in ca.commandList) {
                CommandDo(c);
            }
        }

        void CommandAtomicUndo(CommandAtomic ca) {
            foreach (var c in ca.commandList.Reverse<Command>()) {
                CommandUndo(c);
            }
        }

        /// <summary>
        /// アンドゥー用リスト。
        /// </summary>
        List<CommandAtomic> mCommandList = new List<CommandAtomic>();

        CommandAtomic mCommandAtomic = null;

        private void AddCmdToUndoList(CommandAtomic ca) {
            mCommandList.Add(ca);
            mButtonUndo.IsEnabled = true;
        }

        private void DeleteEdgesByPointIdx(int pIdx) {
            System.Diagnostics.Debug.Assert(mCommandAtomic != null);

            var delEdgeList = new List<Edge>();

            foreach (var e in mEP.mEdgeList) {
                if (e.fromPointIdx == pIdx
                        || e.toPointIdx == pIdx) {
                    delEdgeList.Add(e);
                }
            }

            foreach (var e in delEdgeList) {
                // アンドゥー用リストに追加。
                var cmd = new Command(Command.CommandType.DeleteEdge, null, e);
                CommandDo(cmd);
                mCommandAtomic.Add(cmd);
            }
        }

        private void CommandDo(Command c) {
            switch (c.cmd) {
            case Command.CommandType.DeleteEdge:
                // エッジを削除。
                mDataGridEdgeProc.EdgeRemoved(c.edge);
                mEP.EdgeDrawablesRemove(c.edge);
                mEP.mEdgeList.Remove(c.edge);
                break;
            case Command.CommandType.AddEdge:
                // エッジを追加。
                mEP.EdgeDrawablesCreate(c.edge, mDP.mBrush);
                mEP.mEdgeList.Add(c.edge);
                mDataGridEdgeProc.EdgeAdded(c.edge);
                break;
            case Command.CommandType.DeletePoint:
                // 点を削除。
                mDataGridPointProc.PointRemoved(c.point);
                mPP.PointDrawableRemove(c.point);
                mPP.mPointList.Remove(c.point);
                break;
            case Command.CommandType.AddPoint:
                // 点を追加。
                mPP.PointDrawableCreate(c.point, mDP.mBrush);
                mPP.mPointList.Add(c.point);
                mDataGridPointProc.PointAdded(c.point);
                break;
            case Command.CommandType.ChangePointIdx:
                throw new System.NotImplementedException();
                /*
                // エッジリストに入っている点番号を更新する。
                foreach (var e in mEP.mEdgeList) {
                    if (e.fromPointIdx == c.beforeIdx) {
                        e.fromPointIdx = c.afterIdx;
                    }
                    if (e.toPointIdx == c.beforeIdx) {
                        e.toPointIdx = c.afterIdx;
                    }
                }
                */
            }

            UpdateGraphStatus();
        }

        private void CommandUndo(Command c) {
            switch (c.cmd) {
            case Command.CommandType.AddEdge:
                // エッジを削除。
                mDataGridEdgeProc.EdgeRemoved(c.edge);
                mEP.EdgeDrawablesRemove(c.edge);
                mEP.mEdgeList.Remove(c.edge);
                break;
            case Command.CommandType.DeleteEdge:
                // エッジを追加。
                mEP.EdgeDrawablesCreate(c.edge, mDP.mBrush);
                mEP.mEdgeList.Add(c.edge);
                mDataGridEdgeProc.EdgeAdded(c.edge);
                break;
            case Command.CommandType.AddPoint:
                // 点を削除。
                mDataGridPointProc.PointRemoved(c.point);
                mPP.PointDrawableRemove(c.point);
                mPP.mPointList.Remove(c.point);
                break;
            case Command.CommandType.DeletePoint:
                // 点を追加。
                mPP.PointDrawableCreate(c.point, mDP.mBrush);
                mPP.mPointList.Add(c.point);
                mDataGridPointProc.PointAdded(c.point);
                break;
            case Command.CommandType.ChangePointIdx:
                throw new System.NotImplementedException();
                /*
                // エッジリストに入っている点番号を更新する。
                foreach (var e in mEP.mEdgeList) {
                    if (e.fromPointIdx == c.afterIdx) {
                        e.fromPointIdx = c.beforeIdx;
                    }
                    if (e.toPointIdx == c.afterIdx) {
                        e.toPointIdx = c.beforeIdx;
                    }
                }
                */
            }

            UpdateGraphStatus();
        }

#endregion Command

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // イベントハンドラー。

        enum TmpDrawablesRemoveOpt {
            RemoveFromPoint = 1,
            RemoveToPoint = 2,
            RemoveEdge = 4,
            RemoveAll = 7
        };

        /// <summary>
        /// 一時点を削除する。
        /// </summary>
        /// <param name="opt">TmpDrawablesRemoveOptの組み合わせ。</param>
        private void TmpDrawablesRemove(int opt) {
            if (0 != (opt & (int)TmpDrawablesRemoveOpt.RemoveFromPoint)) {
                mPP.TmpFromPointRemove();
            }

            if (0 != (opt & (int)TmpDrawablesRemoveOpt.RemoveToPoint)) {
                mPP.TmpToPointRemove();
            }

            if (0 != (opt & (int)TmpDrawablesRemoveOpt.RemoveEdge)) {
                mEP.TmpEdgeRemove();
            }
        }

        /// <summary>
        /// 始点未決定状態で左クリック：始点を決定する。
        /// </summary>
        private void SetFirstPointLeftClicked(WWVectorD2 pos) {
            mPP.TmpFromPointRemove();

            // クリック地点に確定の点を作る。
            var pInf = mPP.TestHit(pos, mDP.mPointSz);
            if (pInf == null) {
                // クリックした場所には点は未だ無い。
                // 確定の始点を追加する → pInf。
                pInf = new PointInf(pos);
                var cmd = new Command(Command.CommandType.AddPoint, pInf, null);
                CommandDo(cmd);
                AddCmdToUndoList(new CommandAtomic(cmd));
                mPP.mFromPoint = pInf;
            } else {
                // クリック地点に確定の点あり。
                mPP.mFromPoint = pInf;
            }

            // エッジ追加モードに遷移。
            mMode = Mode.ModeAddEdge;
            UpdateDescription();
        }

        private void DeleteEdge(Edge e) {
            var cmd = new Command(Command.CommandType.DeleteEdge, null, e);
            CommandDo(cmd);

            // アンドゥー用リストに追加。
            AddCmdToUndoList(new CommandAtomic(cmd));
            UpdateGraphStatus();
        }

        /// <summary>
        /// 点と、点につながるエッジをすべて消す。
        /// </summary>
        private void DeletePoint(PointInf p) {
            var ca = new CommandAtomic();

            // 点番号idxに接続しているエッジをすべて消す。
            foreach (var e in mEP.mEdgeList) {
                if (e.fromPointIdx == p.Idx
                        || e.toPointIdx == p.Idx) {
                    var cmd = new Command(Command.CommandType.DeleteEdge, null, e);
                    ca.Add(cmd);
                }
            }

            {   // 点idxを消す。
                var cmd = new Command(Command.CommandType.DeletePoint, p, null);
                ca.Add(cmd);
            }

            CommandAtomicDo(ca);

            AddCmdToUndoList(ca);
            UpdateGraphStatus();
        }

        /// <summary>
        /// DeletePointEdgeモードで、選択色になっている点とエッジを通常色に戻し、選択状態を解除。
        /// </summary>
        private void DeletePointEdgeCancel() {
            mEP.TmpEdgeChangeColor(mDP.mBrush);
            mEP.mTmpEdge = null;
            mPP.FromPointChangeColor(mDP.mBrush);
            mPP.mFromPoint = null;
        }

        /// <summary>
        /// 削除モード：点またはエッジを消す
        /// </summary>
        private void DeletePointEdgeLeftClicked(WWVectorD2 pos) {
            DeletePointEdgeCancel();

            var e = mEP.FindNearestEdge(pos);
            var p = mPP.TestHit(pos, mDP.mPointSz);

            if (e != null) {
                if (p != null) {
                    // 近いほうがヒット。
                    if (mEP.EdgeDistanceFromPos(e, pos, 1.0, PointProc.FindPointMode.FindFromPointList)
                            < WWVectorD2.Distance(p.xy, pos)) {
                        // eのほうが近い。
                        DeleteEdge(e);
                    } else {
                        // pのほうが近い。
                        DeletePoint(p);
                    }
                } else {
                    // エッジ。
                    DeleteEdge(e);
                }
            } else if (p != null) {
                // p。
                DeletePoint(p);
            }
        }

        /// <summary>
        /// 始点決定状態で左クリック：終点を決定する。始点から終点に向かうエッジを追加。終点が新たに始点となる。
        ///   既存点を左クリックしたとき、点の追加を行わずエッジを追加する。
        /// </summary>
        private void PointAddLeftClicked(WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X
                    || pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                // Canvas外のクリック。
                return;
            }

            // 始点決定状態。
            System.Diagnostics.Debug.Assert(mPP.mFromPoint != null);

            // 始点決定状態で左クリック：終点を決定する。始点から終点に向かうエッジを追加。終点が新たに始点となる。
            // 既存点を左クリックしたとき、点の追加を行わずエッジを追加する。

            var ca = new CommandAtomic();

            // クリック地点に確定点が存在するか？
            var pInf = mPP.TestHit(pos, mDP.mPointSz);
            if (pInf == null) {
                // クリックした場所には確定点は未だ無い。

                // 仮の終点を削除。
                TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveToPoint);
                mPP.mToPoint = null;

                // 確定の終点を追加する。
                pInf = new PointInf(pos);
                var cmd = new Command(Command.CommandType.AddPoint, pInf, null);
                CommandDo(cmd);
                ca.Add(cmd);
            } else if (WWVectorD2.Distance(pInf.xy, mPP.mFromPoint.xy) < 0.5) {
                // クリックした点が、始点と同じ点。
                // 特に何もしないで戻る。
                return;
            }

            // クリック地点に始点とは異なる終点pInfが存在する状態。
            // 始点の色を通常色にする。
            mPP.PointChangeColor(mPP.mFromPoint, mDP.mBrush);

            var edge = mEP.FindEdge(mPP.mFromPoint.Idx, pInf.Idx, EdgeProc.FEOption.SamePosition);
            if (edge == null) {
                // 始点→終点のエッジが無いので追加。
                var cmd = new Command(Command.CommandType.AddEdge, null, new Edge(mPP.mFromPoint.Idx, pInf.Idx));
                CommandDo(cmd);
                ca.Add(cmd);
            }

            // コマンドが集まったのでアンドゥーリストに足す。
            if (0 < ca.commandList.Count) {
                AddCmdToUndoList(ca);
            }

            // クリックした点を新たな始点にする。
            mPP.mFromPoint = pInf;
            mPP.PointChangeColor(mPP.mFromPoint, mDP.mBrightBrush);
        }

        /// <summary>
        /// 始点決定状態で右クリック：始点が未決定の状態に遷移。
        /// </summary>
        private void PointAddRightClicked(WWVectorD2 pos) {
            TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveAll);
            if (mMode == Mode.ModeAddEdge) {
                // Edge追加モードのとき、始点設定モードに遷移。
                mMode = Mode.ModeSetFirstPoint;
                UpdateDescription();
            }
        }

        private void RedrawAll() {
            if (!int.TryParse(mTextBoxPointSize.Text, out mDP.mPointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }
            if (!int.TryParse(mTextBoxArrowSize.Text, out mDP.mArrowSz)) {
                MessageBox.Show("Error: Arrow Size parse error!");
                return;
            }
            if (!int.TryParse(mTextBoxGridSize.Text, out mDP.mGridSz)) {
                MessageBox.Show("Error: Grid Size parse error!");
                return;
            }

            RedrawGrid();
            mPP.RedrawPoints();
            mEP.RedrawAllEdges();
        }

        private void mRadioButtonAddPoint_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            // 遷移前のモード。
            switch (mMode) {
            case Mode.ModeDeletePointEdge:
                DeletePointEdgeCancel();
                break;
            }

            mMode = Mode.ModeSetFirstPoint;
            Cursor = Cursors.Cross;
            UpdateDescription();

            TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveAll);
        }

        private void mRadioButtonDeletePoint_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mMode = Mode.ModeDeletePointEdge;
            Cursor = Cursors.Cross;
            UpdateDescription();

            TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveAll);
        }

        private void CanvasMouseDownLeft(MouseButtonEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            var pos = PointProc.SnapToGrid(posExact.X, posExact.Y, mDP.mGridSz);

            switch (mMode) {
            case Mode.ModeSetFirstPoint:
                SetFirstPointLeftClicked(pos);
                break;
            case Mode.ModeAddEdge:
                PointAddLeftClicked(pos);
                break;
            case Mode.ModeDeletePointEdge:
                DeletePointEdgeLeftClicked(pos);
                break;
            }
        }

        private void CanvasMouseDownRight(MouseButtonEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            var pos = PointProc.SnapToGrid(posExact.X, posExact.Y, mDP.mGridSz);

            switch (mMode) {
            case Mode.ModeAddEdge:
                PointAddRightClicked(pos);
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

        /// <summary>
        /// 始点が存在するとき
        ///   マウスがホバーしたら
        ///     一時的エッジを描画。
        /// </summary>
        private void CanvasMouseMove(MouseEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            var pos = PointProc.SnapToGrid(posExact.X, posExact.Y, mDP.mGridSz);

            if (e.LeftButton == MouseButtonState.Released) {
                switch (mMode) {
                case Mode.ModeSetFirstPoint:
                    mPP.SetFirstPointMouseMove(pos);
                    break;
                case Mode.ModeAddEdge:
                    TmpEdgeRedrawMouseMove(pos);
                    break;
                case Mode.ModeDeletePointEdge:
                    DeletePointEdgeMouseMove(pos);
                    break;
                }
            } else {
                // マウスが左ドラッグ。
            }

        }

        WWVectorD2 mPrevPos = null;

        /// <summary>
        /// マウス移動イベントハンドラー。
        /// </summary>
        private void mCanvas_MouseMove(object sender, MouseEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            if (mPrevPos != null && WWVectorD2.Distance(mPrevPos,
                    new WWVectorD2(posExact.X, posExact.Y)) < 1.0) {
                // マウス移動量が小さい場合、何もしない。
                return;
            }
            mPrevPos = new WWVectorD2(posExact.X, posExact.Y);

            CanvasMouseMove(e);
        }

        private void mButtonResize_Click(object sender, RoutedEventArgs e) {
            int prevGridSz = mDP.mGridSz;
            if (!int.TryParse(mTextBoxGridSize.Text, out mDP.mGridSz) || mDP.mGridSz <= 3) {
                MessageBox.Show("Error: Grid Size parse error. Grid Size should be integer larger than 3");
                mDP.mGridSz = prevGridSz;
                mTextBoxGridSize.Text = string.Format("{0}", mDP.mGridSz);
                return;
            }

            double ratio = (double)mDP.mGridSz / prevGridSz;

            RedrawGrid();

            foreach (var p in mPP.mPointList) {
                mPP.PointDrawableRemove(p);

                p.xy = p.xy.Scale(ratio);

                mPP.PointDrawableCreate(p, mDP.mBrush);
            }

            foreach (var edge in mEP.mEdgeList) {
                mEP.EdgeDrawablesRemove(edge);
                mEP.EdgeDrawablesCreate(edge, mDP.mBrush);
            }
        }

        private void mCanvas_MouseLeave(object sender, MouseEventArgs e) {
            switch (mMode) {
            case Mode.ModeSetFirstPoint:
                TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveAll);
                break;
            case Mode.ModeAddEdge:
                TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveAll);
                mMode = Mode.ModeSetFirstPoint;
                UpdateDescription();
                break;

            case Mode.ModeDeletePointEdge:
                DeletePointEdgeCancel();
                break;
            }
        }

        private void mButtonUndo_Click(object sender, RoutedEventArgs e) {
            TmpDrawablesRemove((int)TmpDrawablesRemoveOpt.RemoveAll);

            // アンドゥー用リストから1個コマンドを取り出し
            // アンドゥーする。
            var ca = mCommandList[mCommandList.Count - 1];
            mCommandList.RemoveAt(mCommandList.Count - 1);

            CommandAtomicUndo(ca);

            mEP.RedrawAllEdges();

            mButtonUndo.IsEnabled = 0 < mCommandList.Count;

            if (mMode == Mode.ModeAddEdge) {
                // Edge追加モードのとき、始点設定モードに遷移。
                mMode = Mode.ModeSetFirstPoint;
                UpdateDescription();
            }
        }

        private void mButtonRedraw_Click(object sender, RoutedEventArgs e) {
            RedrawAll();
        }

        private void mDataGridP_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            mDataGridPointProc.CellEditEnding(sender, e);
        }
        private void mDataGridE_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            mDataGridEdgeProc.CellEditEnding(sender, e);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            RedrawGrid();
        }
    }
}
