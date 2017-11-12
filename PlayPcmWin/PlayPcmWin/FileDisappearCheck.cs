using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace PlayPcmWin {
    class FileDisappearCheck {
        public delegate void FileDisappearedEventHandler(string path);

        private static Dictionary<string, FileDisappearCheck> mInstances = new Dictionary<string, FileDisappearCheck>();

        public static void Clear() {
            mInstances.Clear();
        }

        private static void DumpKeyList() {
            int i=0;
            Console.WriteLine("Total {0}", mInstances.Count);
            foreach (var item in mInstances) {
                Console.WriteLine("{0}: {1}", i, item.Key);
                ++i;
            }
        }

        /// <summary>
        /// あるディレクトリの中の特定のアイテム(ファイルまたはサブディレクトリ)が消えたらイベントを送出する。
        /// </summary>
        /// <param name="dirPath">アイテムがあるディレクトリ</param>
        /// <param name="itemName">アイテム(サブディレクトリまたはファイル)</param>
        public FileDisappearCheck(string dirPath, string itemName, FileDisappearedEventHandler ev) {
            // Path.GetDirectoryName(path), Path.GetFileName(path));

            if (dirPath == null) {
                return;
            }

            string path = dirPath + "\\" + itemName;

            lock (mInstances) {
                if (mInstances.ContainsKey(path)) {
                    return;
                }


                mPath = path;
                mFileDisappeared = ev;

                mFsw = new FileSystemWatcher(dirPath, itemName);
                mFsw.NotifyFilter = NotifyFilters.DirectoryName
                        | NotifyFilters.FileName;
                mFsw.Deleted += new FileSystemEventHandler(OnDeleted);
                mFsw.Renamed += new RenamedEventHandler(OnRenamed);
                mFsw.Error += new ErrorEventHandler(OnError);
                mFsw.EnableRaisingEvents = true;
                mInstances.Add(path, this);

                // DumpKeyList();

                // 親ディレクトリも調べる。
                if (mInstances.ContainsKey(dirPath)) {
                    return;
                }
            }

            // 親ディレクトリが削除されたときもイベントを送出する。
            var di = Directory.GetParent(dirPath);
            if (di != null && di.FullName != null && 0 < di.FullName.Length) {
                string ppDir = di.FullName;

                StringBuilder sb = new StringBuilder();
                for (int i=ppDir.Length; i < dirPath.Length; ++i) {
                    sb.Append(dirPath[i]);
                }

                string subDir = sb.ToString();
                subDir = subDir.TrimStart('\\', '/');

                var p = new FileDisappearCheck(ppDir, subDir, ev);
            }
        }

        void OnError(object sender, ErrorEventArgs e) {
            Console.WriteLine("FileSystemWatcher raised Error event");

            if (e.GetException().GetType() == typeof(InternalBufferOverflowException)) {
                Console.WriteLine(("FileSystemWatcher InternalBufferOverflowException " + e.GetException().Message));
            }
            mFileDisappeared("");
        }

        void OnRenamed(object sender, RenamedEventArgs e) {
            mFileDisappeared(mPath);
        }

        void OnDeleted(object sender, FileSystemEventArgs e) {
            mFileDisappeared(mPath);
        }

        private string mPath;
        private FileSystemWatcher mFsw;
        private FileDisappearedEventHandler mFileDisappeared;
    }
}
