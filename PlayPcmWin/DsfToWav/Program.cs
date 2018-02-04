using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayPcmWin;
using System.IO;
using PcmDataLib;
using WavRWLib2;

namespace DsfToWav {
    public class Program {
        static void PrintUsage() {
            Console.WriteLine("Usage: DsfToWav dsffilename wavefilename");
            System.Environment.Exit(1);
        }

        static void Main(string[] args) {
            if (args.Length != 2) {
                PrintUsage();
            }
            int returnValue = 0;

            if (0 == ".dsf".CompareTo(Path.GetExtension(args[0]))) {
                bool b = RunDsfToWav(args[0], args[1]);
                if (!b) {
                    Console.WriteLine("Error occured. Failed.");
                    returnValue = 1;
                }
            }

            if (0 == ".dff".CompareTo(Path.GetExtension(args[0]))) {
                bool b = RunDffToWav(args[0], args[1]);
                if (!b) {
                    Console.WriteLine("Error occured. Failed.");
                    returnValue = 1;
                }
            }

            System.Environment.Exit(returnValue);
        }

        public static bool RunDsfToWav(string fromPath, string toPath) {
            DsfReader dr = new DsfReader();
            PcmData pcm;

            using (BinaryReader br = new BinaryReader(File.Open(fromPath, FileMode.Open))) {
                using (BinaryWriter bw = new BinaryWriter(File.Open(toPath, FileMode.Create))) {
                    var result = dr.ReadStreamBegin(br, out pcm);
                    if (result != DsfReader.ResultType.Success) {
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

        static bool RunDffToWav(string fromPath, string toPath) {
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
