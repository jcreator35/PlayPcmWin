// 日本語。
using System;

namespace WWMath {
    /// <summary>
    /// Linearサポートベクトルマシン2分類器。
    /// 学習済みモデルの係数を使用し、観測値をもとにAかB、2つのどちらかに分類する分類器。
    /// 学習済みモデル生成は教師データ(観測値と分類結果の組)をたくさん用意してMatlab等で行う。
    /// </summary>
    public class BinaryLinearSVMClassifier {

        /// <summary>
        /// betaは、観測値のベクトルの要素数と同じ個数ある。
        /// </summary>
        private float[] mBeta;
        private float mScale;
        private float mBias;

        /// <summary>
        /// 学習によって得られた係数beta, scale, biasで初期化。
        /// </summary>
        public BinaryLinearSVMClassifier(float[] beta, float scale, float bias) {
            if (beta.Length == 0 || Math.Abs(scale) < 1.0e-8f) {
                throw new ArgumentException();
            }

            mBeta = beta;
            mScale = scale;
            mBias = bias;
        }

        /// <summary>
        /// 観測値を入力、A,B2つのどちらかに分類する。
        /// </summary>
        /// <param name="inputX">観測された値。</param>
        /// <returns>分類結果。正のときA、負のときBに分類された。</returns>
        public float Predict(float[] inputX) {
            if (inputX.Length == 0) {
                throw new ArgumentException();
            }

            // y = dot(inputX, mBeta) / scale + bias;

            float y = 0.0f;

            for (int i = 0; i < inputX.Length; ++i) {
                y += inputX[i] * mBeta[i];
            }

            y /= mScale;

            y += mBias;

            return y;
        }
    }
}
