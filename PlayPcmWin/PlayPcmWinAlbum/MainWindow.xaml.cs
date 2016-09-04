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
        private DataGridPlayListHandler mDataGridPlayListHandler;

        public MainWindow() {
            InitializeComponent();
            mDataGridPlayListHandler = new DataGridPlayListHandler(mDataGridPlayList);
        }

        private enum State {
            Init,
            ReadContentList,
            CreateContentList,
            AlbumBrowsing,
            AlbumTrackBrowsing,
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
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Visible;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Hidden;
                mProgressBar.Visibility = Visibility.Collapsed;
                mTextBlockMessage.Visibility = Visibility.Visible;
                mMenuItemBack.IsEnabled = false;
                mMenuItemRefresh.IsEnabled = true;
                break;
            case State.AlbumBrowsing:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Visible;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Hidden;
                mProgressBar.Visibility = Visibility.Collapsed;
                mTextBlockMessage.Visibility = Visibility.Collapsed;
                mMenuItemBack.IsEnabled = false;
                mMenuItemRefresh.IsEnabled = true;
                break;
            case State.AlbumTrackBrowsing:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Hidden;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Visible;
                mProgressBar.Visibility = Visibility.Collapsed;
                mMenuItemBack.IsEnabled = true;
                mMenuItemRefresh.IsEnabled = false;
                break;
            }

            mState = t;
        }

        private bool ReadContentList() {
            return mContentList.Load();
        }

        private BackgroundContentListBuilder mBwContentListBuilder;

        private bool CreateContentList() {
            mTilePanel.Clear();

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = "C:\\audio";
            if (System.Windows.Forms.DialogResult.OK != dialog.ShowDialog()) {
                return false;
            }

            mBwContentListBuilder = new BackgroundContentListBuilder(mContentList);
            mBwContentListBuilder.AddProgressChanged(BackgroundContentListBuilder_ProgressChanged);
            mBwContentListBuilder.AddRunWorkerCompleted(BackgroundContentListBuilder_RunWorkerCompleted);

#if false
            // バグっているときの調査用。
            var result = mBwContentListBuilder.BackgroundDoWorkImpl(dialog.SelectedPath, false);
#else
            // バックグラウンド実行。
            mBwContentListBuilder.RunWorkerAsync(dialog.SelectedPath);
            ChangeDisplayState(State.CreateContentList);
#endif
            return true;
        }

        private void BackgroundContentListBuilder_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var rpa = (BackgroundContentListBuilder.ReportProgressArgs)e.UserState;

            mTextBlockMessage.Text = rpa.text;
            mProgressBar.Value = e.ProgressPercentage;
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

            mTilePanel.Clear();

            mTilePanel.UpdateTileSize(mTileSize);
            for (int i=0; i < mContentList.AlbumCount; ++i) {
                var album = mContentList.AlbumNth(i);
                var tic = new TiledItemContent(album.Name, album.AudioFileNth(0).AlbumCoverArt, album);
                var tileItem = new AlbumTile(tic, OnAlbumTileClicked, mAppExitToken.Token);

                mTilePanel.AddVirtualChild(tileItem);
                mTileItems.Add(tileItem);
            }
            mTilePanel.UpdateChildPosition();
        }

        private void CancelAll() {
            if (mAppExitToken != null) {
                mAppExitToken.Cancel();
                mAppExitToken = null;
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            CancelAll();
        }

        private void OnAlbumTileClicked(AlbumTile sender, TiledItemContent content) {
            Console.WriteLine("clicked {0}", content.DisplayName);
            var album = content.Tag as ContentList.Album;

            mDataGridPlayListHandler.ShowAlbum(album);
            ChangeDisplayState(State.AlbumTrackBrowsing);
        }

        private void mMenuItemBack_Click(object sender, RoutedEventArgs e) {
            ChangeDisplayState(State.AlbumBrowsing);
        }

        private void mMenuItemRefresh_Click(object sender, RoutedEventArgs e) {
            if (!CreateContentList()) {
                Close();
                return;
            }
        }

        private void dataGridPlayList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Console.WriteLine("DataGridPlayList_SelectionChanged()");
        }
    }
}
