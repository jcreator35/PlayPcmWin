using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWImpulseResponse {
    class MLSDeconvolution {
        private int mOrder;
        private List<int> mFromReorder;
        private List<int> mToReorder;

        public MLSDeconvolution(int order) {
            mOrder = order;
            int N = mOrder;
            int P = (1 << N) - 1;

            byte[] mlsSeq;
            {
                var mls = new MaximumLengthSequence(N);
                mlsSeq = mls.Sequence();
                /*
                for (int i = 0; i < mlsSeq.Length; ++i) {
                    Console.Write("{0} ", mlsSeq[i]);
                }
                Console.WriteLine("");
                */
            }

            // S: MLS行列の上N行。
            var S = new MatrixGF2(N, P);
            {
                for (int y = 0; y < N; ++y) {
                    for (int x = 0; x < P; ++x) {
                        S.Set(y, x, (0 != mlsSeq[(x + y) % P]) ? GF2.One : GF2.Zero);
                    }
                }
            }
            //S.Print("S");

            // σ: MLS行列の左上NxN
            var σ = S.Subset(0, 0, N, N);
            //σ.Print("σ");

            var σInv = σ.Inverse();
            //σInv.Print("σ^-1");

            // L: Psの転置 x σInv
            var L = S.Transpose().Mul(σInv);
            //L.Print("L");

            // Ps: S行列の列の値を2進数として、順番入れ替えのための情報PsReorderを作る
            mFromReorder = new List<int>();
            mFromReorder.Add(0);
            for (int c = 0; c < P; ++c) {
                int sum = 0;
                for (int r = 0; r < N; ++r) {
                    sum += (1 << (N - 1 - r)) * S.At(r, c).Val;
                }
                //Console.WriteLine("Ps: c={0} sum={1}", c, sum);
                mFromReorder.Add(sum);
            }

            // Pl: L行列の列の値を2進数として、順番入れ替えのための情報PlReorderを作る
            mToReorder = new List<int>();
            mToReorder.Add(0);
            for (int r = 0; r < P; ++r) {
                int sum = 0;
                for (int c = 0; c < N; ++c) {
                    sum += (1 << (N - 1 - c)) * L.At(r, c).Val;
                }
                //Console.WriteLine("Pl: r={0} sum={1}", r, sum);
                mToReorder.Add(sum);
            }
        }

        /// <summary>
        /// このシーケンスをリピート再生し、(1 &lt;&lt; order)サンプル録音し、Deconvoutionを呼び出す。
        /// </summary>
        /// <returns></returns>
        public double[] MLSSequence() {
            var mls = new MaximumLengthSequence(mOrder);
            var b = mls.Sequence();
            var rv = new double[b.Length];
            for (int i = 0; i < b.Length; ++i) {
                rv[i] = b[i] * 2.0 - 1.0;
            }

            return rv;
        }

        /// <summary>
        /// Maximum Length Sequenceを録再したものを入力し、
        /// MLSとのCircular Cross Correlationを計算し出力する。
        /// </summary>
        /// <param name="recorded">Maximum Length Sequenceを録再したもの。長さは(1 &lt;&lt; order)サンプル。</param>
        /// <returns></returns>
        public double[] Deconvolution(double[] from) {
            int N = mOrder;
            int P = (1 << N) - 1;

            if (from.Length != P + 1) {
                throw new ArgumentException("from.Length should be (1 << order)");
            }

            // Walsh-Hadamard変換を使ってMLS deconvolutionを行う。
            var reorderedFrom = new double[P + 1];
            for (int i = 0; i < P + 1; ++i) {
                reorderedFrom[mFromReorder[i]] = from[i];
            }

            var hR = FastWalshHadamardTransform.Transform(reorderedFrom);

            var hTo = new double[P + 1];
            for (int i = 0; i < P + 1; ++i) {
                hTo[i] = -(hR[mToReorder[i]] - 1.0) / (P+1);
            }
            //Print(hTo, "hTo");

            return hTo;
        }

#if true
        public void Test(double[] recorded) {
            // 動作テスト

            int N = mOrder;
            int P = (1 << N) - 1;

            var from = new double[P + 1];
            {
                int copySize = recorded.Length;
                if (from.Length < copySize) {
                    copySize = from.Length;
                }
                Array.Copy(recorded, from, copySize);
            }

            byte[] mlsSeq;
            {
                var mls = new MaximumLengthSequence(N);
                mlsSeq = mls.Sequence();
                for (int i = 0; i < mlsSeq.Length; ++i) {
                    Console.Write("{0} ", mlsSeq[i]);
                }
                Console.WriteLine("");
            }

            {
                var mlsD = new double[mlsSeq.Length];
                for (int i = 0; i < mlsD.Length; ++i) {
                    mlsD[i] = mlsSeq[i] * 2.0 - 1.0;
                }
                var ccc = CrossCorrelation.CalcCircularCrossCorrelation(mlsD, mlsD);
                for (int i = 0; i < ccc.Length; ++i) {
                    Console.Write("{0:g2} ", ccc[i]);
                }
                Console.WriteLine("");
            }

            var mlsMat = new MatrixGF2(P, P);
            {
                for (int y = 0; y < P; ++y) {
                    for (int x = 0; x < P; ++x) {
                        mlsMat.Set(y, x, (0 != mlsSeq[(x + y) % P]) ? GF2.One : GF2.Zero);
                    }
                }
            }
            mlsMat.Print("MLS matrix");

            // σ: mlsMatの左上N*N要素
            var σ = mlsMat.Subset(0, 0, N, N);
            σ.Print("σ");

            var σInv = σ.Inverse();
            σInv.Print("σ^-1");

            // S: MLS行列の上N行。
            var S = mlsMat.Subset(0, 0, N, P);
            S.Print("S");

            // Sの転置S^T
            S.Transpose().Print("S^T");

            // L: Sの転置 x σInv
            var L = S.Transpose().Mul(σInv);
            L.Print("L");

            // L x S == MLS行列
            var LS = L.Mul(S);
            LS.Print("L x S");

            int diff = LS.CompareTo(mlsMat);
            System.Diagnostics.Debug.Assert(diff == 0);

            // 2進で0～P-1までの値が入っている行列
            var B = new MatrixGF2(P + 1, N);
            var Bt = new MatrixGF2(N, P + 1);
            for (int r = 0; r < P + 1; ++r) {
                for (int c = 0; c < N; ++c) {
                    int v = r & (1 << (N - 1 - c));
                    var b = (v == 0) ? GF2.Zero : GF2.One;
                    B.Set(r, c, b);
                    Bt.Set(c, r, b);
                }
            }

            B.Print("B");
            Bt.Print("Bt");

            // アダマール行列H8
            var H8 = MatrixGF2.Mul(B, Bt);
            H8.Print("H8");

            var vTest = new double[P + 1];
            for (int i = 0; i < P + 1; ++i) {
                vTest[i] = i;
            }

            var r1 = H8.ToMatrix().Mul(vTest);
            Print(r1, "R1");

            var r2 = FastWalshHadamardTransform.Transform(vTest);
            Print(r2, "R2");

            // Ps: S行列の列の値を2進数として、順番入れ替え行列を作る
            var Ps = new MatrixGF2(P + 1, P + 1);
            var PsReorder = new List<int>();
            PsReorder.Add(0);
            for (int c = 0; c < P; ++c) {
                int sum = 0;
                for (int r = 0; r < N; ++r) {
                    sum += (1 << (N - 1 - r)) * S.At(r, c).Val;
                }
                Console.WriteLine("Ps: c={0} sum={1}", c, sum);
                Ps.Set(sum, c + 1, GF2.One);
                PsReorder.Add(sum);
            }
            Ps.Print("Ps");

            {
                var testMat = new WWMatrix(P + 1, 1);
                for (int r = 0; r < P + 1; ++r) {
                    testMat.Set(r, 0, PsReorder[r]);
                }

                var PsTest = Ps.ToMatrix().Mul(testMat);
                PsTest.Print("Ps x n");
            }

            // Pl: L行列の列の値を2進数として、順番入れ替え行列を作る
            var Pl = new MatrixGF2(P + 1, P + 1);
            var PlReorder = new List<int>();
            PlReorder.Add(0);
            for (int r = 0; r < P; ++r) {
                int sum = 0;
                for (int c = 0; c < N; ++c) {
                    sum += (1 << (N - 1 - c)) * L.At(r, c).Val;
                }
                Console.WriteLine("Pl: r={0} sum={1}", r, sum);
                Pl.Set(r + 1, sum, GF2.One);
                PlReorder.Add(sum);
            }
            Pl.Print("Pl");

            S.Print("S");
            var BtPs = Bt.Mul(Ps);
            BtPs.Print("BtPs");

            L.Print("L");
            var PlB = Pl.Mul(B);
            PlB.Print("PlB");

            {
                var test2Mat = new WWMatrix(P + 1, 1);
                for (int r = 0; r < P + 1; ++r) {
                    test2Mat.Set(r, 0, r);
                }

                var PlTest = Pl.ToMatrix().Mul(test2Mat);
                PlTest.Print("Pl x n");
            }

            mlsMat.Print("MLS mat");
            var Mhat = Pl.Mul(H8).Mul(Ps);
            Mhat.Print("Mhat");

            // MLS deconvolution
            var decon = Mhat.ToMatrix().Mul(from);
            Print(decon, "decon");

            // 同じ処理をWalsh-Hadamard変換を使って行う。
            var reorderedFrom = new double[P + 1];
            for (int i = 0; i < P + 1; ++i) {
                reorderedFrom[PsReorder[i]] = from[i];
            }

#if true
            var hR = FastWalshHadamardTransform.Transform(reorderedFrom);
#else
            var hR = H8.ToMatrix().Mul(reorderedFrom);
#endif

            var hTo = new double[P + 1];
            for (int i = 0; i < P + 1; ++i) {
                hTo[i] = hR[PlReorder[i]];
            }
            Print(hTo, "hTo");
        }
#endif

        private static void Print(double[] v, string s) {
            Console.Write("{0} := {{", s);
            for (int i = 0; i < v.Length; ++i) {
                Console.Write("{0} ", v[i]);
            }
            Console.WriteLine("}}");
        }

    }

}
