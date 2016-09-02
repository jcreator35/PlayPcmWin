using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.ComponentModel;

namespace PlayPcmWinAlbum {
    public partial class MainWindow : Window {
        private List<AlbumTile> mTileItems = new List<AlbumTile>();
        private Size mTileSize = new Size(256, 256);
        private CancellationTokenSource mAppExitToken = new CancellationTokenSource();

        private ContentList mContentList = new ContentList();

        public MainWindow() {
            InitializeComponent();
        }

        private enum State {
            Init,
            ReadContentList,
            CreateContentList,
            AlbumBrowsing,
        };

        private State mState = State.Init;

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // アルバム一覧を読み出す。
            if (ReadContentList()) {
                UpdateContentList();
                ChangeDisplayState(State.AlbumBrowsing);
            } else {
                // アルバム一覧作成。
                if (!CreateContentList()) {
                    Close();
                    return;
                }
            }
        }

        private void ChangeDisplayState(State t) {

            switch (t) {
            case State.Init:
            case State.CreateContentList:
            case State.ReadContentList:
                progressBar.Visibility = Visibility.Visible;
                textBlockMessage.Visibility = Visibility.Visible;
                break;
            case State.AlbumBrowsing:
                progressBar.Visibility = Visibility.Collapsed;
                textBlockMessage.Visibility = Visibility.Collapsed;
                break;
            }

            mState = t;
        }

        private bool ReadContentList() {
            return mContentList.Load();
        }

        private BackgroundContentListBuilder mBwContentListBuilder;

        private bool CreateContentList() {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = "C:\\audio";
            if (System.Windows.Forms.DialogResult.OK != dialog.ShowDialog()) {
                return false;
            }

            mBwContentListBuilder = new BackgroundContentListBuilder(mContentList);
            mBwContentListBuilder.AddProgressChanged(BackgroundContentListBuilder_ProgressChanged);
            mBwContentListBuilder.AddRunWorkerCompleted(BackgroundContentListBuilder_RunWorkerCompleted);

#if false
            var result = new BackgroundContentListBuilder.RunWorkerCompletedResult();
            mBwContentListBuilder.BackgroundDoWorkImpl(dialog.SelectedPath, result);
#else
            mBwContentListBuilder.RunWorkerAsync(dialog.SelectedPath);
            ChangeDisplayState(State.CreateContentList);
#endif
            return true;
        }

        private void BackgroundContentListBuilder_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var rpa = (BackgroundContentListBuilder.ReportProgressArgs)e.UserState;

            textBlockMessage.Text = rpa.text;
            progressBar.Value = e.ProgressPercentage;
        }

        private void BackgroundContentListBuilder_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var result = (BackgroundContentListBuilder.RunWorkerCompletedResult)e.Result;

            ChangeDisplayState(State.AlbumBrowsing);

            if (result == null) {
                Console.WriteLine("Error");

            } else if (result.fileCount == 0) {
                MessageBox.Show(string.Format(Properties.Resources.ErrorMusicFileNotFound, result.path));
                Close();
            } else {
                mContentList.Save();
                UpdateContentList();
            }
        }

        private void UpdateContentList() {
            AlbumTile.UpdateTileSize(mTileSize);

            tilePanel.UpdateTileSize(mTileSize);
            for (int i=0; i < mContentList.AlbumCount; ++i) {
                var album = mContentList.AlbumNth(i);
                var tic = new TiledItemContent(album.Name, album.RepresentativeAudioFile.AlbumCoverArt);
                var tileItem = new AlbumTile(tic, OnAlbumTileClicked, mAppExitToken.Token);

                tilePanel.AddVirtualChild(tileItem);
                mTileItems.Add(tileItem);
            }
            tilePanel.UpdateChildPosition();
        }

        private void CancelAll() {
            if (mAppExitToken != null) {
                mAppExitToken.Cancel();
                mAppExitToken = null;
            }
        }

        private void OnAlbumTileClicked(AlbumTile sender, TiledItemContent content) {
            Console.WriteLine("clicked {0}", content.DisplayName);
        }

        private void Window_Closed(object sender, EventArgs e) {
            CancelAll();
        }
    }
}
