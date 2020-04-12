using System.Windows.Controls;
using System.Windows.Shapes;
using WWMath;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;

namespace WWUserControls {
    class PointProc {
        DrawParams mDP;
        public PointProc(DrawParams dp) {
            mDP = dp;
        }

        public List<PointInf> mPointList = new List<PointInf>();

        public PointInf mFromPoint = null;
        public PointInf mToPoint = null;

        /// <summary>
        /// FromPointがTmp点のとき消す。
        /// FromPointが確定点のとき確定色にする。
        /// </summary>
        public void TmpFromPointRemove() {
            if (mFromPoint != null) {
                if (PointExists(mPointList, mFromPoint.xy)) {
                    // mFromPoint地点に確定の点がある。
                    // 色を確定の色に変更。
                    PointChangeColor(mFromPoint, mDP.mBrush);
                } else {
                    // mFromPointは仮の点で、確定の点は無い。
                    PointDrawableRemove(mFromPoint);
                }

                mFromPoint = null;
            }
        }

        /// <summary>
        /// FromPointが存在するとき色を変更する。
        /// </summary>
        public void FromPointChangeColor(Brush b) {
            if (mFromPoint != null) {
                PointChangeColor(mFromPoint, b);
            }
        }

        /// <summary>
        /// mToPointを消す。
        /// mToPointは常に仮の点。
        /// </summary>
        public void TmpToPointRemove() {
            PointDrawableRemove(mToPoint);
            mToPoint = null;
        }

        /// <summary>
        /// 点の描画物をキャンバスから削除し、点の描画物自体も削除。
        /// </summary>
        public void PointDrawableRemove(PointInf p) {
            if (p == null) {
                return;
            }
            if (p.tbIdx != null) {
                mDP.mCanvas.Children.Remove(p.tbIdx);
                p.tbIdx = null;
            }

            if (p.circle != null) {
                mDP.mCanvas.Children.Remove(p.circle);
                p.circle = null;
            }

            if (p.earthCircle != null) {
                mDP.mCanvas.Children.Remove(p.earthCircle);
                p.earthCircle = null;
            }

            Console.WriteLine("Point drawable removed");
        }

        /// <summary>
        /// 点の描画物を作り、キャンバスに追加する。
        /// </summary>
        public void PointDrawableCreate(PointInf pi, Brush brush) {
            {
                System.Diagnostics.Debug.Assert(pi.circle == null);

                pi.circle = new Ellipse();
                pi.circle.Width = mDP.mPointSz;
                pi.circle.Height = mDP.mPointSz;
                pi.circle.Fill = brush;

                Canvas.SetLeft(pi.circle, pi.xy.X - mDP.mPointSz / 2);
                Canvas.SetTop(pi.circle, pi.xy.Y - mDP.mPointSz / 2);

                mDP.mCanvas.Children.Add(pi.circle);
            }

            if (pi.Earthed) {
                System.Diagnostics.Debug.Assert(pi.earthCircle == null);

                int margin = 3;

                pi.earthCircle = new Ellipse();
                pi.earthCircle.Width = mDP.mPointSz + margin*2;
                pi.earthCircle.Height = mDP.mPointSz + margin * 2;
                pi.earthCircle.Stroke = brush;

                Canvas.SetLeft(pi.earthCircle, pi.xy.X - mDP.mPointSz / 2 - margin);
                Canvas.SetTop(pi.earthCircle, pi.xy.Y - mDP.mPointSz / 2 - margin);

                mDP.mCanvas.Children.Add(pi.earthCircle);
            }

            {
                pi.tbIdx = new TextBlock();
                pi.tbIdx.Text = string.Format("p{0}\nb={1}", pi.Idx, pi.B);
                pi.tbIdx.FontSize = mDP.mPointFontSz;
                pi.tbIdx.Foreground = mDP.mPointTextFgBrush;
                pi.tbIdx.Background = brush;
                pi.tbIdx.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var tbWH = pi.tbIdx.DesiredSize;
                Canvas.SetLeft(pi.tbIdx, pi.xy.X - tbWH.Width / 2);
                Canvas.SetTop(pi.tbIdx, pi.xy.Y - tbWH.Height / 2);

                mDP.mCanvas.Children.Add(pi.tbIdx);
            }
            Console.WriteLine("Point drawable added");
        }
        
        /// <summary>
        /// 点の係数が変更された。
        /// </summary>
        public void PointParamChanged(PointInf pi, double newB) {
            pi.tbIdx.Text = string.Format("p{0}\nb={1}", pi.Idx, newB);

            // 表示位置を調整する。
            pi.tbIdx.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tbWH = pi.tbIdx.DesiredSize;
            Canvas.SetLeft(pi.tbIdx, pi.xy.X - tbWH.Width / 2);
            Canvas.SetTop(pi.tbIdx, pi.xy.Y - tbWH.Height / 2);
        }

        /// <summary>
        /// 点pの描画色を変更。描画物がないときは作ってキャンバスに追加。
        /// </summary>
        public void PointChangeColor(PointInf p, Brush brush) {
            if (p.circle == null) {
                PointDrawableCreate(p, brush);
                return;
            }

            // 点描画物有り。色を変える。
            p.circle.Fill = brush;
            p.tbIdx.Background = brush;
        }

        /// <summary>
        /// 新しいPointInfを作り、キャンバスに追加。
        /// 一時的な点用。
        /// </summary>
        public PointInf NewPoint(WWVectorD2 pos, Brush brush) {
            var pInf = new PointInf(pos);
            PointDrawableCreate(pInf, brush);
            return pInf;
        }

        /// <summary>
        /// グリッドにアラインする。
        /// </summary>
        public static WWVectorD2 SnapToGrid(double xD, double yD, int gridSz) {
            int x = (int)xD;
            x = ((x + gridSz / 2) / gridSz) * gridSz;

            int y = (int)yD;
            y = ((y + gridSz / 2) / gridSz) * gridSz;

            return new WWVectorD2(x, y);
        }

        /// <summary>
        /// すべての点を再描画する。
        /// ①既存点をキャンバスから削除。
        /// ②新しい点を作成、キャンバスに追加。
        /// </summary>
        public void RedrawPoints() {
            foreach (var p in mPointList) {
                PointDrawableRemove(p);
                PointDrawableCreate(p, mDP.mBrush);
            }
        }

        public void UpdateEarthedPoint(PointInf ep) {
            foreach (var p in mPointList) {
                if (p == ep) {
                    p.Earthed = true;
                } else {
                    p.Earthed = false;
                }
            }

            RedrawPoints();
        }


        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Find

        public enum FindPointMode {
            FindAll,
            FindFromPointList,
        };

        /// <summary>
        /// FindAll: 確定の点、または仮の点から点を探す。
        /// FindFromPointList: 確定の点から探す。
        /// </summary>
        public PointInf FindPointByIdx(int idx, FindPointMode fpm) {
            // 確定の点。
            foreach (var p in mPointList) {
                if (p.Idx == idx) {
                    return p;
                }
            }

            if (fpm == FindPointMode.FindAll) {
                // 仮の点。
                if (mFromPoint != null && mFromPoint.Idx == idx) {
                    return mFromPoint;
                }
                if (mToPoint != null && mToPoint.Idx == idx) {
                    return mToPoint;
                }
            }

            return null;
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
        public PointInf TestHit(WWVectorD2 xy, double threshold) {
            foreach (var p in mPointList) {
                if (WWVectorD2.Distance(p.xy, xy) < threshold) {
                    return p;
                }
            }

            return null;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // イベント。

        public void MouseMoveUpdateTmpPoint(WWVectorD2 pos) {
            if (mFromPoint == null) {
                // 始点が無い。
                Console.WriteLine("MMP FP none");
                return;
            }

            // 始点有り。
            Console.WriteLine("MMP ({0:0.0} {0:0.0})", pos.X, pos.Y);

            var toPoint = TestHit(pos, mDP.mPointSz);
            if (toPoint == null) {
                // 始点が存在し、マウスポインタ位置に確定の終点が無い。
                if (mToPoint != null && WWVectorD2.Distance(mToPoint.xy, pos) < 1) {
                    // マウスポインタ位置に仮の終点mToPointが存在。
                    Console.WriteLine("MMP already toPoint");
                } else {
                    // 仮の終点位置が異なるので作り直す。
                    PointDrawableRemove(mToPoint);
                    mToPoint = null;
                    mToPoint = NewPoint(pos, mDP.mBrightBrush);
                    Console.WriteLine("MMP create toPoint");
                }
            } else {
                // マウスポインタ位置に確定の終点が存在する。
                // 仮の終点は不要。
                PointDrawableRemove(mToPoint);
                mToPoint = null;
            }
        }

        /// <summary>
        /// 始点が未決定の状態でマウスがホバーしている。
        /// </summary>
        public void SetFirstPointMouseMove(WWVectorD2 pos) {
            var point = TestHit(pos, mDP.mPointSz);

            if (mFromPoint == null) {
                // 始点が無い。
                Console.WriteLine("SFPMM FP none");

                if (point == null) {
                    // 始点mFromPointが無く、マウスホバー位置に確定の点も無い。
                    // マウスポインタ位置に仮の始点を作る。
                    mFromPoint = NewPoint(pos, mDP.mBrightBrush);
                    Console.WriteLine("SFPMM create fromPoint");
                    return;
                }

                // 始点が無く、マウスホバー位置に確定の点が有る。
                // 確定の点の色をハイライト色に変更。
                // mFromPointをセットする。
                mFromPoint = point;
                PointChangeColor(mFromPoint, mDP.mBrightBrush);
                return;
            }

            // 始点mFromPoint有り。
            Console.WriteLine("SFPMM ({0:0.0} {0:0.0})", pos.X, pos.Y);

            if (point == null) {
                // 始点mFromPointが存在し、マウスポインタ位置に確定の点が無い。

                if (WWVectorD2.Distance(mFromPoint.xy, pos) < 1) {
                    // マウスポインタ位置に仮の始点mFromPointが存在。
                    Console.WriteLine("SFPMM no need to change tmp FromPoint");
                } else {
                    // 仮の始点位置が異なるので作り直す。
                    TmpFromPointRemove();
                    mFromPoint = NewPoint(pos, mDP.mBrightBrush);
                    Console.WriteLine("SFPMM create FromPoint");
                }
            } else {
                // 始点mFromPointが存在し、マウスホバー位置に確定の始点pointが存在する。
                Console.WriteLine("SFPMM remove tmp drawable and set point");

                // 始点mFromPointが仮の点のときは消す。
                TmpFromPointRemove();

                // マウスホバー位置の確定の点をmFromPointにセットする。
                // 確定の点の色をハイライト色に変更。
                // mFromPointをセットする。
                mFromPoint = point;
                PointChangeColor(mFromPoint, mDP.mBrightBrush);
                return;
            }
        }



    };
}
