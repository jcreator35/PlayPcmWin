using System;
using System.IO;

namespace QuantizationDemo {
    class WaveForms {
        public double[] OriginalSignal { get; set; }
        public double[] Noise { get; set; }
        public double[] OriginalSignalPlusNoise { get; set; }
        public double[] Quantized { get; set; }
        public double[] QuantizationNoise { get; set; }

        public int QuantizationBit { get { return mQuantizationBit; } }

        private double mSignalAmplitude;
        private double mNoiseDb;
        private int mQuantizationBit;
        private int mSampleCount;

        private double Quantize(double pcm, int bitsPerSample) {
            System.Diagnostics.Debug.Assert(0 < bitsPerSample && bitsPerSample <= 32);

            long sample32 = (long)(pcm * (Int32.MaxValue+1L) + 0.5 * Math.Pow(2.0, 32 - bitsPerSample));
            if (Int32.MaxValue < sample32) {
                sample32 = Int32.MaxValue;
            }
            if (sample32 < Int32.MinValue) {
                sample32 = Int32.MinValue;
            }

            // sample32に32bit int型のサンプル値が入っている
                 
            if (bitsPerSample < 32) {
                // 量子化ビット数を減らす。
                sample32 >>= (32 - bitsPerSample);
                sample32 <<= (32 - bitsPerSample);
            }

            return sample32 * (1.0 / (Int32.MaxValue+1L));
        }

        private void WriteWav(double[] pcm, string path) {
            // 32bit floatに変換してbyte[]に入れる。
            var pcmTo = new byte[mSampleCount * 4];
            for (int idx = 0; idx < mSampleCount; ++idx) {
                float f = (float)pcm[idx];
                var b = BitConverter.GetBytes(f);
                for (int o = 0; o < 4; ++o) {
                    pcmTo[idx * 4 + o] = b[o];
                }
            }

            using (var bw = new BinaryWriter(File.Open(path, FileMode.Create))) {
                WavWriter.Write(1, 32, 32, 44100, mSampleCount, 3, pcmTo, bw);
            }
        }

        public bool Update(double signalAmplitudeDb, double noiseDbParam, int quantizationBitParam,
                double signalStepAngle, int sampleCount) {
            OriginalSignal = null;
            Noise = null;
            OriginalSignalPlusNoise = null;
            Quantized = null;
            QuantizationNoise = null;

            mSignalAmplitude = Math.Pow(10, signalAmplitudeDb / 20.0);
            mNoiseDb = noiseDbParam;
            mQuantizationBit = quantizationBitParam;
            mSampleCount = sampleCount;

            OriginalSignal = new double[sampleCount];
            for (int i = 0; i < sampleCount; ++i) {
                double rad = signalStepAngle * i;
                while (2.0 * Math.PI <= rad) {
                    rad -= 2.0 * Math.PI;
                }
                OriginalSignal[i] = mSignalAmplitude * Math.Sin(rad);
            }

            {
                Noise = new double[sampleCount];
                var gng = new WWAudioFilter.GaussianNoiseGenerator();
                var noiseScale = Math.Pow(10, mNoiseDb / 20.0);
                for (int i = 0; i < sampleCount; ++i) {
                    Noise[i] = gng.NextFloat() * noiseScale;
                }
            }

            OriginalSignalPlusNoise = new double[sampleCount];
            for (int i = 0; i < sampleCount; ++i) {
                OriginalSignalPlusNoise[i] = OriginalSignal[i] + Noise[i];
            }

            Quantized = new double[sampleCount];
            QuantizationNoise = new double[sampleCount];
            for (int i = 0; i < sampleCount; ++i) {
                Quantized[i] = Quantize(OriginalSignalPlusNoise[i], mQuantizationBit);
                QuantizationNoise[i] = OriginalSignalPlusNoise[i] - Quantized[i];
            }

#if true
            WriteWav(OriginalSignal,          "Output_Original.wav");
            WriteWav(Noise,                   "Output_Noise.wav");
            WriteWav(OriginalSignalPlusNoise, "Output_OriginalPlusNoise.wav");
            WriteWav(QuantizationNoise,       "Output_QuantizationNoise.wav");
            WriteWav(Quantized,               "Output_QuantizerOutput.wav");
#endif

            return true;
        }

    }
}
