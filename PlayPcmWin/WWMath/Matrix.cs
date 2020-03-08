using System;
using System.Linq;

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
        /// <param name="numRow">行の数</param>
        /// <param name="numColumn">列の数</param>
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
        /// <param name="numRow">行の数</param>
        /// <param name="numColumn">列の数</param>
        public Matrix(int numRow, int numColumn, double[] v) {
            mRow = numRow;
            mCol = numColumn;

            if (mRow * mCol != v.Length) {
                throw new ArgumentException("v.length and row * column mismatch");
            }

            m = new double[v.Length];
            Array.Copy(v, m, v.Length);
        }

        /// <summary>
        /// column番号をx,row番号をyとするとAt(y,x)
        /// </summary>
        public double At(int row, int column) {
            if (row < 0 || mRow <= row) {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || mCol <= column) {
                throw new ArgumentOutOfRangeException("column");
            }

            return m[column +mCol * row];
        }

        /// <summary>
        /// 行列のセルに値を入れる。
        /// column番号がx、row番号がy、値がvのときSet(y,x,v)と書く。
        /// </summary>
        public void Set(int row, int column, double v) {
            m[column + mCol *row] = v;
        }

        /// <summary>
        /// 単位行列にする。
        /// 自分自身を変更する。
        /// </summary>
        public void SetIdentity() {
            if (mRow != mCol) {
                throw new InvalidOperationException("Matrix is not square");
            }

            for (int y = 0; y < mRow; ++y) {
                for (int x = 0; x < mCol; ++x) {
                    Set(y, x, x == y ? 1.0 : 0.0);
                }
            }
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
        /// 自分自身を変更しない。
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

        public enum ResultEnum {
            Success,
            UnsupportedMatrixShape,
            FailedToChoosePivot,
        };

        /// <summary>
        /// https://en.wikipedia.org/wiki/Crout_matrix_decomposition
        /// </summary>
        public static ResultEnum LUdecompose(Matrix inA, out Matrix outL, out Matrix outU, double epsilon = 1.0e-7) {
            if (inA.Row < 2 || inA.Row != inA.Column) {
                outL = new Matrix(0, 0);
                outU = new Matrix(0, 0);
                return ResultEnum.UnsupportedMatrixShape;
            }

            int N = inA.Row;
            outL = new Matrix(N, N);
            outU = new Matrix(N, N);

            outU.SetIdentity();

            for (int x = 0; x < N; ++x) {
                for (int y = x; y < N; ++y) {
                    double sum = 0;
                    for (int k = 0; k < x; ++k) {
                        sum += outL.At(y,k) * outU.At(k,x);
                    }
                    outL.Set(y,x, inA.At(y,x) - sum);
                }

                for (int y=x; y<N; ++y) {
                    double sum = 0;
                    for (int k = 0; k < x; ++k) {
                        sum += outL.At(x,k) * outU.At(k,y);
                    }
                    if (Math.Abs(outL.At(x,x)) <= epsilon) {
                        return ResultEnum.FailedToChoosePivot;
                    }

                    outU.Set(x, y, (inA.At(x, y) - sum) / outL.At(x, x));
                }
            }

            return ResultEnum.Success;
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

        /// <summary>
        /// aとbが大体同じ時true。異なるときfalse。
        /// </summary>
        public static bool IsSame(Matrix a, Matrix b, double epsilon = 1.0e-7) {
            if (a.Column != b.Column
                    || a.Row != b.Row) {
                return false;
            }

            for (int y = 0; y < a.Row; ++y) {
                for (int x = 0; x < a.Column; ++x) {
                    double distance = Math.Abs(a.At(y, x) - b.At(y, x));

                    if (epsilon < distance) {
                        return false;
                    }
                }
            }

            return true;
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
