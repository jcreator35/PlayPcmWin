using PcmDataLib;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Text;
using Wasapi;
using System.Threading;

namespace RecPcmWin {
    public class Preference : WWXmlRW.SaveLoadContents {
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public const int CurrentVersion = 1;

        public int Version { get; set; }

        public int SampleRate { get; set; }
        public WasapiCS.SampleFormatType SampleFormat { get; set; }
        public int NumOfChannels { get; set; }
        public int WasapiBufferSizeMS { get; set; }
        public WasapiCS.DataFeedMode WasapiDataFeedMode { get; set; }
        public int RecordingBufferSizeMB { get; set; }

        public string PreferredDeviceIdString { get; set; }

        public string CultureString { get; set; }

        public int YellowLevelDb { get; set; }
        public bool UpdateLevelMeterWhileRecording { get; set; }

        public bool SetDwChannelMask { get; set; }

        /// <summary>
        ///  -1: PeakHold = ∞
        /// </summary>
        public int PeakHoldSeconds { get; set; }

        public int ReleaseTimeDbPerSec { get; set; }

        public Preference() {
            Reset();
        }

        /// <summary>
        /// デフォルト設定値。
        /// </summary>
        public void Reset() {
            Version = CurrentVersion;

            SampleRate = 44100;
            SampleFormat = WasapiCS.SampleFormatType.Sint16;
            NumOfChannels = 2;
            WasapiBufferSizeMS = 200;
            WasapiDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
            RecordingBufferSizeMB = 256;
            PreferredDeviceIdString = "";
            CultureString = Thread.CurrentThread.CurrentCulture.Name;
            YellowLevelDb = -12;
            UpdateLevelMeterWhileRecording = false;
            PeakHoldSeconds = 1;
            SetDwChannelMask = false;
            ReleaseTimeDbPerSec = 100;
        }
    }

    sealed class PreferenceStore {
        private const string m_fileName = "RecPcmWinPreference.xml";

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
