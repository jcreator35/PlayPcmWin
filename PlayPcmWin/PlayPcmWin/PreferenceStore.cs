using WasapiPcmUtil;
using PcmDataLib;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Text;

namespace PlayPcmWin {
    public enum PlayListDispModeType {
        /// <summary>
        /// 選択モード
        /// </summary>
        Select,
        /// <summary>
        /// 項目編集モード
        /// </summary>
        EditItem,
    }

    public class Preference : WWXmlRW.SaveLoadContents {
        // SaveLoadContents IF
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public const int DefaultLatencyMilliseconds = 170;
        public const int CurrentVersion = 4;

        public int Version { get; set; }

        public int LatencyMillisec { get; set; }
        public WasapiSharedOrExclusiveType WasapiSharedOrExclusive { get; set; }
        public WasapiDataFeedModeType WasapiDataFeedMode { get; set; }
        public RenderThreadTaskType RenderThreadTaskType { get; set; }
        public BitsPerSampleFixType BitsPerSampleFixType { get; set; }

        public bool ReplaceGapWithKokomade { get; set; }

        public string PreferredDeviceName { get; set; }
        public string PreferredDeviceIdString { get; set; }

        public double MainWindowLeft { get; set; }
        public double MainWindowTop { get; set; }
        public double MainWindowWidth { get; set; }
        public double MainWindowHeight { get; set; }

        public bool ManuallySetMainWindowDimension { get; set; }
        public bool ParallelRead { get; set; }
        public bool PlayRepeat { get; set; }
        public bool PlayAllTracks { get; set; }
        public int PlayingTimeSize { get; set; }
        public bool PlayingTimeFontBold { get; set; }
        public string PlayingTimeFontName { get; set; }
        public double WindowScale { get; set; }
        public bool SettingsIsExpanded { get; set; }
        public bool StorePlaylistContent { get; set; }
        public bool DispCoverart { get; set; }
        public bool RefrainRedraw { get; set; }
        public bool Shuffle { get; set; }
        public int ZeroFlushMillisec { get; set; }
        public int TimePeriodHundredNanosec { get; set; }
        public bool AlternatingRowBackground { get; set; }
        public long AlternatingRowBackgroundArgb { get; set; }
        public int ResamplerConversionQuality { get; set; }
        public int CueEncodingCodePage { get; set; }
        public bool SootheLimiterApo { get; set; }

        public int LastPlayItemIndex { get; set; }

        // deprecated replaced with BpsConvNoiseShaping
        public bool EnableNoiseShaping { get; set; }

        public WasapiPcmUtil.NoiseShapingType BpsConvNoiseShaping { get; set; }

        public bool SortDropFolder { get; set; }
        public bool SortDroppedFiles { get; set; }
        public bool BatchReadEndpointToEveryTrack { get; set; }
        public bool VerifyFlacMD5Sum { get; set; }
        public bool GpuRendering { get; set; }
        public bool DwmEnableMmcss { get; set; }
        public bool DwmEnableMmcssCall { get; set; }

        private List<string> playListColumnsOrder = new List<string>();
        public Collection<string> PlayListColumnsOrder {
            get { return new Collection<string>(playListColumnsOrder); }
        }

        public int OpenFileDialogFilterIndex { get; set; }

        public void PlayListColumnsOrderRemoveRange(int idx, int count) {
            playListColumnsOrder.RemoveRange(idx, count);
        }

        private void SetDefaultPlayListColumnsOrder() {
            var pl = playListColumnsOrder;
            pl.Clear();

            pl.Add("Title");
            pl.Add("Duration");
            pl.Add("Artist");
            pl.Add("AlbumTitle");
            pl.Add("SampleRate");

            pl.Add("QuantizationBitRate");
            pl.Add("NumChannels");
            pl.Add("BitRate");
            pl.Add("IndexNr");
            pl.Add("ReadSeparaterAfter");
        }

        public Preference() {
            Reset();
        }

        /// <summary>
        /// デフォルト設定値。
        /// </summary>
        public void Reset() {
            Version = CurrentVersion;
            LatencyMillisec = DefaultLatencyMilliseconds;
            WasapiSharedOrExclusive = WasapiSharedOrExclusiveType.Exclusive;
            WasapiDataFeedMode = WasapiDataFeedModeType.EventDriven;
            RenderThreadTaskType = RenderThreadTaskType.ProAudio;
            BitsPerSampleFixType = BitsPerSampleFixType.AutoSelect;
            BpsConvNoiseShaping = NoiseShapingType.NoiseShaping1stOrder;
            PreferredDeviceName = "";
            PreferredDeviceIdString = "";
            ReplaceGapWithKokomade = false;
            ManuallySetMainWindowDimension = true;
            ParallelRead = false;
            PlayRepeat = true;
            PlayAllTracks = true;
            Shuffle = false;
            PlayingTimeSize = 16;
            PlayingTimeFontBold = true;
            PlayingTimeFontName = "Courier New";
            WindowScale = 1.0f;
            SettingsIsExpanded = true;
            StorePlaylistContent = true;
            DispCoverart = true;
            RefrainRedraw = false;
            ZeroFlushMillisec = 500;
            TimePeriodHundredNanosec = 10000;
            LastPlayItemIndex = 0;
            EnableNoiseShaping = true;
            DwmEnableMmcss = true;
            DwmEnableMmcssCall = false;

            MainWindowLeft   = 100;
            MainWindowTop    = 100;
            MainWindowWidth  = 1000;
            MainWindowHeight = 640;

            AlternatingRowBackground = true;
            AlternatingRowBackgroundArgb = 0xfff8fcfc;
            ResamplerConversionQuality = 60;

            CueEncodingCodePage = Encoding.Default.CodePage;

            SootheLimiterApo = true;

            SetDefaultPlayListColumnsOrder();

            SortDropFolder = true;
            SortDroppedFiles = false;
            BatchReadEndpointToEveryTrack = false;
            VerifyFlacMD5Sum = false;
            GpuRendering = true;
            OpenFileDialogFilterIndex = -1;
        }

        /// <summary>
        /// ウィンドウサイズセット。
        /// </summary>
        public void SetMainWindowLeftTopWidthHeight(
                double left, double top,
                double width, double height) {
            MainWindowLeft   = left;
            MainWindowTop    = top;
            MainWindowWidth  = width;
            MainWindowHeight = height;
        }
    }

    sealed class PreferenceStore {
        private const string m_fileName = "PlayPcmWinPreference.xml";

        private PreferenceStore() {
        }

        public static Preference Load() {
            var xmlRW = new WWXmlRW.XmlRW<Preference>(m_fileName);

            Preference p = xmlRW.Load();

            // postprocess playlist columns order info...
            if (p.PlayListColumnsOrder.Count == 10) {
                // OK: older format. no playlist column info.
            } else if (p.PlayListColumnsOrder.Count == 20) {
                // OK: load success. delete former 10 items inserted by Reset()
                p.PlayListColumnsOrderRemoveRange(0, 10);
            } else {
                System.Console.WriteLine("E: Preference PlayListColumnOrder item count {0}", p.PlayListColumnsOrder.Count);
                p.Reset();
            }

            // (読み込んだ値が都合によりサポートされていない場合、このタイミングでロード後に強制的に上書き出来る)

            // サンプルレートの選択アルゴリズム。廃止された選択肢が選ばれていたらAutoSelectに設定する。
            switch (p.BitsPerSampleFixType) {
            case BitsPerSampleFixType.Variable:
            case BitsPerSampleFixType.VariableSint16Sint24:
            case BitsPerSampleFixType.VariableSint16Sint32V24:
                p.BitsPerSampleFixType = BitsPerSampleFixType.AutoSelect;
                break;
            default:
                break;
            }

            // 並列読み込みが有効のときはFLACのMD5計算を行わない。
            if (p.ParallelRead) {
                p.VerifyFlacMD5Sum = false;
            }

            // EnableNoiseShaping(古いフラグ)が無効のときはBpsConvNoiseShaping(新しい設定パラメータ)をNoneに設定。
            if (!p.EnableNoiseShaping) {
                p.BpsConvNoiseShaping = NoiseShapingType.None;
            }

            return p;
        }

        public static bool Save(Preference p) {
            var xmlRW = new WWXmlRW.XmlRW<Preference>(m_fileName);
            return xmlRW.Save(p);
        }
    }
}
