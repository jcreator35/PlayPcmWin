using System.Windows.Media;
using System.Windows.Shapes;
using WWMath;
using System.Windows.Controls;

namespace WWUserControls {
    public class DrawParams {
        public Canvas mCanvas;
        
        public int mPointSz;
        public int mArrowSz;
        public int mGridSz;

        public double mPointFontSz = 10;

        public int Z_Grid = -20;
        public int Z_Edge = -10;
        //private int Z_Point = -1;

        public Brush mGridBrush = new SolidColorBrush(Colors.LightGray);
        public Brush mBrush = new SolidColorBrush(Colors.Black);
        public Brush mBrightBrush = new SolidColorBrush(Colors.Blue);
        public Brush mPointTextFgBrush = new SolidColorBrush(Colors.White);
        public Brush mEdgeTextFgBrush = new SolidColorBrush(Colors.Black);
        public Brush mEdgeTextBgBrush = new SolidColorBrush(Colors.LightGray);
        public Brush mErrBrush = new SolidColorBrush(Colors.Red);

        public DrawParams() {
        }
    }
}
