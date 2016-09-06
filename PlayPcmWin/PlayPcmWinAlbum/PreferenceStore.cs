using WasapiPcmUtil;
using PcmDataLib;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Text;
using Wasapi;

namespace PlayPcmWinAlbum {
    public class Preference : WWXmlRW.SaveLoadContents {
        // SaveLoadContents IF
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public const int DefaultBufferSizeMillisec = 170;
        public const int CurrentVersion = 1;

        public int Version { get; set; }

        public int BufferSizeMillisec { get; set; }
        public WasapiDataFeedModeType WasapiDataFeedMode { get; set; }
        public string PreferredDeviceName { get; set; }
        public string PreferredDeviceIdString { get; set; }

        public Preference() {
            Reset();
        }

        /// <summary>
        /// デフォルト設定値。
        /// </summary>
        public void Reset() {
            Version = CurrentVersion;
            BufferSizeMillisec = DefaultBufferSizeMillisec;
            WasapiDataFeedMode = WasapiDataFeedModeType.EventDriven;
            PreferredDeviceName = "";
            PreferredDeviceIdString = "";
        }
    }

    sealed class PreferenceStore {
        private const string m_fileName = "PlayPcmWinAlbumPreference.xml";

        private PreferenceStore() {
        }

        public static Preference Load() {
            var xmlRW = new WWXmlRW.XmlRW<Preference>(m_fileName);

            Preference p = xmlRW.Load();

            // (読み込んだ値が都合によりサポートされていない場合、このタイミングでロード後に強制的に上書き出来る)
            return p;
        }

        public static bool Save(Preference p) {
            var xmlRW = new WWXmlRW.XmlRW<Preference>(m_fileName);
            return xmlRW.Save(p);
        }
    }
}
