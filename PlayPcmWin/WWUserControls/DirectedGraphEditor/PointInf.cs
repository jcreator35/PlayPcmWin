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
        public Ellipse circle;
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
        }
    }
}
