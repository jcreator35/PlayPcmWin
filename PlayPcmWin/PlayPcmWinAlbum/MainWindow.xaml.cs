using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace PlayPcmWinAlbum {
    public partial class MainWindow : Window {
        private List<AlbumTile> mTileItems = new List<AlbumTile>();
        private Size mTileSize = new Size(256, 256);
        private CancellationTokenSource mAppExitToken = new CancellationTokenSource();
        private ContentList mContentList = new ContentList();
        private DataGridPlayListHandler mDataGridPlayListHandler;
        private PlaybackController mPlaybackController = new PlaybackController();
        private bool mInitialized = false;

        private enum State {
            Init,
            ReadContentList,
            CreateContentList,
            AlbumBrowsing,
            AlbumTrackBrowsing,
        };

        private State mState = State.Init;
        private BackgroundContentListBuilder mBwContentListBuilder;

        public MainWindow() {
            InitializeComponent();
            mDataGridPlayListHandler = new DataGridPlayListHandler(mDataGridPlayList);
            mLabelAlbumName.Content = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

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
            mPlaybackController.Init();
        }

        private void ChangeDisplayState(State t) {

            switch (t) {
            case State.Init:
            case State.CreateContentList:
            case State.ReadContentList:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Visible;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Hidden;
                mDockPanelPlayback.Visibility = System.Windows.Visibility.Hidden;
                mProgressBar.Visibility = Visibility.Collapsed;
                mTextBlockMessage.Visibility = Visibility.Visible;
                mMenuItemBack.IsEnabled = false;
                mMenuItemRefresh.IsEnabled = true;
                break;
            case State.AlbumBrowsing:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Visible;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Hidden;
                mDockPanelPlayback.Visibility = System.Windows.Visibility.Hidden;
                mProgressBar.Visibility = Visibility.Collapsed;
                mTextBlockMessage.Visibility = Visibility.Collapsed;
                mMenuItemBack.IsEnabled = false;
                mMenuItemRefresh.IsEnabled = true;
                break;
            case State.AlbumTrackBrowsing:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Hidden;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Visible;
                mDockPanelPlayback.Visibility = System.Windows.Visibility.Visible;
                mProgressBar.Visibility = Visibility.Collapsed;
                mMenuItemBack.IsEnabled = true;
                mMenuItemRefresh.IsEnabled = false;
                break;
            }

            mState = t;
        }

        private void UpdatePlaybackControlState() {
            var state = mPlaybackController.GetState();
            switch (state) {
            case PlaybackController.State.Stopped:
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = false;
                break;
            case PlaybackController.State.Playing:
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        private bool ReadContentList() {
            return mContentList.Load();
        }

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

            mPlaybackController.Stop();
            mPlaybackController.Term();
        }

        private void OnAlbumTileClicked(AlbumTile sender, TiledItemContent content) {
            Console.WriteLine("clicked {0}", content.DisplayName);
            var album = content.Tag as ContentList.Album;
            ShowAlbum(album);
        }

        private void DispCoverArt(byte[] albumCoverArt) {
            if (albumCoverArt.Length == 0) {
                mImageCoverArt.Source = null;
                mImageCoverArt.Visibility = System.Windows.Visibility.Collapsed;
            } else {
                try {
                    using (var stream = new MemoryStream(albumCoverArt)) {
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.UriSource = null;
                        bi.StreamSource = stream;
                        bi.EndInit();

                        mImageCoverArt.Source = bi;
                        mImageCoverArt.Visibility = System.Windows.Visibility.Visible;
                    }
                } catch (IOException ex) {
                    Console.WriteLine("D: DispCoverart {0}", ex);
                    mImageCoverArt.Source = null;
                } catch (System.IO.FileFormatException ex) {
                    Console.WriteLine("D: DispCoverart {0}", ex);
                    mImageCoverArt.Source = null;
                }
            }
        }

        private string mPreferredDeviceIdString = "";

        private void UpdateDeviceList() {
            mListBoxPlaybackDevice.Items.Clear();
            mPlaybackController.EnumerateDevices();
            if (mPlaybackController.GetDeviceCount() == 0) {
                MessageBox.Show("Error: playback device not found");
                Close();
            }

            for (int i = 0; i < mPlaybackController.GetDeviceCount(); ++i) {
                var attr = mPlaybackController.GetDeviceAttribute(i);
                mListBoxPlaybackDevice.Items.Add(attr.Name);
                if (0 == string.Compare(mPreferredDeviceIdString, attr.DeviceIdString)) {
                    mListBoxPlaybackDevice.SelectedIndex = i;
                }
            }

            if (mListBoxPlaybackDevice.SelectedIndex < 0) {
                mListBoxPlaybackDevice.SelectedIndex = 0;
                mPreferredDeviceIdString = mPlaybackController.GetDeviceAttribute(0).DeviceIdString;
            }
        }

        private void ShowAlbum(ContentList.Album album) {
            mContentList.AlbumSelected(album);

            UpdateDeviceList();

            var albumCoverArt = album.AudioFileNth(0).AlbumCoverArt;
            DispCoverArt(albumCoverArt);

            mLabelAlbumName.Content = album.Name;
            mDataGridPlayListHandler.ShowAlbum(album);
            ChangeDisplayState(State.AlbumTrackBrowsing);
            UpdatePlaybackControlState();
        }

        private void mMenuItemBack_Click(object sender, RoutedEventArgs e) {
            mPlaybackController.Stop();
            mLabelAlbumName.Content = "";
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

        private void mMenuItemSettings_Click(object sender, RoutedEventArgs e) {

        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e) {
            mPlaybackController.Play(mListBoxPlaybackDevice.SelectedIndex, mContentList.GetSelectedAlbum());

            UpdatePlaybackControlState();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            mPlaybackController.Stop();

            UpdatePlaybackControlState();
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e) {

        }

        private void buttonPrev_Click(object sender, RoutedEventArgs e) {

        }

        private void buttonNext_Click(object sender, RoutedEventArgs e) {

        }

        private void slider1_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {

        }

        private void mListBoxPlaybackDevice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mPreferredDeviceIdString = mPlaybackController.GetDeviceAttribute(mListBoxPlaybackDevice.SelectedIndex).DeviceIdString;
        }
    }
}
