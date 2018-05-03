// 日本語UTF-8

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PcmDataLib;
using Wasapi;
using WasapiPcmUtil;
using WWUtil;
using System.Net.Sockets;
using System.Windows.Threading;

namespace PlayPcmWin
{
    public sealed partial class MainWindow : Window
    {
        private const int TYPICAL_READ_FRAMES = 4 * 1024 * 1024;

        private const string PLAYING_TIME_UNKNOWN = "--:-- / --:--";
        private const string PLAYING_TIME_ALLZERO = "00:00 / 00:00";

        /// <summary>
        /// ログの表示行数。
        /// </summary>
        private const int LOG_LINE_NUM = 100;

        AudioPlayer ap = new AudioPlayer();

        /// <summary>
        /// スライダー位置の更新頻度 (500ミリ秒)
        /// </summary>
        private const long SLIDER_UPDATE_TICKS = 500 * 10000;

        /// <summary>
        /// 共有モードの音量制限。
        /// </summary>
        const double SHARED_MAX_AMPLITUDE = 0.98;

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
            new PlayListColumnInfo("ComposerName", DataGridLength.Auto),

            new PlayListColumnInfo("SampleRate", DataGridLength.Auto),
            new PlayListColumnInfo("QuantizationBitRate", DataGridLength.Auto),
            new PlayListColumnInfo("NumChannels", DataGridLength.SizeToCells),
            new PlayListColumnInfo("BitRate", DataGridLength.Auto),
            new PlayListColumnInfo("TrackNr", DataGridLength.SizeToCells),

            new PlayListColumnInfo("IndexNr", DataGridLength.SizeToCells),
            new PlayListColumnInfo("FileExtension", DataGridLength.Auto),
            new PlayListColumnInfo("ReadSeparaterAfter", DataGridLength.SizeToCells)
        };

        /// <summary>
        /// 再生リスト項目情報。
        /// </summary>
        private ObservableCollection<PlayListItemInfo> m_playListItems = new ObservableCollection<PlayListItemInfo>();

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

        NextTask m_taskAfterStop = new NextTask();

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
            public PlaylistSave3 pl;
            public ReadPpwPlaylistMode mode;
            public PlaylistReadWorkerArg(PlaylistSave3 pl, ReadPpwPlaylistMode mode) {
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

            PlaylistSave3 pl;
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
                int errCount = ReadFileHeader(p.PathName, PcmHeaderReader.ReadHeaderMode.OnlyConcreteFile, null);
                if (0 == errCount && 0 < ap.PcmDataListForDisp.Count()) {
                    // 読み込み成功。読み込んだPcmDataの曲名、アーティスト名、アルバム名、startTick等を上書きする。

                    // pcmDataのメンバ。
                    var pcmData = ap.PcmDataListForDisp.Last();
                    pcmData.DisplayName = p.Title;
                    pcmData.AlbumTitle = p.AlbumName;
                    pcmData.ArtistName = p.ArtistName;
                    pcmData.ComposerName = p.ComposerName;
                    pcmData.StartTick = p.StartTick;
                    pcmData.EndTick = p.EndTick;
                    pcmData.CueSheetIndex = p.CueSheetIndex;
                    pcmData.TrackId = p.TrackId;

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
            if (0 < m_loadErrorMessages.Length) {
                AddLogText(m_loadErrorMessages.ToString());
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
            var s = new PlaylistSave3();

            for (int i=0; i<ap.PcmDataListForDisp.Count(); ++i) {
                var p = ap.PcmDataListForDisp.At(i);
                var playListItem = m_playListItems[i];

                s.Add(new PlaylistItemSave3().Set(
                        p.DisplayName, p.AlbumTitle, p.ArtistName, p.ComposerName, p.FullPath,
                        p.CueSheetIndex, p.StartTick, p.EndTick, playListItem.ReadSeparaterAfter, p.LastWriteTime, p.TrackId));
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

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

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

            int hr = ap.WasapiInit();
            AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.Init() {0:X8}{1}", hr, Environment.NewLine));

            m_wasapiStateChangedDelegate = new Wasapi.WasapiCS.StateChangedCallback(WasapiStatusChanged);
            ap.wasapi.RegisterStateChangedCallback(m_wasapiStateChangedDelegate);

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

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private const int WM_DEVICECHANGE = 0x219;
        private const uint DBT_DEVICEREMOVECOMPLETE = 0x8004u;

        // WM_DEVICECHANGE イベントを取得する。
        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParamP, IntPtr lParamP, ref bool handled) {
            uint wParam = (uint)wParamP.ToInt64();

            switch (msg) {
            case WM_DEVICECHANGE:
                if (wParam == DBT_DEVICEREMOVECOMPLETE) {
                    Console.WriteLine("WM_DEVICECHANGE DBG_DEVICEREMOVECOMPLETE");
                    FileDisappearedEventProc("");
                }
                break;
            }
            
            return IntPtr.Zero;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

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
            dataGridColumnComposerName.Header = Properties.Resources.MainDataGridColumnComposer;
            dataGridColumnBitRate.Header = Properties.Resources.MainDataGridColumnBitRate;
            dataGridColumnDuration.Header = Properties.Resources.MainDataGridColumnDuration;
            dataGridColumnIndexNr.Header = Properties.Resources.MainDataGridColumnIndexNr;

            dataGridColumnNumChannels.Header = Properties.Resources.MainDataGridColumnNumChannels;
            dataGridColumnQuantizationBitRate.Header = Properties.Resources.MainDataGridColumnQuantizationBitRate;
            dataGridColumnReadSeparaterAfter.Header = Properties.Resources.MainDataGridColumnReadSeparaterAfter;
            dataGridColumnSampleRate.Header = Properties.Resources.MainDataGridColumnSampleRate;
            dataGridColumnTitle.Header = Properties.Resources.MainDataGridColumnTitle;
            dataGridColumnFileExtension.Header = Properties.Resources.MainDataGridColumnFileExtension;

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
                    Console.WriteLine("E: unknown playlist column name {0}", item.Name);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }
                columnIdxList.Add(idx);
            }

            if (columnIdxList.Count != dataGridPlayList.Columns.Count) {
                Console.WriteLine("E: playlist column count mismatch {0}", columnIdxList.Count);
                System.Diagnostics.Debug.Assert(false);
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
            } catch (System.NotSupportedException ex) {
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

            slider1.IsEnabled = true;
            labelPlayingTime.Content = PLAYING_TIME_ALLZERO;

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
                progressBar1.Visibility = System.Windows.Visibility.Collapsed;
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

                    var stat = ap.wasapi.GetSessionStatus();
                    if (WasapiCS.StreamType.DoP == stat.StreamType) {
                        statusBarText.Content = string.Format(CultureInfo.InvariantCulture, "{0} WASAPI{1} {2}kHz {3} {4}ch DoP DSD {5:F1}MHz. Audio buffer size={6:F1}ms",
                                Properties.Resources.MainStatusPlaying,
                                radioButtonShared.IsChecked == true ? Properties.Resources.Shared : Properties.Resources.Exclusive,
                                stat.DeviceSampleRate * 0.001,
                                SampleFormatTypeToStr(stat.DeviceSampleFormat),
                                stat.DeviceNumChannels, stat.DeviceSampleRate * 0.000016,
                                1000.0 * stat.EndpointBufferFrameNum / stat.DeviceSampleRate);
                    } else {
                        statusBarText.Content = string.Format(CultureInfo.InvariantCulture, "{0} WASAPI{1} {2}kHz {3} {4}ch PCM {5:F2}dB. Audio buffer size={6:F1}ms",
                                Properties.Resources.MainStatusPlaying,
                                radioButtonShared.IsChecked == true ? Properties.Resources.Shared : Properties.Resources.Exclusive,
                                stat.DeviceSampleRate * 0.001,
                                SampleFormatTypeToStr(stat.DeviceSampleFormat),
                                stat.DeviceNumChannels,
                                20.0 * Math.Log10(ap.wasapi.GetScalePcmAmplitude()),
                                1000.0 * stat.EndpointBufferFrameNum / stat.DeviceSampleRate);
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
                if (radioButtonShared.IsChecked == true) {
                    // 共有モード。
                    statusBarText.Content = Properties.Resources.MainStatusReadingFiles;
                } else {
                    // 排他モード。
                    switch (m_deviceSetupParams.StreamType) {
                    case WasapiCS.StreamType.PCM:
                        statusBarText.Content = Properties.Resources.MainStatusReadingFiles;
                        break;
                    case WasapiCS.StreamType.DoP:
                        statusBarText.Content = Properties.Resources.MainStatusReadingFilesDoP;
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                    }
                }
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

            hr = ap.wasapi.EnumerateDevices(WasapiCS.DeviceType.Play);
            AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.DoDeviceEnumeration(Play) {0:X8}{1}", hr, Environment.NewLine));

            int nDevices = ap.wasapi.GetDeviceCount();
            for (int i = 0; i < nDevices; ++i) {
                var attr = ap.wasapi.GetDeviceAttributes(i);
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
        /// 再生中の場合は、停止。
        /// 再生中でない場合は、再生停止後イベントtaskAfterStopをここで実行する。
        /// 再生中の場合は、停止完了後にtaskAfterStopを実行する。
        /// </summary>
        /// <param name="taskAfterStop"></param>
        void Stop(NextTask taskAfterStop, bool stopGently) {
            m_taskAfterStop = taskAfterStop;

            if (ap.IsPlayWorkerBusy()) {
                ap.PlayStop(stopGently);
                // 再生停止したらPlayRunWorkerCompletedでイベントを開始する。
            } else {
                // 再生停止後イベントをここで、いますぐ開始。
                PerformPlayCompletedTask();
            }
        }

        void StopBlocking()
        {
            Stop(new NextTask(NextTaskType.None), false);
            m_readFileWorker.CancelAsync();

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
            System.Diagnostics.Debug.Assert(!ap.IsPlayWorkerBusy());

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

            try {
                DeleteKeyListener();
                FileDisappearCheck.Clear();

                if (ap.wasapi != null) {
                    // バックグラウンドスレッドにjoinして、完全に止まるまで待ち合わせするブロッキング版のStopを呼ぶ。
                    // そうしないと、バックグラウンドスレッドによって使用中のオブジェクトが
                    // この後のUnsetupの呼出によって開放されてしまい問題が起きる。
                    ap.SetPlayEventCallback(null);
                    StopBlocking();
                    UnsetupDevice();
                    ap.WasapiTerm();

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

            } catch (System.Exception ex) {
                Console.WriteLine("{0}", ex);
            }
        }

        private void Exit() {
            Term();
            // Application.Current.Shutdown();
            Close();
        }

        /// <summary>
        /// ap.wasapi.Unsetupを行う。
        /// 既にUnsetup状態の場合は、空振りする。
        /// </summary>
        private void UnsetupDevice() {
            if (!m_deviceSetupParams.IsSetuped()) {
                return;
            }

            ap.wasapi.Unsetup();
            AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.Unsetup(){0}", Environment.NewLine));
            m_deviceSetupParams.Unsetuped();
        }

        private int PcmChannelsToSetupChannels(int numChannels) {
            int ch = numChannels;

            if (m_preference.AddSilentForEvenChannel) {
                // 偶数チャンネルに繰り上げする。
                ch = (ch + 1) & (~1);
            }

            // モノラル1chのPCMデータはMonoToStereo()によってステレオ2chに変換してから再生する。
            if (1 == numChannels) {
                ch = 2;
            }

            switch (m_preference.ChannelCount2) {
            case ChannelCount2Type.SourceChannelCount:
                break;
            case ChannelCount2Type.Ch2:
            case ChannelCount2Type.Ch4:
            case ChannelCount2Type.Ch6:
            case ChannelCount2Type.Ch8:
            case ChannelCount2Type.Ch10:
            case ChannelCount2Type.Ch16:
            case ChannelCount2Type.Ch18:
            case ChannelCount2Type.Ch24:
            case ChannelCount2Type.Ch26:
            case ChannelCount2Type.Ch32:
                // チャンネル数変更。
                ch = (int)m_preference.ChannelCount2;
                break;
            case ChannelCount2Type.MixFormatChannelCount: {
                    // ミックスフォーマットのチャンネル数にする。
                    var mixFormat = ap.wasapi.GetMixFormat(listBoxDevices.SelectedIndex);
                    if (mixFormat == null) {
                        // 異常だが、この後ログが出るのでここではスルーする。
                        ch = 2;
                    } else {
                        ch = mixFormat.numChannels;
                    }
                }
                break;
            }

            return ch;
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

            int startWavDataId = ap.PcmDataListForPlay.GetFirstPcmDataIdOnGroup(loadGroupId);
            System.Diagnostics.Debug.Assert(0 <= startWavDataId);

            var startPcmData = ap.PcmDataListForPlay.FindById(startWavDataId);

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
                        startPcmData.SampleDataType == PcmData.DataType.DoP ? WasapiCS.StreamType.DoP : WasapiCS.StreamType.PCM,
                        m_preference.MMThreadPriority)) {
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
                        startPcmData.SampleDataType == PcmData.DataType.DoP ? WasapiCS.StreamType.DoP : WasapiCS.StreamType.PCM,
                        m_preference.MMThreadPriority);

                int channelMask = WasapiCS.GetTypicalChannelMask(m_deviceSetupParams.NumChannels);

                int hr = ap.wasapi.Setup(
                        useDeviceId, WasapiCS.DeviceType.Play,
                        m_deviceSetupParams.StreamType, m_deviceSetupParams.SampleRate, m_deviceSetupParams.SampleFormat,
                        m_deviceSetupParams.NumChannels, channelMask,
                        GetMMCSSCallType(), m_preference.MMThreadPriority,
                        PreferenceSchedulerTaskTypeToWasapiCSSchedulerTaskType(m_deviceSetupParams.ThreadTaskType),
                        PreferenceShareModeToWasapiCSShareMode(m_deviceSetupParams.SharedOrExclusive), PreferenceDataFeedModeToWasapiCS(m_deviceSetupParams.DataFeedMode),
                        m_deviceSetupParams.LatencyMillisec, m_deviceSetupParams.ZeroFlushMillisec, m_preference.TimePeriodHundredNanosec,
                        m_preference.IsFormatSupportedCall);
                AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.Setup({0} {1}kHz {2} {3}ch {4} {5} {6} latency={7}ms zeroFlush={8}ms timePeriod={9}ms mmThreadPriority={10}) channelMask=0x{11:X8} {12:X8}{13}",
                        m_deviceSetupParams.StreamType, m_deviceSetupParams.SampleRate * 0.001, m_deviceSetupParams.SampleFormat,
                        m_deviceSetupParams.NumChannels, m_deviceSetupParams.ThreadTaskType, 
                        m_deviceSetupParams.SharedOrExclusive, m_deviceSetupParams.DataFeedMode,
                        m_deviceSetupParams.LatencyMillisec, m_deviceSetupParams.ZeroFlushMillisec, 
                        m_preference.TimePeriodHundredNanosec * 0.0001, m_preference.MMThreadPriority,
                        channelMask, hr, Environment.NewLine));
                if (0 <= hr) {
                    // 成功
                    break;
                }

                // 失敗
                UnsetupDevice();
                if (i == (candidateNum - 1)) {
                    string s = string.Format(CultureInfo.InvariantCulture, "{0}: ap.wasapi.Setup({1} {2}kHz {3} {4}ch {5} {6}ms {7} {8}) {9} {10:X8} {11}{13}{13}{12}",
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
                            WasapiCS.GetErrorMessage(hr),
                            Properties.Resources.SetupFailAdvice,
                            Environment.NewLine);
                    MessageBox.Show(s);
                    return false;
                }
            }

            {
                var stat = ap.wasapi.GetSessionStatus();
                AddLogText(string.Format(CultureInfo.InvariantCulture, "Endpoint buffer size = {0} frames.{1}",
                        stat.EndpointBufferFrameNum, Environment.NewLine));

                var attr = ap.wasapi.GetDeviceAttributes(useDeviceId);
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
            ap.ClearPlayList();

            m_playListItems.Clear();
            PlayListItemInfo.SetNextRowId(1);

            FileDisappearCheck.Clear();

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

        /// <summary>
        /// ファイルヘッダを読んでメタ情報を抽出する。
        /// </summary>
        /// <returns>エラー発生回数。mode == OnlyConcreteFileの時、0: 成功、1: 失敗。</returns>
        int ReadFileHeader(string path, PcmHeaderReader.ReadHeaderMode mode, PlaylistTrackInfo plti) {
            PcmHeaderReader phr = new PcmHeaderReader(Encoding.GetEncoding(m_preference.CueEncodingCodePage),
                    m_preference.SortDropFolder, (pcmData, readSeparatorAfter, readFromPpwPlaylist) => {
                        // PcmDataのヘッダが読み込まれた時。再生リストに追加する。

                        if (0 < ap.PcmDataListForDisp.Count()
                            && !ap.PcmDataListForDisp.Last().IsSameFormat(pcmData)) {
                            // 1個前のファイルとデータフォーマットが異なる。
                            // Setupのやり直しになるのでファイルグループ番号を変える。
                            ++m_groupIdNextAdd;
                        }

                        pcmData.Id = ap.PcmDataListForDisp.Count();
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

                        var pli = new PlayListItemInfo(pcmData, new FileDisappearCheck.FileDisappearedEventHandler(FileDisappearedEvent));

                        if (readSeparatorAfter) {
                            pli.ReadSeparaterAfter = true;
                            ++m_groupIdNextAdd;
                        }

                        ap.PcmDataListForDisp.Add(pcmData);
                        m_playListItems.Add(pli);

                        pli.PropertyChanged += new PropertyChangedEventHandler(PlayListItemInfoPropertyChanged);
                    });
            int nError = phr.ReadFileHeader(path, mode, plti);
            if (phr.ErrorMessageList().Count != 0) {
                foreach (string s in phr.ErrorMessageList()) {
                    m_loadErrorMessages.Append(s);
                    m_loadErrorMessages.Append("\n");
                }
            }
            return nError;
        }

        /// <summary>
        /// 再生リストを調べて消えたファイルを消す。
        /// </summary>
        /// <returns>リストから消したファイルの個数。</returns>
        private int RemoveDisappearedFilesFromPlayList(string path) {
            List<int> items = new List<int>();

            for (int i=0; i < m_playListItems.Count; ++i) {
                var pli = m_playListItems[i];
                if (!System.IO.File.Exists(pli.Path)) {
                    items.Add(i);
                }
            }

            if (items.Count == 0) {
                // 消すものはない。
                return 0;
            }

            RemovePlaylistItems(items);
            m_FileDisappearedProcAfter = false;

            AddLogText(Properties.Resources.SomeFilesAreDisappeared + "\n");

            return items.Count;
        }

        private bool m_FileDisappearedProcAfter;

        private void FileDisappearedEventProc(string path) {
            Dispatcher.BeginInvoke(new Action(delegate() {
                switch (m_state) {
                case State.再生リストあり:
                    RemoveDisappearedFilesFromPlayList(path);
                    break;
                default:
                    m_FileDisappearedProcAfter = true;
                    break;
                }
            }));
        }

        private void FileDisappearedEvent(string path) {
            Console.WriteLine("FileDisappeared {0}", path);

            FileDisappearedEventProc(path);
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

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
                AddLogText(m_loadErrorMessages.ToString());
                MessageBox.Show(m_loadErrorMessages.ToString(), Properties.Resources.ReadFailedFiles, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            m_loadErrorMessages = null;

            if (0 < m_playListItems.Count) {
                ChangeState(State.再生リストあり);
            }
            UpdateUIStatus();
        }

        private void MenuItemFileSaveCueAs_Click(object sender, RoutedEventArgs e) {
            if (ap.PcmDataListForDisp.Count() == 0) {
                MessageBox.Show(Properties.Resources.NothingToStore);
                return;
            }

            System.Diagnostics.Debug.Assert(0 < ap.PcmDataListForDisp.Count());
            var pcmData0 = ap.PcmDataListForDisp.At(0);

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
                var pcmData = ap.PcmDataListForDisp.At(i);

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
            if (ap.PcmDataListForDisp.Count() == 0) {
                MessageBox.Show(Properties.Resources.NothingToStore);
                return;
            }

            System.Diagnostics.Debug.Assert(0 < ap.PcmDataListForDisp.Count());
            var pcmData0 = ap.PcmDataListForDisp.At(0);

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
                    AddLogText(m_loadErrorMessages.ToString());
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
                System.Diagnostics.Process.Start("http://sourceforge.net/projects/playpcmwin/");
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
        /// ビットフォーマット変換クラス。
        /// ノイズシェイピングのerror値を持っているので都度作らないようにする。
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
            
            //Console.WriteLine("D: ReadFileDoWork({0}) started", readGroupId);

            PcmReader.CalcMD5SumIfAvailable = m_preference.VerifyFlacMD5Sum;

            ReadFileRunWorkerCompletedArgs r = new ReadFileRunWorkerCompletedArgs();
            try {
                r.hr = -1;
                r.message = string.Empty;

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                m_readProgressInfo = new ReadProgressInfo(
                        0, 0, 0, 0, ap.PcmDataListForPlay.CountPcmDataOnPlayGroup(readGroupId));

                ap.wasapi.ClearPlayList();

                mPcmUtil = new PcmUtil(ap.PcmDataListForPlay.At(0).NumChannels);

                ap.wasapi.AddPlayPcmDataStart();
                for (int i = 0; i < ap.PcmDataListForPlay.Count(); ++i) {
                    PcmDataLib.PcmData pd = ap.PcmDataListForPlay.At(i);
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
                r.hr = ap.wasapi.ResampleIfNeeded(m_deviceSetupParams.ResamplerConversionQuality);
                if (r.hr < 0) {
                    r.message = "Resample({0}) failed! " + string.Format(CultureInfo.InvariantCulture, "0x{1:X8}", m_deviceSetupParams.ResamplerConversionQuality, r.hr);
                    args.Result = r;
                    return;
                }

                ap.wasapi.ScalePcmAmplitude(1.0);
                if (m_preference.ReduceVolume) {
                    // PCMの音量を6dB下げる。
                    // もしもDSDの時は下げない。
                    double scale = m_preference.ReduceVolumeScale();
                    ap.wasapi.ScalePcmAmplitude(scale);
                } else if (m_preference.WasapiSharedOrExclusive == WasapiSharedOrExclusiveType.Shared
                        && m_preference.SootheLimiterApo) {
                    // Limiter APO対策の音量制限。
                    double maxAmplitude = ap.wasapi.ScanPcmMaxAbsAmplitude();
                    if (SHARED_MAX_AMPLITUDE < maxAmplitude) {
                        m_readFileWorker.ReportProgress(95, string.Format(CultureInfo.InvariantCulture, "Scaling amplitude by {0:0.000}dB ({1:0.000}x) to soothe Limiter APO...{2}",
                                20.0 * Math.Log10(SHARED_MAX_AMPLITUDE / maxAmplitude), SHARED_MAX_AMPLITUDE / maxAmplitude, Environment.NewLine));
                        ap.wasapi.ScalePcmAmplitude(SHARED_MAX_AMPLITUDE / maxAmplitude);
                    }
                }

                ap.wasapi.AddPlayPcmDataEnd();

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
            if (endFrame < 0) {
                if (0 < pd.NumFrames) {
                    endFrame = pd.NumFrames;
                } else {
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
                    if (pr.NumFrames < endFrame) {
                        endFrame = pr.NumFrames;
                    }
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
                if (!ap.wasapi.AddPlayPcmDataAllocateMemory(pd.Id, allocBytes)) {
                    //ClearPlayList(PlayListClearMode.ClearWithoutUpdateUI); //< メモリを空ける：効果があるか怪しい
                    r.message = string.Format(CultureInfo.InvariantCulture, Properties.Resources.MemoryExhausted);
                    Console.WriteLine("D: ReadFileSingleDoWork() lowmemory");
                    return false;
                }
            }

            GC.Collect();

            bool result = true;
            if (m_preference.ParallelRead && PcmReader.IsTheFormatParallelizable(PcmReader.GuessFileFormatFromFilePath(pd.FullPath))
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

        private ReadFileResult ReadOnePcmFileFragment(
                BackgroundWorker bw,
                PcmDataLib.PcmData pd, long readStartFrame,
                long wantFramesTotal, long writeOffsFrame) {
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
                    wantFrames = (int)( wantFramesTotal - frameCount );
                }

                int readResult;
                byte[] part = pr.StreamReadOne(wantFrames, out readResult);
                if (null == part) {
                    pr.StreamEnd();
                    if (readResult < 0) {
                        return new ReadFileResultFailed(pd.Id, WWFlacRWCS.FlacRW.ErrorCodeToStr(readResult));
                    }

                    Console.WriteLine("D: ReadOnePcmFileFragment() lowmemory");
                    return lowMemoryFailed;
                }

                // 実際に読み出されたフレーム数readFrames。
                int readFrames = part.Length / ( pd.BitsPerFrame / 8 );

                //Console.WriteLine("part size = {0}", part.Length);

                pd.SetSampleLargeArray(new LargeArray<byte>(part));
                part = null;

                //Console.WriteLine("pd.SetSampleLargeArray {0}", pd.GetSampleLargeArray().LongLength);

                // 必要に応じてpartの量子化ビット数の変更処理を行い、pdAfterに新しく確保したPCMデータ配列をセット。

                var bpsConvArgs = new PcmFormatConverter.BitsPerSampleConvArgs(m_preference.BpsConvNoiseShaping);
                PcmData pdAfter = null;
                if (m_preference.WasapiSharedOrExclusive == WasapiSharedOrExclusiveType.Exclusive) {
                    pdAfter = mPcmUtil.BitsPerSampleConvAsNeeded(pd, m_deviceSetupParams.SampleFormat, bpsConvArgs);
                    pd.ForgetDataPart();
                } else {
                    pdAfter = pd;
                }

                if (pdAfter.GetSampleLargeArray() == null ||
                        0 == pdAfter.GetSampleLargeArray().LongLength) {
                    // サンプルが存在しないのでWasapiにAddしない。
                    break;
                }

                //Console.WriteLine("pdAfter.SampleLargeArray {0}", pdAfter.GetSampleLargeArray().LongLength);

                if (pdAfter.NumChannels == 1) {
                    // モノラル1ch→ステレオ2ch変換。
                    pdAfter = pdAfter.MonoToStereo();
                }
                if (m_preference.AddSilentForEvenChannel) {
                    // 偶数チャンネルにするために無音を追加。
                    pdAfter = pdAfter.AddSilentForEvenChannel();
                }

                pdAfter = pdAfter.ConvertChannelCount(m_deviceSetupParams.NumChannels);

                /*
                // これは駄目だった！もっと手前でDoPマーカーの判定をしてDoPとPCMが混在しないようにする必要がある。
                // PCMのとき、DoPマーカーが付いていたらDSDフラグを立てる。
                if (m_deviceSetupParams.SharedOrExclusive == WasapiSharedOrExclusiveType.Exclusive &&
                        pdAfter.ScanDopMarkerAndUpdate()) {
                    ap.wasapi.UpdateStreamType(pd.Id, WasapiCS.StreamType.DoP);
                }
                */

                //Console.WriteLine("pdAfter.ConvertChannelCount({0}) SampleLargeArray {1}", m_deviceSetupParams.NumChannels, pdAfter.GetSampleLargeArray().LongLength);

                long posBytes = ( writeOffsFrame + frameCount ) * pdAfter.BitsPerFrame / 8;

                bool result = false;
                lock (pd) {
                    //Console.WriteLine("ap.wasapi.AddPlayPcmDataSetPcmFragment({0}, {1} {2})", pd.Id, posBytes, pdAfter.GetSampleLargeArray().ToArray().Length);

                    result = ap.wasapi.AddPlayPcmDataSetPcmFragment(pd.Id, posBytes, pdAfter.GetSampleLargeArray().ToArray());
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
            // 1曲リピートか、または(全曲リピート再生で、GroupIdが0しかない)場合、WASAPI再生スレッドのリピート設定が可能。
            ComboBoxPlayModeType playMode = (ComboBoxPlayModeType)comboBoxPlayMode.SelectedIndex;
            if (playMode == ComboBoxPlayModeType.OneTrackRepeat
                    || (playMode == ComboBoxPlayModeType.AllTracksRepeat
                    && 0 == ap.PcmDataListForPlay.CountPcmDataOnPlayGroup(1))) {
                repeat = true;
            }
            ap.wasapi.SetPlayRepeat(repeat);
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
                // ファイル読み込みが失敗した。
                Console.WriteLine("ReadFileRunWorkerCompleted with error");
                MessageBox.Show(r.message);
                m_taskAfterStop.Set(NextTaskType.None);
            }

            if (0 < r.individualResultList.Count) {
                foreach (var fileResult in r.individualResultList) {
                    AddLogText(fileResult.ToString(ap.PcmDataListForPlay.FindById(fileResult.PcmDataId).FileName));
                }
            }

            // WasapiCSのリピート設定。
            UpdatePlayRepeat();

            switch (m_taskAfterStop.Type) {
            case NextTaskType.PlaySpecifiedGroup:
            case NextTaskType.PlayPauseSpecifiedGroup:
                // ファイル読み込み完了後、再生を開始する。
                // 再生するファイルは、タスクで指定されたファイル。
                // このwavDataIdは、再生開始ボタンが押された時点で選択されていたファイル。
                int wavDataId = m_taskAfterStop.PcmDataId;

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
                // 再生断念。
                ChangeState(State.再生リストあり);
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
            //Console.WriteLine("StartReadFiles({0})", loadGroupId);

            progressBar1.Visibility = System.Windows.Visibility.Visible;
            progressBar1.Value = 0;

            m_loadingGroupId = loadGroupId;
            
            m_readFileWorker.RunWorkerAsync(loadGroupId);
        }

        private void ButtonPlayClicked() {
            var di = listBoxDevices.SelectedItem as DeviceAttributes;
            if (!UseDevice()) {
                return;
            }

            if (IsPlayModeShuffle()) {
                // シャッフル再生する
                ap.CreateShuffledPlayList();
                ReadStartPlayByWavDataId(ap.PcmDataListForPlay.At(0).Id);
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
                ap.CreateOneTrackPlayList(wavDataId);
                ReadStartPlayByWavDataId(wavDataId);
                return;
            }

            // 全曲再生
            ap.CreateAllTracksPlayList();
            ReadStartPlayByWavDataId(wavDataId);
        }

        private void ButtonPauseClicked() {
            int hr = 0;

            switch (m_state) {
            case State.再生中:
                hr = ap.wasapi.Pause();
                AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.Pause() {0:X8}{1}", hr, Environment.NewLine));
                if (0 <= hr) {
                    ChangeState(State.再生一時停止中);
                    UpdateUIStatus();
                } else {
                    // Pause失敗＝すでに再生していない または再生一時停止ができない状況。ここで状態遷移する必要はない。
                }
                break;
            case State.再生一時停止中:
                hr = ap.wasapi.Unpause();
                AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.Unpause() {0:X8}{1}", hr, Environment.NewLine));
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

            NextTaskType nextTask = NextTaskType.PlaySpecifiedGroup;
            if (m_taskAfterStop.Type != NextTaskType.None) {
                nextTask = m_taskAfterStop.Type;
            }

            var pcmData = ap.PcmDataListForPlay.FindById(wavDataId);
            if (null == pcmData) {
                // 1曲再生モードの時。再生リストを作りなおす。
                ap.CreateOneTrackPlayList(wavDataId);
                m_loadedGroupId = -1;
                pcmData = ap.PcmDataListForPlay.FindById(wavDataId);
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

                m_taskAfterStop.Set(nextTask, pcmData.GroupId, pcmData.Id);
                StartReadPlayGroupOnTask();
                return true;
            }

            // wavDataIdのグループがm_LoadedGroupIdである。ロードされている。
            // 連続再生フラグの設定と、現在のグループが最後のグループかどうかによって
            // m_LoadedGroupIdの再生が自然に完了したら、行うタスクを決定する。
            UpdateNextTask();

            if (!SetupDevice(pcmData.GroupId) ||
                    !StartPlay(wavDataId)) {
                //dataGridPlayList.SelectedIndex = 0;
                ChangeState(State.ファイル読み込み完了);

                DeviceDeselect();
                UpdateDeviceList();
                return false;
            }

            if (nextTask == NextTaskType.PlayPauseSpecifiedGroup) {
                ButtonPauseClicked();
            }
            return true;
        }

        /// <summary>
        /// 現在のグループの最後のファイルの再生が終わった後に行うタスクを判定し、
        /// m_taskにセットする。
        /// </summary>
        private void UpdateNextTask() {
            if (0 == ap.PcmDataListForPlay.CountPcmDataOnPlayGroup(1)) {
                // ファイルグループが1個しかない場合、
                // wasapiUserの中で自発的にループ再生する。
                // ファイルの再生が終わった=停止。
                m_taskAfterStop.Set(NextTaskType.None);
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

            if (0 < ap.PcmDataListForPlay.CountPcmDataOnPlayGroup(nextGroupId)) {
                m_taskAfterStop.Set(NextTaskType.PlaySpecifiedGroup, nextGroupId, ap.PcmDataListForPlay.GetFirstPcmDataIdOnGroup(nextGroupId));
                return;
            }

            if (IsPlayModeRepeat()) {
                m_taskAfterStop.Set(NextTaskType.PlaySpecifiedGroup, 0, 0);
                return;
            }

            m_taskAfterStop.Set(NextTaskType.None);
        }

        /// <summary>
        /// ただちに再生を開始する。
        /// wavDataIdのGroupが、ロードされている必要がある。
        /// </summary>
        /// <returns>false: 再生開始できなかった。</returns>
        private bool StartPlay(int wavDataId) {
            System.Diagnostics.Debug.Assert(0 <= wavDataId);
            var playPcmData = ap.PcmDataListForPlay.FindById(wavDataId);
            if (playPcmData.GroupId != m_loadedGroupId) {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            ChangeState(State.再生中);
            UpdateUIStatus();

            m_sw.Reset();
            m_sw.Start();

            // 再生バックグラウンドタスク開始。PlayDoWorkが実行される。
            // 再生バックグラウンドタスクを止めるには、Stop()を呼ぶ。
            // 再生バックグラウンドタスクが止まったらPlayRunWorkerCompletedが呼ばれる。
            int hr = ap.StartPlayback(wavDataId, new AudioPlayer.PlayEventCallback(PlayEventHandler));
            {
                var stat = ap.wasapi.GetWorkerThreadSetupResult();

                if (m_preference.RenderThreadTaskType != RenderThreadTaskType.None) {
                    AddLogText(string.Format(CultureInfo.InvariantCulture, "AvSetMMThreadCharacteristics({0}) result={1:X8}{2}",
                        m_preference.RenderThreadTaskType, stat.AvSetMmThreadCharacteristicsResult, Environment.NewLine));
                    

                    if (m_preference.MMThreadPriority != WasapiCS.MMThreadPriorityType.None) {
                        AddLogText(string.Format(CultureInfo.InvariantCulture, "AvSetMMThreadPriority({0}) result={1:X8}{2}",
                            m_preference.MMThreadPriority, stat.AvSetMmThreadPriorityResult, Environment.NewLine));
                    }
                }

                if (m_preference.DwmEnableMmcssCall) {
                    AddLogText(string.Format(CultureInfo.InvariantCulture, "DwmEnableMMCSS({0}) result={1:X8}{2}",
                        m_preference.DwmEnableMmcss, stat.DwmEnableMMCSSResult, Environment.NewLine));
                }
            }

            AddLogText(string.Format(CultureInfo.InvariantCulture,
                    "ap.wasapi.StartPlayback({0}) {1:X8}{2}", wavDataId, hr, Environment.NewLine));
            if (hr < 0) {
                m_taskAfterStop.Set(NextTaskType.None);

                MessageBox.Show(string.Format(CultureInfo.InvariantCulture,
                        Properties.Resources.PlayStartFailed + "！{0:X8}  {1}", hr, WasapiCS.GetErrorMessage(hr)));
                ap.PlayStop(false);
                return false;
            }

            return true;
        }

        private void PlayEventHandler(AudioPlayer.PlayEvent ev) {
            switch (ev.eventType) {
            case AudioPlayer.PlayEventType.ProgressChanged:
                PlayProgressChanged(ev);
                break;
            case AudioPlayer.PlayEventType.Finished:
            case AudioPlayer.PlayEventType.Canceled:
                PlayRunWorkerCompleted(ev);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        long mLastSliderPositionUpdateTime = 0;

        /// <summary>
        /// 再生の進行状況をUIに反映する。
        /// </summary>
        private void PlayProgressChanged(AudioPlayer.PlayEvent ev) {
            var bw = ev.bw;

            if (null == ap.wasapi) {
                return;
            }

            if (bw.CancellationPending) {
                // ワーカースレッドがキャンセルされているので、何もしない。
                return;
            }

            // 再生中PCMデータ(または一時停止再開時再生予定PCMデータ等)の再生位置情報を画面に表示する。
            WasapiCS.PcmDataUsageType usageType = WasapiCS.PcmDataUsageType.NowPlaying;
            int pcmDataId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (pcmDataId < 0) {
                pcmDataId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
                usageType = WasapiCS.PcmDataUsageType.PauseResumeToPlay;
            }
            if (pcmDataId < 0) {
                pcmDataId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.SpliceNext);
                usageType = WasapiCS.PcmDataUsageType.SpliceNext;
            }

            string playingTimeString = string.Empty;
            if (pcmDataId < 0) {
                playingTimeString = PLAYING_TIME_UNKNOWN;
            } else {
                if (dataGridPlayList.SelectedIndex != GetPlayListIndexOfPcmDataId(pcmDataId)) {
                    dataGridPlayList.SelectedIndex = GetPlayListIndexOfPcmDataId(pcmDataId);
                    dataGridPlayList.ScrollIntoView(dataGridPlayList.SelectedItem);
                }

                PcmDataLib.PcmData pcmData = ap.PcmDataListForPlay.FindById(pcmDataId);

                var stat    = ap.wasapi.GetSessionStatus();
                var playPos = ap.wasapi.GetPlayCursorPosition(usageType);

                long now = DateTime.Now.Ticks;
                if (now - mLastSliderPositionUpdateTime > SLIDER_UPDATE_TICKS) {
                    // スライダー位置の更新。0.5秒に1回
                    slider1.Maximum = playPos.TotalFrameNum;
                    if (!mSliderSliding || playPos.TotalFrameNum <= slider1.Value) {
                        slider1.Value = playPos.PosFrame;
                    }
                    mLastSliderPositionUpdateTime = now;
                }

                if (pcmData.TrackId != 0) {
                    // CUEシートなのでトラック番号を表示する。
                    if (pcmData.CueSheetIndex == 0) {
                        // INDEX 00区間はマイナス表示。
                        // INDEX 00区間の曲長さ表示は次の曲の長さを表示する。
                        long nextSampleRate = stat.DeviceSampleRate;
                        long nextTotalFrameNum = playPos.TotalFrameNum;
                        var nextPcmData = ap.PcmDataListForPlay.FindById(pcmDataId+1);
                        if (nextPcmData != null) {
                            nextTotalFrameNum = nextPcmData.NumFrames;
                            nextSampleRate = nextPcmData.SampleRate;
                        } else {
                            // シャッフル再生時に起こるｗｗｗｗ
                        }

                        playingTimeString = string.Format(CultureInfo.InvariantCulture, "Tr.{0:D2} -{1} / {2}",
                                pcmData.TrackId,
                                Util.SecondsToMSString((int)((playPos.TotalFrameNum + stat.DeviceSampleRate - playPos.PosFrame) / stat.DeviceSampleRate)),
                                Util.SecondsToMSString((int)(nextTotalFrameNum / nextSampleRate)));
                    } else {
                        playingTimeString = string.Format(CultureInfo.InvariantCulture, "Tr.{0:D2}  {1} / {2}",
                                pcmData.TrackId,
                                Util.SecondsToMSString((int)(playPos.PosFrame / stat.DeviceSampleRate)),
                                Util.SecondsToMSString((int)(playPos.TotalFrameNum / stat.DeviceSampleRate)));
                    }
                } else {
                    playingTimeString = string.Format(CultureInfo.InvariantCulture, "{0} / {1}",
                            Util.SecondsToMSString((int)(playPos.PosFrame / stat.DeviceSampleRate)),
                            Util.SecondsToMSString((int)(playPos.TotalFrameNum / stat.DeviceSampleRate)));
                }
            }

            // 再生時間表示の再描画をできるだけ抑制する。負荷が減る効果がある
            if (playingTimeString != string.Empty && 0 != string.Compare((string)labelPlayingTime.Content, playingTimeString)) {
                labelPlayingTime.Content = playingTimeString;
            } else {
                //System.Console.WriteLine("time disp update skipped");
            }
        }

        /// <summary>
        /// m_taskに指定されているグループをロードし、ロード完了したら指定ファイルを再生開始する。
        /// ファイル読み込み完了状態にいるときに呼ぶ。
        /// </summary>
        private void StartReadPlayGroupOnTask() {
            m_loadedGroupId = -1;

            switch (m_taskAfterStop.Type) {
            case NextTaskType.PlaySpecifiedGroup:
            case NextTaskType.PlayPauseSpecifiedGroup:
                break;
            default:
                // 想定されていない状況
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            // 再生状態→再生グループ切り替え中状態に遷移。
            ChangeState(State.再生グループ読み込み中);
            UpdateUIStatus();

            StartReadFiles(m_taskAfterStop.GroupId);
        }

        private bool PerformTaskAfterStop() {
            // 再生終了後に行うタスクがある場合、ここで実行する。
            switch (m_taskAfterStop.Type) {
            case NextTaskType.PlaySpecifiedGroup:
            case NextTaskType.PlayPauseSpecifiedGroup:
                UnsetupDevice();

                if (IsPlayModeOneTrack()) {
                    // 1曲再生モードの時、再生リストを作りなおす。
                    ap.CreateOneTrackPlayList(m_taskAfterStop.PcmDataId);
                }

                if (null == m_pliUpdatedByUserSelectWhileLoading) {
                    // 次に再生する曲を選択状態にする。
                    dataGridPlayList.SelectedIndex =
                        GetPlayListIndexOfPcmDataId(m_taskAfterStop.PcmDataId);

                    UpdateUIStatus();
                }

                if (SetupDevice(m_taskAfterStop.GroupId)) {
                    StartReadPlayGroupOnTask();
                    return true;
                }

                // デバイスの設定を試みたら、失敗した。
                // FALL_THROUGHする。
                break;
            default:
                break;
            }

            return false;
        }

        /// <summary>
        /// 再生終了後タスクを実行する。
        /// </summary>
        private void PerformPlayCompletedTask() {
            if (m_FileDisappearedProcAfter && 0 < RemoveDisappearedFilesFromPlayList("")) {
                // 1個以上ファイルが消えた。再生終了後タスクを実行せずに停止する。
                
                m_taskAfterStop.Type = NextTaskType.None;
            } else {
                bool rv = PerformTaskAfterStop();

                if (rv) {
                    // 次の再生が始まる。
                    return;
                }
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

            GC.Collect();
        }

        /// <summary>
        /// 再生終了。
        /// </summary>
        private void PlayRunWorkerCompleted(AudioPlayer.PlayEvent ev) {
            m_sw.Stop();

            if (ev.eventType == AudioPlayer.PlayEventType.Canceled) {
                // 再生中に×ボタンを押すとここに来る。
                // 再生中に次の曲ボタンを押した場合もここに来る。
                Console.WriteLine("PlayRunWorkerCompleted with cancel");
            }

            if (ev.ercd < 0) {
                AddLogText(string.Format(CultureInfo.InvariantCulture, "Error: play stopped with error {0:X8} {1}{2}",
                    ev.ercd, WasapiCS.GetErrorMessage(ev.ercd), Environment.NewLine));
                return;
            }

            AddLogText(string.Format(CultureInfo.InvariantCulture, Properties.Resources.PlayCompletedElapsedTimeIs + " {0}{1}", m_sw.Elapsed, Environment.NewLine));
            PerformPlayCompletedTask();
        }

        private void ButtonStopClicked() {
            ChangeState(State.再生停止開始);
            UpdateUIStatus();

            // 停止ボタンで停止した場合は、停止後何もしない。
            Stop(new NextTask(NextTaskType.None), true);
            AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.Stop(){0}", Environment.NewLine));
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
                    ap.wasapi.SetPosFrame((long)slider1.Value);
                }
            }
        }
        private void slider1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.Source != slider1) {
                return;
            }

            if (!buttonPlay.IsEnabled &&
                    mLastSliderValue != (long)slider1.Value) {
                ap.wasapi.SetPosFrame((long)slider1.Value);
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

        static readonly int[] gInspectNumChannels = new int[] {
                2,
                4,
                6,
                8,
        };

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
            var attr = ap.wasapi.GetDeviceAttributes(listBoxDevices.SelectedIndex);

            AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.InspectDevice()\r\nDeviceFriendlyName={0}\r\nDeviceIdString={1}{2}",
                attr.Name, attr.DeviceIdString, Environment.NewLine));

            foreach (int numChannels in gInspectNumChannels) {
                int channelMask = WasapiCS.GetTypicalChannelMask(numChannels);
                AddLogText(string.Format(CultureInfo.InvariantCulture,
                        "Num of channels={0}, dwChannelMask=0x{1:X}:\n", numChannels, channelMask));

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
                        int hr = ap.wasapi.InspectDevice(listBoxDevices.SelectedIndex,
                                WasapiCS.DeviceType.Play, ifmt.sampleRate,
                                WasapiCS.BitAndFormatToSampleFormatType(ifmt.bitsPerSample, ifmt.validBitsPerSample, ifmt.bitFormat), numChannels, channelMask);
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "|| {0} {1:X8} ", hr==0 ? "OK" : "NA", hr));
                    }
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "||{0}", Environment.NewLine));
                    AddLogText(sb.ToString());
                    AddLogText(string.Format(CultureInfo.InvariantCulture, "++-------------++-------------++-------------++-------------++-------------++-------------++-------------++-------------++{0}", Environment.NewLine));
                }
                AddLogText("\n");
            }

            var mixFormat = ap.wasapi.GetMixFormat(listBoxDevices.SelectedIndex);
            if (mixFormat == null) {
                AddLogText(string.Format(CultureInfo.InvariantCulture, "IAudioClient::GetMixFormat() failed!\n"));
            } else {
                AddLogText(string.Format(CultureInfo.InvariantCulture, "IAudioClient::GetMixFormat()\n  {0}Hz {2}ch {1}, dwChannelMask=0x{3:X}\n",
                    mixFormat.sampleRate, mixFormat.sampleFormat, mixFormat.numChannels, mixFormat.dwChannelMask));
            }

            var devicePeriod = ap.wasapi.GetDevicePeriod(listBoxDevices.SelectedIndex);
            if (devicePeriod == null) {
                AddLogText(string.Format(CultureInfo.InvariantCulture, "IAudioClient::GetDevicePeriod() failed!\n"));
            } else {
                AddLogText(string.Format(CultureInfo.InvariantCulture, "IAudioClient::GetDevicePeriod()\n  default={0}ms, min={1}ms\n",
                    devicePeriod.defaultPeriod / 10000.0,
                    devicePeriod.minimumPeriod / 10000.0));
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

        /// <summary>
        /// ログを追加する。
        /// </summary>
        /// <param name="s">追加するログ。行末に\r\nを入れる必要あり。</param>
        private void AddLogText(string s) {
            // Console.Write(s);

            // ログを適当なエントリ数で流れるようにする。
            // sは複数行の文字列が入っていたり、改行が入っていなかったりするので、行数制限にはなっていない。
            m_logList.Add(s);
            while (LOG_LINE_NUM < m_logList.Count) {
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
        private void ChangePlayWavDataById(int wavDataId, NextTaskType nextTask) {
            System.Diagnostics.Debug.Assert(0 <= wavDataId);

            var playingId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            var pauseResumeId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
            if (playingId < 0 && pauseResumeId < 0 && 0 <= m_loadingGroupId) {
                // 再生中でなく、ロード中の場合。
                // ロード完了後ReadFileRunWorkerCompleted()で再生する曲を切り替えるための
                // 情報をセットする。
                m_pliUpdatedByUserSelectWhileLoading = m_playListItems[dataGridPlayList.SelectedIndex];
                return;
            }

            if (playingId < 0 && pauseResumeId < 0) {
                // 再生中でなく、再生一時停止中でなく、ロード中でもない場合。
                ap.wasapi.UpdatePlayPcmDataById(wavDataId);
                return;
            }

            // 再生中か再生一時停止中である。
            var pcmData = ap.PcmDataListForPlay.FindById(wavDataId);
            if (null == pcmData) {
                // 再生リストの中に次に再生する曲が見つからない。1曲再生の時起きる。
                Stop(new NextTask(nextTask, 0, wavDataId), true);
                return;
            }

            var groupId = pcmData.GroupId;

            var playPcmData = ap.PcmDataListForPlay.FindById(playingId);
            if (playPcmData == null) {
                playPcmData = ap.PcmDataListForPlay.FindById(pauseResumeId);
            }
            if (playPcmData.GroupId == groupId) {
                // 同一ファイルグループのファイルの場合、すぐにこの曲が再生可能。
                ap.wasapi.UpdatePlayPcmDataById(wavDataId);
                AddLogText(string.Format(CultureInfo.InvariantCulture, "ap.wasapi.UpdatePlayPcmDataById({0}){1}", wavDataId, Environment.NewLine));
            } else {
                // ファイルグループが違う場合、再生を停止し、グループを読み直し、再生を再開する。
                Stop(new NextTask(nextTask, groupId, wavDataId), true);
            }
        }

        /// <summary>
        /// dataGridPlayListの項目(トラック)を削除する。
        /// 項目番号はap.PcmDataListForDispの番号でもある。
        /// </summary>
        private void RemovePlaylistItems(List<int> items) {
            if (0 == items.Count) {
                return;
            }

            if (items.Count == m_playListItems.Count) {
                // すべて消える。再生開始などが出来なくなるので別処理。
                ClearPlayList(PlayListClearMode.ClearWithUpdateUI);
                return;
            }

            {
                // 再生リストの一部項目が消える。
                // PcmDataのIDが飛び飛びになるので番号を振り直す。
                // PcmDataのGroupIdも飛び飛びになるが、特に問題にならないようなので付け直さない。
                items.Sort();

                for (int i=items.Count - 1; 0 <= i; --i) {
                    int idx = items[i];
                    ap.PcmDataListForDisp.RemoveAt(idx);
                    m_playListItems.RemoveAt(idx);
                    // dataGridPlayList.UpdateLayout();
                }

                GC.Collect();

                for (int i = 0; i < ap.PcmDataListForDisp.Count(); ++i) {
                    ap.PcmDataListForDisp.At(i).Id = i;
                    ap.PcmDataListForDisp.At(i).Ordinal = i;
                }

                dataGridPlayList.UpdateLayout();

                UpdateUIStatus();
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
            var items = new List<int>();
            items.Add(dataGridPlayList.SelectedIndex);

            RemovePlaylistItems(items);
        }

        private delegate int UpdateOrdinal(int v);

        private void buttonNextOrPrevClickedWhenPlaying(UpdateOrdinal updateOrdinal) {
            NextTaskType nextTask = NextTaskType.PlaySpecifiedGroup;
            var wavDataId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (wavDataId < 0) {
                wavDataId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.PauseResumeToPlay);
                nextTask = NextTaskType.PlayPauseSpecifiedGroup;
            } else {
                // 再生リストに登録されている曲数が1曲で、しかも
                // その曲を再生中に、次の曲または前の曲ボタンが押された場合、曲を頭出しする。
                if (1 == ap.PcmDataListForDisp.Count()) {
                    ap.wasapi.SetPosFrame(0);
                    return;
                }
            }

            var playingPcmData = ap.PcmDataListForPlay.FindById(wavDataId);
            if (null == playingPcmData) {
                return;
            }

            var ordinal = playingPcmData.Ordinal;
            int nextPcmDataId = 0;
            for (int i = 0; i < 2; ++i) {
                ordinal = updateOrdinal(ordinal);
                if (ordinal < 0) {
                    ordinal = 0;
                }
                if (ap.PcmDataListForDisp.Count() <= ordinal) {
                    ordinal = 0;
                }

                int nextCueSheetIndex = -1;
                if (IsPlayModeShuffle()) {
                    // シャッフル再生。
                    nextCueSheetIndex = ap.PcmDataListForPlay.At(ordinal).CueSheetIndex;
                    nextPcmDataId     = ap.PcmDataListForPlay.At(ordinal).Id;
                } else {
                    // 全曲再生、1曲再生。1曲再生の時はPlayには1曲だけ入っている。
                    nextCueSheetIndex = ap.PcmDataListForDisp.At(ordinal).CueSheetIndex;
                    nextPcmDataId     = ap.PcmDataListForDisp.At(ordinal).Id;
                }

                // 次の曲がIndex0の時、その次の曲にする。
                if (nextCueSheetIndex == 0) {
                    continue;
                } else {
                    break;
                }
            }

            if (ordinal == playingPcmData.Ordinal) {
                // 1曲目再生中に前の曲を押した場合頭出しする。
                ap.wasapi.SetPosFrame(0);
                return;
            }

            ChangePlayWavDataById(nextPcmDataId, nextTask);
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

            if (null == ap.wasapi) {
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
                ChangePlayWavDataById(pli.PcmData().Id, NextTaskType.PlaySpecifiedGroup);
                return;
            }

            // 再生中の場合。

            var playingId = ap.wasapi.GetPcmDataId(WasapiCS.PcmDataUsageType.NowPlaying);
            if (playingId < 0) {
                return;
            }

            // 再生中で、しかも、マウス押下中にこのイベントが来た場合で、
            // しかも、この曲を再生していない場合、この曲を再生する。
            if (null != pli.PcmData() &&
                playingId != pli.PcmData().Id) {
                ChangePlayWavDataById(pli.PcmData().Id, NextTaskType.PlaySpecifiedGroup);
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
        private void WasapiStatusChanged(StringBuilder idStr, int dwNewState) {
            //Console.WriteLine("WasapiStatusChanged {0}", idStr);
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
        /// ap.PcmDataListForDispのIdとGroupIdをリナンバーする。
        /// </summary>
        private void PcmDataListForDispItemsRenumber() {
            m_groupIdNextAdd = 0;
            for (int i = 0; i < ap.PcmDataListForDisp.Count(); ++i) {
                var pcmData = ap.PcmDataListForDisp.At(i);
                var pli = m_playListItems[i];

                if (0 < i) {
                    var prevPcmData = ap.PcmDataListForDisp.At(i - 1);
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

            var old = ap.PcmDataListForDisp.At(oldIdx);
            ap.PcmDataListForDisp.RemoveAt(oldIdx);
            ap.PcmDataListForDisp.Insert(newIdx, old);

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

        private void UpdatePreferenceAudioFilterListFrom(ObservableCollection<PreferenceAudioFilter> from) {
            mPreferenceAudioFilterList = new List<PreferenceAudioFilter>();
            foreach (var i in from) {
                mPreferenceAudioFilterList.Add(i);
            }
        }

        private void buttonSoundEffectsSettings_Click(object sender, RoutedEventArgs e) {
            var dialog = new SoundEffectsConfiguration();
            dialog.SetAudioFilterList(mPreferenceAudioFilterList);
            var result = dialog.ShowDialog();

            if (true == result) {
                UpdatePreferenceAudioFilterListFrom(dialog.AudioFilterList);

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
                sfu.Update(ap.wasapi, mPreferenceAudioFilterList);
            } else {
                sfu.Update(ap.wasapi, new List<PreferenceAudioFilter>());
            }
        }

        // PPWServer ■■■■■■■■■■■■■■■■■■■■■■■■■

        private const int PPWSERVER_LISTEN_PORT = 2002;
        private BackgroundWorker mBWPPWServer = new BackgroundWorker();
        private PPWServer mPPWServer = null;

        private void MenuItemPPWServerSettings_Click(object sender, RoutedEventArgs e) {
            var sw = new PPWServerSettingsWindow();

            if (mPPWServer != null) {
                sw.SetServerState(PPWServerSettingsWindow.ServerState.Started,
                    mPPWServer.ListenIPAddress, mPPWServer.ListenPort);
            } else {
                sw.SetServerState(PPWServerSettingsWindow.ServerState.Stopped, "", -1);
            }

            var r = sw.ShowDialog();
            if (r != true) {
                return;
            }

            if (mPPWServer == null) {
                // サーバー起動。
                mBWPPWServer = new BackgroundWorker();
                mBWPPWServer.DoWork += new DoWorkEventHandler(mBWPPWServer_DoWork);
                mBWPPWServer.WorkerSupportsCancellation = true;
                mBWPPWServer.WorkerReportsProgress = true;
                mBWPPWServer.ProgressChanged += new ProgressChangedEventHandler(mBWPPWServer_ProgressChanged);
                mBWPPWServer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBWPPWServer_RunWorkerCompleted);
                mBWPPWServer.RunWorkerAsync();
            } else {
                // サーバー終了。
                mBWPPWServer.CancelAsync();
                while (mBWPPWServer.IsBusy) {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new System.Threading.ThreadStart(delegate { }));
                    System.Threading.Thread.Sleep(100);
                }
                mBWPPWServer = null;
            }
        }

        void mBWPPWServer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
        }

        void  mBWPPWServer_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string s = e.UserState as string;
            AddLogText(s);
        }

        void  mBWPPWServer_DoWork(object sender, DoWorkEventArgs e)
        {
            mPPWServer = new PPWServer();
            mPPWServer.Run(new PPWServer.RemoteCmdRecvDelegate(PPWServerRemoteCmdRecv), mBWPPWServer, PPWSERVER_LISTEN_PORT);
            mPPWServer = null;
        }

        private void PPWServerRemoteCmdRecv(RemoteCommand cmd) {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background, new Action(() => {
                    // ここはMainWindowのUIスレッド。

                    switch (cmd.cmd) {
                    case RemoteCommandType.PlaylistWant:
                        // 再生リストを送る。
                        var plCmd = new RemoteCommand(RemoteCommandType.PlaylistSend);
                        plCmd.trackIdx = dataGridPlayList.SelectedIndex;
                        foreach (var a in m_playListItems) {
                            var p = new RemoteCommandPlayListItem(
                                a.PcmData().DurationMilliSec,
                                a.PcmData().SampleRate,
                                a.PcmData().ValidBitsPerSample,
                                a.AlbumTitle, a.ArtistName, a.Title, a.PcmData().PictureData);
                            plCmd.playlist.Add(p);
                        }
                        mPPWServer.SendAsync(plCmd);
                        break;
                    }
                }));

        }
    }
}
