using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WWMath {
    public class MatrixGF2 {
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
        public MatrixGF2(int row, int column) {
            mRow = row;
            mCol = column;
            m = new GF2[mRow * mCol];
            for (int i = 0; i < m.Length; ++i) {
                m[i] = GF2.Zero;
            }
        }

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
        public MatrixGF2(int numRow, int numColumn, GF2[] v) {
            mRow = numRow;
            mCol = numColumn;

            if (mRow * mCol != v.Length) {
                throw new ArgumentException("v.length and row * column mismatch");
            }

            m = new GF2[mRow * mCol];
            Array.Copy(v, m, m.Length);
        }

        public GF2 At(int row, int column) {
            if (row < 0 || mRow <= row) {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || mCol <= column) {
                throw new ArgumentOutOfRangeException("column");
            }

            return m[column + mCol * row];
        }

        public void Set(int row, int column, GF2 v) {
            m[column + mCol * row] = v;
        }

        /// <summary>
        /// 単位行列をセットする。
        /// 自分自身を変更する。
        /// </summary>
        public void SetIdentity() {
            if (mRow != mCol) {
                throw new InvalidOperationException();
            }

            for (int r=0; r < mRow; ++r) {
                for (int c=0; c < mCol; ++c) {
                    var v = ( r == c ) ? GF2.One : GF2.Zero;
                    Set(r, c, v);
                }
            }
        }

        /// <summary>
        /// rv = lhs * rhs
        /// 自分自身(lhs,rhs)を変更しない。
        /// </summary>
        /// <returns>乗算結果。</returns>
        public static MatrixGF2 Mul(MatrixGF2 lhs, MatrixGF2 rhs) {
            if (lhs.Column != rhs.Row) {
                throw new ArgumentException("lhs.Column != rhs.Row");
            }

            int loopCount = lhs.Column;

            var rv = new MatrixGF2(lhs.Row, rhs.Column);
            Parallel.For(0, rv.Row, r => {
                for (int c = 0; c < rv.Column; ++c) {
                    GF2 v = GF2.Zero;
                    for (int i = 0; i < loopCount; ++i) {
                        v = GF2.Add(v, GF2.Mul(lhs.At(r, i), rhs.At(i, c)));
                    }
                    rv.Set(r, c, v);
                }
            });

            return rv;
        }

        /// <summary>
        /// rv := this * a
        /// 自分自身を変更しない。
        /// </summary>
        /// <returns>乗算結果。</returns>
        public MatrixGF2 Mul(MatrixGF2 a) {
            return Mul(this, a);
        }

        /// <summary>
        /// 自分自身を変更しない
        /// </summary>
        /// <returns>逆行列</returns>
        public MatrixGF2 Inverse() {
            if (mRow != mCol) {
                throw new NotImplementedException();
            }
            int n = mRow;

            // org := this
            var org = new MatrixGF2(n, n, m);

            // inv := 単位行列
            var inv = new MatrixGF2(n, n);
            inv.SetIdentity();

            // ピボットの候補
            var pivotList = new List<int>();
            for (int i=0; i < n; ++i) {
                pivotList.Add(i);
            }

            for (int c=0; c < n; ++c) {
                // c列の値が1の行を1つだけにする。

                // c列が値1の行を探す。
                int pivRow = -1;
                foreach (var r in pivotList) {
                    if (GF2.One == org.At(r, c)) {
                        pivRow = r;
                        break;
                    }
                }
                if (pivRow < 0) {
                    // 逆行列が存在しない。
                    return null;
                }
                pivotList.Remove(pivRow);

                // pivot以外の行のc列の値を0にする。
                for (int r=0; r < n; ++r) {
                    if (r == pivRow) {
                        continue;
                    }

                    if (org.At(r, c) == GF2.One) {
                        org.AddRow(pivRow, r);
                        inv.AddRow(pivRow, r);
                    }
                }
            }

            // 各々の列は値1が1度だけ現れる。

            // sが正方行列になるように行を入れ替える。
            for (int c=0; c < n; ++c) {
                // c列の値が1の行を探す。
                int oneRow = -1;
                for (int r=0; r < n; ++r) {
                    if (org.At(r, c) == GF2.One) {
                        oneRow = r;
                        break;
                    }
                }

                if (oneRow == c) {
                    // 既にc列c行が1である。
                    continue;
                }

                // c行とoneRow行を入れ替えてc列c行を1にする。
                org.SwapRow(oneRow, c);
                inv.SwapRow(oneRow, c);
            }

            return inv;
        }

        /// <summary>
        /// toRow := toRow + fromRow
        /// 自分自身を変更する。
        /// </summary>
        private void AddRow(int fromRow, int toRow) {
            if (fromRow < 0 || mRow <= fromRow) {
                throw new ArgumentOutOfRangeException("fromRow");
            }
            if (toRow < 0 || mRow <= toRow) {
                throw new ArgumentOutOfRangeException("toRow");
            }

            for (int c=0; c < mCol; ++c) {
                var v = GF2.Add(At(fromRow, c), At(toRow, c));
                Set(toRow, c, v);
            }
        }

        /// <summary>
        /// 行aとbを入れ替える。
        /// 自分自身を変更する。
        /// </summary>
        private void SwapRow(int a, int b) {
            if (a < 0 || mRow <= a) {
                throw new ArgumentOutOfRangeException("a");
            }
            if (b < 0 || mRow <= b) {
                throw new ArgumentOutOfRangeException("b");
            }

            if (a == b) {
                return;
            }

            for (int c=0; c < mCol; ++c) {
                var vA = At(a, c);
                var vB = At(b, c);
                Set(a, c, vB);
                Set(b, c, vA);
            }
        }

        public int Row {
            get {
                return mRow;
            }
        }

        public int Column {
            get {
                return mCol;
            }
        }

        public MatrixGF2 Subset(int startR, int startC, int numR, int numC) {
            if (startR < 0 || mRow <= startR) {
                throw new ArgumentOutOfRangeException("startR");
            }
            if (startC < 0 || mCol <= startC) {
                throw new ArgumentOutOfRangeException("startC");
            }
            if (numR < 0 || mRow < startR + numR) {
                throw new ArgumentOutOfRangeException("numR");
            }
            if (numC < 0 || mCol < startC + numC) {
                throw new ArgumentOutOfRangeException("numC");
            }

            var rv = new MatrixGF2(numR, numC);
            for (int r = 0; r < numR; ++r) {
                for (int c = 0; c < numC; ++c) {
                    rv.Set(r, c, At(startR + r, startC + c));
                }
            }

            return rv;
        }

        /// <summary>
        /// 転置した行列を戻す。
        /// 自分自身を変更しない。
        /// </summary>
        /// <returns>Transposed Matrix</returns>
        public MatrixGF2 Transpose() {
            var rv = new MatrixGF2(mCol, mRow);
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mCol; ++c) {
                    rv.Set(c, r, At(r, c));
                }
            }
            return rv;
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
        /// 他の行列aと同じかどうか。
        /// </summary>
        /// <returns>同じなら0。異なるときは0以外。</returns>
        public int CompareTo(MatrixGF2 a) {
            if (mRow != a.mRow || mCol != a.mCol) {
                // 異なる。
                return 1;
            }

            int acc = 0;
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mCol; ++c) {
                    acc += At(r, c).Add(a.At(r, c)).Val;
                }
            }

            return acc;
        }

        public Matrix ToMatrix() {
            Matrix rv = new Matrix(mRow, mCol);
            for (int r = 0; r < mRow; ++r) {
                for (int c = 0; c < mCol; ++c) {
                    rv.Set(r, c, At(r, c).Val);
                }
            }

            return rv;
        }

        private int mRow;
        private int mCol;
        private GF2[] m;
    }
}
