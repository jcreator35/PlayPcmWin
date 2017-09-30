using System;
using System.IO;

namespace PlayPcmWin {
    class FileDisappearCheck {
        public delegate void FileDisapeearedEventHandler(string path);

        public FileDisappearCheck(string path, FileDisapeearedEventHandler ev) {
            mPath = path;
            mFileDisappeared = ev;

            mFsw = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            mFsw.Deleted += new FileSystemEventHandler(OnDeleted);
            mFsw.Renamed += new RenamedEventHandler(OnRenamed);
            mFsw.Error += new ErrorEventHandler(OnError);
        }

        void OnError(object sender, ErrorEventArgs e) {
            Console.WriteLine("FileSystemWatcher raised Error event");

            if (e.GetException().GetType() == typeof(InternalBufferOverflowException)) {
                Console.WriteLine(("FileSystemWatcher InternalBufferOverflowException " + e.GetException().Message));
            }
        }

        void OnRenamed(object sender, RenamedEventArgs e) {
            mFileDisappeared(mPath);
        }

        void OnDeleted(object sender, FileSystemEventArgs e) {
            mFileDisappeared(mPath);
        }

        private string mPath;
        private FileSystemWatcher mFsw;
        private FileDisapeearedEventHandler mFileDisappeared;
    }
}
