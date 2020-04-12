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
        /// エッジの係数。
        /// </summary>
        public double coef;

        // 描画物。
        public Line line;
        public Polygon arrow;
        public TextBlock tbIdx;

        /// <summary>
        /// エッジ作成。点Idxのfromとtoが必要。
        /// </summary>
        public Edge(int from, int to) {
            edgeIdx = mNextEdgeIdx++;

            fromPointIdx = from;
            toPointIdx = to;
            coef = 1.0;
        }
    };
}

