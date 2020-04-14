using System.Windows;
using System.Text;
using WWMath;
using System.Collections.Generic;

namespace WWDirectedGraphTest {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        StringBuilder mSBLog = new StringBuilder();
        private void AddLog(string s) {
            mSBLog.Append(s);
            mTextBoxLog.Text = mSBLog.ToString();
            mTextBoxLog.ScrollToEnd();
        }

        private void ClearLog() {
            mSBLog.Clear();
            mTextBoxLog.Clear();
        }

        private void Calc() {
            ClearLog();

            var pList = mDGEditor.PointList();
            var eList = mDGEditor.EdgeList();
            var earthP = mDGEditor.EarthedPoint();

            if (pList.Count == 0 || eList.Count == 0 || earthP == null) {
                return;
            }

            var A = new WWMatrix(eList.Count, pList.Count - 1);
            {   // 行列A(接続行列)を作成する。
                // ストラング, 計算理工学 p143

                for (int e = 0; e < eList.Count; ++e) {
                    var edge = eList[e];

                    int nP = 0;
                    for (int p = 0; p < pList.Count; ++p) {
                        var point = pList[p];
                        if (point == earthP) {
                            // アースされている点は除外。
                            continue;
                        }

                        double v = 0;
                        // エッジのfromがpのとき、-1
                        // エッジのto  がpのとき、+1
                        if (edge.fromPointIdx == point.Idx) {
                            v = -1;
                        }
                        if (edge.toPointIdx == point.Idx) {
                            v = +1;
                        }

                        A.Set(e, nP, v);

                        ++nP;
                    }
                }
            }
            AddLog(string.Format("A: {0}", A.ToString()));

            var C = new WWMatrix(eList.Count, eList.Count);
            var Cinv = new WWMatrix(eList.Count, eList.Count);
            {   // 行列C 重み行列。

                for (int x = 0; x < eList.Count; ++x) {
                    var edge = eList[x];
                    C.Set(x, x, edge.C);
                    Cinv.Set(x, x, 1.0 / edge.C);
                }
            }

            // A^T
            var AT = A.Transpose();

            // K = A^T C Aを作る。
            var K = new WWMatrix(pList.Count - 1, pList.Count - 1);
            {
                var CA = WWMatrix.Mul(C, A);
                K = WWMatrix.Mul(AT, CA);
            }
            AddLog(string.Format("K: {0}", K.ToString()));

            var b = new WWMatrix(eList.Count, 1);
            {   // 電圧源b
                for (int i = 0; i < eList.Count; ++i) {
                    var e = eList[i];
                    b.Set(i, 0, e.B);
                }
            }
            AddLog(string.Format("b: {0}", b.ToString()));

            var f = new WWMatrix(pList.Count - 1, 1);
            {   // 電流源f
                // 各点について、電流を集計する。

                // pの点番号と、fのidx番号の対応テーブルを作る。
                int nF = 0;
                var pListIdxToFIdxTable = new Dictionary<int, int>();
                for (int i = 0; i < pList.Count; ++i) {
                    var point = pList[i];

                    if (point == earthP) {
                        // アースされている点は除外。
                        pListIdxToFIdxTable.Add(point.Idx, -1);
                        continue;
                    }
                    pListIdxToFIdxTable.Add(point.Idx, nF);
                    ++nF;
                }

                // fを作る。
                for (int i = 0; i < eList.Count; ++i) {
                    var edge = eList[i];

                    { // fromは＋する。
                        int fIdx = pListIdxToFIdxTable[edge.fromPointIdx];
                        if (0 <= fIdx) {
                            var v = f.At(fIdx, 0);
                            f.Set(fIdx, 0, v + edge.F);
                        }
                    }
                    { // toは－する。
                        int fIdx = pListIdxToFIdxTable[edge.toPointIdx];
                        if (0 <= fIdx) {
                            var v = f.At(fIdx, 0);
                            f.Set(fIdx, 0, v - edge.F);
                        }
                    }
                }
            }
            AddLog(string.Format("f: {0}", f.ToString()));

            WWMatrix KKT;
            {   // KKT行列
                var Cinv_A = WWMatrix.JoinH(Cinv, A);
                var AT_zero = WWMatrix.JoinH(AT, new WWMatrix(A.Column, A.Column));
                KKT = WWMatrix.JoinV(Cinv_A, AT_zero);
            }
            AddLog(string.Format("KKT: {0}", KKT.ToString()));

            WWMatrix bf;
            {   // bとfを縦に連結した縦ベクトル。
                bf = WWMatrix.JoinV(b, f);
            }
            AddLog(string.Format("bf: {0}", bf.ToString()));

        }

        private void ButtonCalc_Click(object sender, RoutedEventArgs rea) {
            Calc();
        }
    }
}
