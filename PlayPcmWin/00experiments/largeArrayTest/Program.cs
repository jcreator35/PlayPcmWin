using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace largeArrayTest {
    class Program {
        static void Main(string[] args) {
            var ar = new byte[2 * 1000 * 1024 * 1024];

            for (int i = 0; i < ar.Length; ++i) {
                ar[i] = (byte)i;
            }

            Console.WriteLine("done.");
        }
    }
}
