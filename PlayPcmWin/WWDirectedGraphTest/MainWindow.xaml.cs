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

            var earthP = mDGEditor.EarthedPoint();
            if (earthP == null) {
                CalcConnectionMat();
            } else {
                SolveKKT();
            }
        }

        /// <summary>
        /// 接続行列Aを戻す。
        /// </summary>
        private WWMatrix CalcA() {
            var pList = mDGEditor.PointList();
            var eList = mDGEditor.EdgeList();
            var earthP = mDGEditor.EarthedPoint();

            int aRow = eList.Count;

            int aCol = pList.Count;
            if (earthP != null) {
                // アースによって1列除去。これによりATAが可逆になる。
                aCol = pList.Count - 1;
            }

            var A = new WWMatrix(aRow, aCol);
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

            return A;
        }

        /// <summary>
        /// 行列C 重み行列。
        /// </summary>
        private WWMatrix CalcC() {
            var eList = mDGEditor.EdgeList();
            var C = new WWMatrix(eList.Count, eList.Count);
            {   // 行列C 重み行列。
                for (int x = 0; x < eList.Count; ++x) {
                    var edge = eList[x];
                    C.Set(x, x, edge.C);
                }
            }

            return C;
        }

        /// <summary>
        /// 重み行列Cの逆行列。
        /// </summary>
        private WWMatrix CalcCinv() {
            var eList = mDGEditor.EdgeList();
            var Cinv = new WWMatrix(eList.Count, eList.Count);
            {   // 行列C 重み行列。
                for (int x = 0; x < eList.Count; ++x) {
                    var edge = eList[x];
                    Cinv.Set(x, x, 1.0 / edge.C);
                }
            }

            return Cinv;
        }

        /// <summary>
        /// 接続行列AとA^T*CAを表示。
        /// </summary>
        private void CalcConnectionMat() {
            var pList = mDGEditor.PointList();
            var eList = mDGEditor.EdgeList();
            if (pList.Count == 0 || eList.Count == 0) {
                return;
            }

            var A = CalcA();
            AddLog(string.Format("A: {0}", A.ToString()));

            var C = CalcC();
            AddLog(string.Format("C: {0}", C.ToString()));

            var CA = WWMatrix.Mul(C, A);

            var AT = A.Transpose();

            var AT_CA = WWMatrix.Mul(AT, CA);
            AddLog(string.Format("A^T*CA: {0}", AT_CA.ToString()));
        }

        /// <summary>
        /// KKT行列を作って解く。
        /// </summary>
        private void SolveKKT() {
            var pList = mDGEditor.PointList();
            var eList = mDGEditor.EdgeList();
            var earthP = mDGEditor.EarthedPoint();

            if (pList.Count == 0 || eList.Count == 0) {
                return;
            }

            var A = CalcA();
            AddLog(string.Format("A: {0}", A.ToString()));

            var C = CalcC();

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
            {   // fを作る。
                int nP = 0;
                for (int i = 0; i < pList.Count; ++i) {
                    var point = pList[i];

                    if (point == earthP) {
                        // アースされている点は除外。
                        continue;
                    }
                    f.Set(nP, 0, point.F);
                    ++nP;
                }
            }
            AddLog(string.Format("f: {0}", f.ToString()));

            WWMatrix KKT;
            {   // KKT行列
                var Cinv = CalcCinv();
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

            var bfA = bf.ToArray();

            // KKT u = bfを解いて uを求める。
            var uA = WWLinearEquation.SolveKu_eq_f(KKT, bfA);
            var u = new WWMatrix(uA.Length, 1, uA);
            AddLog(string.Format("u: {0}", u.ToString()));
        }

        private void ButtonCalc_Click(object sender, RoutedEventArgs rea) {
            Calc();
        }
    }
}
