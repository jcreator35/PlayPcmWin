
namespace WWMath {
    public class WWVectorD2 {

        private double[] v = new double[2];
        public WWVectorD2() {
        }

        public WWVectorD2(double x, double y) {
            v[0] = x;
            v[1] = y;
        }

        public double X {
            get {
                return v[0];
            }
        }

        public double Y {
            get {
                return v[1];
            }
        }
    }
}
