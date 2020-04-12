using System.Windows.Media;
using System.Windows.Shapes;
using WWMath;

namespace WWUserControls {
    public class DrawUtil {
        /// <summary>
        /// 線描画物作成。
        /// </summary>
        public static Line NewLine(WWVectorD2 xy1, WWVectorD2 xy2, Brush stroke) {
            var l = new Line();
            l.X1 = xy1.X;
            l.Y1 = xy1.Y;
            l.X2 = xy2.X;
            l.Y2 = xy2.Y;
            l.Stroke = stroke;
            return l;
        }

    }
}
