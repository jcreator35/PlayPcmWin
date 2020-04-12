using System.Windows.Controls;
using System.Windows.Shapes;
using WWMath;

namespace WWUserControls {
    public class Edge {
        public static int mNextEdgeIdx = 0;

        private int edgeIdx;
        public int EdgeIdx {
            get { return edgeIdx; }
        }

        public int fromPointIdx;
        public int toPointIdx;

        /// <summary>
        /// �G�b�W�̌W�� �R���_�N�^���X���B
        /// </summary>
        public double C { get; set; }

        /// <summary>
        /// �G�b�W�̓d�������B
        /// </summary>
        public double F { get; set; }

        // �`�敨�B
        public Line line;
        public Polygon arrow;
        public TextBlock tbIdx;

        /// <summary>
        /// �G�b�W�쐬�B�_Idx��from��to���K�v�B
        /// </summary>
        public Edge(int from, int to) {
            edgeIdx = mNextEdgeIdx++;

            fromPointIdx = from;
            toPointIdx = to;
            C = 1.0;
            F = 0;
        }
    };
}

