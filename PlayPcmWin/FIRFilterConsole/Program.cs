// 日本語。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWUtil;
using WWFlacRWCS;
using System.Threading.Tasks;

namespace FIRFilterConsole {
    class Program {

        /// <summary>
        /// ファイルに1行に1個ずつFIRフィルターの係数が入っている。
        /// comma separatedの場合にも対応。
        /// 例: 以下の内容のファイルの場合、9要素のfloat配列を戻す。
        /// 1.0, 2.1, 3.2,
        /// 4.3, 5.4, 6.5,
        /// 7.6, 8.7, 9,8,
        /// </summary>
        float[] ReadCoeffsFile(string path) {
            var result = new List<float>();

            using (var sr = new System.IO.StreamReader(path)) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    var tokens = line.Split(',');
                    foreach (var s in tokens) {
                        float f;
                        if (float.TryParse(s, out f)) {
                            result.Add(f);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        bool Run(string inFile, float scale, string firCoeffsFile, string outFile) {
            int rv;

            // 係数ファイルを読む。
            var coeffs = ReadCoeffsFile(firCoeffsFile);
            if (coeffs.Length == 0) {
                Console.WriteLine("Error: FIR coeffs file read error: {0}", firCoeffsFile);
                return false;
            }

            Console.WriteLine("  FIR taps = {0}", coeffs.Length);

            // coeffsをスケールする。
            for (int i=0; i<coeffs.Length;++i) {
                coeffs[i] = coeffs[i] * scale;
            }

            var writePcmList = new List<LargeArray<byte>>();
            Metadata meta;
            byte[] picture = null;

            {
                var fr = new FlacRW();
                // 入力FLACファイルを読む。
                rv = fr.DecodeAll(inFile);
                if (rv < 0) {
                    Console.WriteLine("Error: FLAC read error {0}: {1}", FlacRW.ErrorCodeToStr(rv), inFile);
                    return false;
                }

                fr.GetDecodedMetadata(out meta);
                if (0 < meta.pictureBytes) {
                    picture = new byte[meta.pictureBytes];
                    rv = fr.GetDecodedPicture(out picture, picture.Length);
                    if (rv < 0) {
                        Console.WriteLine("Error: FLAC get picture error {0}: {1}", FlacRW.ErrorCodeToStr(rv), inFile);
                        return false;
                    }
                }

                Console.WriteLine("  SampleRate={0}Hz, BitDepth={1}, Channels={2}", meta.sampleRate, meta.bitsPerSample, meta.channels);

                for (int ch = 0; ch < meta.channels; ++ch) {
                    writePcmList.Add(null);
                }

                // FIR係数を畳み込む。
                // 申し訳程度の最適化：2チャンネル音声の場合2スレッドで並列動作。
                float maxMagnitude = 0;
                Parallel.For(0, meta.channels, ch => {
                    var inData = fr.GetFloatPcmOfChannel(ch, 0, meta.totalSamples);
#if true
                    var conv = new FIRConvolution();

                    var convoluted = conv.Convolution(inData, coeffs);

                    if (maxMagnitude < conv.MaxMagnitude) {
                        maxMagnitude = conv.MaxMagnitude;
                    }
                    var pcm = FlacRW.ConvertToByteArrayPCM(convoluted, meta.BytesPerSample);
#else
                    // debug: output unchanged input file
                    var pcm = FlacRW.ConvertToByteArrayPCM(inData, meta.BytesPerSample);
#endif
                    writePcmList[ch] = pcm;
                });

                if (8388607.0f / 8388608.0f < maxMagnitude) {
                    Console.WriteLine("Error: convolution result PCM value overflow. Please reduce input PCM gain further by {0:0.00} dB",
                        20.0 * Math.Log10(maxMagnitude));
                    return false;
                } else {
                    Console.WriteLine("  Max PCM magnitude = {0}", maxMagnitude);
                }

                fr.DecodeEnd();
            }

            {
                // FLACファイルを書き込む。
                var fw = new FlacRW();
                fw.EncodeInit(meta);
                for (int ch = 0; ch < meta.channels; ++ch) {
                    fw.EncodeAddPcm(ch, writePcmList[ch]);
                }
                if (picture != null) {
                    fw.EncodeSetPicture(picture);
                }

                rv = fw.EncodeRun(outFile);
                if (rv < 0) {
                    Console.WriteLine("Error: FLAC write error {0}: {1}", FlacRW.ErrorCodeToStr(rv), outFile);
                    return false;
                }

                fw.EncodeEnd();
            }

            return true;
        }

        static void PrintUsage() {
            Console.WriteLine("Usage: FIRFilterConsole inFile.flac gaindB firFilterFile.txt outFile.flac");
            Console.WriteLine("  firFilterFile contains FIR filter coefficients, one coeff in one line.");
            Console.WriteLine("Example: FIRFilterConsole inFile.flac -3.1 PreEmp.txt outFile.flac");
        }

        static void Main(string[] args) {
            // parse command line
            if (args.Length != 4) {
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }

            string inFile = args[0];

            float gaindB = 0;
            if (!float.TryParse(args[1], out gaindB)) {
                Console.WriteLine("Error: gaindB should be floating point value: parse error string {0}", args[1]);
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }
            float scale = (float)Math.Pow(10.0, gaindB / 20.0);
            
            string firCoeffsFile = args[2];
            string outFile = args[3];

            if (0 == string.Compare(inFile, outFile)) {
                Console.WriteLine("Error: inFile and outFile is the same file!");
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }

            var self = new Program();

            bool rv = self.Run(inFile, scale, firCoeffsFile, outFile);
            if (!rv) {
                Environment.ExitCode = 1;
            }
        }
    }
}
