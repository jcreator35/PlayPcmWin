using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace WWMath {
    public class WWMatrix {
        private int mRow;
        private int mCol;
        private double[] m;

        /// <summary>
        /// 行列の種類。
        /// </summary>
        public enum MatType {
            /// 不明。
            Undetermined = 1,
            Square = 2,
            LowerTriangular = 4,
            UpperTriangular = 8,
            Diagonal = 16,
            Tridiagonal = 32,
        };

        private ulong mMatTypeFlags = (ulong)MatType.Undetermined;

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
        public WWMatrix(int numRow, int numColumn) {
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
        public WWMatrix(int numRow, int numColumn, double[] v) {
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
        public WWMatrix DeepCopy() {
            var r = new WWMatrix(mRow, mCol, m);
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
            mMatTypeFlags = (ulong)MatType.Undetermined;
        }

        /// <summary>
        /// 単位行列にする。
        /// 自分自身を変更する。
        /// </summary>
        public WWMatrix SetIdentity() {
            if (mRow != mCol) {
                throw new InvalidOperationException("Matrix is not square");
            }

            for (int y = 0; y < mRow; ++y) {
                for (int x = 0; x < mCol; ++x) {
                    Set(y, x, x == y ? 1.0 : 0.0);
                }
            }

            mMatTypeFlags =
                  (ulong)MatType.Square 
                | (ulong)MatType.LowerTriangular
                | (ulong)MatType.UpperTriangular
                | (ulong)MatType.Diagonal
                | (ulong)MatType.Tridiagonal;

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
            mMatTypeFlags = (ulong)MatType.Undetermined;
        }

        public static WWMatrix Mul(WWMatrix lhs, WWMatrix rhs) {
            if (lhs.Column != rhs.Row) {
                throw new ArgumentException("lhs.Column != rhs.Row");
            }

            int loopCount = lhs.Column;

            var rv = new WWMatrix(lhs.Row, rhs.Column);
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
        public WWMatrix Mul(WWMatrix a) {
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

            var aT = new WWMatrix(a.Length, 1, a);
            var rv = Mul(aT);

            return rv.ToArray();
        }

        /// <summary>
        /// 転置した行列を作って戻す。
        /// 自分自身を変更しない。
        /// </summary>
        /// <returns>転置した行列。</returns>
        public WWMatrix Transpose() {
            var t = new WWMatrix(mCol, mRow);
            for (int y = 0; y < mRow; ++y) {
                for (int x = 0; x < mCol; ++x) {
                    t.Set(x, y, At(y, x));
                }
            }

            return t;
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
        public static ResultEnum LUdecompose(WWMatrix inA, out WWMatrix outL, out WWMatrix outU, double epsilon = 1.0e-7) {
            if (inA.Row < 2 || inA.Row != inA.Column) {
                outL = new WWMatrix(0, 0);
                outU = new WWMatrix(0, 0);
                return ResultEnum.UnsupportedMatrixShape;
            }

            int N = inA.Row;
            outL = new WWMatrix(N, N);
            outU = new WWMatrix(N, N);

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
        /// https://www.student.cs.uwaterloo.ca/~cs370/notes/LUExample2.pdf
        /// </summary>
        public static ResultEnum LUdecompose2(WWMatrix inA, out WWMatrix outL, out WWMatrix outP, out WWMatrix outU, double epsilon = 1.0e-7) {
            if (inA.Row < 2 || inA.Row != inA.Column) {
                outP = new WWMatrix(0, 0);
                outL = new WWMatrix(0, 0);
                outU = new WWMatrix(0, 0);
                return ResultEnum.UnsupportedMatrixShape;
            }

            int N = inA.Row;
            outP = new WWMatrix(N, N).SetIdentity();
            outL = new WWMatrix(N, N).SetIdentity();
            outU = new WWMatrix(N, N);

            outU = inA.DeepCopy();

            var PA = new WWMatrix(N, N);

            // Lの0行目は [1 0 0 ... 0]

            for (int c = 0; c < N - 1; ++c) {
                // c個目のピボットを選択する。
                // Uのc列を比較し、絶対値が最大のものを選ぶ。
                // Pのc行目が決定する。
                int maxRow = c;
                {
                    double maxAbs = 0;
                    for (int y = c; y < N; ++y) {
                        if (maxAbs < Math.Abs(outU.At(y, c))) {
                            maxAbs = Math.Abs(outU.At(y, c));
                            maxRow = y;
                        }
                    }
                }
                // ピボットは、maxRow行c列。

                outU.ExchangeRows(c, maxRow);
                outP.ExchangeRows(c, maxRow);

                outL.ExchangeRows(c, maxRow);
                outL.ExchangeCols(c, maxRow);

                // 並び替えによりuccがピボットになった。
                double ucc = outU.At(c, c);
                if (WWMathUtil.IsAlmostZero(ucc, epsilon)) {
                    return ResultEnum.FailedToChoosePivot;
                }

                for (int k = c + 1; k < N; ++k) {
                    // Uのk列目の計算。
                    // Uのk列目を0にするために、1列目を何倍して足すか。
                    double ukc = outU.At(k, c);

                    double ratio = ukc / ucc;
                    for (int x = 0; x < N; ++x) {
                        double ucx = outU.At(c, x);
                        double ukx = outU.At(k, x);
                        outU.Set(k, x, ukx - ratio * ucx);
                    }

                    // Lのk行c列 = ratio。
                    outL.Set(k, c, ratio);
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

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendFormat("rows={0},cols={1}\n", mRow, mCol);
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mCol; ++c) {
                    sb.AppendFormat("\t{0}", At(r, c));
                }
                sb.AppendFormat("\n");
            }
            return sb.ToString();
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
        /// 2つの行列を左側右側として連結した1個の行列を戻す。
        /// </summary>
        /// <param name="left">左側の行列</param>
        /// <param name="right">右側の行列</param>
        /// <returns>[left right]</returns>
        public static WWMatrix JoinH(WWMatrix left, WWMatrix right) {
            if (left.Row != right.Row) {
                throw new ArgumentException("left.Row != right.Row");
            }

            var r = new WWMatrix(left.Row, left.Column + right.Column);
            for (int y = 0; y < left.Row; ++y) {
                for (int x = 0; x < left.Column; ++x) {
                    r.Set(y, x, left.At(y, x));
                }
            }
            for (int y = 0; y < right.Row; ++y) {
                for (int x = 0; x < right.Column; ++x) {
                    r.Set(y,left.Column+x, right.At(y,x));
                }
            }

            return r;
        }

        /// <summary>
        /// 2つの行列を上側、下側として連結した1個の行列を戻す。
        /// </summary>
        /// <param name="top">上側の行列</param>
        /// <param name="bottom">下側の行列</param>
        /// <returns>[top;bottom]</returns>
        public static WWMatrix JoinV(WWMatrix top, WWMatrix bottom) {
            if (top.Column != bottom.Column) {
                throw new ArgumentException("top.column != bottom.column");
            }

            var r = new WWMatrix(top.Row + bottom.Row, top.Column);
            for (int y = 0; y < top.Row; ++y) {
                for (int x = 0; x < top.Column; ++x) {
                    r.Set(y, x, top.At(y, x));
                }
            }
            for (int y = 0; y < bottom.Row; ++y) {
                for (int x = 0; x < bottom.Column; ++x) {
                    r.Set(top.Row+y, x, bottom.At(y, x));
                }
            }

            return r;
        }

        /// <summary>
        /// aとbが大体同じ時true。異なるときfalse。
        /// </summary>
        public static bool IsSame(WWMatrix a, WWMatrix b, double epsilon = 1.0e-7) {
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
