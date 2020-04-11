using System;
using System.Collections.Generic;
using System.Threading;

namespace WWUtil {
    public class DirectoryUtil {

        /// <summary>
        /// Collect recursively file names with specified ext
        /// this code is from https://msdn.microsoft.com/en-us/library/bb513869.aspx
        /// キャンセルイベントが発生すると、OperationCanceledExceptionをthrowします。
        /// </summary>
        /// <param name="root">root directory to search files</param>
        /// <param name="extension">collect files with this extension. if null is specified, all files are collected</param>
        /// <returns>collected file list</returns>
        public static string[] CollectFilesOnFolder(string root, string extension, CancellationTokenSource cts = null) {
            var result = new List<string>();

            var dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root)) {
                throw new ArgumentException("root");
            }
            dirs.Push(root);

            while (dirs.Count > 0) {
                if (cts != null && cts.IsCancellationRequested) {
                    cts.Token.ThrowIfCancellationRequested();
                }

                var currentDir = dirs.Pop();
                string[] subDirs;

                try {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                } catch (UnauthorizedAccessException e) {
                    //Console.WriteLine(e);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    Console.WriteLine(e);
                    continue;
                } catch (System.Exception ex) {
                    Console.WriteLine(ex);
                    continue;
                }

                string[] files = null;
                try {
                    files = System.IO.Directory.GetFiles(currentDir);
                } catch (UnauthorizedAccessException e) {
                    //Console.WriteLine(e);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    Console.WriteLine(e);
                    continue;
                } catch (System.Exception ex) {
                    Console.WriteLine(ex);
                    continue;
                }

                foreach (string file in files) {
                    try {
                        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        if (extension == null) {
                            // 無条件に足します。
                            result.Add(fi.FullName);
                        } else {
                            // 拡張子がextensionと一致するファイルだけ足します。
                            if (String.Equals(extension.ToUpper(), fi.Extension.ToUpper(), StringComparison.Ordinal)) {
                                result.Add(fi.FullName);
                                //Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                            }
                        }
                    } catch (System.IO.FileNotFoundException e) {
                        Console.WriteLine(e);
                        continue;
                    } catch (System.IO.PathTooLongException ex) {
                        //Console.WriteLine(ex);
                        continue;
                    } catch (System.Exception ex) {
                        Console.WriteLine(ex);
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
