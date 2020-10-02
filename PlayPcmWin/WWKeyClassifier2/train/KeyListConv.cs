using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyListConv {
    class Program {

        static void Main(string[] args) {
            if (args.Length != 3) {
                Console.WriteLine("Usage: KeyListConv read.csv semitoneCount write.csv");
                return;
            }

            int semitoneCount = 0;
            if (!int.TryParse(args[1], out semitoneCount)) {
                Console.WriteLine("Error: semitoneCount parse error");
            }

            var self = new Program();
            self.Run(args[0], semitoneCount, args[2]);
        }

        private string KeyModulatePlus(string keyName) {
            switch (keyName) {
                case "Cdur":
                    return "Desdur";
                case "Gdur":
                    return "Asdur";
                case "Ddur":
                    return "Esdur";
                case "Adur":
                    return "Bdur";
                case "Edur":
                    return "Fdur";
                case "Hdur":
                    return "Cdur";
                case "Cesdur":
                    return "Cdur";
                case "Gesdur":
                    return "Gdur";
                case "Fisdur":
                    return "Gdur";
                case "Desdur":
                    return "Ddur";
                case "Cisdur":
                    return "Ddur";
                case "Asdur":
                    return "Adur";
                case "Esdur":
                    return "Edur";
                case "Bdur":
                    return "Hdur";
                case "Fdur":
                    return "Fisdur";
                case "Amoll":
                    return "Bmoll";
                case "Emoll":
                    return "Fmoll";
                case "Hmoll":
                    return "Cmoll";
                case "Fismoll":
                    return "Gmoll";
                case "Cismoll":
                    return "Dmoll";
                case "Gismoll":
                    return "Amoll";
                case "Dismoll":
                    return "Emoll";
                case "Esmoll":
                    return "Emoll";
                case "Bmoll":
                    return "Hmoll";
                case "Fmoll":
                    return "Fismoll";
                case "Cmoll":
                    return "Cismoll";
                case "Gmoll":
                    return "Gismoll";
                case "Dmoll":
                    return "Dismoll";
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return "";
            }
        }

        private string KeyModulateMinus(string keyName) {
            switch (keyName) {
                case "Cdur":
                    return "Hdur";
                case "Gdur":
                    return "Fisdur";
                case "Ddur":
                    return "Desdur";
                case "Adur":
                    return "Asdur";
                case "Edur":
                    return "Esdur";
                case "Hdur":
                    return "Bdur";
                case "Cesdur":
                    return "Bdur";
                case "Gesdur":
                    return "Fdur";
                case "Fisdur":
                    return "Fdur";
                case "Desdur":
                    return "Cdur";
                case "Cisdur":
                    return "Cdur";
                case "Asdur":
                    return "Gdur";
                case "Esdur":
                    return "Ddur";
                case "Bdur":
                    return "Adur";
                case "Fdur":
                    return "Edur";
                case "Amoll":
                    return "Gismoll";
                case "Emoll":
                    return "Dismoll";
                case "Hmoll":
                    return "Bmoll";
                case "Fismoll":
                    return "Fmoll";
                case "Cismoll":
                    return "Cmoll";
                case "Gismoll":
                    return "Gmoll";
                case "Dismoll":
                    return "Dmoll";
                case "Esmoll":
                    return "Dmoll";
                case "Bmoll":
                    return "Amoll";
                case "Fmoll":
                    return "Emoll";
                case "Cmoll":
                    return "Hmoll";
                case "Gmoll":
                    return "Fismoll";
                case "Dmoll":
                    return "Cismoll";
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return "";
            }
        }

        string KeyModulate(string key, int step) {
            while (step != 0) { 
                if (0 < step) {
                    key = KeyModulatePlus(key);
                    --step;
                } else if (step < 0) {
                    key = KeyModulateMinus(key);
                    ++step;
                }
            }
            return key;
        }

        bool Run(string fromPath, int semitoneCount, string toPath) {

            // ピッチが上がる→時間は短くなる。
            double ratio = 1.0 / Math.Pow(2.0, semitoneCount / 12.0);

            int lineno = 1;

            using (var sr = new StreamReader(fromPath)) {
                using (var sw = new StreamWriter(toPath)) {
                    {
                        // 最初の行はそのままコピーする。
                        string firstLine = sr.ReadLine();
                        sw.WriteLine(firstLine);
                        ++lineno;
                    }

                    string line;
                    while (null != (line = sr.ReadLine())) {
                        var tokens = line.Split(new char[] { ',' });
                        if (tokens.Length != 4) {
                            Console.WriteLine("Error: token length != 4 on line {0}", lineno);
                            return false;
                        }

                        string nr = tokens[0];

                        double fromSec;
                        if (!double.TryParse(tokens[1], out fromSec)) {
                            Console.WriteLine("Error: fromSec parse error on line {0} token {1}", lineno, tokens[1]);
                            return false;
                        }

                        double toSec;
                        if (!double.TryParse(tokens[2], out toSec)) {
                            Console.WriteLine("Error: fromSec parse error on line {0} token {1}", lineno, tokens[2]);
                            return false;
                        }

                        string keyName = tokens[3];

                        fromSec *= ratio;
                        toSec *= ratio;
                        var keyNameNew = KeyModulate(keyName, semitoneCount);

                        sw.WriteLine("{0},{1},{2},{3}", nr, fromSec, toSec, keyNameNew);

                        ++lineno;
                    }
                }
            }

            return true;
        }
    }
}
