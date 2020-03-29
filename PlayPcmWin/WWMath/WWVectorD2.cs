// 日本語。

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

        public double Length() {
            return Math.Sqrt(X * X + Y * Y);
        }

        public WWVectorD2 Scale(double s) {
            return new WWVectorD2(X * s, Y * s);
        }

        static public double Distance(WWVectorD2 a, WWVectorD2 b) {
            double d = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
            return d;
        }

        static public WWVectorD2 Add(WWVectorD2 a, WWVectorD2 b) {
            return new WWVectorD2(a.X + b.X, a.Y + b.Y);
        }

        /// <returns>a - b</returns>
        static public WWVectorD2 Sub(WWVectorD2 a, WWVectorD2 b) {
            return new WWVectorD2(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// 自分自身は変更しない。
        /// </summary>
        /// <returns>長さが1になるよう拡縮されたベクトルv</returns>
        public WWVectorD2 Normalize() {
            double length = Length();
            return new WWVectorD2(X / length, Y / length);
        }
    }
}
