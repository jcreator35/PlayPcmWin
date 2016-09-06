using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Globalization;
using Wasapi;
using System.Windows.Controls;

namespace PlayPcmWinAlbum {
    public partial class MainWindow : Window {
        private List<AlbumTile> mTileItems = new List<AlbumTile>();
        private Size mTileSize = new Size(256, 256);
        private CancellationTokenSource mAppExitToken = new CancellationTokenSource();
        private ContentList mContentList = new ContentList();
        private DataGridPlayListHandler mDataGridPlayListHandler;
        private PlaybackController mPlaybackController = new PlaybackController();
        private bool mInitialized = false;
        private BackgroundWorker mBackgroundLoad = new BackgroundWorker();
        private BackgroundWorker mBackgroundPlay = new BackgroundWorker();
        private string mPreferredDeviceIdString = "";
        private const int PROGRESS_REPORT_INTERVAL_MS = 100;
        private const int SLIDER_UPDATE_TICKS = 500;
        private const int DEFAULT_ZERO_FLUSH_MILLISEC = 1000;

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
            mBackgroundLoad.WorkerSupportsCancellation = true;
            mBackgroundPlay.WorkerSupportsCancellation = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;

            // アルバム一覧を読み出す。
            if (ReadContentList()) {
                UpdateContentList();
                ChangeDisplayState(State.AlbumBrowsing);
            } else {
                // アルバム一覧作成。
                if (!RefreshContentList()) {
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
                mStatusBar.Visibility = System.Windows.Visibility.Collapsed;
                mProgressBar.Visibility = Visibility.Collapsed;
                mTextBlockMessage.Visibility = Visibility.Visible;
                mMenuItemBack.IsEnabled = false;
                mMenuItemRefresh.IsEnabled = true;
                break;
            case State.AlbumBrowsing:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Visible;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Hidden;
                mDockPanelPlayback.Visibility = System.Windows.Visibility.Hidden;
                mStatusBar.Visibility = System.Windows.Visibility.Collapsed;
                mProgressBar.Visibility = Visibility.Collapsed;
                mTextBlockMessage.Visibility = Visibility.Collapsed;
                mMenuItemBack.IsEnabled = false;
                mMenuItemRefresh.IsEnabled = true;
                break;
            case State.AlbumTrackBrowsing:
                mAlbumScrollViewer.Visibility = System.Windows.Visibility.Hidden;
                mDataGridPlayList.Visibility = System.Windows.Visibility.Visible;
                mDockPanelPlayback.Visibility = System.Windows.Visibility.Visible;
                mStatusBar.Visibility = System.Windows.Visibility.Visible;
                mProgressBar.Visibility = Visibility.Collapsed;
                mMenuItemBack.IsEnabled = true;
                mMenuItemRefresh.IsEnabled = false;
                break;
            }

            mState = t;
        }

        private void UpdatePlaybackControlState(PlaybackController.State state) {
            switch (state) {
            case PlaybackController.State.Stopped:
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = false;
                mButtonPause.IsEnabled = false;
                mProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                mLabelPlayingTime.Content = PlaybackTime.PLAYING_TIME_ALLZERO;
                mStatusBarText.Content = Properties.Resources.MainStatusStopped;
                mGroupBoxPlaybackDevice.IsEnabled = true;
                mGroupBoxWasapiSettings.IsEnabled = true;
                break;
            case PlaybackController.State.Playing:
                {
                    mButtonPlay.IsEnabled = false;
                    mButtonStop.IsEnabled = true;
                    mButtonPause.IsEnabled = true;
                    mProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                    mGroupBoxPlaybackDevice.IsEnabled = false;
                    mGroupBoxWasapiSettings.IsEnabled = false;

                    var df = mPlaybackController.GetDeviceFormat();
                    mStatusBarText.Content = string.Format(Properties.Resources.MainStatusPlaying,
                            df.SampleRate, df.SampleFormat, df.NumChannels);
                }
                break;
            case PlaybackController.State.Loading:
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = false;
                mButtonPause.IsEnabled = false;
                mProgressBar.Visibility = System.Windows.Visibility.Visible;
                mStatusBarText.Content = Properties.Resources.MainStatusReadingFiles;
                mGroupBoxPlaybackDevice.IsEnabled = false;
                mGroupBoxWasapiSettings.IsEnabled = false;
                break;
            case PlaybackController.State.Paused:
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = true;
                mButtonPause.IsEnabled = false;
                mProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                mStatusBarText.Content = Properties.Resources.MainStatusPaused;
                mGroupBoxPlaybackDevice.IsEnabled = false;
                mGroupBoxWasapiSettings.IsEnabled = false;
                break;

            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        private bool ReadContentList() {
            return mContentList.Load();
        }

        private bool RefreshContentList() {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (System.Windows.Forms.DialogResult.OK != dialog.ShowDialog()) {
                return false;
            }

            mTilePanel.Clear();

            mBwContentListBuilder = new BackgroundContentListBuilder(mContentList);
            mBwContentListBuilder.AddProgressChanged(OnBackgroundContentListBuilder_ProgressChanged);
            mBwContentListBuilder.AddRunWorkerCompleted(OnBackgroundContentListBuilder_RunWorkerCompleted);

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

        private void OnBackgroundContentListBuilder_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var rpa = (BackgroundContentListBuilder.ReportProgressArgs)e.UserState;

            mTextBlockMessage.Text = rpa.text;
            mProgressBar.Value = e.ProgressPercentage;
        }

        private void OnBackgroundContentListBuilder_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var result = (BackgroundContentListBuilder.RunWorkerCompletedResult)e.Result;

            ChangeDisplayState(State.AlbumBrowsing);

            if (result == null) {
                Console.WriteLine("Error");

            } else if (result.fileCount == 0) {
                var dr = MessageBox.Show(string.Format(Properties.Resources.ErrorMusicFileNotFound, result.path), "FLAC file is not found!", MessageBoxButton.YesNo);
                if (dr == MessageBoxResult.Yes) {
                    // 別のフォルダを指定してもう一度探す。
                    OnMenuItemRefresh_Click(sender, null);
                } else {
                    Close();
                }
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
            mBackgroundPlay.CancelAsync();
            while (mBackgroundPlay.IsBusy) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));
                System.Threading.Thread.Sleep(100);
            }
            mBackgroundLoad.CancelAsync();
            while (mBackgroundLoad.IsBusy) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));
                System.Threading.Thread.Sleep(100);
            }

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
            album.UpdateIds();
            mContentList.AlbumSelected(album);

            UpdateDeviceList();

            var albumCoverArt = album.AudioFileNth(0).AlbumCoverArt;
            DispCoverArt(albumCoverArt);

            mLabelAlbumName.Content = album.Name;
            mDataGridPlayListHandler.ShowAlbum(album);
            ChangeDisplayState(State.AlbumTrackBrowsing);
            UpdatePlaybackControlState(PlaybackController.State.Stopped);
        }

        private void OnMenuItemBack_Click(object sender, RoutedEventArgs e) {
            mPlaybackController.Stop();
            mLabelAlbumName.Content = "";
            ChangeDisplayState(State.AlbumBrowsing);
        }

        private void OnMenuItemRefresh_Click(object sender, RoutedEventArgs e) {
            RefreshContentList();
        }

        private void OnDataGrid1_LoadingRow(object sender, DataGridRowEventArgs e) {
            e.Row.MouseDoubleClick += new MouseButtonEventHandler(OnDataGridPlayList_RowMouseDoubleClick);
        }

        private void OnDataGridPlayList_RowMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (mPlaybackController.GetState() == PlaybackController.State.Stopped
                    && e.ChangedButton == MouseButton.Left && mDataGridPlayList.IsReadOnly) {
                // 再生されていない状態で、再生リスト再生モードで項目左ボタンダブルクリックされたら再生開始する
                OnButtonPlay_Click(sender, e);
            }
        }

        private bool mPlayListMouseDown = false;

        private void OnDataGridPlayList_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            mPlayListMouseDown = true;

        }

        private void OnDataGridPlayList_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            mPlayListMouseDown = false;
        }

        private void OnDataGridPlayList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Console.WriteLine("DataGridPlayList_SelectionChanged()");
            if (!mPlayListMouseDown) {
                return;
            }

            if (mPlaybackController.GetState() != PlaybackController.State.Playing) {
                // 選択された曲を再生開始する。
                PlayAudioFile(mDataGridPlayList.SelectedIndex);
                return;
            }
 
            // 再生中の場合。

            var playingId = mPlaybackController.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (playingId < 0) {
                return;
            }

            // 再生中で、しかも、マウス押下中にこのイベントが来た場合で、
            // しかも、この曲を再生していない場合、この曲を再生する。
            if (mDataGridPlayList.SelectedIndex != playingId) {
                PlayAudioFile(mDataGridPlayList.SelectedIndex);
            }
        }

        class BackgroundLoadArgs {
            public ContentList.Album Album { get; set;}
            public int First { get; set; }
            public int DeviceIdx { get; set; }
            public BackgroundLoadArgs(ContentList.Album album, int first, int deviceIdx) {
                Album = album;
                First = first;
                DeviceIdx = deviceIdx;
            }
        };

        class BackgroundLoadResult {
            public BackgroundLoadArgs Args { get; set; }
            public bool Result { get; set; }
            public BackgroundLoadResult(BackgroundLoadArgs args, bool result) {
                Args = args;
                Result = result;
            }
        };

        private bool SetWasapiParams() {
            int bufferSizeMs = 170;
            if (!Int32.TryParse(mTextBoxBufferSizeMs.Text, out bufferSizeMs) || bufferSizeMs <= 0) {
                MessageBox.Show("Error: WASAPI buffer size should be integer value larger than zero");
                return false;
            }
            WasapiCS.DataFeedMode dfm = mRadioButtonEvent.IsChecked == true ? WasapiCS.DataFeedMode.EventDriven : WasapiCS.DataFeedMode.TimerDriven;

            mPlaybackController.SetWasapiParams(bufferSizeMs, DEFAULT_ZERO_FLUSH_MILLISEC, dfm);
            return true;
        }

        private void PlayAudioFile(int idx) {
            var album = mContentList.GetSelectedAlbum();
            var af = album.AudioFileNth(idx);

            switch (mPlaybackController.GetState()) {
            case PlaybackController.State.Playing:
            case PlaybackController.State.Paused:
                // 再生中。またはポーズ中。
                if (mPlaybackController.LoadedGroupId() == af.GroupId) {
                    // 再生中のグループと同じグループである。
                    // 再生曲を切り替える。
                    mPlaybackController.Play(idx);
                    UpdatePlaybackControlState(mPlaybackController.GetState());
                    return;
                } else {
                    // 異なるグループの曲なので再ロードが必要。
                    // 再生停止してロードする。
                    mPlaybackController.Stop();
                }
                break;
            default:
                break;
            }

            if (!SetWasapiParams()) {
                return;
            }

            // 選択曲が含まれるグループをロードする。
            var args = new BackgroundLoadArgs(
                    mContentList.GetSelectedAlbum(), idx, mListBoxPlaybackDevice.SelectedIndex);

            var playList = CreatePlayList(args.Album, args.First);
            int ercd = mPlaybackController.PlaylistCreateStart(args.DeviceIdx, args.Album.AudioFileNth(args.First));
            if (ercd < 0) {
                MessageBox.Show(string.Format(Properties.Resources.ErrorPlaybackStartFailed, ercd));
                return;
            }

            UpdatePlaybackControlState(PlaybackController.State.Loading);
            mBackgroundLoad = new BackgroundWorker();
            mBackgroundLoad.WorkerReportsProgress = true;
            mBackgroundLoad.WorkerSupportsCancellation = true;
            mBackgroundLoad.DoWork += new DoWorkEventHandler(OnBackgroundLoad_DoWork);
            mBackgroundLoad.ProgressChanged += new ProgressChangedEventHandler(OnBackgroundLoad_ProgressChanged);
            mBackgroundLoad.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnBackgroundLoad_RunWorkerCompleted);
            mBackgroundLoad.RunWorkerAsync(args);
        }

        private void OnButtonPlay_Click(object sender, RoutedEventArgs e) {
            PlayAudioFile(mDataGridPlayList.SelectedIndex);
        }

        /// <summary>
        /// album[first]と同一グループのファイル一覧作成。
        /// </summary>
        private static List<ContentList.AudioFile> CreatePlayList(ContentList.Album album, int first) {
            var afList = new List<ContentList.AudioFile>();
            var firstAf = album.AudioFileNth(first);

            int groupId = firstAf.GroupId;

            for (int i = 0; i < album.AudioFileCount; ++i) {
                var af = album.AudioFileNth(i);

                if (groupId == af.GroupId) {
                    afList.Add(af);
                }
            }
            return afList;
        }

        void OnBackgroundLoad_DoWork(object sender, DoWorkEventArgs e) {
            mBackgroundLoad.ReportProgress(0);

            var args = e.Argument as BackgroundLoadArgs;

            var playList = CreatePlayList(args.Album, args.First);

            int added = 0;
            for (int i = 0; i < playList.Count; ++i) {
                var af = playList[i];
                if (mPlaybackController.LoadAdd(af)) {
                    ++added;
                }

                if (mBackgroundLoad.CancellationPending) {
                    e.Cancel = true;
                    return;
                }
                mBackgroundLoad.ReportProgress((i + 1) * 100 / playList.Count);
            }
            mPlaybackController.PlaylistCreateEnd();

            e.Result = new BackgroundLoadResult(args, 0 < added);
        }

        void OnBackgroundLoad_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (mBackgroundLoad.CancellationPending) {
                // アプリ終了。
                return;
            }

            mProgressBar.Value = e.ProgressPercentage;
        }

        void OnBackgroundLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                return;
            }

            var result = e.Result as BackgroundLoadResult;
            if (result.Result) {
                mPlaybackController.Play(result.Args.First);
            } else {
                MessageBox.Show("Error: File load failed!");
            }

            UpdatePlaybackControlState(mPlaybackController.GetState());
            mBackgroundPlay = new BackgroundWorker();
            mBackgroundPlay.WorkerSupportsCancellation = true;
            mBackgroundPlay.WorkerReportsProgress = true;
            mBackgroundPlay.ProgressChanged += new ProgressChangedEventHandler(OnBackgroundPlay_ProgressChanged);
            mBackgroundPlay.DoWork += new DoWorkEventHandler(OnBackgroundPlay_DoWork);
            mBackgroundPlay.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnBackgroundPlay_RunWorkerCompleted);
            mBackgroundPlay.RunWorkerAsync();
        }

        void OnBackgroundPlay_DoWork(object sender, DoWorkEventArgs e) {
            bool bEnd = true;
            do {
                if (mBackgroundPlay.CancellationPending) {
                    e.Cancel = true;
                    return;
                }

                mBackgroundPlay.ReportProgress(0);
                bEnd = mPlaybackController.Run(PROGRESS_REPORT_INTERVAL_MS);
            } while (!bEnd);
        }

        long mLastSliderPositionUpdateTime = 0;

        void OnBackgroundPlay_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (mBackgroundPlay.CancellationPending) {
                // アプリ終了。
                return;
            }

            // 再生中PCMデータ(または一時停止再開時再生予定PCMデータ等)の再生位置情報を画面に表示する。
            WasapiCS.PcmDataUsageType usageType = WasapiCS.PcmDataUsageType.NowPlaying;
            int pcmDataId = mPlaybackController.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (pcmDataId < 0) {
                pcmDataId = mPlaybackController.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
                usageType = WasapiCS.PcmDataUsageType.PauseResumeToPlay;
            }
            if (pcmDataId < 0) {
                pcmDataId = mPlaybackController.GetPcmDataId(WasapiCS.PcmDataUsageType.SpliceNext);
                usageType = WasapiCS.PcmDataUsageType.SpliceNext;
            } 
            
            string playingTimeString = string.Empty;
            if (pcmDataId < 0) {
                playingTimeString = PlaybackTime.PLAYING_TIME_UNKNOWN;
            } else {
                if (mDataGridPlayList.SelectedIndex != pcmDataId) {
                    mDataGridPlayList.SelectedIndex = pcmDataId;
                    mDataGridPlayList.ScrollIntoView(pcmDataId);
                }

                var playPos = mPlaybackController.GetCursorLocation(usageType);
                var stat = mPlaybackController.GetSessionStatus();

                long now = DateTime.Now.Ticks;
                if (now - mLastSliderPositionUpdateTime > SLIDER_UPDATE_TICKS) {
                    // スライダー位置の更新。0.5秒に1回
                    mSlider1.Maximum = playPos.TotalFrameNum;
                    if (!mSliderSliding || playPos.TotalFrameNum <= mSlider1.Value) {
                        mSlider1.Value = playPos.PosFrame;
                    }
                    mLastSliderPositionUpdateTime = now;
                }

                playingTimeString = PlaybackTime.CreateDisplayString(
                    (int)(playPos.PosFrame / stat.DeviceSampleRate),
                    (int)(playPos.TotalFrameNum / stat.DeviceSampleRate));
            }

            // 再生時間表示の再描画をできるだけ抑制する。負荷が減る効果がある
            if (playingTimeString != string.Empty && 0 != string.Compare((string)mLabelPlayingTime.Content, playingTimeString)) {
                mLabelPlayingTime.Content = playingTimeString;
            } else {
                //System.Console.WriteLine("time disp update skipped");
            }
        }

        void OnBackgroundPlay_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                // アプリ終了。
                return;
            }

            UpdatePlaybackControlState(mPlaybackController.GetState());
        }

        private void OnButtonStop_Click(object sender, RoutedEventArgs e) {
            mPlaybackController.Stop();
            UpdatePlaybackControlState(PlaybackController.State.Stopped);
        }

        private void OnButtonPause_Click(object sender, RoutedEventArgs e) {
            if (mPlaybackController.Pause()) {
                UpdatePlaybackControlState(PlaybackController.State.Paused);
            }
        }

        private void ButtonNextOrPrevClickedWhenPlaying(UpdateOrdinal updateOrdinal) {
            int albumAudioFileCount = mContentList.GetSelectedAlbum().AudioFileCount;

            int idx = mPlaybackController.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (idx < 0) {
                // fixme:
                // 曲を再生中ではなく、再生準備中の場合など。
                // wavDataId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
                //nextTask = NextTaskType.PlayPauseSpecifiedGroup;
                return;
            } else {
                // 再生リストに登録されている曲数が1曲で、しかも
                // その曲を再生中に、次の曲または前の曲ボタンが押された場合、曲を頭出しする。
                if (1 == albumAudioFileCount) {
                    mPlaybackController.SetPosFrame(0);
                    return;
                }
            }

            int nextIdx = updateOrdinal(idx);
            if (nextIdx < 0) {
                nextIdx = 0;
            }
            if (albumAudioFileCount <= nextIdx) {
                nextIdx = 0;
            }

            if (nextIdx == idx) {
                // 1曲目再生中に前の曲を押した場合頭出しする。
                mPlaybackController.SetPosFrame(0);
                return;
            }

            PlayAudioFile(nextIdx);
        }

        private void ButtonNextOrPrevClickedWhenStop(UpdateOrdinal updateOrdinal) {
            var idx = mDataGridPlayList.SelectedIndex;
            idx = updateOrdinal(idx);
            if (idx < 0) {
                idx = 0;
            } else if (mDataGridPlayList.Items.Count <= idx) {
                idx = 0;
            }
            mDataGridPlayList.SelectedIndex = idx;
            mDataGridPlayList.ScrollIntoView(mDataGridPlayList.SelectedItem);
        }

        private delegate int UpdateOrdinal(int v);
        private void ButtonNextOrPrevClicked(UpdateOrdinal updateOrdinal) {
            switch (mPlaybackController.GetState()) {
            case PlaybackController.State.Paused:
            case PlaybackController.State.Playing:
                ButtonNextOrPrevClickedWhenPlaying(updateOrdinal);
                break;
            case PlaybackController.State.Stopped:
                ButtonNextOrPrevClickedWhenStop(updateOrdinal);
                break;
            }
        }

        private void OnButtonPrev_Click(object sender, RoutedEventArgs e) {
            ButtonNextOrPrevClicked((x) => { return --x; });
        }

        private void OnButtonNext_Click(object sender, RoutedEventArgs e) {
            ButtonNextOrPrevClicked((x) => { return ++x; });
        }

        private bool mSliderSliding = false;
        private long mLastSliderValue = 0;

        private void OnSlider1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.Source != mSlider1) {
                return;
            }

            mLastSliderValue = (long)mSlider1.Value;
            mSliderSliding = true;
        }

        private void OnSlider1_MouseMove(object sender, MouseEventArgs e) {
            if (e.Source != mSlider1) {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                mLastSliderValue = (long)mSlider1.Value;
                if (!mButtonPlay.IsEnabled) {
                    mPlaybackController.SetPosFrame((long)mSlider1.Value);
                }
            }
        }
        private void OnSlider1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.Source != mSlider1) {
                return;
            }

            if (!mButtonPlay.IsEnabled &&
                    mLastSliderValue != (long)mSlider1.Value) {
                mPlaybackController.SetPosFrame((long)mSlider1.Value);
            }

            mLastSliderValue = 0;
            mSliderSliding = false;
        }

        private void OnListBoxPlaybackDevice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mPreferredDeviceIdString = mPlaybackController.GetDeviceAttribute(mListBoxPlaybackDevice.SelectedIndex).DeviceIdString;
        }
    }
}
