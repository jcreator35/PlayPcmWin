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
        private KeyClassifierCore mKeyClassifierCore;

        /// <summary>
        /// 学習データの仕様。
        /// </summary>
        private const int SAMPLE_RATE   = 44100;

        /// <summary>
        /// 学習データの仕様。
        /// </summary>
        private const int WINDOW_LENGTH = 16384;

        /// <summary>
        /// 学習データの仕様。
        /// </summary>
        private const int TEMPORAL_WIDTH = 8;

        /// <summary>
        /// keyを確定するために必要な連続推定一致数。
        /// </summary>
        private const int KEY_COUNTER = 12;

        public enum PitchEnum {
            ConcertPitch,
            BaroquePitch,
        };
        
        private Stopwatch mReportSW = new Stopwatch();
        private BackgroundWorker mBW;
        private List<int> mKeyHistory = new List<int>();

        public KeyClassifier() {
            mKeyClassifierCore = new KeyClassifierCore();
            /*
            // 分類器動作テスト。
            int nCorrect = 0;
            for (int i = 0; i < mKeyClassifierCore.TrainDataAry.Count(); ++i) {
                var td = mKeyClassifierCore.TrainDataAry[i];
                int yPred = mKeyClassifierCore.Classify(td.x);
                if (td.y == yPred) {
                    ++nCorrect;
                } else {
                    //Console.WriteLine("Wrong.");
                }
            }
            Console.WriteLine("Correct {0}%", 100.0f * nCorrect / mKeyClassifierCore.TrainDataAry.Count());
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

        private KeyClassifierCore.KeyEnum[] HumanKeyEnum = new KeyClassifierCore.KeyEnum[] {
            KeyClassifierCore.KeyEnum.Cdur,
            KeyClassifierCore.KeyEnum.Desdur,
            KeyClassifierCore.KeyEnum.Ddur,
            KeyClassifierCore.KeyEnum.Esdur,
            KeyClassifierCore.KeyEnum.Edur,
            KeyClassifierCore.KeyEnum.Fdur,
            KeyClassifierCore.KeyEnum.Fisdur,
            KeyClassifierCore.KeyEnum.Gdur,
            KeyClassifierCore.KeyEnum.Asdur,
            KeyClassifierCore.KeyEnum.Adur,
            KeyClassifierCore.KeyEnum.Bdur,
            KeyClassifierCore.KeyEnum.Hdur,
            KeyClassifierCore.KeyEnum.Cmoll,
            KeyClassifierCore.KeyEnum.Cismoll,
            KeyClassifierCore.KeyEnum.Dmoll,
            KeyClassifierCore.KeyEnum.Dismoll,
            KeyClassifierCore.KeyEnum.Emoll,
            KeyClassifierCore.KeyEnum.Fmoll,
            KeyClassifierCore.KeyEnum.Fismoll,
            KeyClassifierCore.KeyEnum.Gmoll,
            KeyClassifierCore.KeyEnum.Gismoll,
            KeyClassifierCore.KeyEnum.Amoll,
            KeyClassifierCore.KeyEnum.Bmoll,
            KeyClassifierCore.KeyEnum.Hmoll,};

        /// <summary>
        /// CSV形式のテーブル出力。
        /// </summary>
        private void PrintCsvTable() {
            Console.Write("-, ");
            foreach (var x in HumanKeyEnum) {
                Console.Write("{0}, ", x);
            }
            Console.WriteLine("");

            int nY = -1;
            foreach (var y in HumanKeyEnum) {
                var keyY = y;
                ++nY;

                Console.Write("{0}, ", y);
                int nX = -1;
                foreach (var x in HumanKeyEnum) {
                    var keyX = x;
                    ++nX;

                    float v = mKeyClassifierCore.ResultTable()[(int)x][(int)y];

                    if (nY <= nX) {
                        Console.Write("-, ");
                    } else if (0 < v) {
                        Console.Write("{0},", keyX);
                    } else if (v < 0) {
                        Console.Write("{0},", keyY);
                    } else {
                        Console.Write("-, ");
                    }
                }
                Console.WriteLine("");
            }
        }

        /// <summary>
        /// 調を調べてLRCファイルを出力する。
        /// </summary>
        /// <returns>エラーの文字列。成功のとき空文字列。</returns>
        public string Classify(string inputAudioPath, string outputLrcPath, PitchEnum pitchEnum, BackgroundWorker bw) {
            WWMFReaderCs.WWMFReader.Metadata meta;
            LargeArray<byte> dataByteAry;

            int hr = WWMFReaderCs.WWMFReader.ReadHeaderAndData(inputAudioPath, out meta, out dataByteAry);
            if (hr < 0) {
                return string.Format("Error: Read failed {0} {1}\n", hr, inputAudioPath);
            }

            if (meta.sampleRate != SAMPLE_RATE || meta.bitsPerSample != 16 || meta.numChannels != 2) {
                return string.Format("Error: File format is not {0}Hz 16bit 2ch. ({1}Hz {2}bit {3}ch) {4}\n", SAMPLE_RATE, meta.sampleRate, meta.bitsPerSample, meta.numChannels, inputAudioPath);
            }

            if (bw != null) {
                bw.ReportProgress(0, string.Format("Read {0}\nProcessing...\n", inputAudioPath));
            }
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

            // Hann窓 w。
            double[] wD = WWWindowFunc.HannWindow(WINDOW_LENGTH);
            float[] w = Array.ConvertAll(wD, xD => (float)xD);

            var fft = new WWRadix2FftF(WINDOW_LENGTH);

            mKeyHistory = new List<int>();

            // Keyの推定値predKeysを作成する。-1のとき不明。
            var predKeys = new List<int>();

            for (long i = 0; i < dataF.LongLength - (WINDOW_LENGTH * TEMPORAL_WIDTH); i += WINDOW_LENGTH / 2) {
                var x = new List<float>();

                // TEMPORAL_WIDTH (=8)個のFFTを実行する。
                for (int j = 0; j < TEMPORAL_WIDTH; ++j) {
                    // 窓関数を掛けてFFTし、大きさを取って実数配列にし、鍵盤の周波数binnの値をxに追加。
                    var v = PrepareSampleDataForFFT(dataF, i + j * WINDOW_LENGTH, w);
                    var vC = WWComplexF.FromRealArray(v);
                    var fC = fft.ForwardFft(vC).ToArray();
                    var fR = WWComplexF.ToMagnitudeRealArray(fC);
                    foreach (var k in fIdx) {
                        x.Add(fR[k]);
                    }
                }

                // keyを推定する。
                var keyP = mKeyClassifierCore.Classify(x.ToArray());

                // 推定結果のダンプ。
                //PrintCsvTable();


                // keyCounter個同じkeyが連続したら確定する。
                var key = FilterKey(keyP);
                predKeys.Add(key);
                //Console.WriteLine("{0}, {1} {2}", (double)i / SAMPLE_RATE, mKeyClassifierCore.KeyIdxToStr(keyP), key);

                if (1000 < mReportSW.ElapsedMilliseconds) {
                    int percentage = (int)(100 * i / dataF.LongLength);
                    if (bw == null) {
                        Console.Write("{0}% \r", percentage);
                    } else {
                        bw.ReportProgress(percentage, "");
                    }
                    mReportSW.Restart();
                }
            }

            if (bw == null) {
                Console.WriteLine("     \r");
            }

            mReportSW.Stop();

            {
                var r = WriteLRC(predKeys, pitchEnum, outputLrcPath);
                return r;
            }
        }

        /// <summary>
        /// MM:SS.cc 形式の文字列を戻す。
        /// </summary>
        private string SecondToTimeStr(double v) {
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

        private string WriteLRC(List<int> predKeys, PitchEnum pitchEnum, string outputPath) {
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

                    if (pitchEnum == PitchEnum.BaroquePitch) {
                        // コンサートピッチの評価値をバロックピッチに変換する。
                        key = mKeyClassifierCore.KeyIdxToBaroquePitch(key);
                    }

                    if (key == lastKey) {
                        continue;
                    }

                    // 値が変化したので書き込む。

                    if (key < 0) {
                        double timeSec = (double)i * WINDOW_LENGTH / 2 / SAMPLE_RATE;
                        sw.WriteLine("[{0}] -", SecondToTimeStr(timeSec));
                    } else {
                        // 同じkeyがKEY_COUNTER個連続したら確定するため
                        // KEY_COUNTER-1個遅延して出るので、その分時間を過去にする。
                        double timeSec = (double)(i-(KEY_COUNTER-1)) * WINDOW_LENGTH / 2 / SAMPLE_RATE;
                        sw.WriteLine("[{0}]{1}", SecondToTimeStr(timeSec), mKeyClassifierCore.KeyIdxToStr(key));
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

            // 同じkeyが連続KEY_COUNTER個続いたら確定する。

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

        private float[] PrepareSampleDataForFFT(LargeArray<float> from, long fromPos, float[] w) {
            var r = new float[w.Length];
            for (int i = 0; i < r.Length; ++i) {
                float v = from.At(fromPos + i);
                r[i] = v * w[i];
            }

            return r;
        }
    }
}
