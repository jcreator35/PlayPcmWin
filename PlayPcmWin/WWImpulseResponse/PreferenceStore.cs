using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Wasapi;

namespace WWImpulseResponse {
    public class Preference : WWXmlRW.SaveLoadContents {
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public const int CurrentVersion = 1;

        public int Version { get; set; }

        public int MLSOrder { get; set; }

        public int SampleRate { get; set; }

        public WasapiCS.SampleFormatType PlaySampleFormat { get; set; }
        public WasapiCS.SampleFormatType RecSampleFormat { get; set; }
        public int NumOfChannels { get; set; }
        public int TestChannel { get; set; }
        public int PlayWasapiBufferSizeMS { get; set; }
        public int RecWasapiBufferSizeMS { get; set; }
        public WasapiCS.DataFeedMode PlayDataFeedMode { get; set; }
        public WasapiCS.DataFeedMode RecDataFeedMode { get; set; }

        public string PlayPreferredDeviceIdString { get; set; }
        public string RecPreferredDeviceIdString { get; set; }

        public bool SetDwChannelMask { get; set; }

        /// <summary>
        ///  -1: PeakHold = ∞
        /// </summary>
        public int PeakHoldSeconds { get; set; }
        public int YellowLevelDb { get; set; }
        public int ReleaseTimeDbPerSec { get; set; }

        public Preference() {
            Reset();
        }

        /// <summary>
        /// デフォルト設定値。
        /// </summary>
        public void Reset() {
            Version = CurrentVersion;
            MLSOrder = 16;
            SampleRate = 48000;
            PlaySampleFormat = WasapiCS.SampleFormatType.Sint16;
            RecSampleFormat = WasapiCS.SampleFormatType.Sint16;
            NumOfChannels = 2;
            TestChannel = 0;
            PlayWasapiBufferSizeMS = 100;
            RecWasapiBufferSizeMS = 100;
            PlayDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
            RecDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
            PlayPreferredDeviceIdString = "";
            RecPreferredDeviceIdString = "";
            SetDwChannelMask = true;
        }
    }

    sealed class PreferenceStore {
        private const string m_fileName = "WWImpulseResponsePreference.xml";

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
