using System.Windows.Controls;
using System.Windows.Shapes;
using WWMath;

namespace WWUserControls {
    public class PointInf {
        private int idx;
        public int Idx {
            get {
                return idx;
            }
        }

        /// <summary>
        /// ノードの電圧源等。
        /// </summary>
        public double B { get; set; }

        public bool Earthed { get; set; }

        public Ellipse circle;
        public Ellipse earthCircle;
        public TextBlock tbIdx;
        public WWVectorD2 xy;
        public static int mNextPointIdx = 0;

        /// <summary>
        /// 点。Idx, 位置aXYと描画物eがある。
        /// 一時的な点は描画物eを作って渡す。
        /// 確定した点は、e = nullで作成し、コマンド実行時に描画物を作る。
        /// </summary>
        public PointInf(WWVectorD2 aXY) {
            idx = mNextPointIdx++;
            xy = aXY;
            B = 0;
        }
    }
}
