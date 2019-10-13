
namespace WWMath {
    public class WWOverlappedFft {
        WWTimeDependentForwardFourierTransform mFFTfwd;
        WWTimeDependentInverseFourierTransform mFFTinv;

        /// <summary>
        /// 入力データが完全に復元できる特殊な窓関数を使用したオーバーラップ・アドFFT。
        /// あらかじめSetNumSamples()を呼ぶと入出力サンプル数が一致する。
        /// </summary>
        /// <param name="fftLength">Forward FFT長。</param>
        /// <param name="ifftLength">Inverse FFT長。0のときForward FFT長を使用。</param>
        public WWOverlappedFft(int fftLength, int ifftLength=0) {
            mFFTfwd = new WWTimeDependentForwardFourierTransform(fftLength, WWTimeDependentForwardFourierTransform.WindowType.Hann);

            if (ifftLength == 0) {
                ifftLength = fftLength;
            }
            mFFTinv = new WWTimeDependentInverseFourierTransform(ifftLength);
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
        /// これを指定しない場合Drain()したデータをInverseFft()したとき最後のデータが多めに出てくる。
        /// </summary>
        public void SetNumOutSamples(long n) {
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
