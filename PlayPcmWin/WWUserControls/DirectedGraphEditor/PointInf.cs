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
        /// �p�����[�^�[f : �m�[�h�̓d�������B
        /// </summary>
        public double F { get; set; }

        public bool Earthed { get; set; }

        public Ellipse circle;
        public Ellipse earthCircle;
        public TextBlock tbIdx;
        public WWVectorD2 xy;
        public static int mNextPointIdx = 0;

        /// <summary>
        /// �_�BIdx, �ʒuaXY�ƕ`�敨������B
        /// </summary>
        public PointInf(WWVectorD2 aXY) {
            idx = mNextPointIdx++;
            xy = aXY;
            F = 0;
        }
    }
}
