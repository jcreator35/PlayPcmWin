using System;
using System.Windows;

namespace WWArbitraryResampler {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            if (ProcessCommandline(e.Args)) {
                Shutdown();
            }
        }

        private void PrintUsage() {
            string appName = "WWArbitraryResampler";
            Console.WriteLine("Commandline usage: {0} inputPath gpuId pitchScale outputPath", appName);
        }

        /// <summary>
        /// コマンドライン引数が5個の時、設定をコマンドライン引数から得てコンバートします。
        /// </summary>
        /// <returns>true: コマンドライン処理を行った。 false: コマンドライン特にない。</returns>
        private bool ProcessCommandline(string [] args) {
            int hr = 0;
            if (4 != args.Length) {
                PrintUsage();
                return false;
            }

            var conv = new Converter();

            conv.Init();
            conv.UpdateAdapterList();

            string inPath = args[0];

            // gpuIdのアダプターがあるか調べる。
            int gpuId = 0;
            if (!int.TryParse(args[1], out gpuId) || gpuId < 0) {
                Console.WriteLine("Error: gpuId should be 0 or larger integer value.");
                PrintUsage();
                return true;
            }
            bool gpuFound = false;
            foreach (var item in conv.AdapterList) {
                if (item.gpuId == gpuId) {
                    gpuFound = true;
                    break;
                }
            }
            if (!gpuFound) {
                Console.WriteLine("Error: Adapter of gpuId={0} is not found.", gpuId);
                PrintUsage();
                return true;
            }

            double pitchScale = 1.0;
            if (!double.TryParse(args[2], out pitchScale) || pitchScale < 0.5 || 2.0 < pitchScale) {
                Console.WriteLine("Error: pitchScale value should be 0.5 <= pitchScale <= 2.0");
                PrintUsage();
                return true;
            }

            string outPath = args[3];

            var ca = new Converter.ConvertArgs(gpuId, inPath, outPath, 1.0 / pitchScale);
            hr = conv.Convert(ca, null);
            if (hr < 0) { 
                Console.WriteLine("Error: result={0:x}", hr);
            }
            return true;
        }

    }
}
