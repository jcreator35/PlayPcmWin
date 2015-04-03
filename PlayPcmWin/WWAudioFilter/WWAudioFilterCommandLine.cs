﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    class WWAudioFilterCommandLine {
        private static readonly string COMMAND_CONVERT = "-convert";

        private void PrintUsage(string programName) {
            Console.WriteLine("Commandline Usage: {0} {1} filterFile FLACinputFile FLACoutputFile", programName, COMMAND_CONVERT);
        }

        void ProgressReportCallback(int percentage, WWAudioFilterCore.ProgressArgs args) {
        }

        public bool ParseCommandLine() {
            var argDictionary = new Dictionary<string, string>();

            var args = Environment.GetCommandLineArgs();
            if (5 != args.Length || !COMMAND_CONVERT.Equals(args[1])) {
                PrintUsage(args[0]);
                return false;
            }

            string filterFile = args[2];
            string inputFile = args[3];
            string outputFile = args[4];

            var filters = WWAudioFilterCore.LoadFiltersFromFile(filterFile);
            if (filters == null) {
                Console.WriteLine("E: failed to load filter file: {0}", filterFile);
                PrintUsage(args[0]);
                return false;
            }

            var af = new WWAudioFilterCore();

            int rv = af.Run(inputFile, filters, outputFile, ProgressReportCallback);
            if (rv < 0) {
                Console.WriteLine("E: failed to process. {0}", WWFlacRWCS.FlacRW.ErrorCodeToStr(rv));
            }

            return true;
        }
    }
}
