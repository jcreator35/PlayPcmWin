using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayPcmWinAlbum {
    class DirectoryUtil {
        // this code is from https://msdn.microsoft.com/en-us/library/bb513869.aspx
        public static string[] CollectFlacFilesOnFolder(string root, string extension) {
            var result = new List<string>();

            var dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root)) {
                throw new ArgumentException("root");
            }
            dirs.Push(root);

            while (dirs.Count > 0) {
                var currentDir = dirs.Pop();
                string[] subDirs;
                try {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                } catch (UnauthorizedAccessException e) {
                    Console.WriteLine(e);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    Console.WriteLine(e);
                    continue;
                }

                string[] files = null;
                try {
                    files = System.IO.Directory.GetFiles(currentDir);
                } catch (UnauthorizedAccessException e) {
                    Console.WriteLine(e);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    Console.WriteLine(e);
                    continue;
                }

                foreach (string file in files) {
                    try {
                        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        if (String.Equals(extension.ToUpper(), fi.Extension.ToUpper(), StringComparison.Ordinal)) {
                            result.Add(fi.FullName);
                            //Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                        }
                    } catch (System.IO.FileNotFoundException e) {
                        Console.WriteLine(e);
                        continue;
                    }
                }

                foreach (string str in subDirs) {
                    dirs.Push(str);
                }
            }

            return result.ToArray();
        }
    }
}
