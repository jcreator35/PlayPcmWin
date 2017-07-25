using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    /// <summary>
    /// ■■行列の要素はcolumn majorで並んでいる。■■
    /// </summary>
    public class Matrix<T> {
        /// <summary>
        /// ctor
        /// row行column列の行列を作る。
        /// ■■行列の要素はcolumn majorで並んでいる。■■
        /// </summary>
        /// <param name="row">行の数</param>
        /// <param name="column">列の数</param>
        public Matrix(int numRow, int numColumn) {
            mRow = numRow;
            mColumn = numColumn;
            m = new T[numColumn * numRow];
        }

        public int Row {
            get { return mRow; }
        }

        public int Column {
            get { return mColumn; }
        }

        public T At(int row, int column) {
            if (row < 0 || mRow <= row) {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || mColumn <= column) {
                throw new ArgumentOutOfRangeException("column");
            }

            return m[column * mRow + row];
        }

        public void Set(int row, int column, T v) {
            m[column * mRow + row] = v;
        }

        public void Set(T[] v) {
            if (v.Length != mRow * mColumn) {
                throw new ArgumentException("v.Length mismatch");
            }
            Array.Copy(v, m, v.Length);
        }

        public static Matrix<double> Mul(Matrix<double> lhs, Matrix<double> rhs) {
            if (lhs.Column != rhs.Row) {
                throw new ArgumentException("lhs.Column != rhs.Row");
            }

            int loopCount = lhs.Column;

            var rv = new Matrix<double>(lhs.Row, rhs.Column);
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

        public static Matrix<int> Mul(Matrix<int> lhs, Matrix<int> rhs) {
            if (lhs.Column != rhs.Row) {
                throw new ArgumentException("lhs.Column != rhs.Row");
            }

            int loopCount = lhs.Column;

            var rv = new Matrix<int>(lhs.Row, rhs.Column);
            for (int r = 0; r < rv.Row; ++r) {
                for (int c = 0; c < rv.Column; ++c) {
                    int v = 0;
                    for (int i = 0; i < loopCount; ++i) {
                        v += lhs.At(r, i) * rhs.At(i, c);
                    }
                    rv.Set(r, c, v);
                }
            }

            return rv;
        }

        public delegate T UpdateDelegate(int row, int column, T vIn);

        public void Update(UpdateDelegate f) {
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mColumn; ++c) {
                    T from = At(r, c);
                    T to = f(r, c, from);
                    Set(r,c,to);
                }
            }
        }

        public void PrintDouble(string s) {
            Console.WriteLine("{0}: {1}x{2}", s, mRow, mColumn);
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mColumn; ++c) {
                    Console.Write("{0:F4} ", At(r, c));
                }
                Console.WriteLine("");
            }
        }

        public void PrintInt(string s) {
            Console.WriteLine("{0}: {1}x{2}", s, mRow, mColumn);
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mColumn; ++c) {
                    Console.Write("{0} ", At(r, c));
                }
                Console.WriteLine("");
            }
        }

        private int mRow;
        private int mColumn;
        private T[] m;
    }
}
