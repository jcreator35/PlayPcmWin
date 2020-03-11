using System;
using System.Linq;
using System.Collections.Generic;

namespace WWMath {
    public class Matrix {
        private int mRow;
        private int mCol;
        private double[] m;

        /// <summary>
        /// 行列の種類。
        /// </summary>
        public enum MatType {
            Square = 1,
            LowerTriangular = 2,
            UpperTriangular = 4,
            Diagonal = 8,
            Tridiagonal = 16,
        };

        private ulong mMatTypeFlags = 0;

        public int Row {
            get { return (int)mRow; }
        }

        public int Column {
            get { return (int)mCol; }
        }

        private double[] ToArray() {
            return m.ToArray();
        }

        /// <summary>
        /// MatType combination
        /// </summary>
        public ulong MatTypeFlags {
            get {
                return mMatTypeFlags;
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
        /// コピーを作って戻す。
        /// mの実体も複製される。
        /// </summary>
        public Matrix DeepCopy() {
            var r = new Matrix(mRow, mCol, m);
            return r;
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
        public Matrix SetIdentity() {
            if (mRow != mCol) {
                throw new InvalidOperationException("Matrix is not square");
            }

            for (int y = 0; y < mRow; ++y) {
                for (int x = 0; x < mCol; ++x) {
                    Set(y, x, x == y ? 1.0 : 0.0);
                }
            }
            return this;
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
                    if (WWMathUtil.IsAlmostZero(outL.At(x,x),epsilon)) {
                        return ResultEnum.FailedToChoosePivot;
                    }

                    outU.Set(x, y, (inA.At(x, y) - sum) / outL.At(x, x));
                }
            }

            return ResultEnum.Success;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Crout_matrix_decomposition
        /// </summary>
        public static ResultEnum LUdecompose2(Matrix inA, out Matrix outL, out Matrix outP, out Matrix outU, double epsilon = 1.0e-7) {
            if (inA.Row < 2 || inA.Row != inA.Column) {
                outP = new Matrix(0, 0);
                outL = new Matrix(0, 0);
                outU = new Matrix(0, 0);
                return ResultEnum.UnsupportedMatrixShape;
            }

            int N = inA.Row;
            outP = new Matrix(N, N).SetIdentity();
            outL = new Matrix(N, N).SetIdentity();
            outU = new Matrix(N, N);

            outU = inA.DeepCopy();

            var PA = new Matrix(N, N);

            // Lの0行目は [1 0 0 ... 0]
            {
                // 最初のピボットを選択する。
                // column=0で、絶対値が最大のものを探す。
                int maxRow = 0;
                double max = 0;
                for (int y = 0; y < N; ++y) {
                    if (max < inA.At(y, 0)) {
                        max = inA.At(y, 0);
                        maxRow = y;
                    }
                }
                // ピボットは、maxRow行0列。

                // Uの0行目、Pの0行目が決定する。
                outU.ExchangeRows(0, maxRow);
                outP.ExchangeRows(0, maxRow);

                // 並び替えによりu00がピボットになった。
                double u00 = outU.At(0, 0);
                if (WWMathUtil.IsAlmostZero(u00, epsilon)) {
                    return ResultEnum.FailedToChoosePivot;
                }

                for (int k = 1; k < N; ++k) {
                    // Uの0行k列の計算。
                    // Uの0行k列を0にするために、0行0列目をratio倍して引く。
                    double uk0 = outU.At(k, 0);

                    double ratio = uk0 / u00;
                    for (int x = 0; x < N; ++x) {
                        double u0x = outU.At(0, x);
                        double ukx = outU.At(k, x);
                        outU.Set(k, x, ukx - ratio * u0x);
                    }

                    // Lの0行k列 = ratio。
                    outL.Set(k, 0, ratio);
                }
            }

            {
                // 2つめのピボットを選択する。
                // Uの1行1列と2行1列を比較し、絶対値が最大のものを選ぶ。
                // Pの2行目が決定する。
                int maxRow = 1;
                double max = 0;
                for (int y = 1; y < N; ++y) {
                    if (max < outU.At(y, 1)) {
                        max = outU.At(y, 1);
                        maxRow = y;
                    }
                }
                // ピボットは、maxRow行1列。

                outU.ExchangeRows(1, maxRow);
                outP.ExchangeRows(1, maxRow);

                outL.ExchangeRows(1, maxRow);
                outL.ExchangeCols(1, maxRow);

                // 並び替えによりu11がピボットになった。
                double u11 = outU.At(1, 1);
                if (WWMathUtil.IsAlmostZero(u11, epsilon)) {
                    return ResultEnum.FailedToChoosePivot;
                }

                for (int k = 2; k < N; ++k) {
                    // Uのk列目の計算。
                    // Uのk列目を0にするために、1列目を何倍して足すか。
                    double uk1 = outU.At(k, 1);

                    double ratio = uk1 / u11;
                    for (int x = 0; x < N; ++x) {
                        double u1x = outU.At(1, x);
                        double ukx = outU.At(k, x);
                        outU.Set(k, x, ukx - ratio * u1x);
                    }

                    // Lの1行k列 = ratio。
                    outL.Set(k, 1, ratio);
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
        /// 行を交換する。
        /// </summary>
        public void ExchangeRows(int rowA, int rowB) {
            if (rowA < 0 || mRow <= rowA) {
                throw new ArgumentOutOfRangeException("rowA");
            }
            if (rowB < 0 || mRow <= rowB) {
                throw new ArgumentOutOfRangeException("rowB");
            }

            if (rowA == rowB) {
                return;
            }

            for (int x = 0; x < mCol; ++x) {
                // (rowA,x)の値と(rowB,x)の値を交換する。
                double t = At(rowA, x);
                Set(rowA, x, At(rowB, x));
                Set(rowB, x, t);
            }
        }

        /// <summary>
        /// 列を交換する。
        /// </summary>
        public void ExchangeCols(int colA, int colB) {
            if (colA < 0 || mCol <= colA) {
                throw new ArgumentOutOfRangeException("colA");
            }
            if (colB < 0 || mCol <= colB) {
                throw new ArgumentOutOfRangeException("colB");
            }

            if (colA == colB) {
                return;
            }

            for (int x = 0; x < mRow; ++x) {
                // (x, colA)の値と(x, colB)の値を交換する。
                double t = At(x, colA);
                Set(x, colA, At(x, colB));
                Set(x, colB, t);
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
                    if (!WWMathUtil.IsAlmostZero(a.At(y, x) - b.At(y, x), epsilon)) {
                        return false;
                    }
                }
            }

            return true;
        }
        
        public ulong DetermineMatType(double epsilon = 1.0e-7) {
            mMatTypeFlags = 0;

            if (mRow != mCol) {
                // 正方行列でない。
                return mMatTypeFlags;
            }

            // 正方行列である。
            mMatTypeFlags |= (ulong)MatType.Square;

            {   // 上三角行列か。
                // 上三角行列＝左下が0。
                bool b = true;
                for (int x = 0; x < mCol; ++x) {
                    for (int y = x + 1; y < mRow; ++y) {
                        if (!WWMathUtil.IsAlmostZero(At(y, x), epsilon)) {
                            b = false;
                            break;
                        }
                    }
                    if (!b) {
                        break;
                    }
                }
                if (b) {
                    mMatTypeFlags |= (ulong)MatType.UpperTriangular;
                }
            }

            {   // 下三角行列か。
                // 下三角行列＝右上が0。
                bool b = true;
                for (int y = 0; y < mRow; ++y) {
                    for (int x = y+1; x < mCol; ++x) {
                        if (!WWMathUtil.IsAlmostZero(At(y, x), epsilon)) {
                            b = false;
                            break;
                        }
                    }
                    if (!b) {
                        break;
                    }
                }
                if (b) {
                    mMatTypeFlags |= (ulong)MatType.LowerTriangular;
                }
            }

            if (0 != (mMatTypeFlags & (ulong)MatType.UpperTriangular)
                    && 0 != (mMatTypeFlags & (ulong)MatType.LowerTriangular)) {
                // 上三角かつ下三角のとき、対角行列。
                mMatTypeFlags |= (ulong)MatType.Diagonal;
            }

            {   // 三重対角行列か。
                bool isTriDiag = true;
                    
                // 右上が0である事。
                for (int y = 0; y < mRow; ++y) {
                    for (int x = y + 2; x < mCol; ++x) {
                        if (!WWMathUtil.IsAlmostZero(At(y, x), epsilon)) {
                            isTriDiag = false;
                            break;
                        }
                    }
                    if (!isTriDiag) {
                        break;
                    }
                }
                if (isTriDiag) {
                    // 左下が0である事。
                    for (int x = 0; x < mCol; ++x) {
                        for (int y = x + 2; y < mRow; ++y) {
                            if (!WWMathUtil.IsAlmostZero(At(y, x), epsilon)) {
                                isTriDiag = false;
                                break;
                            }
                        }
                        if (!isTriDiag) {
                            break;
                        }
                    }
                }
                if (isTriDiag) {
                    mMatTypeFlags |= (ulong)MatType.Tridiagonal;
                }
            }
            return mMatTypeFlags;
        }
    }
}
