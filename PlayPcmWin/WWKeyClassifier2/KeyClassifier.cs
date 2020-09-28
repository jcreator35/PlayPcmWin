// 日本語。
using System;
using System.Collections.Generic;
using System.Linq;
using WWMath;
using WWUtil;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace WWKeyClassifier2 {
    class KeyClassifier {
        private KeyClassifierCore mKeyClassifier;

        private const int SAMPLE_RATE   = 44100;
        private const int WINDOW_LENGTH = 16384;
        private const int TEMPORAL_WIDTH = 8;

        // keyを確定するために必要な連続推定一致数。
        private const int KEY_COUNTER = 16;
        
        private Stopwatch mReportSW = new Stopwatch();

        private BackgroundWorker mBW;

        public KeyClassifier() {
            mKeyClassifier = new KeyClassifierCore();
            /*
            // 分類器動作テスト。
            int nCorrect = 0;
            for (int i = 0; i < mKeyClassifier.TrainDataAry.Count(); ++i) {
                var td = mKeyClassifier.TrainDataAry[i];
                int yPred = mKeyClassifier.Classify(td.x);
                if (td.y == yPred) {
                    ++nCorrect;
                } else {
                    //Console.WriteLine("Wrong.");
                }
            }
            Console.WriteLine("Correct {0}%", 100.0f * nCorrect / mKeyClassifier.TrainDataAry.Count());
            */
        }

        private LargeArray<float> StereoToMono(LargeArray<byte> v) {
            var m = new LargeArray<float>(v.LongLength / 4);
            for (long i = 0; i < v.LongLength / 4; ++i) {
                short l = (short)((int)v.At(i * 4 + 0) + 256 * v.At(i * 4 + 1));
                short r = (short)((int)v.At(i * 4 + 2) + 256 * v.At(i * 4 + 3));

                short mix = (short)((l+r)/2);
                m.Set(i, mix / 32768.0f);
            }

            return m;
        }

        List<int> mKeyHistory = new List<int>();

        /// <summary>
        /// 調を調べてLRCファイルを出力する。
        /// </summary>
        /// <returns>エラーの文字列。成功のとき空文字列。</returns>
        public string Classify(string inputAudioPath, string outputLrcPath, BackgroundWorker bw) {
            WWMFReaderCs.WWMFReader.Metadata meta;
            LargeArray<byte> dataByteAry;

            int hr = WWMFReaderCs.WWMFReader.ReadHeaderAndData(inputAudioPath, out meta, out dataByteAry);
            if (hr < 0) {
                return string.Format("Error: Read failed {0} {1}\n", hr, inputAudioPath);
            }

            if (meta.sampleRate != 44100 || meta.bitsPerSample != 16 || meta.numChannels != 2) {
                return string.Format("Error: File format is not 44100Hz 16bit 2ch. ({0}Hz {1}bit {2}ch) {3}\n", meta.sampleRate, meta.bitsPerSample, meta.numChannels, inputAudioPath);
            }

            bw.ReportProgress(0, string.Format("Read {0}\nProcessing...\n", inputAudioPath));
            mReportSW.Start();

            // ステレオをモノラル float値にする。
            var dataF = StereoToMono(dataByteAry);

            // 鍵盤の周波数binn番号。
            var fIdx = new List<int>();
            double f = 110.0; // 110Hz, A2
            while (f < 1320.0) {
                fIdx.Add((int)(f * (((double)WINDOW_LENGTH / 2) / (SAMPLE_RATE / 2))));
                f *= Math.Pow(2, 1.0 / 12);
            }

            // Hann窓。
            var w = WWWindowFunc.HannWindow(WINDOW_LENGTH);
            var fft = new WWRadix2FftLargeArray(WINDOW_LENGTH);

            mKeyHistory = new List<int>();

            // Keyの推定値predKeysを作成する。-1のとき不明。
            var predKeys = new List<int>();

            for (long i = 0; i < dataF.LongLength-(WINDOW_LENGTH*TEMPORAL_WIDTH); i += WINDOW_LENGTH/2) {
                var x = new List<float>();

                // TEMPORAL_WIDTH (=8)個のFFTを実行する。
                for (int j = 0; j < TEMPORAL_WIDTH; ++j) {
                    // 窓関数を掛けてFFTし、大きさを取って実数配列にし、鍵盤の周波数binnの値をxに追加。
                    var v = PrepareSampleDataForFFT(dataF, i + j * WINDOW_LENGTH, w);
                    var vC = WWComplex.FromRealArray(v);
                    var vCL = new LargeArray<WWComplex>(vC);
                    var fC = fft.ForwardFft(vCL).ToArray();
                    var fR = WWComplex.ToMagnitudeRealArray(fC);
                    foreach (var k in fIdx) {
                        x.Add((float)fR[k]);
                    }
                }

                // keyを推定する。
                var keyP = mKeyClassifier.Classify(x.ToArray());

                // keyCounter個同じkeyが連続したら確定する。
                var key = FilterKey(keyP);
                predKeys.Add(key);
                //Console.WriteLine("{0}, {1} {2}", (double)i / SAMPLE_RATE, mKeyClassifier.KeyIdxToStr(keyP), key);

                if (1000 < mReportSW.ElapsedMilliseconds) {
                    bw.ReportProgress((int)(100 * i / dataF.LongLength), "");
                    mReportSW.Restart();
                }
            }

            mReportSW.Stop();

            WriteLRC(predKeys, outputLrcPath);

            return "";
        }

        private string SecondToDurationStr(double v) {
            System.Diagnostics.Debug.Assert(0 <= v);
            int minutes = (int)(v/60);
            v -= minutes*60;
            int seconds = (int)(v);
            v -= seconds;
            int subsec = (int)(v * 100);
            if (100 <= subsec) {
                subsec = 99;
            }

            var s = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, subsec);
            return s;
        }

        private string WriteLRC(List<int> predKeys, string outputPath) {
            // key推定値が一つもないときエラー。
            int keyCount = 0;
            foreach (var k in predKeys) {
                if (0 <= k) {
                    ++keyCount;
                }
            }
            if (keyCount == 0) {
                return "Error: KeyClassifier failed to predict any key on this audio file!\n";
            }

            int lastKey = -1;

            using (var sw = new StreamWriter(outputPath)) {
                for (int i = 0; i < predKeys.Count(); ++i) {
                    int key = predKeys[i];

                    if (key == lastKey) {
                        continue;
                    }

                    // 値が変化したので書き込む。
                    double durationSec = (double)i * WINDOW_LENGTH /2 / SAMPLE_RATE;

                    if (key < 0) {
                        sw.WriteLine("[{0}] -", SecondToDurationStr(durationSec));
                    } else {
                        sw.WriteLine("[{0}]{1}", SecondToDurationStr(durationSec), mKeyClassifier.KeyIdxToStr(key));
                    }
                    lastKey = key;
                }
            }

            // 成功。
            return "";
        }

        private int FilterKey(int key) {
            mKeyHistory.Add(key);
            while (KEY_COUNTER < mKeyHistory.Count()) {
                mKeyHistory.RemoveAt(0);
            }

            // 連続で同じkeyがKEY_COUNTER個続いたら確定する。

            if (mKeyHistory.Count() != KEY_COUNTER) {
                return -1;
            }

            int firstKey = mKeyHistory[0];
            for (int i = 1; i < mKeyHistory.Count(); ++i) {
                if (firstKey != mKeyHistory[i]) {
                    return -1;
                }
            }

            return firstKey;
        }

        private float[] PrepareSampleDataForFFT(LargeArray<float> from, long fromPos, double[] w) {
            var r = new float[w.Length];
            for (int i = 0; i < r.Length; ++i) {
                float v = from.At(fromPos + i);
                r[i] = (float)(v * w[i]);
            }

            return r;
        }
    }
}
