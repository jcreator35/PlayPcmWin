// 日本語。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private int mPointSz = 12;
        private int mArrowSz = 12;
        private int mGridSz = 16;

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            mPointSz = int.Parse(mTextBoxPointSize.Text);
            mArrowSz = int.Parse(mTextBoxArrowSize.Text);
            mGridSz  = int.Parse(mTextBoxGridSize.Text);

            RedrawGrid();
            UpdateDescription();
        }

        List<Line> mGridLines = new List<Line>();

        private void RedrawGrid() {
            foreach (var p in mGridLines) {
                mCanvas.Children.Remove(p);
            }
            mGridLines.Clear();

            for (int x = 0; x < mCanvas.ActualWidth; x += mGridSz) {
                var p = NewLine(new WWVectorD2(x, 0), new WWVectorD2(x, mCanvas.ActualHeight), mGridBrush);
                mCanvas.Children.Add(p);

                mGridLines.Add(p);
            }

            for (int y = 0; y < mCanvas.ActualHeight; y += mGridSz) {
                var p = NewLine(new WWVectorD2(0, y), new WWVectorD2(mCanvas.ActualWidth, y), mGridBrush);
                mCanvas.Children.Add(p);

                mGridLines.Add(p);
            }
        }

        enum Mode {
            ModeAddPoint,
            ModeDeletePont,
        };

        Mode mMode = Mode.ModeAddPoint;

        private void UpdateDescription() {
            if (!mInitialized) {
                return;
            }

            switch (mMode) {
            case Mode.ModeAddPoint:
                mLabelDescription.Content = "Left click to add new point with edge. Right click to stop adding edge.";
                break;
            case Mode.ModeDeletePont:
                mLabelDescription.Content = "Left click existing point / edge to delete it.";
                break;
            }
        }

        private void UpdateGraphStatus() {
            mLabelNumOfPoints.Content = string.Format("Num of Points = {0}", mPointList.Count);
            mLabelNumOfEdges.Content = string.Format("Num of Edges = {0}", mEdgeList.Count);
        }

        Brush mGridBrush   = new SolidColorBrush(Colors.LightGray);
        Brush mBrush       = new SolidColorBrush(Colors.Black);
        Brush mBrightBrush = new SolidColorBrush(Colors.SlateBlue);
        Brush mErrBrush    = new SolidColorBrush(Colors.Red);

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Pointの処理。

#region PointInf

        class PointInf {
            private int idx;
            public int Idx {
                get {
                    return idx;
                }
            }
            public Ellipse drawable;
            public WWVectorD2 xy;
            private static int mNextPointIdx = 100;

            /// <summary>
            /// 点。Idx, 位置(x,y)と描画物eがある。
            /// 一時的な点は描画物eを作って渡す。
            /// 確定した点は、e = nullで作成し、コマンド実行時に描画物を作る。
            /// </summary>
            public PointInf(Ellipse e, double x, double y)
                : this(e, new WWVectorD2(x, y)) {
            }

            /// <summary>
            /// 点。Idx, 位置aXYと描画物eがある。
            /// 一時的な点は描画物eを作って渡す。
            /// 確定した点は、e = nullで作成し、コマンド実行時に描画物を作る。
            /// </summary>
            public PointInf(Ellipse e, WWVectorD2 aXY) {
                idx = mNextPointIdx++;
                drawable = e;
                xy = aXY;
            }
        }

        List<PointInf> mPointList = new List<PointInf>();

        PointInf mFromPoint = null;
        PointInf mToPoint = null;

        /// <summary>
        /// 点の描画物をキャンバスから削除し、点の描画物自体も削除。
        /// </summary>
        private PointInf PointDrawableRemove(PointInf p) {
            if (p != null && p.drawable != null) {
                mCanvas.Children.Remove(p.drawable);
                p.drawable = null;
                Console.WriteLine("Point drawable removed");
            }
            return p;
        }

        /// <summary>
        /// 点の描画物を作り、キャンバスに追加する。
        /// </summary>
        private PointInf PointDrawableCreate(PointInf pi, Brush brush) {
            System.Diagnostics.Debug.Assert(pi.drawable == null);

            pi.drawable = new Ellipse();
            pi.drawable.Width  = mPointSz;
            pi.drawable.Height = mPointSz;
            pi.drawable.Fill   = brush;

            Canvas.SetLeft(pi.drawable, pi.xy.X - mPointSz / 2);
            Canvas.SetTop( pi.drawable, pi.xy.Y - mPointSz / 2);
            mCanvas.Children.Add(pi.drawable);
            Console.WriteLine("Point drawable added");
            return pi;
        }

        /// <summary>
        /// 点pの描画色を変更し、キャンバスに追加。
        /// </summary>
        private PointInf PointChangeColor(PointInf p, Brush brush) {
            if (p.drawable == null) {
                PointDrawableCreate(p, brush);
                return p;
            }

            // 点描画物有り。色を変える。
            p.drawable.Fill = brush;
            return p;
        }

        /// <summary>
        /// 新しいPointInfを作り、キャンバスに追加。
        /// 一時的な点用。
        /// </summary>
        private PointInf NewPoint(WWVectorD2 pos, Brush brush) {
            var pInf = new PointInf(null, pos);
            PointDrawableCreate(pInf, brush);
            return pInf;
        }

        /// <summary>
        /// 確定の点、または仮の点。
        /// </summary>
        private PointInf FindPointByIdx(int idx) {
            // 確定の点。
            foreach (var p in mPointList) {
                if (p.Idx == idx) {
                    return p;
                }
            }

            // 仮の点。
            if (mFromPoint != null && mFromPoint.Idx == idx) {
                return mFromPoint;
            }
            if (mToPoint != null && mToPoint.Idx == idx) {
                return mToPoint;
            }

            return null;
        }

        /// <summary>
        /// グリッドにアラインする。
        /// </summary>
        private WWVectorD2 SnapToGrid(double xD, double yD, int gridSz) {
            int x = (int)xD;
            x = ((x + gridSz / 2) / gridSz) * gridSz;

            int y = (int)yD;
            y = ((y + gridSz / 2) / gridSz) * gridSz;

            return new WWVectorD2(x, y);
        }

        /// <summary>
        /// リストに点xyが存在するか。
        /// </summary>
        private bool PointExists(List<PointInf> points, WWVectorD2 xy) {
            foreach (var p in points) {
                if (WWVectorD2.Distance(p.xy, xy) < 1) {
                    return true;
                }
            }

            return false;
        }

        /// <returns>既存の点と当たったら既存点のPointInfを戻す。</returns>
        private PointInf TestHit(double x, double y, double threshold) {
            return TestHit(new WWVectorD2(x, y), threshold);
        }

        /// <returns>既存の点と当たったら既存点のPointInfを戻す。</returns>
        private PointInf TestHit(WWVectorD2 xy, double threshold) {
            foreach (var p in mPointList) {
                if (WWVectorD2.Distance(p.xy, xy) < threshold) {
                    return p;
                }
            }

            return null;
        }

        /// <summary>
        /// すべての点を再描画する。
        /// ①既存点をキャンバスから削除。
        /// ②新しい点を作成、キャンバスに追加。
        /// </summary>
        private void RedrawPoints() {
            foreach (var p in mPointList) {
                PointDrawableRemove(p);
                PointDrawableCreate(p, mBrush);
            }
        }

#endregion PointInf

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // エッジ。

#region Edge

        class Edge {
            public int fromPointIdx;
            public int toPointIdx;
            public Line line;
            public Polygon arrow;

            /// <summary>
            /// 一時的エッジの場合、aLine, aArrowを作って渡す。
            /// CommandDo()に渡すエッジの場合、aLine, aArrowをnullで作成。
            /// </summary>
            public Edge(Line aLine, Polygon aArrow, int from, int to) {
                line = aLine;
                arrow = aArrow;
                fromPointIdx = from;
                toPointIdx = to;
            }
        };

        List<Edge> mEdgeList = new List<Edge>();

        Edge mTmpEdge = null;

        /// <summary>
        /// エッジの線描画物作成。
        /// </summary>
        private Line NewLine(WWVectorD2 xy1, WWVectorD2 xy2, Brush stroke) {
            var l = new Line();
            l.X1 = xy1.X;
            l.Y1 = xy1.Y;
            l.X2 = xy2.X;
            l.Y2 = xy2.Y;
            l.Stroke = stroke;
            return l;
        }

        private void MouseMoveUpdateTmpPoint(WWVectorD2 pos) {
            if (mFromPoint == null) {
                // 始点が無い。
                Console.WriteLine("MMP FP none");
                return;
            }

            // 始点有り。
            Console.WriteLine("MMP ({0:0.0} {0:0.0})", pos.X, pos.Y);

            var toPoint = TestHit(pos, mPointSz);
            if (toPoint == null) {
                // 始点が存在し、マウスポインタ位置に確定の終点が無い。
                if (mToPoint != null && WWVectorD2.Distance(mToPoint.xy, pos) < 1) {
                    // マウスポインタ位置に仮の終点mToPointが存在。
                    Console.WriteLine("MMP already toPoint");
                } else {
                    // 仮の終点位置が異なるので作り直す。
                    PointDrawableRemove(mToPoint);
                    mToPoint = null;
                    mToPoint = NewPoint(pos, mBrightBrush);
                    Console.WriteLine("MMP create toPoint");
                }
            } else {
                // マウスポインタ位置に確定の終点が存在する。
                // 仮の終点は不要。
                PointDrawableRemove(mToPoint);
                mToPoint = null;
            }
        }

        private void MouseMoveUpdateTmpEdge(WWVectorD2 pos) {
            if (mFromPoint == null) {
                // 始点が無い。
                Console.WriteLine("MME FP none");
                return;
            }

            // 始点mFromPoint有り。
            Console.WriteLine("MME ({0:0.0} {0:0.0})", pos.X, pos.Y);

            // TmpEdgeの始点p1と終点p2
            PointInf p1 = null;
            PointInf p2 = null;
            if (mTmpEdge != null) {
                p1 = FindPointByIdx(mTmpEdge.fromPointIdx);
                p2 = FindPointByIdx(mTmpEdge.toPointIdx);
            }

            // マウスポインタ位置に確定の終点toPointがあるか。
            var toPoint = TestHit(pos, mPointSz);
            if (toPoint == null) {
                // マウスポインタ位置に確定の終点が無い。
                // この場合、確定のエッジは無い。
                if (mToPoint != null && WWVectorD2.Distance(mToPoint.xy, pos) < 1) {
                    // マウスポインタ位置に仮の終点mToPointが存在する。
                    Console.WriteLine("MME already toPoint");

                    if (p1 == mFromPoint && p2 == mToPoint) {
                        // mFromPoint → mToPoint
                        // 仮のエッジが既に引かれている。
                        return;
                    }

                    // mFromPoint → mToPointのエッジを引く必要がある。
                    p1 = mFromPoint;
                    p2 = mToPoint;
                } else {
                    // マウスポインタ位置に確定の終点も仮の終点も無い。
                    // 画面外にマウスが行った場合？
                    // 仮のエッジがあれば消す。
                    EdgeDrawablesRemove(mTmpEdge);
                    return;
                }
            } else {
                // 確定の終点toPointがある。
                if (null != FindEdge(mFromPoint.Idx, toPoint.Idx, FEOption.SamePosition)) {
                    // 確定のエッジが既に引かれている。
                    // 仮のエッジがあれば消す。
                    EdgeDrawablesRemove(mTmpEdge);
                    return;
                }

                if (p1 == mFromPoint && p2 == toPoint) {
                    // mFromPoint → toPoint
                    // 仮のエッジが既に引かれている。
                    return;
                }

                // mFromPoint → toPointのエッジを引く必要がある。
                p1 = mFromPoint;
                p2 = toPoint;
            }

            // Edgeを作り直す。
            EdgeDrawablesRemove(mTmpEdge);
            mTmpEdge = NewEdge(p1, p2, mBrightBrush);
            Console.WriteLine("MME created edge");
            return;
        }

        /// <summary>
        /// 始点が存在し、マウスがホバーしているとき、一時的エッジの描画位置を更新する。
        /// </summary>
        private void TmpEdgeRedrawMouseMove(WWVectorD2 pos) {
            MouseMoveUpdateTmpPoint(pos);
            MouseMoveUpdateTmpEdge(pos);
        }

        /// <summary>
        /// エッジの矢印描画物作成。
        /// </summary>
        private Polygon NewArrowPoly(WWVectorD2 pos1, WWVectorD2 pos2, Brush stroke) {
            var dir2to1N = WWVectorD2.Sub(pos1, pos2).Normalize();
            var dir2to1S = dir2to1N.Scale(mArrowSz);

            // 2→1の方向と垂直のベクトル2つ。
            var dirA = new WWVectorD2(-dir2to1N.Y, dir2to1N.X).Scale(mArrowSz * 0.5);
            var dirB = new WWVectorD2(dir2to1N.Y, -dir2to1N.X).Scale(mArrowSz * 0.5);

            var vecA = WWVectorD2.Add(dir2to1S, dirA);
            var vecB = WWVectorD2.Add(dir2to1S, dirB);

            var pos2a = WWVectorD2.Add(pos2, dir2to1N.Scale(mPointSz / 2));
            var posA = WWVectorD2.Add(pos2a, vecA);
            var posB = WWVectorD2.Add(pos2a, vecB);

            var pc = new PointCollection();
            pc.Add(new Point(posB.X, posB.Y));
            pc.Add(new Point(pos2a.X, pos2a.Y));
            pc.Add(new Point(posA.X, posA.Y));

            var poly = new Polygon();
            poly.Points = pc;
            poly.Fill = stroke;
            poly.StrokeThickness = 0;
            return poly;
        }

        /// <summary>
        /// 始点から終点に向かうエッジを作り、キャンバスに登録。
        /// 一時的エッジ用。
        /// エッジ確定追加の場合 new Edge(null, null, fromIdx, toIdx)をCommandDoする。
        /// </summary>
        private Edge NewEdge(PointInf from, PointInf to, Brush brush) {
            var l = NewLine(from.xy, to.xy, brush);
            var arrow = NewArrowPoly(from.xy, to.xy, brush);

            mCanvas.Children.Add(l);
            mCanvas.Children.Add(arrow);

            var edge = new Edge(l, arrow, from.Idx, to.Idx);
            return edge;
        }

        /// <summary>
        /// エッジの描画物をキャンバスから削除し、描画物も削除。
        /// </summary>
        private void EdgeDrawablesRemove(Edge edge) {
            if (edge == null) {
                return;
            }
            if (edge.line != null) {
                mCanvas.Children.Remove(edge.line);
                edge.line = null;
            }
            if (edge.arrow != null) {
                mCanvas.Children.Remove(edge.arrow);
                edge.arrow = null;
            }
        }

        /// <summary>
        /// エッジの描画物を作る。
        /// 描画物が無い状態で呼んで下さい。
        /// </summary>
        private Edge EdgeDrawablesCreate(Edge edge, Brush brush) {
            System.Diagnostics.Debug.Assert(edge.line == null);
            System.Diagnostics.Debug.Assert(edge.arrow == null);

            var p1 = FindPointByIdx(edge.fromPointIdx);
            var p2 = FindPointByIdx(edge.toPointIdx);

            edge.line = NewLine(p1.xy, p2.xy, brush);
            edge.arrow = NewArrowPoly(p1.xy, p2.xy, brush);

            mCanvas.Children.Add(edge.line);
            mCanvas.Children.Add(edge.arrow);

            return edge;
        }

        enum FEOption {
            SamePosition,
            SamePointIdx,
        };

        /// <summary>
        /// 始点がidxFromで終点がidxToのエッジを戻す。
        /// </summary>
        private Edge FindEdge(int idxFrom, int idxTo, FEOption opt) {
            switch (opt) {
            case FEOption.SamePointIdx:
                foreach (var e in mEdgeList) {
                    if (e.fromPointIdx == idxFrom
                        && e.toPointIdx == idxTo) {
                        return e;
                    }
                }
                break;
            case FEOption.SamePosition: {
                    var p1 = FindPointByIdx(idxFrom);
                    var p2 = FindPointByIdx(idxTo);
                    foreach (var e in mEdgeList) {
                        var e1 = FindPointByIdx(e.fromPointIdx);
                        if (WWVectorD2.Distance(e1.xy, p1.xy) < 1) {
                            var e2 = FindPointByIdx(e.toPointIdx);
                            if (WWVectorD2.Distance(e2.xy, p2.xy) < 1) {
                                return e;
                            }
                        }
                    }
                }
                break;
            }

            return null;
        }

        /// <summary>
        /// 始点がfromで終点がtoのエッジを戻す。
        /// </summary>
        private Edge FindEdge(PointInf from, PointInf to, FEOption opt) {
            switch (opt) {
            case FEOption.SamePointIdx:
                foreach (var e in mEdgeList) {
                    if (e.fromPointIdx == from.Idx
                        && e.toPointIdx == to.Idx) {
                        return e;
                    }
                }
                break;
            case FEOption.SamePosition: {
                    foreach (var e in mEdgeList) {
                        var e1 = FindPointByIdx(e.fromPointIdx);
                        if (WWVectorD2.Distance(e1.xy, from.xy) < 1) {
                            var e2 = FindPointByIdx(e.toPointIdx);
                            if (WWVectorD2.Distance(e2.xy, to.xy) < 1) {
                                return e;
                            }
                        }
                    }
                }
                break;
            }

            return null;
        }

        /// <summary>
        /// 始点か終点がposのエッジを戻す。
        /// </summary>
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

        /// <summary>
        /// すべてのエッジを再描画する。
        /// ①既存エッジをキャンバスから削除。
        /// ②新しいエッジを作成。
        /// ③新エッジをキャンバスに追加。
        /// </summary>
        private void RedrawAllEdges() {
            foreach (var e in mEdgeList) {
                var p1 = FindPointByIdx(e.fromPointIdx);
                var p2 = FindPointByIdx(e.toPointIdx);

                mCanvas.Children.Remove(e.line);
                e.line = null;
                mCanvas.Children.Remove(e.arrow);
                e.arrow = null;

                var l = NewLine(p1.xy, p2.xy, mBrush);
                e.line = l;

                var poly = NewArrowPoly(p1.xy, p2.xy, mBrush);
                e.arrow = poly;

                mCanvas.Children.Add(l);
                mCanvas.Children.Add(poly);
            }
        }

        /// <summary>
        /// posに近い場所を横切るEdgeを調べる。
        /// </summary>
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

#endregion Edge

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // コマンド。(アンドゥー用)

#region Command

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
        };

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

            foreach (var e in mEdgeList) {
                if (e.fromPointIdx == pIdx
                        || e.toPointIdx == pIdx) {
                    delEdgeList.Add(e);
                }
            }

            foreach (var e in delEdgeList) {
                // アンドゥー用リストに追加。
                var cmd = new Command(Command.CommandType.DeleteEdge, null, e);
                CommandDo(cmd);
                mCommandAtomic.commandList.Add(cmd);
            }
        }

        private void CommandDo(Command c) {
            switch (c.cmd) {
            case Command.CommandType.DeleteEdge:
                // エッジを削除。
                EdgeDrawablesRemove(c.edge);
                mEdgeList.Remove(c.edge);
                break;
            case Command.CommandType.AddEdge:
                // エッジを追加。
                EdgeDrawablesCreate(c.edge, mBrush);
                mEdgeList.Add(c.edge);
                break;
            case Command.CommandType.DeletePoint:
                // 点を削除。
                PointDrawableRemove(c.point);
                mPointList.Remove(c.point);
                break;
            case Command.CommandType.AddPoint:
                // 点を追加。
                PointDrawableCreate(c.point, mBrush);
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

            UpdateGraphStatus();
        }

        private void CommandUndo(Command c) {
            switch (c.cmd) {
            case Command.CommandType.AddEdge:
                // エッジを削除。
                EdgeDrawablesRemove(c.edge);
                mEdgeList.Remove(c.edge);
                break;
            case Command.CommandType.DeleteEdge:
                // エッジを追加。
                EdgeDrawablesCreate(c.edge, mBrush);
                mEdgeList.Add(c.edge);
                break;
            case Command.CommandType.AddPoint:
                // 点を削除。
                PointDrawableRemove(c.point);
                mPointList.Remove(c.point);
                break;
            case Command.CommandType.DeletePoint:
                // 点を追加。
                PointDrawableCreate(c.point, mBrush);
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

            UpdateGraphStatus();
        }

#endregion Command

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // イベントハンドラー。

        /// <summary>
        /// 一時的点を削除。
        /// </summary>
        private void TmpDrawablesRemove() {
            if (mFromPoint != null) {
                if (PointExists(mPointList, mFromPoint.xy)) {
                    // mFromPoint地点に確定の点がある。
                    // 色を確定の色に変更。
                    PointChangeColor(mFromPoint, mBrush);
                    mFromPoint = null;
                } else {
                    // mFromPointは仮の点で、確定の点は無い。
                    PointDrawableRemove(mFromPoint);
                    mFromPoint = null;
                }
            }

            // mToPointは常に仮の点。
            PointDrawableRemove(mToPoint);

            // mTmpEdgeは常に仮のエッジ。
            EdgeDrawablesRemove(mTmpEdge);
        }

        /// <summary>
        /// 1:始点未決定状態で左クリック：始点を決定する。
        /// 2A:始点決定状態で左クリック：終点を決定する。始点から終点に向かうエッジを追加。終点が新たに始点となる。
        ///   既存点を左クリックしたとき、点の追加を行わずエッジを追加する。
        /// (2B:始点決定状態で右クリック：始点が未決定の状態に遷移。)
        /// </summary>
        private void PointAddLeftClicked(WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X
                    || pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                // Canvas外のクリック。
                // 一時的点と一時的エッジをキャンセル。
                PointDrawableRemove(mFromPoint);
                PointDrawableRemove(mToPoint);
                EdgeDrawablesRemove(mTmpEdge);
                return;
            }

            if (mFromPoint != null) {
                // 始点決定状態。
                // 2A:始点決定状態で左クリック：終点を決定する。始点から終点に向かうエッジを追加。終点が新たに始点となる。
                //   既存点を左クリックしたとき、点の追加を行わずエッジを追加する。

                // クリックした場所に点がすでに存在するか？
                var pInf = TestHit(pos, mPointSz);
                if (pInf == null) {
                    // クリックした場所には点は未だ無い。
                    // 点を追加する。
                    pInf = new PointInf(null, pos);
                    var cmd = new Command(Command.CommandType.AddPoint, pInf, null);
                    CommandDo(cmd);
                    AddCmdToUndoList(new CommandAtomic(cmd));
                } else if (WWVectorD2.Distance(pInf.xy, mFromPoint.xy) < 0.5) {
                    // クリックした点が、始点と同じ点。
                    // 特に何もしないで戻る。
                    return;
                }

                // クリック地点に始点とは異なる終点が存在する状態。
                // 始点の色を通常色にする。
                PointChangeColor(mFromPoint, mBrush);

                var edge = FindEdge(mFromPoint.Idx, pInf.Idx, FEOption.SamePosition);
                if (edge == null) {
                    // 始点→終点のエッジが無いので追加。
                    var cmd = new Command(Command.CommandType.AddEdge, null, new Edge(null, null, mFromPoint.Idx, pInf.Idx));
                    CommandDo(cmd);
                    AddCmdToUndoList(new CommandAtomic(cmd));
                }

                // クリックした点を新たな始点にする。
                mFromPoint = pInf;
                PointChangeColor(mFromPoint, mBrightBrush);

                return;
            } else {
                // 1:始点未決定状態で左クリック：始点を決定する。
                // クリックした場所に点がすでに存在するか？
                var pInf = TestHit(pos, mPointSz);
                if (pInf == null) {
                    // クリックした場所には点は未だ無い。
                    // 点を追加する → pInf。
                    pInf = new PointInf(null, pos);
                    var cmd = new Command(Command.CommandType.AddPoint, pInf, null);
                    CommandDo(cmd);
                    AddCmdToUndoList(new CommandAtomic(cmd));
                }

                // この点を始点とする。
                // 青色で表示する。
                mFromPoint = pInf;
                PointChangeColor(mFromPoint, mBrightBrush);

                return;
            }
        }

        /// <summary>
        /// 始点決定状態で右クリック：始点が未決定の状態に遷移。
        /// </summary>
        private void PointAddRightClicked(WWVectorD2 pos) {
            TmpDrawablesRemove();
        }

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
                mCanvas.Children.Remove(e.arrow);
                e.arrow = null;

                var l = NewLine(p1.xy, p2.xy, mBrush);
                e.line = l;

                var poly = NewArrowPoly(p1.xy, p2.xy, mBrush);
                e.arrow = poly;

                mCanvas.Children.Add(l);
                mCanvas.Children.Add(poly);
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

        private void RedrawAll() {
            if (!int.TryParse(mTextBoxPointSize.Text, out mPointSz)) {
                MessageBox.Show("Error: Point Size parse error!");
                return;
            }
            if (!int.TryParse(mTextBoxArrowSize.Text, out mArrowSz)) {
                MessageBox.Show("Error: Arrow Size parse error!");
                return;
            }
            if (!int.TryParse(mTextBoxGridSize.Text, out mGridSz)) {
                MessageBox.Show("Error: Grid Size parse error!");
                return;
            }

            RedrawGrid();
            RedrawPoints();
            RedrawAllEdges();
        }

        private void mRadioButtonAddPoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeAddPoint;
            Cursor = Cursors.Cross;
            UpdateDescription();
        }

        private void mRadioButtonDeletePoint_Checked(object sender, RoutedEventArgs e) {
            mMode = Mode.ModeDeletePont;
            Cursor = Cursors.Cross;
            UpdateDescription();
        }

        /*
        private void MovePointSub(MovePointMode mm, WWVectorD2 pos) {
            if (pos.X < 0 || mCanvas.ActualWidth <= pos.X
                    || pos.Y < 0 || mCanvas.ActualHeight <= pos.Y) {
                return;
            }

            Console.WriteLine("MovePointSub {0} ({1:0.0} {2:0.0}) nPoints={3} {4}",
                mm, pos.X, pos.Y, mPointList.Count, mMoveState);

            if (mFromPoint != null) {
                // 既存のtmp点を消す。
                mCanvas.Children.Remove(mFromPoint.drawable);
            }

            switch (mm) {
            case MovePointMode.Hover:
                {   // 当たったとき、赤い点を出す。
                    var pInf = TestHit(pos.X, pos.Y, mPointSz);
                    if (pInf == null) {
                        return;
                    }

                    var point = NewPoint(pInf.xy, mErrBrush);
                    mFromPoint = point;

                    mCanvas.Children.Add(point.drawable);
                    
                }
                break;
            case MovePointMode.Select:
                {
                    var pInf = TestHit(pos.X, pos.Y, mPointSz);
                    if (pInf == null) {
                        mMoveState = MovePointState.Init;
                        return;
                    }

                    mMoveState = MovePointState.Selected;

                    // 当たった点を消す。
                    mMoveBeforePoint = pInf;
                    var cmd = new Command(Command.CommandType.DeletePoint, pInf, null);
                    CommandDo(cmd);

                    // 移動コマンドの前半(点削除)作成。
                    System.Diagnostics.Debug.Assert(mCommandAtomic == null);
                    mCommandAtomic = new CommandAtomic(cmd);

                    // マウスポインター位置に赤い点を出す。
                    var el = NewPoint(pos, mErrBrush);

                    var point = new PointInf(el, pos.X, pos.Y);
                    mFromPoint = point;

                    mCanvas.Children.Add(el);
                }
                break;
            case MovePointMode.Move:
                {
                    if (mMoveState == MovePointState.Selected) {
                        // マウスポインター位置に赤い点を出す。
                        var el = NewPoint(pos, mErrBrush);

                        var point = new PointInf(el, pos.X, pos.Y);
                        
                        mFromPoint = point;

                        mCanvas.Children.Add(el);

                        RedrawEdge(mMoveBeforePoint, point);
                    }
                }
                break;
            case MovePointMode.Exec:
                {
                    if (mMoveState == MovePointState.Selected) {
                        // マウスポインター位置の点を追加。
                        var el = NewPoint(pos, mBrush);
                        var point = new PointInf(el, pos.X, pos.Y);

                        {
                            var cmd = new Command(Command.CommandType.AddPoint, point, null);
                            CommandDo(cmd);

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

                            CommandDo(cmd);
                            mCommandAtomic.commandList.Add(cmd);
                        }

                        AddCmdToUndoList(mCommandAtomic);
                        mCommandAtomic = null;

                        RedrawAllEdges();

                        mMoveState = MovePointState.Init;
                    }
                }
                break;
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Edgeの処理。

        private void AddEdgeMouseDown(WWVectorD2 pos) {
            var pInf = TestHit(pos.X, pos.Y, mPointSz);

            Console.WriteLine("AddEdgeMouseDown ({0:0.0} {0:0.0})", pos.X, pos.Y);

            if (mEdgeFromPoint != null) {
                // 既に1点目が選択済み。
                return;
            }

            // 1個目の頂点の選択。
            if (pInf == null) {
                return;
            }

            mEdgeFromPoint = pInf;

            // マウスポインター位置に青い点を出す。
            if (mTmpEdgeP1 != null) {
                mCanvas.Children.Remove(mTmpEdgeP1);
                mTmpEdgeP1 = null;
            }
            var el = NewPoint(pInf.xy, mTmpBrush);
            mTmpEdgeP1 = el;
            mCanvas.Children.Add(el);
        }

        private void AddEdgeMouseUp(WWVectorD2 pos) {
            Console.WriteLine("AddEdgeMouseUp ({0:0.0} {0:0.0})", pos.X, pos.Y);

            if (mTmpEdge != null) {
                mCanvas.Children.Remove(mTmpEdge);
                mTmpEdge = null;
            }

            var pInf = TestHit(pos.X, pos.Y, mPointSz);

            if (pInf == null) {
                // 点を指していない。
                return;
            }

            if (mEdgeFromPoint == pInf) {
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

            if (mEdgeFromPoint == null) {
                return;
            }

            if (null != FindEdge(mEdgeFromPoint.Idx, pInf.Idx)) {
                // 既に有るので足さない。
                CancelAddEdge();
            }

            // 線を引く。
            var edge = NewEdge(mEdgeFromPoint, pInf, mBrush);
            var cmd = new Command(Command.CommandType.AddEdge, null, edge);
            CommandDo(cmd);

            // アンドゥー用リストに追加。
            AddCmdToUndoList(new CommandAtomic(cmd));

            UpdateGraphStatus();

            mEdgeFromPoint = null;
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
            CommandDo(cmd);

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
            var l = NewLine(p1.xy, p2.xy, mErrBrush);
            mTmpEdge = l;
            mCanvas.Children.Add(l);
        }

         */

        private void CanvasMouseDownLeft(MouseButtonEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            var pos = SnapToGrid(posExact.X, posExact.Y, mGridSz);

            switch (mMode) {
            case Mode.ModeAddPoint:
                PointAddLeftClicked(pos);
                break;
            case Mode.ModeDeletePont:
                break;
            }
        }

        private void CanvasMouseDownRight(MouseButtonEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            var pos = SnapToGrid(posExact.X, posExact.Y, mGridSz);

            switch (mMode) {
            case Mode.ModeAddPoint:
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
            var pos = SnapToGrid(posExact.X, posExact.Y, mGridSz);

            /*
            if (mMode == Mode.ModeMovePoint) {
                MovePointSub(
                    (e.LeftButton == MouseButtonState.Pressed) ? MovePointMode.Move : MovePointMode.Hover,
                    pos);
                return;
            }
            */

            if (e.LeftButton == MouseButtonState.Released) {
                // マウスがホバー。
                // 一時的エッジの描画位置更新。
                TmpEdgeRedrawMouseMove(pos);
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

        /*
        private void CanvasMouseUpLeft(MouseButtonEventArgs e) {
            var posExact = e.GetPosition(mCanvas);
            var pos = SnapToGrid(posExact.X, posExact.Y, mGridSz);

            switch (mMode) {
            case Mode.ModeAddPoint:
                PointAddLeftClicked(true, pos);
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
        */

        private void mCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            /*
            if (e.LeftButton == MouseButtonState.Released) {
                CanvasMouseUpLeft(e);
                return;
            }
            */
        }

        private void mButtonSnapToGrid_Click(object sender, RoutedEventArgs e) {
            /*
            if (!int.TryParse(mTextBoxGridSize.Text, out mGridSz)) {
                MessageBox.Show("Error: Grid Size parse error");
                return;
            }

            RedrawGrid();

            var newPoints = new List<PointInf>();

            foreach (var p in mPointList) {
                mCanvas.Children.Remove(p.drawable);

                var xy = SnapToGrid(p.xy.X, p.xy.Y, mGridSz);
                if (PointExists(newPoints, xy)) {
                    // 同じ位置に点が重なっている。
                    // この点pは削除する。
                    continue;
                }

                var el = NewPoint(xy, mBrush);
                mCanvas.Children.Add(el);

                newPoints.Add(new PointInf(el, xy.X, xy.Y));
            }

            mPointList = newPoints;
            */
        }

        private void mCanvas_MouseLeave(object sender, MouseEventArgs e) {
            TmpDrawablesRemove();
        }

        private void mButtonUndo_Click(object sender, RoutedEventArgs e) {
            //CancelAddEdge();

            // アンドゥー用リストから1個コマンドを取り出し
            // アンドゥーする。
            var ca = mCommandList[mCommandList.Count - 1];
            mCommandList.RemoveAt(mCommandList.Count - 1);

            foreach (var c in ca.commandList.Reverse<Command>()) {
                CommandUndo(c);
            }

            RedrawAllEdges();

            mButtonUndo.IsEnabled = 0 < mCommandList.Count;
        }

        private void ButtonRedraw_Click(object sender, RoutedEventArgs e) {
            RedrawAll();
        }
    }
}
