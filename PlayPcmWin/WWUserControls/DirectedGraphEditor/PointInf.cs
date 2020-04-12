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
        /// �m�[�h�̓d�������B
        /// </summary>
        public double B { get; set; }

        public bool Earthed { get; set; }

        public Ellipse circle;
        public Ellipse earthCircle;
        public TextBlock tbIdx;
        public WWVectorD2 xy;
        public static int mNextPointIdx = 0;

        /// <summary>
        /// �_�BIdx, �ʒuaXY�ƕ`�敨e������B
        /// �ꎞ�I�ȓ_�͕`�敨e������ēn���B
        /// �m�肵���_�́Ae = null�ō쐬���A�R�}���h���s���ɕ`�敨�����B
        /// </summary>
        public PointInf(WWVectorD2 aXY) {
            idx = mNextPointIdx++;
            xy = aXY;
            B = 0;
        }
    }
}
