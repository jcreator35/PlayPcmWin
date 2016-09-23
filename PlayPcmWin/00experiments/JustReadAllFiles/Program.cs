using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace JustReadAllFiles {
    class Program {
        const int BUFF_BYTES = 1048576;

        static long ReadAllFilesOnDirectory(DirectoryInfo di) {
            long totalBytes = 0;
            
            var fArray = di.GetFiles();

            Parallel.For(0, fArray.Length, i => {
                try {
                    var f = fArray[i];
                    var fs = f.OpenRead();
                    long readBytes = 0;
                    int bytes = 0;
                    byte[] b = new byte[BUFF_BYTES];
                    do {
                        bytes = fs.Read(b, 0, BUFF_BYTES);
                        readBytes += bytes;
                    } while (bytes == BUFF_BYTES);
                    System.Threading.Interlocked.Add(ref totalBytes, readBytes);

                    //Console.WriteLine("    {0}", f.Name);
                } catch (Exception ex) {
                    //Console.WriteLine("ReadAllFilesOnDirectory {0}", ex);
                }
            });

            return totalBytes;
        }

        static long DirectoryScanRecursive(DirectoryInfo di) {
            long totalBytes = ReadAllFilesOnDirectory(di);

            var dArray = di.GetDirectories();
            Parallel.For(0, dArray.Length, i => {
                try {
                    var subd = dArray[i];
                    Console.WriteLine("  {0}", subd.FullName);
                    long bytes = DirectoryScanRecursive(subd);
                    System.Threading.Interlocked.Add(ref totalBytes, bytes);
                } catch (Exception ex) {
                    //Console.WriteLine("DirectoryScanRecursive {0}", ex);
                }
            });

            return totalBytes;
        }

        static void Main(string[] args) {
            string path = "C:\\";
            if (1 <= args.Length) {
                path = args[0];
            }

            DirectoryInfo di = null;

            try {
                di = new DirectoryInfo(path);
            } catch (Exception ex) {
                Console.WriteLine("Usage: This program accepts 1 argument: directory_path_to_scan");
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            long totalBytes = DirectoryScanRecursive(di);

            sw.Stop();
            Console.WriteLine("Elapsed time: {0}, total read bytes={1}, {2}MB/s", sw.Elapsed, totalBytes, (double)totalBytes / (sw.ElapsedMilliseconds/1000.0) / 1000.0/1000.0);
        }
    }
}
