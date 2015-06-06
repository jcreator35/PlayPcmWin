using System;

namespace WWAudioFilter {
    class ButterworthFilter {
        /// <summary>
        /// バターワースフィルターのFFT coeffs
        /// </summary>
        /// <param name="filterSlopeDbOct">1次 = 6(dB/oct), 2次 = 12(dB/oct)</param>
        /// <returns></returns>
        public static WWComplex[] Design(int sampleRate, double cutoffFrequency, int fftLength, int filterSlopeDbOct) {
            int filterLenP1 = fftLength / 4;
            int filterLength = filterLenP1 - 1;
            
            var fromF = new WWComplex[filterLenP1];

            double orderX2 = 2.0 * (filterSlopeDbOct / 6.0);

            double cutoffRatio = cutoffFrequency / (sampleRate / 2);

            // フィルタのF特
            fromF[0].real = 1.0f;
            for (int i = 1; i <= filterLenP1 / 2; ++i) {
                double omegaRatio = i * (1.0 / (filterLenP1 / 2));
                double v = Math.Sqrt(1.0 / (1.0 + Math.Pow(omegaRatio / cutoffRatio, orderX2)));
                if (Math.Abs(v) < Math.Pow(0.5, 24)) {
                    v = 0.0;
                }
                fromF[i].real = v;
            }
            for (int i = 1; i < filterLenP1 / 2; ++i) {
                fromF[filterLenP1 - i].real = fromF[i].real;
            }

            // IFFTでfromFをfromTに変換
            WWComplex[] fromT;
            {
                var fft = new WWRadix2Fft(filterLenP1);
                fromT = fft.ForwardFft(fromF);

                double compensation = 1.0 / (filterLenP1 * cutoffRatio);
                for (int i = 0; i < filterLenP1; ++i) {
                    fromT[i].Set(
                            fromT[i].real * compensation,
                            fromT[i].imaginary * compensation);
                }
            }
            fromF = null;

            // fromTの中心がFILTER_LENGTH/2番に来るようにする。
            // delayT[0]のデータはfromF[FILTER_LENGTH/2]だが、非対称になるので入れない
            // このフィルタの遅延はFILTER_LENGTH/2サンプルある

            var delayT = new WWComplex[filterLenP1];
            for (int i = 1; i < filterLenP1 / 2; ++i) {
                delayT[i] = fromT[i + filterLenP1 / 2];
            }
            for (int i = 0; i < filterLenP1 / 2; ++i) {
                delayT[i + filterLenP1 / 2] = fromT[i];
            }
            fromT = null;

            // Kaiser窓をかける
            var w = WWWindowFunc.KaiserWindow(filterLenP1 + 1, 9.0);
            for (int i = 0; i < filterLenP1; ++i) {
                delayT[i].Mul(w[i]);
            }

            var delayTL = new WWComplex[fftLength];
            for (int i = 0; i < delayT.Length; ++i) {
                delayTL[i] = delayT[i];
            }
            delayT = null;

            // できたフィルタをFFTする
            WWComplex[] delayF;
            {
                var fft = new WWRadix2Fft(fftLength);
                delayF = fft.ForwardFft(delayTL);

                for (int i = 0; i < fftLength; ++i) {
                    delayF[i].Mul(cutoffRatio);
                }
            }
            delayTL = null;

            return delayF;
        }
    }
}
