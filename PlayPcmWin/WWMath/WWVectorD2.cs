using System.Diagnostics;
using System;

namespace WWMath {
    /// <summary>
    /// インスタンスは、内容不変。
    /// </summary>
    [DebuggerDisplay("({v[0]}, {v[1]})")]
    public class WWVectorD2 {
        /// <summary>
        /// コンストラクターで代入され、以降変更されない。
        /// </summary>
        private double[] v = new double[2];

        public WWVectorD2(double x, double y) {
            v[0] = x;
            v[1] = y;
        }

        public double X {
            get {
                return v[0];
            }
            // setは有りません。
        }

        public double Y {
            get {
                return v[1];
            }
            // setは有りません。
        }

        static public double Distance(WWVectorD2 a, WWVectorD2 b) {
            double d = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
            return d;
        }
    }
}
