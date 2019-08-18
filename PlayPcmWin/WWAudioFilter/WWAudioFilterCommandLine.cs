using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWAudioFilterCore;

namespace WWAudioFilter {
    class WWAudioFilterCommandLine {
        private static readonly string COMMAND_CONVERT = "-convert";

        private void PrintUsage(string programName) {
            Console.WriteLine("Commandline Usage: {0} {1} filterFile inputAudioFile outputAudioFile", programName, COMMAND_CONVERT);
        }

        public bool ParseCommandLine() {
            var argDictionary = new Dictionary<string, string>();

            var args = Environment.GetCommandLineArgs();
            if (5 != args.Length || !COMMAND_CONVERT.Equals(args[1])) {
                PrintUsage(args[0]);
                return false;
            }

            //try {
                string filterFile = args[2];
                string inputFile = args[3];
                string outputFile = args[4];

                var filters = WWAudioFilterCore.WWAudioFilterCore.LoadFiltersFromFile(filterFile);
                if (filters == null) {
                    Console.WriteLine("E: failed to load filter file: {0}", filterFile);
                    PrintUsage(args[0]);
                    return false;
                }

                var af = new WWAudioFilterCore.WWAudioFilterCore();

                int rv = af.Run(inputFile, filters, outputFile, (int percentage, WWAudioFilterCore.WWAudioFilterCore.ProgressArgs args2) => { });
                if (rv < 0) {
                    Console.WriteLine("E: failed to process. {0}", WWFlacRWCS.FlacRW.ErrorCodeToStr(rv));
                }
            /*
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
            */

            return true;
        }
    }
}
