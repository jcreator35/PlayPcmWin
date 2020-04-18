using System.Windows.Controls;
using System.Windows.Shapes;
using WWMath;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;
using System;
using System.Text;

namespace WWUserControls {
    class EdgeProc {
        private DrawParams mDP;
        private PointProc mPP;

        public List<Edge> mEdgeList = new List<Edge>();
        public Edge mTmpEdge = null;

        public EdgeProc(DrawParams dp, PointProc pp) {
            mDP = dp;
            mPP = pp;
        }

        /// <summary>
        /// TmpEdgeを消す。
        /// </summary>
        public void TmpEdgeRemove() {
            EdgeDrawablesRemove(mTmpEdge);
            mTmpEdge = null;
        }

        /// <summary>
        /// TmpEdgeが存在するとき色を変更する。
        /// </summary>
        public void TmpEdgeChangeColor(Brush b) {
            if (mTmpEdge != null) {
                EdgeChangeColor(mTmpEdge, b);
            }
        }

        /// <summary>
        /// 始点から終点に向かうエッジを作り、キャンバスに登録。
        /// 一時的エッジ用。
        /// エッジ確定追加の場合 new Edge(null, null, fromIdx, toIdx)をCommandDoする。
        /// </summary>
        private Edge NewEdge(PointInf from, PointInf to, Brush brush) {
            var edge = new Edge(from.Idx, to.Idx);
            EdgeDrawablesCreate(edge, brush);
            return edge;
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

        /// <summary>
        /// 一時的ポイント移動を反映しエッジ描画更新。
        /// </summary>
        public void RedrawEdge(PointInf removedPoint, PointInf addedPoint) {
            foreach (var e in mEdgeList) {
                PointInf p1 = null;
                if (e.fromPointIdx == removedPoint.Idx) {
                    p1 = addedPoint;
                } else {
                    p1 = mPP.FindPointByIdx(e.fromPointIdx, PointProc.FindPointMode.FindAll);
                }

                PointInf p2 = null;
                if (e.toPointIdx == removedPoint.Idx) {
                    p2 = addedPoint;
                } else {
                    p2 = mPP.FindPointByIdx(e.toPointIdx, PointProc.FindPointMode.FindAll);
                }

                mDP.mCanvas.Children.Remove(e.line);
                e.line = null;
                mDP.mCanvas.Children.Remove(e.arrow);
                e.arrow = null;

                var l = DrawUtil.NewLine(p1.xy, p2.xy, mDP.mBrush);
                e.line = l;

                var poly = NewArrowPoly(p1.xy, p2.xy, mDP.mBrush);
                e.arrow = poly;

                mDP.mCanvas.Children.Add(l);
                mDP.mCanvas.Children.Add(poly);
            }
        }

        /// <summary>
        /// エッジの描画物をキャンバスから削除し、描画物も削除。
        /// </summary>
        public void EdgeDrawablesRemove(Edge edge) {
            if (edge == null) {
                return;
            }
            if (edge.tbIdx != null) {
                mDP.mCanvas.Children.Remove(edge.tbIdx);
                edge.tbIdx = null;
            }
            if (edge.arrow != null) {
                mDP.mCanvas.Children.Remove(edge.arrow);
                edge.arrow = null;
            }
            if (edge.line != null) {
                mDP.mCanvas.Children.Remove(edge.line);
                edge.line = null;
            }
        }

        private static string EdgeDescriptionText(int idx, double C, double b) {
            var sb = new StringBuilder();
            sb.AppendFormat("e{0}\nC={1}", idx, C);
            if (0 != b) {
                sb.AppendFormat("\nb={0}", b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// エッジの描画物を作り、キャンバスに登録。
        /// 描画物が無い状態で呼んで下さい。
        /// </summary>
        public void EdgeDrawablesCreate(Edge edge, Brush brush) {
            System.Diagnostics.Debug.Assert(edge.line == null);
            System.Diagnostics.Debug.Assert(edge.arrow == null);

            var p1 = mPP.FindPointByIdx(edge.fromPointIdx, PointProc.FindPointMode.FindAll);
            var p2 = mPP.FindPointByIdx(edge.toPointIdx, PointProc.FindPointMode.FindAll);

            edge.line = DrawUtil.NewLine(p1.xy, p2.xy, brush);
            edge.arrow = NewArrowPoly(p1.xy, p2.xy, brush);

            Canvas.SetZIndex(edge.line, mDP.Z_Edge);
            Canvas.SetZIndex(edge.arrow, mDP.Z_Edge);
            mDP.mCanvas.Children.Add(edge.line);
            mDP.mCanvas.Children.Add(edge.arrow);

            // 文字を出す。
            var xy = WWVectorD2.Add(p1.xy, p2.xy).Scale(0.5);
            edge.tbIdx = new TextBlock();
            edge.tbIdx.Padding = new Thickness(2);
            edge.tbIdx.Text = EdgeDescriptionText(edge.EdgeIdx, edge.C, edge.B);
            edge.tbIdx.Foreground = mDP.mEdgeTextFgBrush;
            edge.tbIdx.Background = mDP.mEdgeTextBgBrush;
            edge.tbIdx.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tbWH = edge.tbIdx.DesiredSize;
            Canvas.SetLeft(edge.tbIdx, xy.X - tbWH.Width / 2);
            Canvas.SetTop(edge.tbIdx, xy.Y - tbWH.Height / 2);
            Canvas.SetZIndex(edge.tbIdx, mDP.Z_Edge + 1);
            mDP.mCanvas.Children.Add(edge.tbIdx);
        }

        /// <summary>
        /// エッジの係数が変更された。
        /// </summary>
        public void EdgeParamChanged(Edge edge, double newC, double newB) {
            edge.tbIdx.Text = EdgeDescriptionText(edge.EdgeIdx, newC, newB);

            // 表示位置を調整する。
            var p1 = mPP.FindPointByIdx(edge.fromPointIdx, PointProc.FindPointMode.FindAll);
            var p2 = mPP.FindPointByIdx(edge.toPointIdx, PointProc.FindPointMode.FindAll);
            var xy = WWVectorD2.Add(p1.xy, p2.xy).Scale(0.5);
            edge.tbIdx.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tbWH = edge.tbIdx.DesiredSize;
            Canvas.SetLeft(edge.tbIdx, xy.X - tbWH.Width / 2);
            Canvas.SetTop(edge.tbIdx, xy.Y - tbWH.Height / 2);
        }


        /// <summary>
        /// エッジの描画色を変更。
        /// </summary>
        public void EdgeChangeColor(Edge edge, Brush brush) {
            EdgeDrawablesRemove(edge);
            EdgeDrawablesCreate(edge, brush);
        }

        /// <summary>
        /// Edgeとposの最短距離を調べる。
        /// </summary>
        /// <param name="margin">1.0</param>
        public double EdgeDistanceFromPos(Edge e, WWVectorD2 pos, double margin, PointProc.FindPointMode fpm) {
            var p1 = mPP.FindPointByIdx(e.fromPointIdx, fpm);
            var p2 = mPP.FindPointByIdx(e.toPointIdx, fpm);
            return WWSegmentPointDistance.SegmentPointDistance(p1.xy, p2.xy, pos, margin);
        }

        /// <summary>
        /// エッジの矢印描画物作成。
        /// </summary>
        private Polygon NewArrowPoly(WWVectorD2 pos1, WWVectorD2 pos2, Brush stroke) {
            var dir2to1N = WWVectorD2.Sub(pos1, pos2).Normalize();
            var dir2to1S = dir2to1N.Scale(mDP.mArrowSz);

            // 2→1の方向と垂直のベクトル2つ。
            var dirA = new WWVectorD2(-dir2to1N.Y, dir2to1N.X).Scale(mDP.mArrowSz * 0.5);
            var dirB = new WWVectorD2(dir2to1N.Y, -dir2to1N.X).Scale(mDP.mArrowSz * 0.5);

            var vecA = WWVectorD2.Add(dir2to1S, dirA);
            var vecB = WWVectorD2.Add(dir2to1S, dirB);

            var pos2a = WWVectorD2.Add(pos2, dir2to1N.Scale(mDP.mPointSz / 2));
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

        public enum FEOption {
            SamePosition,
            SamePointIdx,
        };

        /// <summary>
        /// 始点がidxFromで終点がidxToのエッジを戻す。
        /// </summary>
        public Edge FindEdge(int idxFrom, int idxTo, FEOption opt) {
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
                    var p1 = mPP.FindPointByIdx(idxFrom, PointProc.FindPointMode.FindAll);
                    var p2 = mPP.FindPointByIdx(idxTo, PointProc.FindPointMode.FindAll);
                    foreach (var e in mEdgeList) {
                        var e1 = mPP.FindPointByIdx(e.fromPointIdx, PointProc.FindPointMode.FindAll);
                        if (WWVectorD2.Distance(e1.xy, p1.xy) < 1) {
                            var e2 = mPP.FindPointByIdx(e.toPointIdx, PointProc.FindPointMode.FindAll);
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
                        var e1 = mPP.FindPointByIdx(e.fromPointIdx, PointProc.FindPointMode.FindAll);
                        if (WWVectorD2.Distance(e1.xy, from.xy) < 1) {
                            var e2 = mPP.FindPointByIdx(e.toPointIdx, PointProc.FindPointMode.FindAll);
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
                var p1 = mPP.FindPointByIdx(e.fromPointIdx, PointProc.FindPointMode.FindAll);
                var p2 = mPP.FindPointByIdx(e.toPointIdx, PointProc.FindPointMode.FindAll);

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
        public void RedrawAllEdges() {
            foreach (var e in mEdgeList) {
                var p1 = mPP.FindPointByIdx(e.fromPointIdx, PointProc.FindPointMode.FindAll);
                var p2 = mPP.FindPointByIdx(e.toPointIdx, PointProc.FindPointMode.FindAll);

                mDP.mCanvas.Children.Remove(e.line);
                e.line = null;
                mDP.mCanvas.Children.Remove(e.arrow);
                e.arrow = null;

                var l = DrawUtil.NewLine(p1.xy, p2.xy, mDP.mBrush);
                e.line = l;

                var poly = NewArrowPoly(p1.xy, p2.xy, mDP.mBrush);
                e.arrow = poly;

                mDP.mCanvas.Children.Add(l);
                mDP.mCanvas.Children.Add(poly);
            }
        }

        /// <summary>
        /// posに近い場所を横切るEdgeを調べる。
        /// </summary>
        public Edge FindNearestEdge(WWVectorD2 pos) {
            Edge nearestEdge = null;
            double nearestDistance = double.MaxValue;
            double margin = 1.0;

            foreach (var e in mEdgeList) {
                double distance = EdgeDistanceFromPos(e, pos, margin, PointProc.FindPointMode.FindAll);
                if (distance < nearestDistance) {
                    // 現時点で最も距離が近いエッジ。
                    nearestDistance = distance;
                    nearestEdge = e;
                }
            }

            return nearestEdge;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // イベント。

        public void MouseMoveUpdateTmpEdge(WWVectorD2 pos) {
            if (mPP.mFromPoint == null) {
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
                p1 = mPP.FindPointByIdx(mTmpEdge.fromPointIdx, PointProc.FindPointMode.FindAll);
                p2 = mPP.FindPointByIdx(mTmpEdge.toPointIdx, PointProc.FindPointMode.FindAll);
            }

            // マウスポインタ位置に確定の終点toPointがあるか。
            var toPoint = mPP.TestHit(pos, mDP.mPointSz);
            if (toPoint == null) {
                // マウスポインタ位置に確定の終点が無い。
                // この場合、確定のエッジは無い。
                if (mPP.mToPoint != null && WWVectorD2.Distance(mPP.mToPoint.xy, pos) < 1) {
                    // マウスポインタ位置に仮の終点mToPointが存在する。
                    Console.WriteLine("MME already toPoint");

                    if (p1 == mPP.mFromPoint && p2 == mPP.mToPoint) {
                        // mFromPoint → mToPoint
                        // 仮のエッジが既に引かれている。
                        return;
                    }

                    // mFromPoint → mToPointのエッジを引く必要がある。
                    p1 = mPP.mFromPoint;
                    p2 = mPP.mToPoint;
                } else {
                    // マウスポインタ位置に確定の終点も仮の終点も無い。
                    // 画面外にマウスが行った場合？
                    // 仮のエッジがあれば消す。
                    EdgeDrawablesRemove(mTmpEdge);
                    return;
                }
            } else {
                // 確定の終点toPointがある。
                if (null != FindEdge(mPP.mFromPoint.Idx, toPoint.Idx, FEOption.SamePosition)) {
                    // 確定のエッジが既に引かれている。
                    // 仮のエッジがあれば消す。
                    EdgeDrawablesRemove(mTmpEdge);
                    return;
                }

                if (p1 == mPP.mFromPoint && p2 == toPoint) {
                    // mFromPoint → toPoint
                    // 仮のエッジが既に引かれている。
                    return;
                }

                // mFromPoint → toPointのエッジを引く必要がある。
                p1 = mPP.mFromPoint;
                p2 = toPoint;
            }

            // Edgeを作り直す。
            EdgeDrawablesRemove(mTmpEdge);
            mTmpEdge = NewEdge(p1, p2, mDP.mBrightBrush);
            Console.WriteLine("MME created edge");
            return;
        }



    };
}
