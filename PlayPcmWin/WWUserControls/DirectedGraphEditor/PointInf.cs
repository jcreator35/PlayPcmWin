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
        /// パラメーターf : ノードの電圧源等。
        /// </summary>
        public double F { get; set; }

        public bool Earthed { get; set; }

        public Ellipse circle;
        public Ellipse earthCircle;
        public TextBlock tbIdx;
        public WWVectorD2 xy;
        public static int mNextPointIdx = 0;

        /// <summary>
        /// 点。Idx, 位置aXYと描画物がある。
        /// </summary>
        public PointInf(WWVectorD2 aXY) {
            idx = mNextPointIdx++;
            xy = aXY;
            F = 0;
        }
    }
}
