using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace WWKeyClassifier2 {
    public partial class App : Application {
        private void Application_Startup(object sender, StartupEventArgs e) {
            if (ParseCommandline(e.Args)) {
                Application.Current.Shutdown();
                return;
            }
        }

        private void PrintUsage() {
            Console.WriteLine("Commandline usage: WWKeyClassifier2 inputFlacFilename [-bp] outputLRCFilename");
        }

        private bool ParseCommandline(string [] args) {
            if (args.Length != 2 && args.Length != 3) {
                PrintUsage();
                return false;
            }

            string inputFlac = args[0];
            string outputLrc;
            KeyClassifier.PitchEnum pitchEnum = KeyClassifier.PitchEnum.ConcertPitch;
            if (args.Length == 3) {
                if (0 != "-bp".CompareTo(args[1])) {
                    Console.WriteLine("Error: Unknown option {0}", args[1]);
                    PrintUsage();
                    return false;
                }

                pitchEnum = KeyClassifier.PitchEnum.BaroquePitch;
                outputLrc = args[2];
            } else {
                outputLrc = args[1];
            }

            Console.WriteLine("{0} input=\"{1}\" outputLRC=\"{2}\"", pitchEnum, inputFlac, outputLrc);

            var kc = new KeyClassifier();
            var r = kc.Classify(inputFlac, outputLrc, pitchEnum, null);
            if (r.Length != 0) {
                Console.Write(r);
            }

            // コマンドライン処理が行われたのでtrueを戻す。
            return true;
        }
    }
}
