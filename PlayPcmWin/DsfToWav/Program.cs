using System;
using System.IO;
using PcmDataLib;
using PlayPcmWin;
using WavRWLib2;

namespace DsfToWav {
    /// <summary>
    /// DSFまたはDSDIFFを読んで、DoP WAVファイルを書く。
    /// </summary>
    public class Program {
        static void PrintUsage() {
            Console.WriteLine("Usage: DsfToWav [-oneBitWav] dsffilename wavefilename");
            System.Environment.Exit(1);
        }

        /// <summary>
        /// 1ビットの情報を16ビットに引き伸ばし 2.8MHz 16ビットWAVとして保存。
        /// </summary>
        bool mOneBitWav = false;


        static void Main(string[] args) {
            var p = new Program();
            int rv = p.Run(args);
            System.Environment.Exit(rv);
        }

        int Run(string[] args) {
            string inPath;
            string outPath;
            if (args.Length == 2) {
                inPath = args[0];
                outPath = args[1];
            } else if (args.Length == 3) {
                if (0 != "-oneBitWav".CompareTo(args[0])) {
                    PrintUsage();
                    return 1;
                }
                mOneBitWav = true;
                inPath = args[1];
                outPath = args[2];
            } else {
                PrintUsage();
                return 1;
            }

            int returnValue = 0;

            if (0 == ".dsf".CompareTo(Path.GetExtension(inPath))) {
                bool b = RunDsfToWav(inPath, outPath);
                if (!b) {
                    Console.WriteLine("Error occured. Failed.");
                    returnValue = 1;
                }
            }

            if (0 == ".dff".CompareTo(Path.GetExtension(inPath))) {
                if (mOneBitWav) {
                    Console.WriteLine("Error: DFF OneBitWav is not implemented");
                    returnValue = 1;
                }

                bool b = RunDffToWav(inPath, outPath);
                if (!b) {
                    Console.WriteLine("Error occured. Failed.");
                    returnValue = 1;
                }
            }

            return returnValue;
        }

        public bool RunDsfToWav(string fromPath, string toPath) {
            DsfReader dr = new DsfReader();
            PcmData pcm;

            using (BinaryReader br = new BinaryReader(File.Open(fromPath, FileMode.Open))) {
                using (BinaryWriter bw = new BinaryWriter(File.Open(toPath, FileMode.Create))) {
                    var result = dr.ReadStreamBegin(br, out pcm);
                    if (result != DsfReader.ResultType.Success) {
                        Console.WriteLine("Error: {0}", result);
                        return false;
                    }

                    if (mOneBitWav) {
                        pcm.SampleDataType = PcmDataLib.PcmData.DataType.PCM;
                        pcm.SetFormat(
                            pcm.NumChannels,
                            16,
                            16,
                            dr.SampleRate,
                            PcmDataLib.PcmData.ValueRepresentationType.SInt,
                            dr.SampleCount);
                    }

                    long numBytes = pcm.NumFrames * pcm.BitsPerFrame;
                    WavWriter.WriteRF64Header(bw, pcm.NumChannels, pcm.BitsPerSample, pcm.SampleRate, pcm.NumFrames);

                    bool bContinue = true;
                    do {
                        var data = dr.ReadStreamReadOne(br, 1024 * 1024);
                        if (data == null || data.Length == 0) {
                            bContinue = false;
                        }

                        if (mOneBitWav) {
                            // 24bitPCMのDoPフレームが出てくるが、1bitずつ、16bit値に伸ばして出力。
                            // DoPのデータのSDMはビッグエンディアンビットオーダーで15ビット目→0ビット目に入っている。
                            int readPos = 0;
                            for (int i = 0; i < data.Length / 3 /pcm.NumChannels; ++i) {
                                for (int ch = 0; ch < pcm.NumChannels; ++ch) {
                                    ushort v = (ushort)((data[readPos + 1] << 8) + (data[readPos + 0] << 0));
                                    for (int bitPos = 15; 0 <= bitPos; --bitPos) {
                                        int b = 1 & (v >> bitPos);
                                        if (b == 1) {
                                            short writePcm = +32767;
                                            bw.Write(writePcm);
                                        } else {
                                            short writePcm = -32767;
                                            bw.Write(writePcm);
                                        }
                                    }
                                    readPos += 3;
                                }
                            }
                        } else {
                            // DoP WAVデータを書き込む。
                            bw.Write(data);
                        }
                    } while (bContinue);

                    if ((numBytes & 1) == 1) {
                        byte pad = 0;
                        bw.Write(pad);
                    }

                    dr.ReadStreamEnd();
                }
            }

            return true;
        }

        public bool RunDffToWav(string fromPath, string toPath) {
            DsdiffReader dr = new DsdiffReader();
            PcmData pcm;

            using (BinaryReader br = new BinaryReader(File.Open(fromPath, FileMode.Open))) {
                using (BinaryWriter bw = new BinaryWriter(File.Open(toPath, FileMode.Create))) {
                    var result = dr.ReadStreamBegin(br, out pcm);
                    if (result != DsdiffReader.ResultType.Success) {
                        Console.WriteLine("Error: {0}", result);
                        return false;
                    }

                    long numBytes = pcm.NumFrames * pcm.BitsPerFrame;
                    WavWriter.WriteRF64Header(bw, pcm.NumChannels, pcm.BitsPerSample, pcm.SampleRate, pcm.NumFrames);

                    bool bContinue = true;
                    do {
                        var data = dr.ReadStreamReadOne(br, 1024 * 1024);
                        if (data == null || data.Length == 0) {
                            bContinue = false;
                        }

                        bw.Write(data);
                    } while (bContinue);

                    if ((numBytes & 1) == 1) {
                        byte pad = 0;
                        bw.Write(pad);
                    }

                    dr.ReadStreamEnd();
                }
            }

            return true;
        }
    }
}
