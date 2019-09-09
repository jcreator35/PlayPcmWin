// 日本語。

using System;
using System.Collections.Generic;
using WWAudioFilterCore;

namespace WWAudioFilter {
    class WWAudioFilterCommandLine {
        private static readonly string COMMAND_CONVERT = "-convert";
        private static readonly string COMMAND_OUTFMT = "-outFmt";

        private void PrintUsage(string programName) {
            Console.WriteLine("Commandline Usage: {0} {1} [-{2} {{16|24|32|F32|64|F64}}] filterFile inputAudioFile outputAudioFile",
                programName, COMMAND_CONVERT, COMMAND_OUTFMT);
        }

        public bool ParseCommandLine() {
            var argDictionary = new Dictionary<string, string>();

            var args = Environment.GetCommandLineArgs();
            if ((5 != args.Length && 7 != args.Length)|| !COMMAND_CONVERT.Equals(args[1])) {
                PrintUsage(args[0]);
                return false;
            }

            bool dither = true;
            var outFmt = WWAFUtil.AFSampleFormat.Auto;
            string filterFile = string.Empty;
            string inputFile = string.Empty;
            string outputFile = string.Empty;
            if (5 == args.Length) {
                filterFile = args[2];
                inputFile = args[3];
                outputFile = args[4];
            } else if (7 == args.Length) {
                filterFile = args[4];
                inputFile = args[5];
                outputFile = args[6];

                if (COMMAND_OUTFMT.Equals(args[2])) {
                    switch (args[3]) {
                    case "16":
                        outFmt = WWAFUtil.AFSampleFormat.PcmInt16;
                        break;
                    case "24":
                        outFmt = WWAFUtil.AFSampleFormat.PcmInt24;
                        break;
                    case "32":
                        outFmt = WWAFUtil.AFSampleFormat.PcmInt32;
                        break;
                    case "F32":
                        outFmt = WWAFUtil.AFSampleFormat.PcmFloat32;
                        break;
                    case "64":
                        outFmt = WWAFUtil.AFSampleFormat.PcmInt64;
                        break;
                    case "F64":
                        outFmt = WWAFUtil.AFSampleFormat.PcmFloat64;
                        break;
                    default:
                        Console.WriteLine("Error: unknown outFmt {0}", args[3]);
                        PrintUsage(args[0]);
                        return false;
                    }
                } else {
                    Console.WriteLine("Error: unknown command {0}", args[3]);
                    PrintUsage(args[0]);
                    return false;
                }
            }

            try {
                var filters = WWAudioFilterCore.AudioFilterCore.LoadFiltersFromFile(filterFile);
                if (filters == null) {
                    Console.WriteLine("E: failed to load filter file: {0}", filterFile);
                    PrintUsage(args[0]);
                    return false;
                }

                var af = new WWAudioFilterCore.AudioFilterCore();

                int rv = af.Run(inputFile, filters, outputFile, outFmt, dither,
                    (int percentage, WWAudioFilterCore.AudioFilterCore.ProgressArgs args2) => { });
                if (rv < 0) {
                    Console.WriteLine("E: failed to process. {0}", WWFlacRWCS.FlacRW.ErrorCodeToStr(rv));
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
            
            return true;
        }
    }
}
