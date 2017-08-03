using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWUtil;

namespace WWMath {
    public class Matrix {
        /// <summary>
        /// ■■行列の要素はrow majorで並んでいる。■■
        /// すなわち、1次元配列にシリアライズするとき、
        /// 0row目の全要素を詰め、1row目の全要素を詰め…という要領で詰める。
        /// <para/>numRow=3, numColumn=3の例。
        /// <para/>
        /// <para/>　　　　　　 column番号
        /// <para/>　　　　　　 0　　1　 2
        /// <para/>row番号0→ ┌a00 a01 a02┐
        /// <para/>row番号1→ │a10 a11 a12│
        /// <para/>row番号2→ └a20 a21 a22┘
        /// <para/>
        /// <para/> ↓
        /// <para/> m[] = { a00, a01, a02, a10, a11, a12, a20, a21, a22 }
        /// </summary>
        /// <param name="row">行の数</param>
        /// <param name="column">列の数</param>
        public Matrix(int numRow, int numColumn) {
            mRow = numRow;
            mCol = numColumn;
            m = new LargeArray<double>(mRow * mCol);
        }

        /// <summary>
        /// ■■行列の要素はrow majorで並んでいる。■■
        /// すなわち、1次元配列にシリアライズするとき、
        /// 0row目の全要素を詰め、1row目の全要素を詰め…という要領で詰める。
        /// <para/>numRow=3, numColumn=3の例。
        /// <para/>
        /// <para/>　　　　　　 column番号
        /// <para/>　　　　　　 0　　1　 2
        /// <para/>row番号0→ ┌a00 a01 a02┐
        /// <para/>row番号1→ │a10 a11 a12│
        /// <para/>row番号2→ └a20 a21 a22┘
        /// <para/>
        /// <para/> ↓
        /// <para/> m[] = { a00, a01, a02, a10, a11, a12, a20, a21, a22 }
        /// </summary>
        /// <param name="row">行の数</param>
        /// <param name="column">列の数</param>
        public Matrix(int numRow, int numColumn, double[] v) {
            mRow = numRow;
            mCol = numColumn;

            if (mRow * mCol != v.Length) {
                throw new ArgumentException("v.length and row * column mismatch");
            }

            m = new LargeArray<double>(v);
        }

        public double At(int row, int column) {
            if (row < 0 || mRow <= row) {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || mCol <= column) {
                throw new ArgumentOutOfRangeException("column");
            }

            return m.At(column +mCol * row);
        }

        public void Set(int row, int column, double v) {
            m.Set(column + mCol *row, v);
        }

        /// <summary>
        /// ■■行列の要素はrow majorで並んでいる。■■
        /// すなわち、1次元配列にシリアライズするとき、
        /// 0row目の全要素を詰め、1row目の全要素を詰め…という要領で詰める。
        /// <para/>numRow=3, numColumn=3の例。
        /// <para/>
        /// <para/>　　　　　　 column番号
        /// <para/>　　　　　　 0　　1　 2
        /// <para/>row番号0→ ┌a00 a01 a02┐
        /// <para/>row番号1→ │a10 a11 a12│
        /// <para/>row番号2→ └a20 a21 a22┘
        /// <para/>
        /// <para/> ↓
        /// <para/> m[] = { a00, a01, a02, a10, a11, a12, a20, a21, a22 }
        /// </summary>
        public void Set(double[] v) {
            if (v.Length != mRow * mCol) {
                throw new ArgumentException("v.Length mismatch");
            }
            m.CopyFrom(v, 0, 0, v.Length);
        }

        public static Matrix Mul(Matrix lhs, Matrix rhs) {
            if (lhs.Column != rhs.Row) {
                throw new ArgumentException("lhs.Column != rhs.Row");
            }

            int loopCount = lhs.Column;

            var rv = new Matrix(lhs.Row, rhs.Column);
            for (int r = 0; r < rv.Row; ++r) {
                for (int c = 0; c < rv.Column; ++c) {
                    double v = 0;
                    for (int i = 0; i < loopCount; ++i) {
                        v += lhs.At(r, i) * rhs.At(i, c);
                    }
                    rv.Set(r,c,v);
                }
            }

            return rv;
        }

        /// <summary>
        /// rv := this x a
        /// 自分自身を変更しない。
        /// </summary>
        /// <returns>乗算結果。</returns>
        public Matrix Mul(Matrix a) {
            return Mul(this, a);
        }

        public delegate double UpdateDelegate(int row, int column, double vIn);

        public void Update(UpdateDelegate f) {
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mCol; ++c) {
                    double from = At(r, c);
                    double to = f(r, c, from);
                    Set(r,c,to);
                }
            }
        }

        public void Print(string s) {
            Console.WriteLine("{0}: {1}x{2}", s, mRow, mCol);
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mCol; ++c) {
                    Console.Write("{0} ", At(r, c));
                }
                Console.WriteLine("");
            }
        }

        public int Row {
            get { return (int)mRow; }
        }

        public int Column {
            get { return (int)mCol; }
        }

        public Matrix Inverse() {
            if (mRow != mCol) {
                throw new InvalidOperationException("row != column");
            }

            int order = (int)mRow;
            switch (order) {
            case 2:
                return Inverse2();
            case 3:
                return Inverse3();
            default:
                throw new NotImplementedException();
            }
        }

        private Matrix Inverse2() {
            if (mRow != 2 || mCol != 2) {
                throw new InvalidOperationException("order != 2");
            }

            double a = m.At(0);
            double b = m.At(2);
            double c = m.At(1);
            double d = m.At(3);

            double det = a * d - b * c;
            return new Matrix(2, 2, new double[] {
                +d / det, -c / det,
                -b / det, +a / det });
        }

        private Matrix Inverse3() {
            if (mRow != 3 || mCol != 3) {
                throw new InvalidOperationException("order != 3");
            }

            double a11 = m.At(0);
            double a12 = m.At(1);
            double a13 = m.At(2);

            double a21 = m.At(3);
            double a22 = m.At(4);
            double a23 = m.At(5);

            double a31 = m.At(6);
            double a32 = m.At(7);
            double a33 = m.At(8);

            double det =
                a11 * a22 * a33 +
                a12 * a23 * a31 +
                a13 * a21 * a32 -
                a13 * a22 * a31 -
                a11 * a23 * a32 -
                a12 * a21 * a33;

            double m11 = a22 * a33 - a23 * a32;
            double m21 = a23 * a31 - a21 * a33;
            double m31 = a21 * a32 - a22 * a31;

            double m12 = a13 * a32 - a12 * a33;
            double m22 = a11 * a33 - a13 * a31;
            double m32 = a12 * a31 - a11 * a32;

            double m13 = a12 * a23 - a13 * a22;
            double m23 = a13 * a21 - a11 * a23;
            double m33 = a11 * a22 - a12 * a21;

            return new Matrix(3,3, new double[] {
                m11 / det, m12 / det, m13 / det,
                m21 / det, m22 / det, m23 / det,
                m31 / det, m32 / det, m33 / det });
        }

        private long mRow;
        private long mCol;
        private LargeArray<double> m;
    }
}
