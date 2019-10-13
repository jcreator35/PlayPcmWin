using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWAudioFilterCore {
    public class WWOverlappedFft {
        WWTimeDependentForwardFourierTransform mFFTfwd;
        WWTimeDependentInverseFourierTransform mFFTinv;

        public WWOverlappedFft(int fftLength) {
            mFFTfwd = new WWTimeDependentForwardFourierTransform(fftLength, WWTimeDependentForwardFourierTransform.WindowType.Hann);
            mFFTinv = new WWTimeDependentInverseFourierTransform(fftLength);
        }

        /// <summary>
        /// 次のForwardFft()呼び出しに渡すサンプルの数をこの値にすると最もスムーズに処理が行われる。
        /// </summary>
        public long WantSamples {
            get {
                return mFFTfwd.WantSamples;
            }
        }

        public int FftLength {
            get {
                return mFFTfwd.ProcessSize;
            }
        }

        public WWComplex[] ForwardFft(double[] timeDomain) {
            return mFFTfwd.Process(timeDomain);
        }

        /// <summary>
        /// 出力するサンプル数をセットする。
        /// これを指定しない場合最後のデータが多めに出てくる。
        /// </summary>
        public void SetNumSamples(long n) {
            mFFTinv.SetNumSamples(n);
        }

        /// <summary>
        /// Forward FFTに滞留している入力データをすべて出力。
        /// </summary>
        public WWComplex[] Drain() {
            return mFFTfwd.Drain();
        }

        public double[] InverseFft(WWComplex[] freqDomain) {
            return mFFTinv.Process(freqDomain);
        }
    }
}
