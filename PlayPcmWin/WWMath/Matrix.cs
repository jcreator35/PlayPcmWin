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
            m = new double[mRow * mCol];
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

            m = new double[v.Length];
            Array.Copy(v, m, v.Length);
        }

        public double At(int row, int column) {
            if (row < 0 || mRow <= row) {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || mCol <= column) {
                throw new ArgumentOutOfRangeException("column");
            }

            return m[column +mCol * row];
        }

        public void Set(int row, int column, double v) {
            m[column + mCol *row] = v;
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
            Array.Copy(v, m, v.Length);
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

        /// <summary>
        /// aを縦行列と見做して後から掛ける
        /// rv := M x aT
        /// </summary>
        /// <returns>乗算結果の行列を転置したもの</returns>
        public double[] Mul(double[] a) {
            if (mCol != a.Length) {
                throw new ArgumentException("a.Length != mat.Col");
            }

            var aT = new Matrix(a.Length, 1, a);
            var rv = Mul(aT);

            return rv.ToArray();
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

        private double[] ToArray() {
            return m.ToArray();
        }

        private long mRow;
        private long mCol;
        private double [] m;
    }
}
