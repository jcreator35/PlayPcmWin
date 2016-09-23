using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayPcmWin;

namespace CueSheetReaderTest {
    class Program {
        static void Main(string[] args) {
            bool result;

            string[] testFileArray = {
                @"C:\test\bach.cue",
                @"C:\test\beethoven.cue" };

            foreach (string path in testFileArray) {
                CueSheetReader csr = new CueSheetReader();
                result = csr.ReadFromFile(path);
                System.Console.WriteLine("{0} result={1}", path, result);
            }
        }
    }
}
