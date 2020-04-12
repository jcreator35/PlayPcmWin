using System.Windows;
using System.Text;
using WWMath;

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
            {   // 行列C 重み行列。
                
                for (int x=0;x<eList.Count-1; ++x) {
                    var edge = eList[x];
                    C.Set(x,x, edge.C);
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

        }

        private void ButtonCalc_Click(object sender, RoutedEventArgs rea) {
            Calc();
        }
    }
}
