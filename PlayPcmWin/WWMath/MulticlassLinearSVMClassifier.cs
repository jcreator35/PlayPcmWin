using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    /// <summary>
    /// 複数個のBinarySVMを組み合わせて多クラス分類する。
    /// (A,B,C)の3クラス分類のためには以下の計3個の2クラス分類器をこの順に並べる。
    /// ・(A,B)2クラス分類器
    /// ・(A,C)2クラス分類器
    /// ・(B,C)2クラス分類器
    /// 同様に、Nクラス分類のためには2クラス分類器を(N-1)*N/2個必要とする。
    /// 学習済みデータをMatlab等で用意する。
    /// </summary>
    public class MulticlassLinearSVMClassifier {
        private int mNumInputXCount;
        private int mNumOutputClasses;
        private string [] mOutputClassLabels;
        private List<BinaryLinearSVMClassifier> mClassifierList = new List<BinaryLinearSVMClassifier>();

        public MulticlassLinearSVMClassifier(int numInputXCount, int numOutputClasses) {
            mNumInputXCount = numInputXCount;
            mNumOutputClasses = numOutputClasses;
        }

        public void AddBinaryClassifier(BinaryLinearSVMClassifier c) {
            mClassifierList.Add(c);
        }

        /// <summary>
        /// optional. 出力ラベル文字列をセット。
        /// </summary>
        /// <param name="labels"></param>
        public void SetOutputClassLabels(string[] labels) {
            if (labels.Length != mNumOutputClasses) {
                throw new ArgumentException();
            }
            mOutputClassLabels = labels;
        }

        /// <summary>
        /// 分類結果の値を文字列にする。
        /// </summary>
        public string ClassifyResultToStr(int idx) {
            return mOutputClassLabels[idx];
        }

        /// <summary>
        /// 多クラス分類する。
        /// </summary>
        /// <param name="inputX">観測値。</param>
        /// <returns>分類結果。0のときA、1のときB、2のときC、…</returns>
        public int Classify(float[] inputX) {
            // 入力値inputXのチェック。
            if (inputX.Length != mNumInputXCount) {
                throw new ArgumentException();
            }

            // 観測値をノーマライズする。
            float[] inputXN;
            var normalizer = new WWNormalize();
            normalizer.Normalize(inputX, out inputXN);

            // 2クラス分類器の個数チェック。
            int nClassifier = mNumOutputClasses * (mNumOutputClasses - 1) / 2;
            System.Diagnostics.Debug.Assert(mClassifierList.Count() == nClassifier);

            // 投票結果置き場。
            var vote = new int[mNumOutputClasses];

            int n = 0;
            for (int i = 0; i < mNumOutputClasses - 1; ++i) {
                for (int j = i + 1; j < mNumOutputClasses; ++j) {
                    // classifier: iかjかを判定する分類器。戻り値が+のときi、-のときj。
                    var classifier = mClassifierList[n];
                    float y = classifier.Predict(inputXN);

                    if (0 <= y) {
                        vote[i] += 1;
                    } else {
                        vote[j] += 1;
                    }

                    ++n;
                }
            }

            // 最も投票数が大きいものを戻す。
            int result = -1;
            {
                int maxCount = -1;
                for (int i = 0; i < mNumOutputClasses; ++i) {
                    if (maxCount < vote[i]) {
                        maxCount = vote[i];
                        result = i;
                    }
                }

                //Console.WriteLine("Classify() y={0} maxCount={1}", result, maxCount);
            }

            return result;
        }

    }
}
