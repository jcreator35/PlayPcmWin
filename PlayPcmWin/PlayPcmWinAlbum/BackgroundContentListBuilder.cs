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
            public volatile int fileCount;
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
            e.Result = BackgroundDoWorkImpl(rootPath, true);
        }

        private Stopwatch mStopWatch = new Stopwatch();

        public RunWorkerCompletedResult BackgroundDoWorkImpl(string rootPath, bool background) {
            mContentList.Clear();

            var result = new RunWorkerCompletedResult();
            result.fileCount = 0;
            result.path = rootPath;

            if (background) {
                mBw.ReportProgress(0, new ReportProgressArgs(string.Format(Properties.Resources.LogCountingFiles, rootPath)));
            }

            var flacList = DirectoryUtil.CollectFlacFilesOnFolder(rootPath, ".FLAC");
            if (background) {
                mBw.ReportProgress(0, new ReportProgressArgs(string.Format(Properties.Resources.LogReportCount, flacList.Length)));
            }

            int finished = 0;

            mStopWatch.Start();

            // Parallel.Forにした時はforの中に持っていく必要あり。
            if (background) {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
            }

            try {
                for (int i = 0; i < flacList.Length; ++i) {

                    string path = flacList[i];
                    var flacrw = new WWFlacRWCS.FlacRW();
                    int ercd = flacrw.DecodeHeader(path);
                    lock (mBw) {
                        if (0 <= ercd) {
                            ++result.fileCount;
                            var m = new WWFlacRWCS.Metadata();
                            flacrw.GetDecodedMetadata(out m);
                            var pic = new byte[0];
                            if (0 < m.pictureBytes) {
                                pic = new byte[m.pictureBytes];
                                flacrw.GetDecodedPicture(out pic, m.pictureBytes);
                            }
                            mContentList.Add(path, m.titleStr, 1, m.albumStr, m.artistStr, pic,
                                m.channels, m.bitsPerSample, m.sampleRate, m.PcmBytes / (m.channels * m.bitsPerSample / 8));
                        } else {
                            Console.WriteLine("FLAC decode failed {0} {1}", WWFlacRWCS.FlacRW.ErrorCodeToStr(ercd), path);
                        }
                        flacrw.DecodeEnd();

                        ++finished;

                        if (1000 < mStopWatch.ElapsedMilliseconds) {
                            if (background) {
                                var text = string.Format("{0} ({1}%) ...", Properties.Resources.LogCreatingMusicList,
                                    100 * finished / flacList.Length);
                                mBw.ReportProgress((int)(1000000L * finished / flacList.Length),
                                    new ReportProgressArgs(text));
                            }

                            mStopWatch.Restart();
                        }
                    }
                }
            } catch (System.IO.IOException ex) {
                Console.WriteLine(ex);
            } catch (System.Exception ex) {
                Console.WriteLine(ex);
            }

            mStopWatch.Stop();
            return result;
        }
    }
}
