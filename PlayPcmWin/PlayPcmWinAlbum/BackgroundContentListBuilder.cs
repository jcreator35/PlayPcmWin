using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlayPcmWinAlbum {
    class BackgroundContentListBuilder {

        private ContentList mContentList;

        public BackgroundContentListBuilder(ContentList cl) {
            mContentList = cl;
        }

        public void AddProgressChanged(ProgressChangedEventHandler eh) {
            mBw.ProgressChanged += eh;
            mBw.WorkerReportsProgress = true;
        }

        public void AddRunWorkerCompleted(RunWorkerCompletedEventHandler eh) {
            mBw.RunWorkerCompleted += eh;
        }

        public void RunWorkerAsync(string path) {
            mBw.DoWork += Background_DoWork;
            mBw.RunWorkerAsync(path);
        }

        private BackgroundWorker mBw = new BackgroundWorker();

        public class RunWorkerCompletedResult {
            public int fileCount;
            public string path;
        }

        public class ReportProgressArgs {
            public string text;
            public ReportProgressArgs(string atext) {
                text = atext;
            }
        };

        private void Background_DoWork(object sender, DoWorkEventArgs e) {
            string rootPath = e.Argument as string;
            var result = new RunWorkerCompletedResult();
            e.Result = result;
            BackgroundDoWorkImpl(rootPath, result);
        }

        private Stopwatch mStopWatch = new Stopwatch();

        public void BackgroundDoWorkImpl(string rootPath, RunWorkerCompletedResult result) {
            result.fileCount = 0;
            result.path = rootPath;

            mBw.ReportProgress(0, new ReportProgressArgs(string.Format(Properties.Resources.LogCountingFiles, rootPath)));

            var flacList = DirectoryUtil.CollectFlacFilesOnFolder(rootPath, ".FLAC");
            mBw.ReportProgress(0, new ReportProgressArgs(string.Format(Properties.Resources.LogReportCount, flacList.Length)));

            int finished = 0;

            mStopWatch.Start();

#if false
            Parallel.For(0, flacList.Length, i => {
#else
            for (int i=0; i<flacList.Length; ++i) {
#endif
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

                string path = flacList[i];
                var flacrw = new WWFlacRWCS.FlacRW();
                int ercd = flacrw.DecodeHeader(path);
                lock (mBw) {
                    if (0 <= ercd) {
                        ++result.fileCount;
                        var m = new WWFlacRWCS.Metadata();
                        flacrw.GetDecodedMetadata(out m);
                        byte[] pic = new byte[0];
                        if (0 < m.pictureBytes) {
                            pic = new byte[m.pictureBytes];
                            flacrw.GetDecodedPicture(out pic, m.pictureBytes);
                        }
                        mContentList.Add(path, m.titleStr, 1, m.albumStr, m.artistStr, pic);

                        flacrw.DecodeEnd();
                    }

                    ++finished;

                    if (1000 < mStopWatch.ElapsedMilliseconds) {
                        string text = string.Format("{0} ({1}/{2}) ...", Properties.Resources.LogCreatingMusicList,
                            finished, flacList.Length);

                        mBw.ReportProgress((int)(1000000L * finished / flacList.Length),
                            new ReportProgressArgs(text));

                        mStopWatch.Restart();
                    }
                }
            }
            
            //);

            mStopWatch.Stop();
        }
    }
}
