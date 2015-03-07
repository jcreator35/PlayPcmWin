// 日本語UTF-8

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wasapi;
using WavRWLib2;
using System.IO;
using System.ComponentModel;
using PcmDataLib;
using WasapiPcmUtil;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace PlayPcmWin
{
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// 再生の進捗状況を取りに行き表示を更新する時間間隔。単位はミリ秒
        /// </summary>
        const int PROGRESS_REPORT_INTERVAL_MS = 500;

        private const string PLAYING_TIME_UNKNOWN = "--:--:--/--:--:--";
        private const string PLAYING_TIME_ALLZERO = "00:00:00/00:00:00";

        /// <summary>
        /// 共有モードの音量制限。
        /// </summary>
        const double SHARED_MAX_AMPLITUDE = 0.98;

        private const int TYPICAL_READ_FRAMES = 4 * 1024 * 1024;

        private WasapiCS wasapi;

        private Wasapi.WasapiCS.StateChangedCallback m_wasapiStateChangedDelegate;

        private Preference m_preference = new Preference();

        class PlayListColumnInfo {
            public string Name { get; set; }
            public DataGridLength Width { get; set; }
            public PlayListColumnInfo(string name, DataGridLength width) {
                Name = name;
                Width = width;
            }
        };
        private PlayListColumnInfo[] m_playlistColumnDefaults = {
            new PlayListColumnInfo("Title", DataGridLength.Auto),
            new PlayListColumnInfo("Duration", DataGridLength.Auto),
            new PlayListColumnInfo("Artist", DataGridLength.Auto),
            new PlayListColumnInfo("AlbumTitle", DataGridLength.Auto),
            new PlayListColumnInfo("SampleRate", DataGridLength.Auto),

            new PlayListColumnInfo("QuantizationBitRate", DataGridLength.Auto),
            new PlayListColumnInfo("NumChannels", DataGridLength.SizeToCells),
            new PlayListColumnInfo("BitRate", DataGridLength.Auto),
            new PlayListColumnInfo("IndexNr", DataGridLength.SizeToCells),
            new PlayListColumnInfo("ReadSeparaterAfter", DataGridLength.SizeToCells)
        };

        /// <summary>
        /// PcmDataの表示用リスト。
        /// </summary>
        private PcmDataList m_pcmDataListForDisp = new PcmDataList();

        /// <summary>
        /// PcmDataの再生用リスト。(通常は表示用リストと同じ。シャッフルの時は順番が入れ替わる)
        /// </summary>
        private PcmDataList m_pcmDataListForPlay = new PcmDataList();

        /// <summary>
        /// 再生リスト項目情報。
        /// </summary>
        private ObservableCollection<PlayListItemInfo> m_playListItems = new ObservableCollection<PlayListItemInfo>();

        private BackgroundWorker m_playWorker;
        private BackgroundWorker m_readFileWorker;
        private BackgroundWorker m_playlistReadWorker;

        private System.Diagnostics.Stopwatch m_sw = new System.Diagnostics.Stopwatch();
        private bool m_playListMouseDown = false;

        /// <summary>
        /// 次にプレイリストにAddしたファイルに振られるGroupId。
        /// </summary>
        private int m_groupIdNextAdd = 0;

        /// <summary>
        /// メモリ上に読み込まれているGroupId。
        /// </summary>
        private int m_loadedGroupId = -1;
        
        /// <summary>
        /// PCMデータ読み込み中グループIDまたは読み込み完了したグループID
        /// </summary>
        private int m_loadingGroupId = -1;


        /// <summary>
        /// デバイスSetup情報。サンプリングレート、量子化ビット数…。
        /// </summary>
        DeviceSetupParams m_deviceSetupParams = new DeviceSetupParams();

        DeviceAttributes m_useDevice;
        bool m_deviceListUpdatePending;

        // 再生停止完了後に行うタスク。
        enum TaskType {
            /// <summary>
            /// 停止する。
            /// </summary>
            None,

            /// <summary>
            /// 指定されたグループをメモリに読み込み、グループの先頭の項目を再生開始する。
            /// </summary>
            PlaySpecifiedGroup,

            /// <summary>
            /// 指定されたグループをメモリに読み込み、グループの先頭の項目を再生一時停止状態にする。
            /// </summary>
            PlayPauseSpecifiedGroup,
        }

        class Task {
            public Task() {
                Type = TaskType.None;
                GroupId = -1;
                WavDataId = -1;
            }

            public Task(TaskType type) {
                Set(type);
            }

            public Task(TaskType type, int groupId, int wavDataId) {
                Set(type, groupId, wavDataId);
            }

            public void Set(TaskType type) {
                // 現時点で、このSet()のtypeはNoneしかありえない。
                System.Diagnostics.Debug.Assert(type == TaskType.None);
                Type = type;
            }

            public void Set(TaskType type, int groupId, int wavDataId) {
                Type = type;
                GroupId = groupId;
                WavDataId = wavDataId;
            }

            public TaskType Type { get; set; }
            public int GroupId { get; set; }
            public int WavDataId { get; set; }
        };

        Task m_task = new Task();

        enum State {
            未初期化,
            再生リスト読み込み中,
            再生リストなし,
            再生リストあり,

            // これ以降の状態にいる場合、再生リストに新しいファイルを追加できない。
            デバイスSetup完了,
            ファイル読み込み完了,
            再生中,
            再生一時停止中,
            再生停止開始,
            再生グループ読み込み中,
        }

        /// <summary>
        /// UIの状態。
        /// </summary>
        private State m_state = State.未初期化;

        private void ChangeState(State nowState) {
            m_state = nowState;
        }

        /// <summary>
        /// 指定されたWavDataIdの、再生リスト位置番号(再生リスト内のindex)を戻す。
        /// </summary>
        /// <param name="pcmDataId">再生リスト位置番号を知りたいPcmDataのId</param>
        /// <returns>再生リスト位置番号(再生リスト内のindex)。見つからないときは-1</returns>
        private int GetPlayListIndexOfPcmDataId(int pcmDataId) {
            for (int i = 0; i < m_playListItems.Count(); ++i) {
                if (m_playListItems[i].PcmData() != null
                        && m_playListItems[i].PcmData().Id == pcmDataId) {
                    return i;
                }
            }

            return -1;
        }

        enum ReadPpwPlaylistMode {
            RestorePlaylistOnProgramStart,
            AppendLoad,
        };

        private class PlaylistReadWorkerArg {
            public PlaylistSave pl;
            public ReadPpwPlaylistMode mode;
            public PlaylistReadWorkerArg(PlaylistSave pl, ReadPpwPlaylistMode mode) {
                this.pl   = pl;
                this.mode = mode;
            }
        };

        /// <summary>
        /// ノンブロッキング版 PPW再生リスト読み込み。
        /// 読み込みを開始した時点で制御が戻り、再生リスト読み込み中状態になる。
        /// その後、バックグラウンドで再生リストを読み込む。完了すると再生リストあり状態に遷移する。
        /// </summary>
        /// <param name="path">string.Emptyのとき: IsolatedStorageに保存された再生リストを読む。</param>
        private void ReadPpwPlaylistStart(string path, ReadPpwPlaylistMode mode) {
            ChangeState(State.再生リスト読み込み中);
            UpdateUIStatus();

            m_loadErrorMessages = new StringBuilder();

            PlaylistSave pl;
            if (path.Length == 0) {
                pl = PpwPlaylistRW.Load();
            } else {
                pl = PpwPlaylistRW.LoadFrom(path);
            }

            progressBar1.Visibility = System.Windows.Visibility.Visible;
            m_playlistReadWorker.RunWorkerAsync(new PlaylistReadWorkerArg(pl, mode));
        }

        /// <summary>
        /// Playlist read worker thread
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">PlaylistSave instance</param>
        void PlaylistReadWorker_DoWork(object sender, DoWorkEventArgs e) {
            var arg = e.Argument as PlaylistReadWorkerArg;
            e.Result = arg;

            if (null == arg.pl) {
                return;
            }

            int readAttemptCount = 0;
            int readSuccessCount=0;
            foreach (var p in arg.pl.Items) {
                int rv = ReadFileHeader(p.PathName, PcmHeaderReader.ReadHeaderMode.OnlyConcreteFile, null);
                if (1 == rv) {
                    // 読み込み成功。読み込んだPcmDataの曲名、アーティスト名、アルバム名、startTick等を上書きする。

                    // pcmDataのメンバ。
                    var pcmData = m_pcmDataListForDisp.Last();
                    pcmData.DisplayName = p.Title;
                    pcmData.AlbumTitle = p.AlbumName;
                    pcmData.ArtistName = p.ArtistName;
                    pcmData.StartTick = p.StartTick;
                    pcmData.EndTick = p.EndTick;
                    pcmData.CueSheetIndex = p.CueSheetIndex;

                    // playList表のメンバ。
                    var playListItem = m_playListItems[readSuccessCount];
                    playListItem.ReadSeparaterAfter = p.ReadSeparaterAfter;
                    ++readSuccessCount;
                }

                ++readAttemptCount;
                m_playlistReadWorker.ReportProgress(100 * readAttemptCount / arg.pl.Items.Count);
            }
        }

        void PlaylistReadWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void EnableDataGridPlaylist() {
            dataGridPlayList.IsEnabled = true;
            dataGridPlayList.ItemsSource = m_playListItems;

            if (0 <= m_preference.LastPlayItemIndex &&
                    m_preference.LastPlayItemIndex < dataGridPlayList.Items.Count) {
                dataGridPlayList.SelectedIndex = m_preference.LastPlayItemIndex;
                dataGridPlayList.ScrollIntoView(dataGridPlayList.SelectedItem);
            }
        }

        void PlaylistReadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var arg = e.Result as PlaylistReadWorkerArg;

            // Showing error MessageBox must be delayed until Window Loaded state because SplashScreen closes all MessageBoxes whose owner is DesktopWindow
            if (null != m_loadErrorMessages && 0 < m_loadErrorMessages.Length) {
                MessageBox.Show(m_loadErrorMessages.ToString(), Properties.Resources.RestoreFailedFiles, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            m_loadErrorMessages = null;
            progressBar1.Visibility = System.Windows.Visibility.Collapsed;

            switch (arg.mode) {
            case ReadPpwPlaylistMode.RestorePlaylistOnProgramStart:
                EnableDataGridPlaylist();
                break;
            default:
                break;
            }

            if (0 < m_playListItems.Count) {
                ChangeState(State.再生リストあり);
            } else {
                ChangeState(State.再生リストなし);
            }
            UpdateUIStatus();
        }

        private bool SavePpwPlaylist(string path) {
            var s = new PlaylistSave();

            for (int i=0; i<m_pcmDataListForDisp.Count(); ++i) {
                var p = m_pcmDataListForDisp.At(i);
                var playListItem = m_playListItems[i];

                s.Add(new PlaylistItemSave().Set(
                        p.DisplayName, p.AlbumTitle, p.ArtistName, p.FullPath,
                        p.CueSheetIndex, p.StartTick, p.EndTick, playListItem.ReadSeparaterAfter));
            }

            if (path.Length == 0) {
                return PpwPlaylistRW.Save(s);
            } else {
                return PpwPlaylistRW.SaveAs(s, path);
            }
        }

        /// <summary>
        /// true: slider is dragging
        /// </summary>
        private bool mSliderSliding = false;

        List<PreferenceAudioFilter> mPreferenceAudioFilterList = new List<PreferenceAudioFilter>();

        /// ///////////////////////////////////////////////////////////////////////////////////////////////////

        public MainWindow()
        {
            InitializeComponent();
            SetLocalizedTextToUI();

            this.AddHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(slider1_MouseLeftButtonDown), true);
            this.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(slider1_MouseLeftButtonUp), true);

            // InitializeComponent()によって、チェックボックスのチェックイベントが発生し
            // m_preferenceの内容が変わるので、InitializeComponent()の後にロードする。

            m_preference = PreferenceStore.Load();

            if (m_preference.ManuallySetMainWindowDimension) {
                // 記録されているウィンドウ形状が、一部分でも画面に入っていたら、そのウィンドウ形状に設定する。
                var windowRect = new System.Drawing.Rectangle(
                        (int)m_preference.MainWindowLeft,
                        (int)m_preference.MainWindowTop,
                        (int)m_preference.MainWindowWidth,
                        (int)m_preference.MainWindowHeight);

                bool inScreen = false;
                foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
                    if (!System.Drawing.Rectangle.Intersect(windowRect, screen.Bounds).IsEmpty) {
                        inScreen = true;
                        break;
                    }
                }
                if (inScreen) {
                    Left = m_preference.MainWindowLeft;
                    Top = m_preference.MainWindowTop;
                    if (100 <= m_preference.MainWindowWidth) {
                        Width = m_preference.MainWindowWidth;
                    }
                    if (100 <= m_preference.MainWindowHeight) {
                        Height = m_preference.MainWindowHeight;
                    }
                }
            }

            if (!m_preference.SettingsIsExpanded) {
                expanderSettings.IsExpanded = false;
            }

            AddLogText(string.Format(CultureInfo.InvariantCulture, "PlayPcmWin {0} {1}{2}",
                    AssemblyVersion, IntPtr.Size == 8 ? "64bit" : "32bit", Environment.NewLine));

            int hr = 0;
            wasapi = new WasapiCS();
            hr = wasapi.Init();
            AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.Init() {0:X8}{1}", hr, Environment.NewLine));

            m_wasapiStateChangedDelegate = new Wasapi.WasapiCS.StateChangedCallback(WasapiStatusChanged);
            wasapi.RegisterStateChangedCallback(m_wasapiStateChangedDelegate);

            textBoxLatency.Text = string.Format(CultureInfo.InvariantCulture, "{0}", m_preference.LatencyMillisec);

            checkBoxSoundEffects.IsChecked = m_preference.SoundEffectsEnabled;
            buttonSoundEffectsSettings.IsEnabled = m_preference.SoundEffectsEnabled;
            mPreferenceAudioFilterList = PreferenceAudioFilterStore.Load();
            UpdateSoundEffects(m_preference.SoundEffectsEnabled);

            switch (m_preference.WasapiSharedOrExclusive) {
            case WasapiSharedOrExclusiveType.Exclusive:
                radioButtonExclusive.IsChecked = true;
                break;
            case WasapiSharedOrExclusiveType.Shared:
                radioButtonShared.IsChecked = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            switch (m_preference.WasapiDataFeedMode) {
            case WasapiDataFeedModeType.EventDriven:
                radioButtonEventDriven.IsChecked = true;
                break;
            case WasapiDataFeedModeType.TimerDriven:
                radioButtonTimerDriven.IsChecked = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            UpdatePlaymodeComboBoxFromPreference();

            UpdateDeviceList();

            RestorePlaylistColumnOrderFromPreference();

            SetupBackgroundWorkers();

            PlayListItemInfo.SetNextRowId(1);
            m_groupIdNextAdd = 0;

            PreferenceUpdated();

            AddKeyListener();
        }

        private void Window_Loaded(object wSender, RoutedEventArgs we) {
            {
                // slider1のTrackをクリックしてThumbがクリック位置に移動した時Thumbがつままれた状態になるようにする
                slider1.ApplyTemplate();
                (slider1.Template.FindName("PART_Track", slider1) as Track).Thumb.MouseEnter += new MouseEventHandler((sliderSender, se) => {
                    if (se.LeftButton == MouseButtonState.Pressed && se.MouseDevice.Captured == null) {
                        var args = new MouseButtonEventArgs(se.MouseDevice, se.Timestamp, MouseButton.Left);
                        args.RoutedEvent = MouseLeftButtonDownEvent;
                        (sliderSender as Thumb).RaiseEvent(args);
                    }
                });
            }

            if (m_preference.StorePlaylistContent) {
                ReadPpwPlaylistStart(string.Empty, ReadPpwPlaylistMode.RestorePlaylistOnProgramStart);
            } else {
                // Issue 130
                EnableDataGridPlaylist();
                ChangeState(State.再生リストなし);
                UpdateUIStatus();
            }
        }

        /// <summary>
        /// 再生モードコンボボックスの項目
        /// </summary>
        enum ComboBoxPlayModeType {
            AllTracks,
            AllTracksRepeat,
            OneTrack,
            OneTrackRepeat,
            Shuffle,
            ShuffleRepeat,
            NUM
        };

        private void SetLocalizedTextToUI() {
            comboBoxPlayMode.Items.Clear();
            comboBoxPlayMode.Items.Add(Properties.Resources.MainPlayModeAllTracks);
            comboBoxPlayMode.Items.Add(Properties.Resources.MainPlayModeAllTracksRepeat);
            comboBoxPlayMode.Items.Add(Properties.Resources.MainPlayModeOneTrack);
            comboBoxPlayMode.Items.Add(Properties.Resources.MainPlayModeOneTrackRepeat);
            comboBoxPlayMode.Items.Add(Properties.Resources.MainPlayModeShuffle);
            comboBoxPlayMode.Items.Add(Properties.Resources.MainPlayModeShuffleRepeat);

            cmenuPlayListClear.Header = Properties.Resources.MainCMenuPlaylistClear;
            cmenuPlayListEditMode.Header = Properties.Resources.MainCMenuPlayListEditMode;

            menuFile.Header = Properties.Resources.MenuFile;
            menuItemFileNew.Header = Properties.Resources.MenuItemFileNew;
            menuItemFileOpen.Header = Properties.Resources.MenuItemFileOpen;
            menuItemFileSaveAs.Header = Properties.Resources.MenuItemFileSaveAs;
            menuItemFileSaveCueAs.Header = Properties.Resources.MenuItemFileSaveCueAs;
            menuItemFileExit.Header = Properties.Resources.MenuItemFileExit;

            menuTool.Header = Properties.Resources.MenuTool;
            menuItemToolSettings.Header = Properties.Resources.MenuItemToolSettings;

            menuPlayList.Header = Properties.Resources.MenuPlayList;
            menuItemPlayListClear.Header = Properties.Resources.MenuItemPlayListClear;
            menuItemPlayListItemEditMode.Header = Properties.Resources.MenuItemPlayListItemEditMode;

            menuHelp.Header = Properties.Resources.MenuHelp;
            menuItemHelpAbout.Header = Properties.Resources.MenuItemHelpAbout;
            menuItemHelpWeb.Header = Properties.Resources.MenuItemHelpWeb;

            groupBoxLog.Header = Properties.Resources.MainGroupBoxLog;
            groupBoxOutputDevices.Header = Properties.Resources.MainGroupBoxOutputDevices;
            groupBoxPlaybackControl.Header = Properties.Resources.MainGroupBoxPlaybackControl;
            groupBoxPlaylist.Header = Properties.Resources.MainGroupBoxPlaylist;
            groupBoxWasapiDataFeedMode.Header = Properties.Resources.MainGroupBoxWasapiDataFeedMode;

            groupBoxWasapiOperationMode.Header = Properties.Resources.MainGroupBoxWasapiOperationMode;
            groupBoxWasapiOutputLatency.Header = Properties.Resources.MainGroupBoxWasapiOutputLatency;
            groupBoxWasapiSettings.Header = Properties.Resources.MainGroupBoxWasapiSettings;

            buttonClearPlayList.Content = Properties.Resources.MainButtonClearPlayList;
            buttonDelistSelected.Content = Properties.Resources.MainButtonDelistSelected;
            buttonInspectDevice.Content = Properties.Resources.MainButtonInspectDevice;
            buttonNext.Content = Properties.Resources.MainButtonNext;
            buttonPause.Content = Properties.Resources.MainButtonPause;

            buttonPlay.Content = Properties.Resources.MainButtonPlay;
            buttonPrev.Content = Properties.Resources.MainButtonPrev;
            buttonSettings.Content = Properties.Resources.MainButtonSettings;
            buttonStop.Content = Properties.Resources.MainButtonStop;

            radioButtonEventDriven.Content = Properties.Resources.MainRadioButtonEventDriven;
            radioButtonExclusive.Content = Properties.Resources.MainRadioButtonExclusive;
            radioButtonShared.Content = Properties.Resources.MainRadioButtonShared;
            radioButtonTimerDriven.Content = Properties.Resources.MainRadioButtonTimerDriven;

            expanderSettings.Header = Properties.Resources.MainExpanderSettings;

            dataGridColumnAlbumTitle.Header = Properties.Resources.MainDataGridColumnAlbumTitle;
            dataGridColumnArtist.Header = Properties.Resources.MainDataGridColumnArtist;
            dataGridColumnBitRate.Header = Properties.Resources.MainDataGridColumnBitRate;
            dataGridColumnDuration.Header = Properties.Resources.MainDataGridColumnDuration;
            dataGridColumnIndexNr.Header = Properties.Resources.MainDataGridColumnIndexNr;

            dataGridColumnNumChannels.Header = Properties.Resources.MainDataGridColumnNumChannels;
            dataGridColumnQuantizationBitRate.Header = Properties.Resources.MainDataGridColumnQuantizationBitRate;
            dataGridColumnReadSeparaterAfter.Header = Properties.Resources.MainDataGridColumnReadSeparaterAfter;
            dataGridColumnSampleRate.Header = Properties.Resources.MainDataGridColumnSampleRate;
            dataGridColumnTitle.Header = Properties.Resources.MainDataGridColumnTitle;

            labelLoadingPlaylist.Content = Properties.Resources.MainStatusReadingPlaylist;

            groupBoxWasapiSoundEffects.Header = Properties.Resources.GroupBoxSoundEffects;
            buttonSoundEffectsSettings.Content = Properties.Resources.ButtonSoundEffectsSettings;
            checkBoxSoundEffects.Content = Properties.Resources.CheckBoxSoundEffects;
        }

        private bool IsPlayModeAllTracks() {
            ComboBoxPlayModeType t = (ComboBoxPlayModeType)comboBoxPlayMode.SelectedIndex;
            return t == ComboBoxPlayModeType.AllTracks
                || t == ComboBoxPlayModeType.AllTracksRepeat;
        }

        private bool IsPlayModeOneTrack() {
            ComboBoxPlayModeType t = (ComboBoxPlayModeType)comboBoxPlayMode.SelectedIndex;
            return t == ComboBoxPlayModeType.OneTrack
                || t == ComboBoxPlayModeType.OneTrackRepeat;
        }

        private bool IsPlayModeShuffle() {
            ComboBoxPlayModeType t = (ComboBoxPlayModeType)comboBoxPlayMode.SelectedIndex;
            return t == ComboBoxPlayModeType.Shuffle
                || t == ComboBoxPlayModeType.ShuffleRepeat;
        }

        private bool IsPlayModeRepeat() {
            ComboBoxPlayModeType t = (ComboBoxPlayModeType)comboBoxPlayMode.SelectedIndex;
            return t == ComboBoxPlayModeType.AllTracksRepeat
                || t == ComboBoxPlayModeType.OneTrackRepeat
                || t == ComboBoxPlayModeType.ShuffleRepeat;
        }

        private void UpdatePlaymodeComboBoxFromPreference() {
            if (m_preference.Shuffle) {
                 if (m_preference.PlayRepeat) {
                     comboBoxPlayMode.SelectedIndex = (int)ComboBoxPlayModeType.ShuffleRepeat;
                 } else {
                     comboBoxPlayMode.SelectedIndex = (int)ComboBoxPlayModeType.Shuffle;
                 }
            } else if (m_preference.PlayAllTracks) {
                if (m_preference.PlayRepeat) {
                    comboBoxPlayMode.SelectedIndex = (int)ComboBoxPlayModeType.AllTracksRepeat;
                } else {
                    comboBoxPlayMode.SelectedIndex = (int)ComboBoxPlayModeType.AllTracks;
                }
            } else {
                if (m_preference.PlayRepeat) {
                    comboBoxPlayMode.SelectedIndex = (int)ComboBoxPlayModeType.OneTrackRepeat;
                } else {
                    comboBoxPlayMode.SelectedIndex = (int)ComboBoxPlayModeType.OneTrack;
                }
            }
        }

        private void SetPreferencePlaymodeFromComboBox() {
            m_preference.PlayAllTracks = IsPlayModeAllTracks();
            m_preference.PlayRepeat    = IsPlayModeRepeat();
            m_preference.Shuffle       = IsPlayModeShuffle();
        }

        // 再生リストの列の順番設定の保存
        private void SavePlaylistColumnOrderToPreference() {
            var idxNameTable = new Dictionary<int, string>();
            int i=0;
            foreach (var item in dataGridPlayList.Columns) {
                idxNameTable.Add(item.DisplayIndex, m_playlistColumnDefaults[i].Name);
                ++i;
            }

            m_preference.PlayListColumnsOrder.Clear();
            foreach (var item in idxNameTable.OrderBy(x => x.Key)) {
                m_preference.PlayListColumnsOrder.Add(item.Value);
            }
        }

        // 再生リストの列の順番を設定から読み出し適用する
        private bool RestorePlaylistColumnOrderFromPreference() {
            var nameIdxTable = new Dictionary<string, int>();
            {
                int i=0;
                foreach (var item in m_preference.PlayListColumnsOrder) {
                    nameIdxTable.Add(item, i);
                    ++i;
                }
            }
            var columnIdxList = new List<int>();
            foreach (var item in m_playlistColumnDefaults) {
                int idx;
                if (!nameIdxTable.TryGetValue(item.Name, out idx)) {
                    Console.WriteLine("E: unknown playlist column name {0}", item);
                    return false;
                }
                columnIdxList.Add(idx);
            }

            if (columnIdxList.Count != dataGridPlayList.Columns.Count) {
                Console.WriteLine("E: playlist column count mismatch {0}", columnIdxList.Count);
                return false;
            }

            {
                int i=0;
                foreach (var item in dataGridPlayList.Columns) {
                    item.DisplayIndex = columnIdxList[i];
                    ++i;
                }
            }
            return true;
        }

        private void SetupBackgroundWorkers() {
            m_readFileWorker = new BackgroundWorker();
            m_readFileWorker.DoWork += new DoWorkEventHandler(ReadFileDoWork);
            m_readFileWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ReadFileRunWorkerCompleted);
            m_readFileWorker.WorkerReportsProgress = true;
            m_readFileWorker.ProgressChanged += new ProgressChangedEventHandler(ReadFileWorkerProgressChanged);
            m_readFileWorker.WorkerSupportsCancellation = true;

            m_playWorker = new BackgroundWorker();
            m_playWorker.WorkerReportsProgress = true;
            m_playWorker.DoWork += new DoWorkEventHandler(PlayDoWork);
            m_playWorker.ProgressChanged += new ProgressChangedEventHandler(PlayProgressChanged);
            m_playWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlayRunWorkerCompleted);
            m_playWorker.WorkerSupportsCancellation = true;

            m_playlistReadWorker = new BackgroundWorker();
            m_playlistReadWorker.WorkerReportsProgress = true;
            m_playlistReadWorker.DoWork += new DoWorkEventHandler(PlaylistReadWorker_DoWork);
            m_playlistReadWorker.ProgressChanged += new ProgressChangedEventHandler(PlaylistReadWorker_ProgressChanged);
            m_playlistReadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlaylistReadWorker_RunWorkerCompleted);
            m_playlistReadWorker.WorkerSupportsCancellation = false;
        }

        private void Window_Closed(object sender, EventArgs e) {
            Term();
        }

        private void MenuItemFileExit_Click(object sender, RoutedEventArgs e) {
            Exit();
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private static string SampleFormatTypeToStr(WasapiCS.SampleFormatType t) {
            switch (t) {
            case WasapiCS.SampleFormatType.Sfloat:
                return "32bit"+Properties.Resources.FloatingPointNumbers;
            case WasapiCS.SampleFormatType.Sint16:
                return "16bit";
            case WasapiCS.SampleFormatType.Sint24:
                return "24bit";
            case WasapiCS.SampleFormatType.Sint32:
                return "32bit";
            case WasapiCS.SampleFormatType.Sint32V24:
                return "32bit("+Properties.Resources.ValidBits + "=24)";
            default:
                System.Diagnostics.Debug.Assert(false);
                return "unknown";
            }
        }

        private void DispCoverart(byte[] pictureData) {

            if (null == pictureData || pictureData.Length <= 0) {
                imageCoverArt.Source = null;
                // imageCoverArt.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            try {
                using (var stream = new MemoryStream(pictureData)) {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.UriSource = null;
                    bi.StreamSource = stream;
                    bi.EndInit();

                    imageCoverArt.Source = bi;
                    // imageCoverArt.Visibility = System.Windows.Visibility.Visible;
                }
            } catch (IOException ex) {
                Console.WriteLine("D: DispCoverart {0}", ex);
                imageCoverArt.Source = null;
            } catch (System.IO.FileFormatException ex) {
                Console.WriteLine("D: DispCoverart {0}", ex);
                imageCoverArt.Source = null;
            }
        }

        private void UpdateCoverart() {
            if (!m_preference.DispCoverart) {
                // do not display coverart
                imageCoverArt.Source = null;
                imageCoverArt.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            // display coverart
            imageCoverArt.Visibility = System.Windows.Visibility.Visible;

            if (dataGridPlayList.SelectedIndex < 0) {
                DispCoverart(null);
                return;
            }
            PcmDataLib.PcmData w = m_playListItems[dataGridPlayList.SelectedIndex].PcmData();
            if (null != w && 0 < w.PictureBytes) {
                DispCoverart(w.PictureData);
            } else {
                DispCoverart(null);
            }
        }

        // 初期状態。再生リストなし。
        private void UpdateUIToInitialState() {
            cmenuPlayListClear.IsEnabled     = false;
            cmenuPlayListEditMode.IsEnabled  = true;
            menuItemFileNew.IsEnabled        = false;
            menuItemFileOpen.IsEnabled       = true;
            menuItemFileSaveAs.IsEnabled     = false;
            menuItemFileSaveCueAs.IsEnabled  = false;
            menuItemPlayListClear.IsEnabled = false;
            menuItemPlayListItemEditMode.IsEnabled = true;

            buttonPlay.IsEnabled             = false;
            buttonStop.IsEnabled             = false;
            buttonPause.IsEnabled            = false;
            comboBoxPlayMode.IsEnabled       = true;

            buttonNext.IsEnabled             = false;
            buttonPrev.IsEnabled             = false;
            groupBoxWasapiOperationMode.IsEnabled = true;
            groupBoxWasapiDataFeedMode.IsEnabled = true;
            groupBoxWasapiOutputLatency.IsEnabled = true;

            buttonClearPlayList.IsEnabled    = false;
            buttonDelistSelected.IsEnabled = false;

            buttonInspectDevice.IsEnabled = 0 < listBoxDevices.Items.Count;

            buttonSettings.IsEnabled = true;
            menuItemToolSettings.IsEnabled = true;

            labelLoadingPlaylist.Visibility = System.Windows.Visibility.Collapsed;
        }

        // 再生リストあり。再生していない状態。
        private void UpdateUIToEditableState() {
            cmenuPlayListClear.IsEnabled = true;
            cmenuPlayListEditMode.IsEnabled = true;
            menuItemFileNew.IsEnabled = true;
            menuItemFileOpen.IsEnabled = true;
            menuItemFileSaveAs.IsEnabled = true;
            menuItemFileSaveCueAs.IsEnabled = true;
            menuItemPlayListClear.IsEnabled = true;
            menuItemPlayListItemEditMode.IsEnabled = true;

            if (0 == listBoxDevices.Items.Count) {
                // 再生デバイスが全く存在しない時
                buttonPlay.IsEnabled = false;
            } else {
                buttonPlay.IsEnabled = true;
            }

            buttonStop.IsEnabled = false;
            buttonPause.IsEnabled = false;
            comboBoxPlayMode.IsEnabled = true;

            buttonNext.IsEnabled = true;
            buttonPrev.IsEnabled = true;
            groupBoxWasapiOperationMode.IsEnabled = true;
            groupBoxWasapiDataFeedMode.IsEnabled = true;
            groupBoxWasapiOutputLatency.IsEnabled = true;

            buttonClearPlayList.IsEnabled = true;
            buttonDelistSelected.IsEnabled = (dataGridPlayList.SelectedIndex >= 0);
            buttonInspectDevice.IsEnabled = 0 < listBoxDevices.Items.Count;

            buttonSettings.IsEnabled = true;
            menuItemToolSettings.IsEnabled = true;

            labelLoadingPlaylist.Visibility = System.Windows.Visibility.Collapsed;
        }

        // 再生リストあり。再生開始処理中。
        private void UpdateUIToNonEditableState() {
            cmenuPlayListClear.IsEnabled = false;
            cmenuPlayListEditMode.IsEnabled = false;
            menuItemFileNew.IsEnabled = false;
            menuItemFileOpen.IsEnabled = false;
            menuItemFileSaveAs.IsEnabled = false;
            menuItemFileSaveCueAs.IsEnabled = false;
            menuItemPlayListClear.IsEnabled = false;
            menuItemPlayListItemEditMode.IsEnabled = false;
            buttonPlay.IsEnabled = false;
            buttonStop.IsEnabled = false;
            buttonPause.IsEnabled = false;
            comboBoxPlayMode.IsEnabled = false;

            buttonNext.IsEnabled = false;
            buttonPrev.IsEnabled = false;
            groupBoxWasapiOperationMode.IsEnabled = false;
            groupBoxWasapiDataFeedMode.IsEnabled = false;
            groupBoxWasapiOutputLatency.IsEnabled = false;

            buttonClearPlayList.IsEnabled = false;
            buttonDelistSelected.IsEnabled = false;
            buttonInspectDevice.IsEnabled = false;

            buttonSettings.IsEnabled = false;
            menuItemToolSettings.IsEnabled = false;

            labelLoadingPlaylist.Visibility = System.Windows.Visibility.Collapsed;
        }

        // 再生中。
        private void UpdateUIToPlayingState() {
            cmenuPlayListClear.IsEnabled = false;
            cmenuPlayListEditMode.IsEnabled = false;
            menuItemFileNew.IsEnabled = false;
            menuItemFileOpen.IsEnabled = false;
            menuItemFileSaveAs.IsEnabled = false;
            menuItemFileSaveCueAs.IsEnabled = false;
            menuItemPlayListClear.IsEnabled = false;
            menuItemPlayListItemEditMode.IsEnabled = false;
            buttonPlay.IsEnabled = false;
            buttonStop.IsEnabled = true;
            buttonPause.IsEnabled = true;
            buttonPause.Content = Properties.Resources.MainButtonPause;
            comboBoxPlayMode.IsEnabled = false;

            buttonNext.IsEnabled = true;
            buttonPrev.IsEnabled = true;
            groupBoxWasapiOperationMode.IsEnabled = false;
            groupBoxWasapiDataFeedMode.IsEnabled = false;
            groupBoxWasapiOutputLatency.IsEnabled = false;

            buttonClearPlayList.IsEnabled = false;
            buttonDelistSelected.IsEnabled = false;
            buttonInspectDevice.IsEnabled = false;

            buttonSettings.IsEnabled = false;
            menuItemToolSettings.IsEnabled = false;

            labelLoadingPlaylist.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void UpdateUIStatus() {
            dataGridPlayList.UpdateLayout();
            UpdateCoverart();

            if (m_preference.RefrainRedraw) {
                // 再描画抑制モード
                slider1.IsEnabled = false;
                labelPlayingTime.Content = PLAYING_TIME_UNKNOWN;
            } else {
                slider1.IsEnabled = true;
                labelPlayingTime.Content = PLAYING_TIME_ALLZERO;
            }

            switch (m_state) {
            case State.再生リストなし:
                UpdateUIToInitialState();
                statusBarText.Content = Properties.Resources.MainStatusPleaseCreatePlaylist;
                break;
            case State.再生リスト読み込み中:
                UpdateUIToInitialState();
                statusBarText.Content = Properties.Resources.MainStatusReadingPlaylist;
                dataGridPlayList.IsEnabled = false;
                if (0 == dataGridPlayList.Items.Count) {
                    labelLoadingPlaylist.Visibility = System.Windows.Visibility.Visible;
                }
                break;
            case State.再生リストあり:
                UpdateUIToEditableState();
                if (0 < dataGridPlayList.Items.Count &&
                        dataGridPlayList.SelectedIndex < 0) {
                    // プレイリストに項目があり、選択されている曲が存在しない時、最初の曲を選択状態にする
                    dataGridPlayList.SelectedIndex = 0;
                }
                statusBarText.Content = Properties.Resources.MainStatusPressPlayButton;
                break;
            case State.デバイスSetup完了:
                // 一覧のクリアーとデバイスの選択、再生リストの作成関連を押せなくする。
                UpdateUIToNonEditableState();
                statusBarText.Content = Properties.Resources.MainStatusReadingFiles;
                break;
            case State.ファイル読み込み完了:
                UpdateUIToNonEditableState();
                statusBarText.Content = Properties.Resources.MainStatusReadCompleted;

                progressBar1.Visibility = System.Windows.Visibility.Collapsed;
                slider1.Value = 0;
                labelPlayingTime.Content = PLAYING_TIME_UNKNOWN;
                break;
            case State.再生中: {
                    UpdateUIToPlayingState();

                    var stat = wasapi.GetSessionStatus();
                    if (WasapiCS.StreamType.DoP == stat.StreamType) {
                        statusBarText.Content = string.Format(CultureInfo.InvariantCulture, "{0} WASAPI{1} {2}kHz {3} {4}ch DoP DSD {5:F1}MHz.",
                                Properties.Resources.MainStatusPlaying,
                                radioButtonShared.IsChecked == true ? Properties.Resources.Shared : Properties.Resources.Exclusive,
                                stat.DeviceSampleRate * 0.001,
                                SampleFormatTypeToStr(stat.DeviceSampleFormat),
                                stat.DeviceNumChannels, stat.DeviceSampleRate * 0.000016);
                    } else {
                        statusBarText.Content = string.Format(CultureInfo.InvariantCulture, "{0} WASAPI{1} {2}kHz {3} {4}ch PCM.",
                                Properties.Resources.MainStatusPlaying,
                                radioButtonShared.IsChecked == true ? Properties.Resources.Shared : Properties.Resources.Exclusive,
                                stat.DeviceSampleRate * 0.001,
                                SampleFormatTypeToStr(stat.DeviceSampleFormat),
                                stat.DeviceNumChannels);
                    }

                    progressBar1.Visibility = System.Windows.Visibility.Collapsed;
                }
                break;
            case State.再生一時停止中:
                UpdateUIToPlayingState();
                buttonPause.Content = Properties.Resources.MainButtonResume;
                buttonNext.IsEnabled             = true;
                buttonPrev.IsEnabled             = true;
                statusBarText.Content = Properties.Resources.MainStatusPaused;

                progressBar1.Visibility = System.Windows.Visibility.Collapsed;
                break;
            case State.再生停止開始:
                UpdateUIToNonEditableState();
                statusBarText.Content = Properties.Resources.MainStatusStopping;
                break;
            case State.再生グループ読み込み中:
                UpdateUIToNonEditableState();
                statusBarText.Content = Properties.Resources.MainStatusChangingPlayGroup;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            // 再生リストモード更新
            if (dataGridPlayList.IsReadOnly) {
                // 再生モード
                menuItemPlayListItemEditMode.IsChecked = false;
                cmenuPlayListEditMode.IsChecked = false;
            } else {
                // 編集モード
                menuItemPlayListItemEditMode.IsChecked = true;
                cmenuPlayListEditMode.IsChecked = true;
            }

            if (m_preference.AlternatingRowBackground) {
                dataGridPlayList.AlternatingRowBackground
                        = new SolidColorBrush(Util.ColorFromArgb(m_preference.AlternatingRowBackgroundArgb));
            } else {
                dataGridPlayList.AlternatingRowBackground = null;
            }
        }

        /// <summary>
        /// デバイス一覧を取得し、デバイス一覧リストを更新する。
        /// 同一デバイスのデバイス番号がずれるので注意。
        /// </summary>
        private void UpdateDeviceList() {
            int hr;

            int selectedIndex = -1;

            listBoxDevices.Items.Clear();

            hr = wasapi.EnumerateDevices(WasapiCS.DeviceType.Play);
            AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.DoDeviceEnumeration(Play) {0:X8}{1}", hr, Environment.NewLine));

            int nDevices = wasapi.GetDeviceCount();
            for (int i = 0; i < nDevices; ++i) {
                var attr = wasapi.GetDeviceAttributes(i);
                listBoxDevices.Items.Add(new DeviceAttributes(i, attr.Name, attr.DeviceIdString));

                if (0 < m_preference.PreferredDeviceName.Length
                        && 0 == string.CompareOrdinal(m_preference.PreferredDeviceName, attr.Name)) {
                    // PreferredDeviceIdStringは3.0.60で追加されたので、存在しないことがある
                    // 存在するときだけチェックする
                    if (0 < m_preference.PreferredDeviceIdString.Length
                            && 0 != string.CompareOrdinal(m_preference.PreferredDeviceIdString, attr.DeviceIdString)) {
                        continue;
                    }

                    // お気に入りデバイスを選択状態にする。
                    selectedIndex = i;
                }
            }

            if (0 < nDevices) {
                if (0 <= selectedIndex && selectedIndex < listBoxDevices.Items.Count) {
                    listBoxDevices.SelectedIndex = selectedIndex;
                } else {
                    listBoxDevices.SelectedIndex = 0;
                }

                buttonInspectDevice.IsEnabled = true;
            }

            if (0 < m_playListItems.Count) {
                ChangeState(State.再生リストあり);
            } else {
                ChangeState(State.再生リストなし);
            }

            UpdateUIStatus();
        }

        /// <summary>
        /// 再生中の場合は、停止を開始する。
        /// (ブロックしないのでこの関数から抜けたときに停止完了していないことがある)
        /// 
        /// 再生中でない場合は、再生停止後イベントtaskAfterStopをここで実行する。
        /// 再生中の場合は、停止完了後にtaskAfterStopを実行する。
        /// </summary>
        /// <param name="taskAfterStop"></param>
        void StopAsync(Task taskAfterStop, bool stopGently) {
            m_task = taskAfterStop;

            if (m_playWorker.IsBusy) {
                m_bStopGently = stopGently;
                m_playWorker.CancelAsync();
                // 再生停止したらPlayRunWorkerCompletedでイベントを開始する。
            } else {
                // 再生停止後イベントをここで、いますぐ開始。
                PerformPlayCompletedTask();
            }
        }

        void StopBlocking()
        {
            StopAsync(new Task(TaskType.None), false);
            m_readFileWorker.CancelAsync();

            // バックグラウンドスレッドにjoinして、完全に止まるまで待ち合わせする。
            while (m_playWorker.IsBusy) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));
                System.Threading.Thread.Sleep(100);
            }

            while (m_readFileWorker.IsBusy) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));
                System.Threading.Thread.Sleep(100);
            }
        }

        /// <summary>
        /// デバイス選択を解除する。再生停止中に呼ぶ。
        /// </summary>
        private void DeviceDeselect() {
            System.Diagnostics.Debug.Assert(!m_playWorker.IsBusy);

            m_useDevice = null;
            UnsetupDevice();

            m_loadedGroupId = -1;
            m_loadingGroupId = -1;

            if (0 < m_playListItems.Count) {
                ChangeState(State.再生リストあり);
            } else {
                ChangeState(State.再生リストなし);
            }
            UpdateUIStatus();
        }

        private void Term() {
            DeleteKeyListener();

            if (wasapi != null) {
                // バックグラウンドスレッドにjoinして、完全に止まるまで待ち合わせするブロッキング版のStopを呼ぶ。
                // そうしないと、バックグラウンドスレッドによって使用中のオブジェクトが
                // この後のUnsetupの呼出によって開放されてしまい問題が起きる。
                StopBlocking();
                UnsetupDevice();
                wasapi.Term();
                wasapi = null;

                // ウィンドウの位置とサイズを保存
                m_preference.SetMainWindowLeftTopWidthHeight(Left, Top, Width, Height);

                // 再生リピート設定を保存
                SetPreferencePlaymodeFromComboBox();

                // 設定画面の表示状態を保存
                m_preference.SettingsIsExpanded = expanderSettings.IsExpanded;

                // 再生リストの列の並び順を覚える
                SavePlaylistColumnOrderToPreference();

                // 最後に再生していた曲の番号
                m_preference.LastPlayItemIndex = dataGridPlayList.SelectedIndex;

                // 設定ファイルを書き出す。
                PreferenceStore.Save(m_preference);

                PreferenceAudioFilterStore.Save(mPreferenceAudioFilterList);

                // 再生リストをIsolatedStorageに保存。
                SavePpwPlaylist(string.Empty);
            }
        }

        private void Exit() {
            Term();
            // Application.Current.Shutdown();
            Close();
        }

        /// <summary>
        /// wasapi.Unsetupを行う。
        /// 既にUnsetup状態の場合は、空振りする。
        /// </summary>
        private void UnsetupDevice() {
            if (!m_deviceSetupParams.IsSetuped()) {
                return;
            }

            wasapi.Unsetup();
            AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.Unsetup(){0}", Environment.NewLine));
            m_deviceSetupParams.Unsetuped();
        }

        private static int PcmChannelsToSetupChannels(int numChannels) {
            // モノラル1chのPCMデータはMonoToStereo()によってステレオ2chに変換してから再生する。
            switch (numChannels) {
            case 1:
                return 2;
            default:
                return numChannels;
            }
        }

        /// <summary>
        /// デバイスSetupを行う。
        /// すでに同一フォーマットのSetupがなされている場合は空振りする。
        /// </summary>
        /// <param name="loadGroupId">再生するグループ番号。この番号のWAVファイルのフォーマットでSetupする。</param>
        /// <returns>false: デバイスSetup失敗。よく起こる。</returns>
        private bool SetupDevice(int loadGroupId) {
            int useDeviceId = listBoxDevices.SelectedIndex;

            int latencyMillisec = 0;
            if (!Int32.TryParse(textBoxLatency.Text, NumberStyles.Number,
                    CultureInfo.CurrentCulture, out latencyMillisec) || latencyMillisec <= 0) {
                latencyMillisec = Preference.DefaultLatencyMilliseconds;
                textBoxLatency.Text = string.Format(CultureInfo.InvariantCulture, "{0}", latencyMillisec);
            }
            m_preference.LatencyMillisec = latencyMillisec;

            int startWavDataId = m_pcmDataListForPlay.GetFirstPcmDataIdOnGroup(loadGroupId);
            System.Diagnostics.Debug.Assert(0 <= startWavDataId);

            var startPcmData = m_pcmDataListForPlay.FindById(startWavDataId);

            // 1つのフォーマットに対して複数(candidateNum個)のSetup()設定選択肢がありうる。

            int candidateNum = SampleFormatInfo.GetSetupSampleFormatCandidateNum(
                    m_preference.WasapiSharedOrExclusive,
                    m_preference.BitsPerSampleFixType,
                    startPcmData.ValidBitsPerSample,
                    startPcmData.SampleValueRepresentationType);
            for (int i = 0; i < candidateNum; ++i) {
                SampleFormatInfo sf = SampleFormatInfo.CreateSetupSampleFormat(
                        m_preference.WasapiSharedOrExclusive,
                        m_preference.BitsPerSampleFixType,
                        startPcmData.BitsPerSample,
                        startPcmData.ValidBitsPerSample,
                        startPcmData.SampleValueRepresentationType,
                        i);

                if (m_deviceSetupParams.Is(
                        startPcmData.SampleRate,
                        sf.GetSampleFormatType(),
                        PcmChannelsToSetupChannels(startPcmData.NumChannels),
                        latencyMillisec,
                        m_preference.ZeroFlushMillisec,
                        m_preference.WasapiDataFeedMode,
                        m_preference.WasapiSharedOrExclusive,
                        m_preference.RenderThreadTaskType,
                        m_preference.ResamplerConversionQuality,
                        startPcmData.SampleDataType == PcmData.DataType.DoP ? WasapiCS.StreamType.DoP : WasapiCS.StreamType.PCM)) {
                    // すでにこのフォーマットでSetup完了している。
                    return true;
                }
            }

            for (int i = 0; i < candidateNum; ++i) {
                SampleFormatInfo sf = SampleFormatInfo.CreateSetupSampleFormat(
                        m_preference.WasapiSharedOrExclusive,
                        m_preference.BitsPerSampleFixType,
                        startPcmData.BitsPerSample,
                        startPcmData.ValidBitsPerSample,
                        startPcmData.SampleValueRepresentationType, i);

                m_deviceSetupParams.Set(
                        startPcmData.SampleRate,
                        sf.GetSampleFormatType(),
                        PcmChannelsToSetupChannels(startPcmData.NumChannels),
                        latencyMillisec,
                        m_preference.ZeroFlushMillisec,
                        m_preference.WasapiDataFeedMode,
                        m_preference.WasapiSharedOrExclusive,
                        m_preference.RenderThreadTaskType,
                        m_preference.ResamplerConversionQuality,
                        startPcmData.SampleDataType == PcmData.DataType.DoP ? WasapiCS.StreamType.DoP : WasapiCS.StreamType.PCM);

                int hr = wasapi.Setup(
                        useDeviceId, WasapiCS.DeviceType.Play,
                        m_deviceSetupParams.StreamType, m_deviceSetupParams.SampleRate, m_deviceSetupParams.SampleFormat,
                        m_deviceSetupParams.NumChannels, GetMMCSSCallType(), PreferenceSchedulerTaskTypeToWasapiCSSchedulerTaskType(m_deviceSetupParams.ThreadTaskType),
                        PreferenceShareModeToWasapiCSShareMode(m_deviceSetupParams.SharedOrExclusive), PreferenceDataFeedModeToWasapiCS(m_deviceSetupParams.DataFeedMode),
                        m_deviceSetupParams.LatencyMillisec, m_deviceSetupParams.ZeroFlushMillisec, m_preference.TimePeriodHundredNanosec);
                AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.Setup({0} {1}kHz {2} {3}ch {4} {5} {6} latency={7}ms zeroFlush={8}ms timePeriod={9}ms) {10:X8}{11}",
                        m_deviceSetupParams.StreamType, m_deviceSetupParams.SampleRate * 0.001, m_deviceSetupParams.SampleFormat,
                        m_deviceSetupParams.NumChannels, m_deviceSetupParams.ThreadTaskType, m_deviceSetupParams.SharedOrExclusive, m_deviceSetupParams.DataFeedMode,
                        m_deviceSetupParams.LatencyMillisec, m_deviceSetupParams.ZeroFlushMillisec, m_preference.TimePeriodHundredNanosec * 0.0001, hr, Environment.NewLine));
                if (0 <= hr) {
                    // 成功
                    break;
                }

                // 失敗
                UnsetupDevice();
                if (i == (candidateNum - 1)) {
                    string s = string.Format(CultureInfo.InvariantCulture, "{0}: wasapi.Setup({1} {2}kHz {3} {4}ch {5} {6}ms {7} {8}) {9} {10:X8}{12}{12}{11}",
                            Properties.Resources.Error,
                            m_deviceSetupParams.StreamType,
                            startPcmData.SampleRate * 0.001,
                            sf.GetSampleFormatType(),
                            PcmChannelsToSetupChannels(startPcmData.NumChannels),
                            Properties.Resources.Latency,
                            latencyMillisec,
                            DfmToStr(m_preference.WasapiDataFeedMode),
                            ShareModeToStr(m_preference.WasapiSharedOrExclusive),
                            Properties.Resources.Failed,
                            hr,
                            Properties.Resources.SetupFailAdvice,
                            Environment.NewLine);
                    MessageBox.Show(s);
                    return false;
                }
            }

            {
                var stat = wasapi.GetSessionStatus();
                AddLogText(string.Format(CultureInfo.InvariantCulture, "Endpoint buffer size = {0} frames.{1}",
                        stat.EndpointBufferFrameNum, Environment.NewLine));

                var attr = wasapi.GetDeviceAttributes(useDeviceId);
            }

            ChangeState(State.デバイスSetup完了);
            UpdateUIStatus();
            return true;
        }

        enum PlayListClearMode {
            // プレイリストをクリアーし、UI状態も更新する。(通常はこちらを使用。)
            ClearWithUpdateUI,

            // ワーカースレッドから呼ぶためUIを操作しない。UIは内部状態とは矛盾した状態になるため
            // この後UIスレッドであらためてClearPlayList(ClearWithUpdateUI)する必要あり。
            ClearWithoutUpdateUI,
        }

        private StringBuilder m_loadErrorMessages;

        private void LoadErrorMessageAdd(string s) {
            s = "*" + s.TrimEnd('\r', '\n') + ". ";
            m_loadErrorMessages.Append(s);
        }

        private WasapiCS.MMCSSCallType GetMMCSSCallType() {
            if (!m_preference.DwmEnableMmcssCall) {
                return WasapiCS.MMCSSCallType.DoNotCall;
            }
            return m_preference.DwmEnableMmcss ? WasapiCS.MMCSSCallType.Enable : WasapiCS.MMCSSCallType.Disable;
        }

        private void ClearPlayList(PlayListClearMode mode) {
            m_pcmDataListForDisp.Clear();
            m_pcmDataListForPlay.Clear();
            m_playListItems.Clear();
            PlayListItemInfo.SetNextRowId(1);

            wasapi.ClearPlayList();

            m_groupIdNextAdd = 0;
            m_loadedGroupId  = -1;
            m_loadingGroupId = -1;

            GC.Collect();

            ChangeState(State.再生リストなし);

            if (mode == PlayListClearMode.ClearWithUpdateUI) {
                //m_playListView.RefreshCollection();

                progressBar1.Value = 0;
                UpdateUIStatus();

                // 再生リスト列幅を初期値にリセットする。
                {
                    int i=0;
                    foreach (var item in dataGridPlayList.Columns) {
                        item.Width = DataGridLength.SizeToCells;
                        item.Width = m_playlistColumnDefaults[i].Width;
                        ++i;
                    }
                }
            }
        }

        int ReadFileHeader(string path, PcmHeaderReader.ReadHeaderMode mode, PlaylistTrackInfo plti) {
            PcmHeaderReader phr = new PcmHeaderReader(Encoding.GetEncoding(m_preference.CueEncodingCodePage),
                    m_preference.SortDropFolder, (pcmData, readSeparatorAfter, readFromPpwPlaylist) => {
                        // PcmDataのヘッダが読み込まれた時。再生リストに追加する。

                        if (0 < m_pcmDataListForDisp.Count()
                            && !m_pcmDataListForDisp.Last().IsSameFormat(pcmData)) {
                            // 1個前のファイルとデータフォーマットが異なる。
                            // Setupのやり直しになるのでファイルグループ番号を変える。
                            ++m_groupIdNextAdd;
                        }

                        pcmData.Id = m_pcmDataListForDisp.Count();
                        pcmData.Ordinal = pcmData.Id;
                        pcmData.GroupId = m_groupIdNextAdd;

                        if (m_preference.BatchReadEndpointToEveryTrack) {
                            // 各々のトラックを個別読込する設定。
                            readSeparatorAfter = true;
                        }
                        if (plti != null) {
                            if ((plti.indexId == 0 && m_preference.ReplaceGapWithKokomade) || plti.readSeparatorAfter) {
                                // プレイリストのINDEX 00 == gap しかも gapのかわりに[ここまで読みこみ]を追加する の場合
                                readSeparatorAfter = true;
                            }
                        }

                        if (!readFromPpwPlaylist) {
                            if (pcmData.CueSheetIndex == 0 && m_preference.ReplaceGapWithKokomade) {
                                // PPWプレイリストからの読み出しではない場合で
                                // INDEX 00 == gap しかも gapのかわりに[ここまで読みこみ]を追加する の場合
                                readSeparatorAfter = true;
                            }
                        }

                        var pli = new PlayListItemInfo(pcmData);

                        if (readSeparatorAfter) {
                            pli.ReadSeparaterAfter = true;
                            ++m_groupIdNextAdd;
                        }

                        m_pcmDataListForDisp.Add(pcmData);
                        m_playListItems.Add(pli);

                        pli.PropertyChanged += new PropertyChangedEventHandler(PlayListItemInfoPropertyChanged);
                    });
            return phr.ReadFileHeader(path, mode, plti);
        }

        //////////////////////////////////////////////////////////////////////////

        private void MainWindowDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void MainWindowDragDrop(object sender, DragEventArgs e)
        {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder(Properties.Resources.DroppedDataIsNotFile);

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }

            if (State.デバイスSetup完了 <= m_state) {
                // 追加不可。
                MessageBox.Show(Properties.Resources.CannotAddFile);
                return;
            }

            // エラーメッセージを貯めて出す。作りがいまいちだが。
            m_loadErrorMessages = new StringBuilder();

            if (m_preference.SortDroppedFiles) {
                paths = (from s in paths orderby s select s).ToArray();
            }

            for (int i = 0; i < paths.Length; ++i) {
                ReadFileHeader(paths[i], PcmHeaderReader.ReadHeaderMode.ReadAll, null);
            }

            if (0 < m_loadErrorMessages.Length) {
                MessageBox.Show(m_loadErrorMessages.ToString(), Properties.Resources.ReadFailedFiles, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            m_loadErrorMessages = null;

            if (0 < m_playListItems.Count) {
                ChangeState(State.再生リストあり);
            }
            UpdateUIStatus();
        }

        private void MenuItemFileSaveCueAs_Click(object sender, RoutedEventArgs e) {
            if (m_pcmDataListForDisp.Count() == 0) {
                MessageBox.Show(Properties.Resources.NothingToStore);
                return;
            }

            System.Diagnostics.Debug.Assert(0 < m_pcmDataListForDisp.Count());
            var pcmData0 = m_pcmDataListForDisp.At(0);

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(pcmData0.FullPath);
            dlg.Filter = Properties.Resources.FilterCueFiles;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            var csw = new CueSheetWriter();

            csw.SetAlbumTitle(m_playListItems[0].AlbumTitle);
            csw.SetAlbumPerformer(m_playListItems[0].PcmData().ArtistName);

            int i = 0;
            foreach (var pli in m_playListItems) {
                var pcmData = m_pcmDataListForDisp.At(i);

                CueSheetTrackInfo cst = new CueSheetTrackInfo();
                cst.title = pli.Title;
                cst.albumTitle = pli.AlbumTitle;
                cst.indexId = pcmData.CueSheetIndex;
                cst.performer = pli.ArtistName;
                cst.readSeparatorAfter = pli.ReadSeparaterAfter;
                cst.startTick = pcmData.StartTick;
                cst.endTick = pcmData.EndTick;
                cst.path = pcmData.FullPath;
                csw.AddTrackInfo(cst);
                ++i;
            }

            result = false;
            try {
                result = csw.WriteToFile(dlg.FileName);
            } catch (IOException ex) {
                Console.WriteLine("E: MenuItemFileSaveCueAs_Click {0}", ex);
            } catch (ArgumentException ex) {
                Console.WriteLine("E: MenuItemFileSaveCueAs_Click {0}", ex);
            } catch (UnauthorizedAccessException ex) {
                Console.WriteLine("E: MenuItemFileSaveCueAs_Click {0}", ex);
            }

            if (result != true) {
                MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", Properties.Resources.SaveFileFailed, dlg.FileName));
            }
        }

        private void MenuItemFileSaveAs_Click(object sender, RoutedEventArgs e) {
            if (m_pcmDataListForDisp.Count() == 0) {
                MessageBox.Show(Properties.Resources.NothingToStore);
                return;
            }

            System.Diagnostics.Debug.Assert(0 < m_pcmDataListForDisp.Count());
            var pcmData0 = m_pcmDataListForDisp.At(0);

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(pcmData0.FullPath);
            dlg.Filter = Properties.Resources.FilterPpwplFiles;

            var result = dlg.ShowDialog();
            if (result == true) {
                if (!SavePpwPlaylist(dlg.FileName)) {
                    MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", Properties.Resources.SaveFileFailed, dlg.FileName));
                }
            }
        }
        
        private void MenuItemFileNew_Click(object sender, RoutedEventArgs e) {
            ClearPlayList(PlayListClearMode.ClearWithUpdateUI);
        }

        private void MenuItemFileOpen_Click(object sender, RoutedEventArgs e)
        {
            if (State.デバイスSetup完了 <= m_state) {
                // 追加不可。
                MessageBox.Show(Properties.Resources.CannotAddFile);
                return;
            }

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterSupportedFiles;
            dlg.Multiselect = true;

            if (0 <= m_preference.OpenFileDialogFilterIndex) {
                dlg.FilterIndex = m_preference.OpenFileDialogFilterIndex;
            }

            var result = dlg.ShowDialog();
            if (result == true) {
                // エラーメッセージを貯めて出す。
                m_loadErrorMessages = new StringBuilder();

                for (int i = 0; i < dlg.FileNames.Length; ++i) {
                    ReadFileHeader(dlg.FileNames[i], PcmHeaderReader.ReadHeaderMode.ReadAll, null);
                }

                if (0 < m_loadErrorMessages.Length) {
                    MessageBox.Show(m_loadErrorMessages.ToString(),
                            Properties.Resources.ReadFailedFiles,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }

                m_loadErrorMessages = null;

                if (0 < m_playListItems.Count) {
                    ChangeState(State.再生リストあり);
                }
                UpdateUIStatus();

                // 最後に選択されていたフィルターをデフォルトとする
                m_preference.OpenFileDialogFilterIndex = dlg.FilterIndex;
            }

        }

        private void MenuItemHelpAbout_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(
                string.Format(CultureInfo.InvariantCulture, "PlayPcmWin {0} {1}{3}{3}{2}",
                        Properties.Resources.Version, AssemblyVersion, Properties.Resources.LicenseText, Environment.NewLine));
        }

        private void MenuItemHelpWeb_Click(object sender, RoutedEventArgs e) {
            try {
                System.Diagnostics.Process.Start("http://code.google.com/p/bitspersampleconv2/wiki/PlayPcmWin");
            } catch (System.ComponentModel.Win32Exception) {
            }
        }

        private static string DfmToStr(WasapiDataFeedModeType dfm) {
            switch (dfm) {
            case WasapiDataFeedModeType.EventDriven:
                return Properties.Resources.EventDriven;
            case WasapiDataFeedModeType.TimerDriven:
                return Properties.Resources.TimerDriven;
            default:
                System.Diagnostics.Debug.Assert(false);
                return "unknown";
            }
        }

        private static string ShareModeToStr(WasapiSharedOrExclusiveType t) {
            switch (t) {
            case WasapiSharedOrExclusiveType.Exclusive:
                return "WASAPI " + Properties.Resources.Exclusive;
            case WasapiSharedOrExclusiveType.Shared:
                return "WASAPI " + Properties.Resources.Shared;
            default:
                System.Diagnostics.Debug.Assert(false);
                return "unknown";
            }
        }

        abstract class ReadFileResult {
            public bool IsSucceeded { get; set; }
            public bool HasMessage { get; set; }
            public int PcmDataId { get; set; }
            public abstract string ToString(string fileName);
        }

        class ReadFileResultSuccess : ReadFileResult {
            public ReadFileResultSuccess(int pcmDataId) {
                PcmDataId = pcmDataId;
                IsSucceeded = true;
                HasMessage = false;
            }

            public override string ToString(string fileName) {
                return string.Empty;
            }
        };

        class ReadFileResultFailed : ReadFileResult {
            private string message;

            public ReadFileResultFailed(int pcmDataId, string message) {
                PcmDataId = pcmDataId;
                this.message = message;
                IsSucceeded = false;
                HasMessage = !String.IsNullOrEmpty(this.message);
            }

            public override string ToString(string fileName) {
                return message;
            }
        };

        class ReadFileResultClipped : ReadFileResult {
            private long clippedCount;

            public ReadFileResultClipped(int pcmDataId, long clippedCount) {
                PcmDataId = pcmDataId;
                this.clippedCount = clippedCount;
                IsSucceeded = false;
                HasMessage = true;
            }

            public override string ToString(string fileName) {
                return string.Format(CultureInfo.InvariantCulture, Properties.Resources.ClippedSampleDetected,
                        fileName, clippedCount);
            }
        };

        class ReadFileResultMD5Sum : ReadFileResult {
            private byte [] md5SumOfPcm;
            private byte [] md5SumInMetadata;

            public ReadFileResultMD5Sum(int pcmDataId, byte[] md5SumOfPcm, byte[] md5SumInMetadata) {
                PcmDataId = pcmDataId;
                this.md5SumOfPcm = md5SumOfPcm;
                this.md5SumInMetadata = md5SumInMetadata;
                if (null == md5SumInMetadata) {
                    // MD5値がメタ情報から取得できなかったので照合は行わずに成功を戻す。
                    IsSucceeded = true;
                } else {
                    IsSucceeded = md5SumOfPcm.SequenceEqual(md5SumInMetadata);
                }
                HasMessage = true;
            }

            public override string ToString(string fileName) {
                if (null == md5SumInMetadata) {
                    return string.Format(CultureInfo.InvariantCulture, Properties.Resources.MD5SumNotAvailable,
                        fileName, MD5SumToStr(md5SumOfPcm)) + Environment.NewLine;
                }

                if (IsSucceeded) {
                    return string.Format(CultureInfo.InvariantCulture, Properties.Resources.MD5SumValid, fileName) + Environment.NewLine;
                }

                return string.Format(CultureInfo.InvariantCulture, Properties.Resources.MD5SumMismatch,
                        fileName, MD5SumToStr(md5SumInMetadata), MD5SumToStr(md5SumOfPcm)) + Environment.NewLine;

            }

            private static string MD5SumToStr(byte[] a) {
                if (null == a) {
                    return "NA";
                }
                return string.Format(CultureInfo.InvariantCulture,
                        "{0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}{8:x2}{9:x2}{10:x2}{11:x2}{12:x2}{13:x2}{14:x2}{15:x2}",
                        a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7],
                        a[8], a[9], a[10], a[11], a[12], a[13], a[14], a[15]);
            }
        };

        class ReadFileRunWorkerCompletedArgs {
            public string message;
            public int hr;
            public List<ReadFileResult> individualResultList = new List<ReadFileResult>();

            public ReadFileRunWorkerCompletedArgs Update(string msg, int resultCode) {
                message = msg;
                hr = resultCode;
                return this;
            }
        }

        struct ReadProgressInfo {
            public int pcmDataId;
            public long startFrame;
            public long endFrame;
            public int trackCount;
            public int trackNum;

            public long readFrames;

            public long WantFramesTotal {
                get {
                    return endFrame - startFrame;
                }
            }

            public ReadProgressInfo(int pcmDataId, long startFrame, long endFrame, int trackCount, int trackNum) {
                this.pcmDataId  = pcmDataId;
                this.startFrame = startFrame;
                this.endFrame   = endFrame;
                this.trackCount = trackCount;
                this.trackNum   = trackNum;
                this.readFrames = 0;
            }

            public void FileReadStart(int pcmDataId, long startFrame, long endFrame) {
                this.pcmDataId  = pcmDataId;
                this.startFrame = startFrame;
                this.endFrame   = endFrame;
                this.readFrames = 0;
            }
        };

        /// <summary>
        /// ファイル読み出しの進捗状況
        /// </summary>
        ReadProgressInfo m_readProgressInfo;

        /// <summary>
        /// ビットフォーマット変換クラス。ノイズシェイピングのerror値を持っているので都度作らないようにする。
        /// </summary>
        private WasapiPcmUtil.PcmUtil mPcmUtil;

        /// <summary>
        ///  バックグラウンド読み込み。
        ///  m_readFileWorker.RunWorkerAsync(読み込むgroupId)で開始する。
        ///  完了するとReadFileRunWorkerCompletedが呼ばれる。
        /// </summary>
        private void ReadFileDoWork(object o, DoWorkEventArgs args) {
            var bw = o as BackgroundWorker;
            int readGroupId = (int)args.Argument;
            // Console.WriteLine("D: ReadFileSingleDoWork({0}) started", readGroupId);

            PcmReader.CalcMD5SumIfAvailable = m_preference.VerifyFlacMD5Sum;

            ReadFileRunWorkerCompletedArgs r = new ReadFileRunWorkerCompletedArgs();
            try {
                r.hr = -1;
                r.message = string.Empty;

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                m_readProgressInfo = new ReadProgressInfo(
                        0, 0, 0, 0, m_pcmDataListForPlay.CountPcmDataOnPlayGroup(readGroupId));

                wasapi.ClearPlayList();

                mPcmUtil = new PcmUtil(m_pcmDataListForPlay.At(0).NumChannels);

                wasapi.AddPlayPcmDataStart();
                for (int i = 0; i < m_pcmDataListForPlay.Count(); ++i) {
                    PcmDataLib.PcmData pd = m_pcmDataListForPlay.At(i);
                    if (pd.GroupId != readGroupId) {
                        continue;
                    }

                    // どーなのよ、という感じがするが。
                    // 効果絶大である。
                    GC.Collect();

                    WasapiPcmUtil.PcmFormatConverter.ClearClippedCounter();

                    long startFrame = (long)(pd.StartTick) * pd.SampleRate / 75;
                    long endFrame   = (long)(pd.EndTick) * pd.SampleRate / 75;

                    bool rv = ReadOnePcmFile(bw, pd, startFrame, endFrame, ref r);
                    if (bw.CancellationPending) {
                        r.hr = -1;
                        r.message = string.Empty;
                        args.Result = r;
                        args.Cancel = true;
                        return;
                    }

                    {
                        long clippedCount = WasapiPcmUtil.PcmFormatConverter.ReadClippedCounter();
                        if (0 < clippedCount) {
                            r.individualResultList.Add(new ReadFileResultClipped(pd.Id, clippedCount));
                        }
                    }

                    if (!rv) {
                        args.Result = r;
                        return;
                    }

                    ++m_readProgressInfo.trackCount;
                }

                // ダメ押し。
                GC.Collect();

                if (m_preference.WasapiSharedOrExclusive == WasapiSharedOrExclusiveType.Shared) {
                    m_readFileWorker.ReportProgress(90, string.Format(CultureInfo.InvariantCulture, "Resampling...{0}", Environment.NewLine));
                }
                r.hr = wasapi.ResampleIfNeeded(m_deviceSetupParams.ResamplerConversionQuality);
                if (r.hr < 0) {
                    r.message = "Resample({0}) failed! " + string.Format(CultureInfo.InvariantCulture, "0x{1:X8}", m_deviceSetupParams.ResamplerConversionQuality, r.hr);
                    args.Result = r;
                    return;
                }

                if (m_preference.WasapiSharedOrExclusive == WasapiSharedOrExclusiveType.Shared
                        && m_preference.SootheLimiterApo) {
                    // Limiter APO対策の音量制限。
                    double maxAmplitude = wasapi.ScanPcmMaxAbsAmplitude();
                    if (SHARED_MAX_AMPLITUDE < maxAmplitude) {
                        m_readFileWorker.ReportProgress(95, string.Format(CultureInfo.InvariantCulture, "Scaling amplitude by {0:0.000}dB ({1:0.000}x) to soothe Limiter APO...{2}",
                                20.0 * Math.Log10(SHARED_MAX_AMPLITUDE / maxAmplitude), SHARED_MAX_AMPLITUDE / maxAmplitude, Environment.NewLine));
                        wasapi.ScalePcmAmplitude(SHARED_MAX_AMPLITUDE / maxAmplitude);
                    }
                }

                wasapi.AddPlayPcmDataEnd();

                mPcmUtil = null;

                // 成功。
                sw.Stop();
                r.message = string.Format(CultureInfo.InvariantCulture, Properties.Resources.ReadPlayGroupNCompleted + Environment.NewLine, readGroupId, sw.ElapsedMilliseconds);
                r.hr = 0;
                args.Result = r;

                m_loadedGroupId = readGroupId;

                // Console.WriteLine("D: ReadFileSingleDoWork({0}) done", readGroupId);
            } catch (IOException ex) {
                args.Result = r.Update(ex.ToString(), -1);
            } catch (ArgumentException ex) {
                args.Result = r.Update(ex.ToString(), -1);
            } catch (UnauthorizedAccessException ex) {
                args.Result = r.Update(ex.ToString(), -1);
            } catch (NullReferenceException ex) {
                args.Result = r.Update(ex.ToString(), -1);
            }
        }

        private class ReadPcmTask : IDisposable {
            MainWindow mw;
            BackgroundWorker bw;
            PcmDataLib.PcmData pd;
            public long readStartFrame;
            public long readFrames;
            public long writeOffsFrame;
            public ManualResetEvent doneEvent;
            public bool result;
            public string message;

            protected virtual void Dispose(bool disposing) {
                if (disposing) {
                    doneEvent.Close();
                }
            }

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public ReadPcmTask(MainWindow mw, BackgroundWorker bw, PcmDataLib.PcmData pd, long readStartFrame, long readFrames, long writeOffsFrame) {
                this.mw = mw;
                this.bw = bw;

                // PcmDataのSampleArrayメンバを各スレッドが物置のように使うので実体をコピーする。
                this.pd = new PcmData();
                this.pd.CopyFrom(pd);

                this.readStartFrame = readStartFrame;
                this.readFrames     = readFrames;
                this.writeOffsFrame = writeOffsFrame;

                this.message = string.Empty;

                doneEvent = new ManualResetEvent(false);
                result = true;
            }

            public void ThreadPoolCallback(Object threadContext) {
                int threadIndex = (int)threadContext;
                var ri = mw.ReadOnePcmFileFragment(bw, pd, readStartFrame, readFrames, writeOffsFrame);
                if (ri.HasMessage) {
                    message += ri.ToString(pd.FileName);
                }
                if (!ri.IsSucceeded) {
                    result = false;
                    message = ri.ToString(pd.FileName);
                }

                doneEvent.Set();
            }

            /// <summary>
            /// このインスタンスの使用を終了する。再利用はできない。
            /// </summary>
            public void End() {
                mw = null;
                bw = null;
                pd = null;
                readStartFrame = 0;
                readFrames = 0;
                writeOffsFrame = 0;
                doneEvent = null;
                result = true;
            }
        };

        /// <summary>
        /// 分割読み込みのそれぞれのスレッドの読み込み開始位置と読み込みバイト数を計算する。
        /// </summary>
        private List<ReadPcmTask> SetupReadPcmTasks(BackgroundWorker bw, PcmDataLib.PcmData pd, long startFrame, long endFrame, int fragmentCount) {
            var result = new List<ReadPcmTask>();

            long readFrames = (endFrame - startFrame) / fragmentCount;
            // すくなくとも4Mフレームずつ読む。その結果fragmentCountよりも少ない場合がある。
            if (readFrames < 4 * 1024 * 1024) {
                readFrames = 4 * 1024 * 1024;
            }

            long readStartFrame = startFrame;
            long writeOffsFrame = 0;
            do {
                if (endFrame < readStartFrame + readFrames) {
                    readFrames = endFrame - readStartFrame;
                }
                var rri = new ReadPcmTask(this, bw, pd, readStartFrame, readFrames, writeOffsFrame);
                result.Add(rri);
                readStartFrame += readFrames;
                writeOffsFrame += readFrames;
            } while (readStartFrame < endFrame);
            return result;
        }

        private void ReadFileReportProgress(long readFrames, WasapiPcmUtil.PcmFormatConverter.BitsPerSampleConvArgs bpsConvArgs) {
            lock (m_readFileWorker) {
                m_readProgressInfo.readFrames += readFrames;
                var rpi = m_readProgressInfo;

                double loadCompletedPercent = 100.0;
                if (m_preference.WasapiSharedOrExclusive == WasapiSharedOrExclusiveType.Shared) {
                    loadCompletedPercent = 90.0;
                }

                double progressPercentage = loadCompletedPercent * (rpi.trackCount + (double)rpi.readFrames / rpi.WantFramesTotal) / rpi.trackNum;
                m_readFileWorker.ReportProgress((int)progressPercentage, string.Empty);
                if (bpsConvArgs != null && bpsConvArgs.noiseShapingOrDitherPerformed) {
                    m_readFileWorker.ReportProgress((int)progressPercentage, string.Format(CultureInfo.InvariantCulture,
                            "{0} {1}/{2} frames done{3}",
                            bpsConvArgs.noiseShaping, rpi.readFrames, rpi.WantFramesTotal, Environment.NewLine));
                }
            }
        }

        private bool ReadOnePcmFile(BackgroundWorker bw, PcmDataLib.PcmData pd, long startFrame, long endFrame, ref ReadFileRunWorkerCompletedArgs r) {
            {
                // endFrameの位置を確定する。
                // すると、rpi.ReadFramesも確定する。
                PcmReader pr = new PcmReader();
                
                int ercd = pr.StreamBegin(pd.FullPath, 0, 0, TYPICAL_READ_FRAMES);
                pr.StreamEnd();
                if (ercd < 0) {
                    r.hr = ercd;
                    r.message = string.Format(CultureInfo.InvariantCulture, "{0}! {1}{5}{2}{5}{3}: {4} (0x{4:X8})",
                            Properties.Resources.ReadError, pd.FullPath, FlacDecodeIF.ErrorCodeToStr(ercd), Properties.Resources.ErrorCode, ercd, Environment.NewLine);
                    Console.WriteLine("D: ReadFileSingleDoWork() !readSuccess");
                    return false;
                }
                if (endFrame < 0 || pr.NumFrames < endFrame) {
                    endFrame = pr.NumFrames;
                }
            }

            // endFrameが確定したので、総フレーム数をPcmDataにセット。
            long wantFramesTotal = endFrame - startFrame;
            pd.SetNumFrames(wantFramesTotal);
            m_readProgressInfo.FileReadStart(pd.Id, startFrame, endFrame);
            ReadFileReportProgress(0, null);

            {
                // このトラックのWasapi PCMデータ領域を確保する。
                long allocBytes = wantFramesTotal * m_deviceSetupParams.UseBytesPerFrame;
                if (!wasapi.AddPlayPcmDataAllocateMemory(pd.Id, allocBytes)) {
                    //ClearPlayList(PlayListClearMode.ClearWithoutUpdateUI); //< メモリを空ける：効果があるか怪しい
                    r.message = string.Format(CultureInfo.InvariantCulture, Properties.Resources.MemoryExhausted);
                    Console.WriteLine("D: ReadFileSingleDoWork() lowmemory");
                    return false;
                }
            }

            bool result = true;
            if (m_preference.ParallelRead && PcmReader.IsTheFormatCompressed(PcmReader.GuessFileFormatFromFilePath(pd.FullPath))
                    && ((m_preference.BpsConvNoiseShaping == NoiseShapingType.None) || !mPcmUtil.IsNoiseShapingOrDitherCapable(pd, m_deviceSetupParams.SampleFormat))) {
                // ファイルのstartFrameからendFrameまでを読みだす。(並列化)
                int fragmentCount = Environment.ProcessorCount;
                var rri = SetupReadPcmTasks(bw, pd, startFrame, endFrame, fragmentCount);
                var doneEventArray = new ManualResetEvent[rri.Count];
                for (int i=0; i < rri.Count; ++i) {
                    doneEventArray[i] = rri[i].doneEvent;
                }

                for (int i=0; i < rri.Count; ++i) {
                    ThreadPool.QueueUserWorkItem(rri[i].ThreadPoolCallback, i);
                }
                WaitHandle.WaitAll(doneEventArray);

                for (int i=0; i < rri.Count; ++i) {
                    if (!rri[i].result) {
                        r.message += rri[i].message + Environment.NewLine;
                        result = false;
                    }
                    rri[i].End();
                }
                rri.Clear();
                doneEventArray = null;
            } else {
                // ファイルのstartFrameからendFrameまでを読み出す。(1スレッド)
                var ri = ReadOnePcmFileFragment(bw, pd, startFrame, wantFramesTotal, 0);
                if (ri.HasMessage) {
                    r.individualResultList.Add(ri);
                }
                result = ri.IsSucceeded;
                if (!ri.IsSucceeded) {
                    r.message += ri.ToString(pd.FileName);
                }
            }

            return result;
        }

        private ReadFileResult ReadOnePcmFileFragment(BackgroundWorker bw, PcmDataLib.PcmData pd, long readStartFrame, long wantFramesTotal, long writeOffsFrame) {
            var lowMemoryFailed = new ReadFileResultFailed(pd.Id, "Low memory");
            ReadFileResult ri = new ReadFileResultSuccess(pd.Id);

            PcmReader pr = new PcmReader();
            int ercd = pr.StreamBegin(pd.FullPath, readStartFrame, wantFramesTotal, TYPICAL_READ_FRAMES);
            if (ercd < 0) {
                Console.WriteLine("D: ReadOnePcmFileFragment() StreamBegin failed");
                return new ReadFileResultFailed(pd.Id, FlacDecodeIF.ErrorCodeToStr(ercd));
            }

            long frameCount = 0;
            do {
                // 読み出したいフレーム数wantFrames。
                int wantFrames = TYPICAL_READ_FRAMES;
                if (wantFramesTotal < frameCount + wantFrames) {
                    wantFrames = (int)(wantFramesTotal - frameCount);
                }

                byte[] part = pr.StreamReadOne(wantFrames);
                if (null == part) {
                    pr.StreamEnd();
                    Console.WriteLine("D: ReadOnePcmFileFragment() lowmemory");
                    return lowMemoryFailed;
                }

                // 実際に読み出されたフレーム数readFrames。
                int readFrames = part.Length / (pd.BitsPerFrame / 8);

                pd.SetSampleArray(part);
                part = null;

                // 必要に応じてpartの量子化ビット数の変更処理を行い、pdAfterに新しく確保したPCMデータ配列をセット。

                var bpsConvArgs = new PcmFormatConverter.BitsPerSampleConvArgs(m_preference.BpsConvNoiseShaping);
                PcmData pdAfter = null;
                if (m_preference.WasapiSharedOrExclusive == WasapiSharedOrExclusiveType.Exclusive) {
                    pdAfter = mPcmUtil.BitsPerSampleConvAsNeeded(pd, m_deviceSetupParams.SampleFormat, bpsConvArgs);
                    pd.ForgetDataPart();
                } else {
                    pdAfter = pd;
                }

                if (pdAfter.GetSampleArray() == null ||
                        0 == pdAfter.GetSampleArray().Length) {
                    // サンプルが存在しないのでWasapiにAddしない。
                    break;
                }

                if (pdAfter.NumChannels == 1) {
                    // モノラル1ch→ステレオ2ch変換。
                    pdAfter = pdAfter.MonoToStereo();
                }

                long posBytes = (writeOffsFrame + frameCount) * pdAfter.BitsPerFrame / 8;

                bool result = false;
                lock (pd) {
                    result = wasapi.AddPlayPcmDataSetPcmFragment(pd.Id, posBytes, pdAfter.GetSampleArray());
                }
                System.Diagnostics.Debug.Assert(result);

                pdAfter.ForgetDataPart();

                // frameCountを進める
                frameCount += readFrames;

                ReadFileReportProgress(readFrames, bpsConvArgs);

                if (bw.CancellationPending) {
                    pr.StreamAbort();
                    return new ReadFileResultFailed(pd.Id, string.Empty);
                }
            } while (frameCount < wantFramesTotal);

            ercd = pr.StreamEnd();
            if (ercd < 0) {
                return new ReadFileResultFailed(pd.Id, string.Format(CultureInfo.InvariantCulture, "{0}: {1}", FlacDecodeIF.ErrorCodeToStr(ercd), pd.FullPath));
            }

            if (pr.MD5SumOfPcm != null) {
                ri = new ReadFileResultMD5Sum(pd.Id, pr.MD5SumOfPcm, pr.MD5SumInMetadata);
            }

            return ri;
        }

        private void ReadFileWorkerProgressChanged(object sender, ProgressChangedEventArgs e) {
            string s = e.UserState as string;
            if (s != null && 0 < s.Length) {
                AddLogText(s);
            }
            progressBar1.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// リピート設定。
        /// </summary>
        private void UpdatePlayRepeat() {
            bool repeat = false;
            // 1曲リピートか(全曲リピート再生で、GroupIdが0しかない場合)、WASAPI再生スレッドのリピート設定が可能。
            ComboBoxPlayModeType playMode = (ComboBoxPlayModeType)comboBoxPlayMode.SelectedIndex;
            if (playMode == ComboBoxPlayModeType.OneTrackRepeat
                    || (playMode == ComboBoxPlayModeType.AllTracksRepeat
                    && 0 == m_pcmDataListForPlay.CountPcmDataOnPlayGroup(1))) {
                repeat = true;
            }
            wasapi.SetPlayRepeat(repeat);
        }

        /// <summary>
        /// バックグラウンドファイル読み込みが完了した。
        /// </summary>
        private void ReadFileRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            if (args.Cancelled) {
                // キャンセル時は何もしないで直ちに終わる。
                return;
            }

            var r = args.Result as ReadFileRunWorkerCompletedArgs;

            AddLogText(r.message);

            if (r.hr < 0) {
                MessageBox.Show(r.message);
                Exit();
                return;
            }

            if (0 < r.individualResultList.Count) {
                foreach (var fileResult in r.individualResultList) {
                    AddLogText(fileResult.ToString(m_pcmDataListForPlay.FindById(fileResult.PcmDataId).FileName));
                }
            }

            // WasapiCSのリピート設定。
            UpdatePlayRepeat();

            switch (m_task.Type) {
            case TaskType.PlaySpecifiedGroup:
            case TaskType.PlayPauseSpecifiedGroup:
                // ファイル読み込み完了後、再生を開始する。
                // 再生するファイルは、タスクで指定されたファイル。
                // このwavDataIdは、再生開始ボタンが押された時点で選択されていたファイル。
                int wavDataId = m_task.WavDataId;

                if (null != m_pliUpdatedByUserSelectWhileLoading) {
                    // (Issue 6)再生リストで選択されている曲が違う曲の場合、
                    // 選択されている曲を再生する。
                    wavDataId = m_pliUpdatedByUserSelectWhileLoading.PcmData().Id;

                    // 使い終わったのでクリアーする。
                    m_pliUpdatedByUserSelectWhileLoading = null;
                }

                ReadStartPlayByWavDataId(wavDataId);
                break;
            default:
                // ファイル読み込み完了後、何もすることはない。
                ChangeState(State.ファイル読み込み完了);
                UpdateUIStatus();
                break;
            }
        }

        /// <summary>
        /// デバイスを選択。
        /// 既に使用中の場合、空振りする。
        /// 別のデバイスを使用中の場合、そのデバイスを未使用にして、新しいデバイスを使用状態にする。
        /// </summary>
        private bool UseDevice()
        {
            // 通常使用するデバイスとする。
            var di = listBoxDevices.SelectedItem as DeviceAttributes;
            m_useDevice = di;
            AddLogText(string.Format(CultureInfo.InvariantCulture, "Device name: {0}{1}", di.Name, Environment.NewLine));
            m_preference.PreferredDeviceName     = di.Name;
            m_preference.PreferredDeviceIdString = di.DeviceIdStr;
            return true;
        }

        /// <summary>
        /// loadGroupIdのファイル読み込みを開始する。
        /// 読み込みが完了したらReadFileRunWorkerCompletedが呼ばれる。
        /// </summary>
        private void StartReadFiles(int loadGroupId) {
            progressBar1.Visibility = System.Windows.Visibility.Visible;
            progressBar1.Value = 0;

            m_loadingGroupId = loadGroupId;
            
            m_readFileWorker.RunWorkerAsync(loadGroupId);
        }

        /// <summary>
        /// 0 <= r < nMaxPlus1の範囲の整数値rをランダムに戻す。
        /// </summary>
        private static int GetRandomNumber(RNGCryptoServiceProvider gen, int nMaxPlus1) {
            var v = new byte[4];
            gen.GetBytes(v);
            return (BitConverter.ToInt32(v, 0) & 0x7fffffff) % nMaxPlus1;
        }

        /// <summary>
        /// シャッフルした再生リストm_pcmDataListForPlayを作成する
        /// </summary>
        private void CreateShuffledPlayList() {
            // 適当にシャッフルされた番号が入っている配列pcmDataIdxArrayを作成。
            var pcmDataIdxArray = new int[m_pcmDataListForDisp.Count()];
            for (int i=0; i < pcmDataIdxArray.Length; ++i) {
                pcmDataIdxArray[i] = i;
            }
            
            var gen = new RNGCryptoServiceProvider();
            int N = pcmDataIdxArray.Length;
            for (int i=0; i < N * 100; ++i) {
                var a = GetRandomNumber(gen, N);
                var b = GetRandomNumber(gen, N);
                if (a == b) {
                    // 入れ替え元と入れ替え先が同じ。あんまり意味ないのでスキップする。
                    continue;
                }

                // a番目とb番目を入れ替える
                var tmp = pcmDataIdxArray[a];
                pcmDataIdxArray[a] = pcmDataIdxArray[b];
                pcmDataIdxArray[b] = tmp;
            }

            // m_pcmDataListForPlayを作成。
            m_pcmDataListForPlay = new PcmDataList();
            for (int i=0; i < pcmDataIdxArray.Length; ++i) {
                var idx = pcmDataIdxArray[i];

                // 再生順番号Ordinalを付け直す
                // GroupIdをバラバラの番号にする(1曲ずつ読み込む)
                var pcmData = new PcmData();
                pcmData.CopyFrom(m_pcmDataListForDisp.At(idx));
                pcmData.Ordinal = i;
                pcmData.GroupId = i;

                m_pcmDataListForPlay.Add(pcmData);
            }
        }

        /// <summary>
        /// 全曲が表示順に並んでいる再生リストm_pcmDataListForPlayを作成。
        /// </summary>
        private void CreateAllTracksPlayList() {
            m_pcmDataListForPlay = new PcmDataList();
            for (int i=0; i < m_pcmDataListForDisp.Count(); ++i) {
                var pcmData = new PcmData();
                pcmData.CopyFrom(m_pcmDataListForDisp.At(i));
                m_pcmDataListForPlay.Add(pcmData);
            }
        }

        /// <summary>
        /// 1曲再生のプレイリストをm_pcmDataListForPlayに作成。
        /// </summary>
        private void CreateOneTrackPlayList(int wavDataId) {
            var pcmData = new PcmData();
            pcmData.CopyFrom(m_pcmDataListForDisp.FindById(wavDataId));
            pcmData.GroupId = 0;

            m_pcmDataListForPlay = new PcmDataList();
            m_pcmDataListForPlay.Add(pcmData);
        }

        private void ButtonPlayClicked() {
            var di = listBoxDevices.SelectedItem as DeviceAttributes;
            if (!UseDevice()) {
                return;
            }

            if (IsPlayModeShuffle()) {
                // シャッフル再生する
                CreateShuffledPlayList();
                ReadStartPlayByWavDataId(m_pcmDataListForPlay.At(0).Id);
                return;
            }

            // 選択されている曲から順番に再生する。
            // 再生する曲のwavDataIdをdataGridの選択セルから取得する
            int wavDataId = 0;
            var selectedCells = dataGridPlayList.SelectedCells;
            if (0 < selectedCells.Count) {
                var cell = selectedCells[0];
                System.Diagnostics.Debug.Assert(cell != null);
                var pli = cell.Item as PlayListItemInfo;
                System.Diagnostics.Debug.Assert(pli != null);
                var pcmData = pli.PcmData();

                if (null != pcmData) {
                    wavDataId = pcmData.Id;
                } else {
                    // ココまで読んだ的な行は、pcmDataを持っていない
                }
            }

            if (IsPlayModeOneTrack()) {
                // 1曲再生。1曲だけ読み込んで再生する。
                CreateOneTrackPlayList(wavDataId);
                ReadStartPlayByWavDataId(wavDataId);
                return;
            }

            // 全曲再生
            CreateAllTracksPlayList();
            ReadStartPlayByWavDataId(wavDataId);
        }

        private void ButtonPauseClicked() {
            int hr = 0;

            switch (m_state) {
            case State.再生中:
                hr = wasapi.Pause();
                AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.Pause() {0:X8}{1}", hr, Environment.NewLine));
                if (0 <= hr) {
                    ChangeState(State.再生一時停止中);
                    UpdateUIStatus();
                } else {
                    // Pause失敗＝すでに再生していない または再生一時停止ができない状況。ここで状態遷移する必要はない。
                }
                break;
            case State.再生一時停止中:
                hr = wasapi.Unpause();
                AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.Unpause() {0:X8}{1}", hr, Environment.NewLine));
                if (0 <= hr) {
                    ChangeState(State.再生中);
                    UpdateUIStatus();
                } else {
                    // Unpause失敗＝すでに再生していない。ここで状態遷移する必要はない。
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e) {
            ButtonPlayClicked();
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e) {
            ButtonPauseClicked();
        }

        /// <summary>
        /// wavDataIdのGroupがロードされていたら直ちに再生開始する。
        /// 読み込まれていない場合、直ちに再生を開始できないので、ロードしてから再生する。
        /// </summary>
        private bool ReadStartPlayByWavDataId(int wavDataId) {
            System.Diagnostics.Debug.Assert(0 <= wavDataId);

            TaskType nextTask = TaskType.PlaySpecifiedGroup;
            if (m_task.Type != TaskType.None) {
                nextTask = m_task.Type;
            }

            var pcmData = m_pcmDataListForPlay.FindById(wavDataId);
            if (null == pcmData) {
                // 1曲再生モードの時。再生リストを作りなおす。
                CreateOneTrackPlayList(wavDataId);
                m_loadedGroupId = -1;
                pcmData = m_pcmDataListForPlay.FindById(wavDataId);
            }

            if (pcmData.GroupId != m_loadedGroupId) {
                // m_LoadedGroupIdと、wavR.GroupIdが異なる場合。
                // 再生するためには、ロードする必要がある。
                UnsetupDevice();

                if (!SetupDevice(pcmData.GroupId)) {
                    //dataGridPlayList.SelectedIndex = 0;
                    ChangeState(State.ファイル読み込み完了);

                    DeviceDeselect();
                    UpdateDeviceList();
                    return false;
                }

                m_task.Set(nextTask, pcmData.GroupId, pcmData.Id);
                StartReadPlayGroupOnTask();
                return true;
            }

            // wavDataIdのグループがm_LoadedGroupIdである。ロードされている。
            // 連続再生フラグの設定と、現在のグループが最後のグループかどうかによって
            // m_LoadedGroupIdの再生が自然に完了したら、行うタスクを決定する。
            UpdateNextTask();

            if (!SetupDevice(pcmData.GroupId)) {
                //dataGridPlayList.SelectedIndex = 0;
                ChangeState(State.ファイル読み込み完了);

                DeviceDeselect();
                UpdateDeviceList();
                return false;
            }
            StartPlay(wavDataId);

            if (nextTask == TaskType.PlayPauseSpecifiedGroup) {
                ButtonPauseClicked();
            }
            return true;
        }

        /// <summary>
        /// 現在のグループの最後のファイルの再生が終わった後に行うタスクを判定し、
        /// m_taskにセットする。
        /// </summary>
        private void UpdateNextTask() {
            if (0 == m_pcmDataListForPlay.CountPcmDataOnPlayGroup(1)) {
                // ファイルグループが1個しかない場合、
                // wasapiUserの中で自発的にループ再生する。
                // ファイルの再生が終わった=停止。
                m_task.Set(TaskType.None);
                return;
            }

            // 順当に行ったら次に再生するグループ番号は(m_loadedGroupId+1)。
            // ①(m_loadedGroupId+1)の再生グループが存在する場合
            //     (m_loadedGroupId+1)の再生グループを再生開始する。
            // ②(m_loadedGroupId+1)の再生グループが存在しない場合
            //     ②-①連続再生(checkBoxContinuous.IsChecked==true)の場合
            //         GroupId==0、pcmDataId=0を再生開始する。
            //     ②-②連続再生ではない場合
            //         停止する。先頭の曲を選択状態にする。
            int nextGroupId = m_loadedGroupId + 1;

            if (0 < m_pcmDataListForPlay.CountPcmDataOnPlayGroup(nextGroupId)) {
                m_task.Set(TaskType.PlaySpecifiedGroup, nextGroupId, m_pcmDataListForPlay.GetFirstPcmDataIdOnGroup(nextGroupId));
                return;
            }

            if (IsPlayModeRepeat()) {
                m_task.Set(TaskType.PlaySpecifiedGroup, 0, 0);
                return;
            }

            m_task.Set(TaskType.None);
        }

        /// <summary>
        /// ただちに再生を開始する。
        /// wavDataIdのGroupが、ロードされている必要がある。
        /// </summary>
        /// <returns>false: 再生開始できなかった。</returns>
        private bool StartPlay(int wavDataId) {
            System.Diagnostics.Debug.Assert(0 <= wavDataId);
            var playPcmData = m_pcmDataListForPlay.FindById(wavDataId);
            if (playPcmData.GroupId != m_loadedGroupId) {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            ChangeState(State.再生中);
            UpdateUIStatus();

            m_sw.Reset();
            m_sw.Start();

            int hr = wasapi.StartPlayback(wavDataId);
            {
                var stat = wasapi.GetWorkerThreadSetupResult();
                if (m_preference.DwmEnableMmcssCall) {
                    AddLogText(string.Format(CultureInfo.InvariantCulture, "DwmEnableMMCSS({0}) result={1:X8}{2}",
                        m_preference.DwmEnableMmcss, stat.DwmEnableMMCSSResult, Environment.NewLine));
                }
                AddLogText(string.Format(CultureInfo.InvariantCulture, "AvSetMMThreadCharacteristics({0}) result={1:X8}{2}",
                    m_preference.RenderThreadTaskType, stat.AvSetMmThreadCharacteristicsResult, Environment.NewLine));
            }

            AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.StartPlayback({0}) {1:X8}{2}", wavDataId, hr, Environment.NewLine));
            if (hr < 0) {
                MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Properties.Resources.PlayStartFailed + "！{0:X8}", hr));
                Exit();
                return false;
            }

            // 再生バックグラウンドタスク開始。PlayDoWorkが実行される。
            // 再生バックグラウンドタスクを止めるには、Stop()を呼ぶ。
            // 再生バックグラウンドタスクが止まったらPlayRunWorkerCompletedが呼ばれる。
            m_playWorker.RunWorkerAsync();
            return true;
        }

        /// <summary>
        /// true: 再生停止 無音を送出してから停止する
        /// </summary>
        private bool m_bStopGently;

        /// <summary>
        /// 再生中。バックグラウンドスレッド。
        /// </summary>
        private void PlayDoWork(object o, DoWorkEventArgs args) {
            //Console.WriteLine("PlayDoWork started");
            var bw = o as BackgroundWorker;
            bool cancelProcessed = false;

            while (!wasapi.Run(PROGRESS_REPORT_INTERVAL_MS)) {
                if (!m_preference.RefrainRedraw) {
                    m_playWorker.ReportProgress(0);
                }
                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending && !cancelProcessed) {
                    Console.WriteLine("PlayDoWork() CANCELED StopGently=" + m_bStopGently);
                    if (m_bStopGently) {
                        // 最後に再生する無音の再生にジャンプする。その後再生するものが無くなって停止する
                        wasapi.UpdatePlayPcmDataById(-1);
                        wasapi.Unpause();
                        cancelProcessed = true;
                    } else {
                        wasapi.Stop();
                        args.Cancel = true;
                    }
                }
            }

            // 正常に最後まで再生が終わった場合、ここでStopを呼んで、後始末する。
            // キャンセルの場合は、2回Stopが呼ばれることになるが、問題ない!!!
            wasapi.Stop();

            // 停止完了後タスクの処理は、ここではなく、PlayRunWorkerCompletedで行う。
        }

        /// <summary>
        /// 再生の進行状況をUIに反映する。
        /// </summary>
        private void PlayProgressChanged(object o, ProgressChangedEventArgs args) {
            var bw = o as BackgroundWorker;

            if (null == wasapi) {
                return;
            }

            if (bw.CancellationPending) {
                // ワーカースレッドがキャンセルされているので、何もしない。
                return;
            }

            // 再生中PCMデータ(または一時停止再開時再生予定PCMデータ等)の再生位置情報を画面に表示する。
            WasapiCS.PcmDataUsageType usageType = WasapiCS.PcmDataUsageType.NowPlaying;
            int pcmDataId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (pcmDataId < 0) {
                pcmDataId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
                usageType = WasapiCS.PcmDataUsageType.PauseResumeToPlay;
            }
            if (pcmDataId < 0) {
                pcmDataId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.SpliceNext);
                usageType = WasapiCS.PcmDataUsageType.SpliceNext;
            }

            if (pcmDataId < 0) {
                labelPlayingTime.Content = PLAYING_TIME_UNKNOWN;
            } else {
                if (dataGridPlayList.SelectedIndex != GetPlayListIndexOfPcmDataId(pcmDataId)) {
                    dataGridPlayList.SelectedIndex = GetPlayListIndexOfPcmDataId(pcmDataId);
                    dataGridPlayList.ScrollIntoView(dataGridPlayList.SelectedItem);
                }

                PcmDataLib.PcmData pcmData = m_pcmDataListForPlay.FindById(pcmDataId);

                var stat    = wasapi.GetSessionStatus();
                var playPos = wasapi.GetPlayCursorPosition(usageType);

                slider1.Maximum = playPos.TotalFrameNum;
                if (!mSliderSliding || playPos.TotalFrameNum <= slider1.Value) {
                    slider1.Value = playPos.PosFrame;
                }

                labelPlayingTime.Content = string.Format(CultureInfo.InvariantCulture, "{0}/{1}",
                        Util.SecondsToHMSString((int)(slider1.Value / stat.DeviceSampleRate)),
                        Util.SecondsToHMSString((int)(playPos.TotalFrameNum / stat.DeviceSampleRate)));
            }
        }

        /// <summary>
        /// m_taskに指定されているグループをロードし、ロード完了したら指定ファイルを再生開始する。
        /// ファイル読み込み完了状態にいるときに呼ぶ。
        /// </summary>
        private void StartReadPlayGroupOnTask() {
            m_loadedGroupId = -1;

            switch (m_task.Type) {
            case TaskType.PlaySpecifiedGroup:
            case TaskType.PlayPauseSpecifiedGroup:
                break;
            default:
                // 想定されていない状況
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            // 再生状態→再生グループ切り替え中状態に遷移。
            ChangeState(State.再生グループ読み込み中);
            UpdateUIStatus();

            StartReadFiles(m_task.GroupId);
        }

        /// <summary>
        /// 再生終了後タスクを実行する。
        /// </summary>
        private void PerformPlayCompletedTask() {
            // 再生終了後に行うタスクがある場合、ここで実行する。
            switch (m_task.Type) {
            case TaskType.PlaySpecifiedGroup:
            case TaskType.PlayPauseSpecifiedGroup:
                UnsetupDevice();

                if (IsPlayModeOneTrack()) {
                    // 1曲再生モードの時、再生リストを作りなおす。
                    CreateOneTrackPlayList(m_task.WavDataId);
                }

                if (SetupDevice(m_task.GroupId)) {
                    StartReadPlayGroupOnTask();
                    return;
                }

                // デバイスの設定を試みたら、失敗した。
                // FALL_THROUGHする。
                break;
            default:
                break;
            }

            // 再生終了後に行うタスクがない。停止する。
            // 再生状態→ファイル読み込み完了状態。

            // 先頭の曲を選択状態にする。
            //dataGridPlayList.SelectedIndex = 0;
            
            ChangeState(State.ファイル読み込み完了);

            DeviceDeselect();

            if (m_deviceListUpdatePending) {
                UpdateDeviceList();
                m_deviceListUpdatePending = false;
            }
        }

        /// <summary>
        /// 再生終了。
        /// </summary>
        private void PlayRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            m_sw.Stop();
            AddLogText(string.Format(CultureInfo.InvariantCulture, Properties.Resources.PlayCompletedElapsedTimeIs + " {0}{1}", m_sw.Elapsed, Environment.NewLine));

            PerformPlayCompletedTask();
        }

        private void ButtonStopClicked() {
            ChangeState(State.再生停止開始);
            UpdateUIStatus();

            // 停止ボタンで停止した場合は、停止後何もしない。
            StopAsync(new Task(TaskType.None), true);
            AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.Stop(){0}", Environment.NewLine));
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            ButtonStopClicked();
        }

        private long mLastSliderValue = 0;

        private void slider1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.Source != slider1) {
                return;
            }

            mLastSliderValue = (long)slider1.Value;
            mSliderSliding = true;
        }

        private void slider1_MouseMove(object sender, MouseEventArgs e) {
            if (e.Source != slider1) {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                mLastSliderValue = (long)slider1.Value;
                if (!buttonPlay.IsEnabled) {
                    wasapi.SetPosFrame((long)slider1.Value);
                }
            }
        }
        private void slider1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.Source != slider1) {
                return;
            }

            if (!buttonPlay.IsEnabled &&
                    mLastSliderValue != (long)slider1.Value) {
                wasapi.SetPosFrame((long)slider1.Value);
            }

            mLastSliderValue = 0;
            mSliderSliding = false;
        }


        struct InspectFormat {
            public int sampleRate;
            public int bitsPerSample;
            public int validBitsPerSample;
            public WasapiCS.BitFormatType bitFormat;
            public InspectFormat(int sr, int bps, int vbps, WasapiCS.BitFormatType bf) {
                sampleRate         = sr;
                bitsPerSample      = bps;
                validBitsPerSample = vbps;
                bitFormat          = bf;
            }
        };

        const int TEST_SAMPLE_RATE_NUM = 8;
        const int TEST_BIT_REPRESENTATION_NUM = 5;

        static readonly InspectFormat [] gInspectFormats = new InspectFormat [] {
                new InspectFormat(44100,  16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(48000,  16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(88200,  16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(96000,  16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(176400, 16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(192000, 16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(352800, 16, 16, WasapiCS.BitFormatType.SInt),
                new InspectFormat(384000, 16, 16, WasapiCS.BitFormatType.SInt),

                new InspectFormat(44100,  24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(48000,  24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(88200,  24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(96000,  24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(176400, 24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(192000, 24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(352800, 24, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(384000, 24, 24, WasapiCS.BitFormatType.SInt),

                new InspectFormat(44100,  32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(48000,  32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(88200,  32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(96000,  32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(176400, 32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(192000, 32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(352800, 32, 24, WasapiCS.BitFormatType.SInt),
                new InspectFormat(384000, 32, 24, WasapiCS.BitFormatType.SInt),

                new InspectFormat(44100,  32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(48000,  32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(88200,  32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(96000,  32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(176400, 32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(192000, 32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(352800, 32, 32, WasapiCS.BitFormatType.SInt),
                new InspectFormat(384000, 32, 32, WasapiCS.BitFormatType.SInt),

                new InspectFormat(44100,  32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(48000,  32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(88200,  32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(96000,  32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(176400, 32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(192000, 32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(352800, 32, 32, WasapiCS.BitFormatType.SFloat),
                new InspectFormat(384000, 32, 32, WasapiCS.BitFormatType.SFloat),
            };

        private void buttonInspectDevice_Click(object sender, RoutedEventArgs e) {
            var attr = wasapi.GetDeviceAttributes(listBoxDevices.SelectedIndex);

            AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.InspectDevice()\r\nDeviceFriendlyName={0}\r\nDeviceIdString={1}{2}", attr.Name, attr.DeviceIdString, Environment.NewLine));
            AddLogText(string.Format(CultureInfo.InvariantCulture, "++-------------++-------------++-------------++-------------++-------------++-------------++-------------++-------------++{0}", Environment.NewLine));
            for (int fmt = 0; fmt < TEST_BIT_REPRESENTATION_NUM; ++fmt) {
                var sb = new StringBuilder();
                for (int sr =0; sr < TEST_SAMPLE_RATE_NUM; ++sr) {
                    int idx = sr + fmt * TEST_SAMPLE_RATE_NUM;
                    System.Diagnostics.Debug.Assert(idx < gInspectFormats.Length);
                    var ifmt = gInspectFormats[idx];
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "||{0,3}kHz {1}{2}V{3}",
                            ifmt.sampleRate / 1000, ifmt.bitFormat == 0 ? "i" : "f",
                            ifmt.bitsPerSample, ifmt.validBitsPerSample));
                }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "||{0}", Environment.NewLine));
                AddLogText(sb.ToString());

                sb.Clear();
                for (int sr =0; sr < TEST_SAMPLE_RATE_NUM; ++sr) {
                    int idx = sr + fmt * TEST_SAMPLE_RATE_NUM;
                    System.Diagnostics.Debug.Assert(idx < gInspectFormats.Length);
                    var ifmt = gInspectFormats[idx];
                    int hr = wasapi.InspectDevice(listBoxDevices.SelectedIndex, ifmt.sampleRate,
                            WasapiCS.BitAndFormatToSampleFormatType(ifmt.bitsPerSample, ifmt.validBitsPerSample, ifmt.bitFormat), 2);
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "|| {0} {1:X8} ", hr==0 ? "OK" : "NA", hr));
                }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "||{0}", Environment.NewLine));
                AddLogText(sb.ToString());
                AddLogText(string.Format(CultureInfo.InvariantCulture, "++-------------++-------------++-------------++-------------++-------------++-------------++-------------++-------------++{0}", Environment.NewLine));
            }
        }

        /// <summary>
        /// SettingsWindowによって変更された表示情報をUIに反映し、設定を反映する。
        /// </summary>
        void PreferenceUpdated() {
            RenderOptions.ProcessRenderMode =
                    m_preference.GpuRendering ? RenderMode.Default : RenderMode.SoftwareOnly;

            var ffc = new FontFamilyConverter();
            var ff = ffc.ConvertFromString(m_preference.PlayingTimeFontName) as FontFamily;
            if (null != ff) {
                labelPlayingTime.FontFamily = ff;
            }
            labelPlayingTime.FontSize = m_preference.PlayingTimeSize;
            labelPlayingTime.FontWeight = m_preference.PlayingTimeFontBold ? FontWeights.Bold : FontWeights.Normal;

            sliderWindowScaling.Value = m_preference.WindowScale;

            UpdateUIStatus();
        }

        List<string> m_logList = new List<string>();
        int m_logLineNum = 100;

        /// <summary>
        /// ログを追加する。
        /// </summary>
        /// <param name="s">追加するログ。行末に\r\nを入れる必要あり。</param>
        private void AddLogText(string s) {
            Console.Write(s);

            // ログを適当なエントリ数で流れるようにする。
            // sは複数行の文字列が入っていたり、改行が入っていなかったりするので、行数制限にはなっていない。
            m_logList.Add(s);
            while (m_logLineNum < m_logList.Count) {
                m_logList.RemoveAt(0);
            }

            var sb = new StringBuilder();
            foreach (var item in m_logList) {
                sb.Append(item);
            }

            textBoxLog.Text = sb.ToString();
            textBoxLog.ScrollToEnd();
        }

        /// <summary>
        /// ロード中に選択曲が変更された場合、ロード後に再生曲変更処理を行う。
        /// ChangePlayWavDataById()でセットし
        /// ReadFileRunWorkerCompleted()で参照する。
        /// </summary>
        private PlayListItemInfo m_pliUpdatedByUserSelectWhileLoading = null;

        /// <summary>
        /// 再生中に、再生曲をwavDataIdの曲に切り替える。
        /// wavDataIdの曲がロードされていたら、直ちに再生曲切り替え。
        /// ロードされていなければ、グループをロードしてから再生。
        /// 
        /// 再生中でない場合は、最初に再生する曲をwavDataIdの曲に変更する。
        /// </summary>
        /// <param name="pcmDataId">再生曲</param>
        private void ChangePlayWavDataById(int wavDataId, TaskType nextTask) {
            System.Diagnostics.Debug.Assert(0 <= wavDataId);

            var playingId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            var pauseResumeId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
            if (playingId < 0 && pauseResumeId < 0 && 0 <= m_loadingGroupId) {
                // 再生中でなく、ロード中の場合。
                // ロード完了後ReadFileRunWorkerCompleted()で再生する曲を切り替えるための
                // 情報をセットする。
                m_pliUpdatedByUserSelectWhileLoading = m_playListItems[dataGridPlayList.SelectedIndex];
                return;
            }

            if (playingId < 0 && pauseResumeId < 0) {
                // 再生中でなく、再生一時停止中でなく、ロード中でもない場合。
                wasapi.UpdatePlayPcmDataById(wavDataId);
                return;
            }

            // 再生中か再生一時停止中である。
            var pcmData = m_pcmDataListForPlay.FindById(wavDataId);
            if (null == pcmData) {
                // 再生リストの中に次に再生する曲が見つからない。1曲再生の時起きる。
                StopAsync(new Task(nextTask, 0, wavDataId), true);
                return;
            }

            var groupId = pcmData.GroupId;

            var playPcmData = m_pcmDataListForPlay.FindById(playingId);
            if (playPcmData == null) {
                playPcmData = m_pcmDataListForPlay.FindById(pauseResumeId);
            }
            if (playPcmData.GroupId == groupId) {
                // 同一ファイルグループのファイルの場合、すぐにこの曲が再生可能。
                wasapi.UpdatePlayPcmDataById(wavDataId);
                AddLogText(string.Format(CultureInfo.InvariantCulture, "wasapi.UpdatePlayPcmDataById({0}){1}", wavDataId, Environment.NewLine));
            } else {
                // ファイルグループが違う場合、再生を停止し、グループを読み直し、再生を再開する。
                StopAsync(new Task(nextTask, groupId, wavDataId), true);
            }
        }

        #region しょーもない関数群

        private static WasapiCS.SchedulerTaskType
        PreferenceSchedulerTaskTypeToWasapiCSSchedulerTaskType(
            RenderThreadTaskType t) {
            switch (t) {
            case RenderThreadTaskType.None:
                return WasapiCS.SchedulerTaskType.None;
            case RenderThreadTaskType.Audio:
                return WasapiCS.SchedulerTaskType.Audio;
            case RenderThreadTaskType.ProAudio:
                return WasapiCS.SchedulerTaskType.ProAudio;
            case RenderThreadTaskType.Playback:
                return WasapiCS.SchedulerTaskType.Playback;
            default:
                System.Diagnostics.Debug.Assert(false);
                return WasapiCS.SchedulerTaskType.None; ;
            }
        }

        private static WasapiCS.ShareMode
        PreferenceShareModeToWasapiCSShareMode(WasapiSharedOrExclusiveType t) {
            switch (t) {
            case WasapiSharedOrExclusiveType.Shared:
                return WasapiCS.ShareMode.Shared;
            case WasapiSharedOrExclusiveType.Exclusive:
                return WasapiCS.ShareMode.Exclusive;
            default:
                System.Diagnostics.Debug.Assert(false);
                return WasapiCS.ShareMode.Exclusive;
            }
        }

        private static WasapiCS.DataFeedMode
        PreferenceDataFeedModeToWasapiCS(WasapiDataFeedModeType t) {
            switch (t) {
            case WasapiDataFeedModeType.EventDriven:
                return WasapiCS.DataFeedMode.EventDriven;
            case WasapiDataFeedModeType.TimerDriven:
                return WasapiCS.DataFeedMode.TimerDriven;
            default:
                System.Diagnostics.Debug.Assert(false);
                return WasapiCS.DataFeedMode.EventDriven;
            }
        }

        #endregion

        // イベント処理 /////////////////////////////////////////////////////

        private void buttonSettings_Click(object sender, RoutedEventArgs e) {
            var sw = new SettingsWindow();
            sw.SetPreference(m_preference);
            sw.ShowDialog();

            PreferenceUpdated();
        }

        private void radioButtonExclusive_Checked(object sender, RoutedEventArgs e) {
            m_preference.WasapiSharedOrExclusive = WasapiSharedOrExclusiveType.Exclusive;
        }

        private void radioButtonShared_Checked(object sender, RoutedEventArgs e) {
            m_preference.WasapiSharedOrExclusive = WasapiSharedOrExclusiveType.Shared;
        }

        private void radioButtonEventDriven_Checked(object sender, RoutedEventArgs e) {
            m_preference.WasapiDataFeedMode = WasapiDataFeedModeType.EventDriven;
        }

        private void radioButtonTimerDriven_Checked(object sender, RoutedEventArgs e) {
            m_preference.WasapiDataFeedMode = WasapiDataFeedModeType.TimerDriven;
        }

        private void buttonRemovePlayList_Click(object sender, RoutedEventArgs e) {
            var selectedItemCount = dataGridPlayList.SelectedItems.Count;
            if (0 == selectedItemCount) {
                return;
            }

            if (selectedItemCount == m_playListItems.Count) {
                // すべて消える。再生開始などが出来なくなるので別処理。
                ClearPlayList(PlayListClearMode.ClearWithUpdateUI);
                return;
            }

            {
                // 再生リストの一部項目が消える。
                // PcmDataのIDが飛び飛びになるので番号を振り直す。
                // PcmDataのGroupIdも飛び飛びになるが、特に問題にならないようなので付け直さない。
                int idx;
                while (0 <= (idx = dataGridPlayList.SelectedIndex)) {
                    m_pcmDataListForDisp.RemoveAt(idx);
                    m_playListItems.RemoveAt(idx);
                    dataGridPlayList.UpdateLayout();
                }
                GC.Collect();

                for (int i = 0; i < m_pcmDataListForDisp.Count(); ++i) {
                    m_pcmDataListForDisp.At(i).Id = i;
                    m_pcmDataListForDisp.At(i).Ordinal = i;
                }
                dataGridPlayList.UpdateLayout();

                UpdateUIStatus();
            }
        }

        private delegate int UpdateOrdinal(int v);

        private void buttonNextOrPrevClickedWhenPlaying(UpdateOrdinal updateOrdinal) {
            TaskType nextTask = TaskType.PlaySpecifiedGroup;
            var wavDataId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (wavDataId < 0) {
                wavDataId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
                nextTask = TaskType.PlayPauseSpecifiedGroup;
            }
            var playingPcmData = m_pcmDataListForPlay.FindById(wavDataId);
            if (null == playingPcmData) {
                return;
            }

            var ordinal = playingPcmData.Ordinal;
            ordinal = updateOrdinal(ordinal);
            if (ordinal < 0) {
                ordinal = 0;
            }

            if (IsPlayModeOneTrack()) {
                // 1曲再生モードの時
                if (m_pcmDataListForDisp.Count() <= ordinal) {
                    ordinal = 0;
                }
                ChangePlayWavDataById(m_pcmDataListForDisp.At(ordinal).Id, nextTask);
            } else {
                // 全曲再生またはシャッフル再生モードの時。
                if (m_pcmDataListForPlay.Count() <= ordinal) {
                    ordinal = 0;
                }
                ChangePlayWavDataById(m_pcmDataListForPlay.At(ordinal).Id, nextTask);
            }
        }

        private void buttonNextOrPrevClickedWhenStop(UpdateOrdinal updateOrdinal) {
            var idx = dataGridPlayList.SelectedIndex;
            idx = updateOrdinal(idx);
            if (idx < 0) {
                idx = 0;
            } else if (dataGridPlayList.Items.Count <= idx) {
                idx = 0;
            }
            dataGridPlayList.SelectedIndex = idx;
            dataGridPlayList.ScrollIntoView(dataGridPlayList.SelectedItem);
        }

        private void buttonNextOrPrevClicked(UpdateOrdinal updateOrdinal) {
            switch (m_state) {
            case State.再生一時停止中:
            case State.再生中:
                buttonNextOrPrevClickedWhenPlaying(updateOrdinal);
                break;
            case State.再生リストあり:
                buttonNextOrPrevClickedWhenStop(updateOrdinal);
                break;
            }
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e) {
            buttonNextOrPrevClicked((x) => { return ++x; });
        }

        private void buttonPrev_Click(object sender, RoutedEventArgs e) {
            buttonNextOrPrevClicked((x) => { return --x; });
        }

        private void dataGrid1_LoadingRow(object sender, DataGridRowEventArgs e) {
            e.Row.MouseDoubleClick += new MouseButtonEventHandler(dataGridPlayList_RowMouseDoubleClick);
        }

        private void dataGridPlayList_RowMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (m_state == State.再生リストあり && e.ChangedButton == MouseButton.Left && dataGridPlayList.IsReadOnly) {
                // 再生されていない状態で、再生リスト再生モードで項目左ボタンダブルクリックされたら再生開始する
                buttonPlay_Click(sender, e);
            }
        }

        private void dataGridPlayList_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            m_playListMouseDown = true;

        }

        private void dataGridPlayList_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            m_playListMouseDown = false;
        }

        private void dataGridPlayList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
            /*
                if (m_state == State.プレイリストあり && 0 <= dataGridPlayList.SelectedIndex) {
                    buttonRemovePlayList.IsEnabled = true;
                } else {
                    buttonRemovePlayList.IsEnabled = false;
                }
            */
        }

        private void dataGridPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateCoverart();

            if (m_state == State.再生リストあり && 0 <= dataGridPlayList.SelectedIndex) {
                buttonDelistSelected.IsEnabled = true;
            } else {
                buttonDelistSelected.IsEnabled = false;
            }

            if (null == wasapi) {
                return;
            }

            if (!m_playListMouseDown ||
                dataGridPlayList.SelectedIndex < 0 ||
                m_playListItems.Count() <= dataGridPlayList.SelectedIndex) {
                return;
            }

            var pli = m_playListItems[dataGridPlayList.SelectedIndex];
            if (pli.PcmData() == null) {
                // 曲じゃない部分を選択したら無視。
                return;
            }

            if (m_state != State.再生中) {
                ChangePlayWavDataById(pli.PcmData().Id, TaskType.PlaySpecifiedGroup);
                return;
            }

            // 再生中の場合。

            var playingId = wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (playingId < 0) {
                return;
            }

            // 再生中で、しかも、マウス押下中にこのイベントが来た場合で、
            // しかも、この曲を再生していない場合、この曲を再生する。
            if (null != pli.PcmData() &&
                playingId != pli.PcmData().Id) {
                ChangePlayWavDataById(pli.PcmData().Id, TaskType.PlaySpecifiedGroup);
            }
        }

        private bool IsWindowMoveMode(MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) {
                return false;
            }

            foreach (MenuItem mi in menu1.Items) {
                if (mi.IsMouseOver) {
                    return false;
                }
            }
            return true;
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl)) {
                // CTRL + マウスホイールで画面のスケーリング

                var scaling = sliderWindowScaling.Value;
                if (e.Delta < 0) {
                    // 1.25の128乗根 = 1.001744829441175331741294013303
                    scaling /= 1.001744829441175331741294013303;
                } else {
                    scaling *= 1.001744829441175331741294013303;
                }
                sliderWindowScaling.Value = scaling;
                m_preference.WindowScale = scaling;
            }
        }

        /// <summary>
        /// デバイスが突然消えたとか、突然増えたとかのイベント。
        /// </summary>
        private void WasapiStatusChanged(StringBuilder idStr) {
            Console.WriteLine("WasapiStatusChanged {0}", idStr);
            Dispatcher.BeginInvoke(new Action(delegate() {
                // AddLogText(string.Format(CultureInfo.InvariantCulture, Properties.Resources.DeviceStateChanged + Environment.NewLine, idStr));
                switch (m_state)
                {
                    case State.未初期化:
                        return;
                    case State.再生リストなし:
                    case State.再生リスト読み込み中:
                    case State.再生リストあり:
                        // 再生中ではない場合、デバイス一覧を更新する。
                        // DeviceDeselect();
                        UpdateDeviceList();
                        break;
                    case State.デバイスSetup完了:
                    case State.ファイル読み込み完了:
                    case State.再生グループ読み込み中:
                    case State.再生一時停止中:
                    case State.再生中:
                    case State.再生停止開始:
                        if (0 == string.Compare(m_useDevice.DeviceIdStr, idStr.ToString(), StringComparison.Ordinal)) {
                            // 再生に使用しているデバイスの状態が変化した場合、再生停止してデバイス一覧を更新する。
                            AddLogText(string.Format(CultureInfo.InvariantCulture, Properties.Resources.UsingDeviceStateChanged + Environment.NewLine,
                                    m_useDevice.Name, m_useDevice.DeviceIdStr));
                            StopBlocking();
                            DeviceDeselect();
                            UpdateDeviceList();
                        } else {
                            // 次の再生停止時にデバイス一覧を更新する。
                            m_deviceListUpdatePending = true;
                        }
                        break;
                }
            }));
        }
        
        #region ドラッグアンドドロップ

        private void dataGridPlayList_CheckDropTarget(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                // ファイルのドラッグアンドドロップ。
                // ここでハンドルせず、MainWindowのMainWindowDragDropに任せる。
                e.Handled = false;
                return;
            }

            e.Handled = true;
            var row = FindVisualParent<DataGridRow>(e.OriginalSource as UIElement);
            if (row == null || !(row.Item is PlayListItemInfo)) {
                // 行がドラッグされていない。
                e.Effects = DragDropEffects.None;
            } else {
                // 行がドラッグされている。
                // Id列を選択している場合のみドラッグアンドドロップ可能。
                //if (0 != "Id".CompareTo(dataGridPlayList.CurrentCell.Column.Header)) {
                //    e.Effects = DragDropEffects.None;
                //}
                // e.Effects = DragDropEffects.Move;
            }
        }

        private void dataGridPlayList_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                // ファイルのドラッグアンドドロップ。
                // ここでハンドルせず、MainWindowのMainWindowDragDropに任せる。
                e.Handled = false;
                return;
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
            DataGridRow row = FindVisualParent<DataGridRow>(e.OriginalSource as UIElement);
            if (row == null || !(row.Item is PlayListItemInfo)) {
                // 行がドラッグされていない。(セルがドラッグされている)
            } else {
                // 再生リスト項目のドロップ。
                m_dropTargetPlayListItem = row.Item as PlayListItemInfo;
                if (m_dropTargetPlayListItem != null) {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }

        private void dataGridPlayList_MouseMove(object sender, MouseEventArgs e) {
            if (m_state == State.再生中 ||
                m_state == State.再生一時停止中) {
                // 再生中は再生リスト項目入れ替え不可能。
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed) {
                // 左マウスボタンが押されていない。
                return;
            }

            var row = FindVisualParent<DataGridRow>(e.OriginalSource as FrameworkElement);
            if ((row == null) || !row.IsSelected) {
                Console.WriteLine("MouseMove row==null || !row.IsSelected");
                return;
            }

            var pli = row.Item as PlayListItemInfo;

            // MainWindow.Drop()イベントを発生させる(ブロック)。
            var finalDropEffect = DragDrop.DoDragDrop(row, pli, DragDropEffects.Move);
            if (finalDropEffect == DragDropEffects.Move && m_dropTargetPlayListItem != null) {
                // ドロップ操作実行。
                // Console.WriteLine("MouseMove do move");

                var oldIndex = m_playListItems.IndexOf(pli);
                var newIndex = m_playListItems.IndexOf(m_dropTargetPlayListItem);
                if (oldIndex != newIndex) {
                    // 項目が挿入された。PcmDataも挿入処理する。
                    m_playListItems.Move(oldIndex, newIndex);
                    PcmDataListItemsMove(oldIndex, newIndex);
                    // m_playListView.RefreshCollection();
                    dataGridPlayList.UpdateLayout();
                }
                m_dropTargetPlayListItem = null;
            }
        }

        private static T FindVisualParent<T>(UIElement element) where T : UIElement {
            var parent = element;
            while (parent != null) {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null) {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private PlayListItemInfo m_dropTargetPlayListItem = null;

        #endregion

        /// <summary>
        /// m_pcmDataListForDispのIdとGroupIdをリナンバーする。
        /// </summary>
        private void PcmDataListForDispItemsRenumber() {
            m_groupIdNextAdd = 0;
            for (int i = 0; i < m_pcmDataListForDisp.Count(); ++i) {
                var pcmData = m_pcmDataListForDisp.At(i);
                var pli = m_playListItems[i];

                if (0 < i) {
                    var prevPcmData = m_pcmDataListForDisp.At(i - 1);
                    var prevPli = m_playListItems[i - 1];

                    if (prevPli.ReadSeparaterAfter || !pcmData.IsSameFormat(prevPcmData)) {
                        /* 1つ前の項目にReadSeparatorAfterフラグが立っている、または
                         * 1つ前の項目とPCMフォーマットが異なる。
                         * ファイルグループ番号を更新する。
                         */
                        ++m_groupIdNextAdd;
                    }
                }

                pcmData.Id = i;
                pcmData.Ordinal = i;
                pcmData.GroupId = m_groupIdNextAdd;
            }
        }

        /// <summary>
        /// oldIdxの項目をnewIdxの項目の後に挿入する。
        /// </summary>
        private void PcmDataListItemsMove(int oldIdx, int newIdx) {
            System.Diagnostics.Debug.Assert(oldIdx != newIdx);

            /* oldIdx==0, newIdx==1, Count==2の場合
             * remove(0)
             * insert(1)
             * 
             * oldIdx==1, newIdx==0, Count==2の場合
             * remove(1)
             * insert(0)
             */

            var old = m_pcmDataListForDisp.At(oldIdx);
            m_pcmDataListForDisp.RemoveAt(oldIdx);
            m_pcmDataListForDisp.Insert(newIdx, old);

            // Idをリナンバーする。
            PcmDataListForDispItemsRenumber();
        }

        void PlayListItemInfoPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ReadSeparaterAfter") {
                // グループ番号をリナンバーする。
                PcmDataListForDispItemsRenumber();
            }
        }

        private void buttonClearPlayList_Click(object sender, RoutedEventArgs e) {
            ClearPlayList(PlayListClearMode.ClearWithUpdateUI);
        }

        private void buttonPlayListItemEditMode_Click(object sender, RoutedEventArgs e) {
            dataGridPlayList.IsReadOnly = !dataGridPlayList.IsReadOnly;

            // dataGridPlayList.IsReadOnlyを見て、他の関連メニュー項目状態が更新される
            UpdateUIStatus();
        }

        private InterceptMediaKeys mKListener = null;

        private void AddKeyListener() {
            System.Diagnostics.Debug.Assert(mKListener == null);

            mKListener = new InterceptMediaKeys();
            mKListener.KeyUp += new InterceptMediaKeys.MediaKeyEventHandler(MediaKeyListener_KeyUp);
        }

        private void DeleteKeyListener() {
            if (mKListener != null) {
                mKListener.Dispose();
                mKListener = null;
            }
        }

        private void MediaKeyListener_KeyUp(object sender, InterceptMediaKeys.MediaKeyEventArgs args) {
            if (args == null) {
                return;
            }

            Dispatcher.BeginInvoke(new Action(delegate() {
                switch (args.Key) {
                case Key.MediaPlayPause:
                    if (buttonPlay.IsEnabled) {
                        ButtonPlayClicked();
                    } else if (buttonPause.IsEnabled) {
                        ButtonPauseClicked();
                    }
                    break;
                case Key.MediaStop:
                    if (buttonStop.IsEnabled) {
                        ButtonStopClicked();
                    }
                    break;
                case Key.MediaNextTrack:
                    if (buttonNext.IsEnabled) {
                        buttonNextOrPrevClicked((x) => { return ++x; });
                    }
                    break;
                case Key.MediaPreviousTrack:
                    if (buttonPrev.IsEnabled) {
                        buttonNextOrPrevClicked((x) => { return --x; });
                    }
                    break;
                }
            }));
        }

        private void checkBoxSoundEffects_Checked(object sender, RoutedEventArgs e) {
            m_preference.SoundEffectsEnabled = true;
            buttonSoundEffectsSettings.IsEnabled = true;

            UpdateSoundEffects(true);
        }

        private void checkBoxSoundEffects_Unchecked(object sender, RoutedEventArgs e) {
            m_preference.SoundEffectsEnabled = false;
            buttonSoundEffectsSettings.IsEnabled = false;

            UpdateSoundEffects(false);
        }

        private void buttonSoundEffectsSettings_Click(object sender, RoutedEventArgs e) {
            var dialog = new SoundEffectsConfiguration();
            dialog.SetAudioFilterList(mPreferenceAudioFilterList);
            var result = dialog.ShowDialog();

            if (true == result) {
                mPreferenceAudioFilterList = dialog.AudioFilterList;

                if (mPreferenceAudioFilterList.Count == 0) {
                    // 音声処理を無効にする。
                    m_preference.SoundEffectsEnabled = false;
                    checkBoxSoundEffects.IsChecked = false;
                    buttonSoundEffectsSettings.IsEnabled = false;
                    UpdateSoundEffects(false);
                } else {
                    UpdateSoundEffects(true);
                }
            }
        }

        private void UpdateSoundEffects(bool bEnable) {
            var sfu = new SoundEffectsUpdater();

            if (bEnable) {
                sfu.Update(wasapi, mPreferenceAudioFilterList);
            } else {
                sfu.Update(wasapi, new List<PreferenceAudioFilter>());
            }
        }
    }
}
