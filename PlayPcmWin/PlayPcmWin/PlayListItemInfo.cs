using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;

namespace PlayPcmWin {
    /// <summary>
    /// 再生リスト1項目の情報。
    /// dataGridPlayList.Itemsの項目と一対一に対応する。
    /// </summary>
    class PlayListItemInfo : INotifyPropertyChanged {
        private static int mNextRowId = 1;
        private PcmDataLib.PcmData mPcmData;
        private bool mReadSeparatorAfter;
        private int mRowId;

        public static void SetNextRowId(int id) {
            mNextRowId = id;
        }

        public int RowId {
            get { return mRowId; }
            set { mRowId = value; }
        }

        public string Id {
            get { return mPcmData.Id.ToString(CultureInfo.CurrentCulture); }
        }

        public string Title {
            get { return mPcmData.DisplayName; }
            set { mPcmData.DisplayName = value; }
        }

        public string ArtistName {
            get { return mPcmData.ArtistName; }
            set { mPcmData.ArtistName = value; }
        }

        public string AlbumTitle {
            get { return mPcmData.AlbumTitle; }
            set { mPcmData.AlbumTitle = value; }
        }

        /// <summary>
        /// 長さ表示用文字列
        /// </summary>
        public string Duration {
            get { return Util.SecondsToHMSString(mPcmData.DurationSeconds); }
        }

        public int NumChannels {
            get { return mPcmData.NumChannels; }
        }

        public int IndexNr {
            get { return mPcmData.CueSheetIndex; }
        }

        public string SampleRate {
            get {
                if (mPcmData.SampleDataType == PcmDataLib.PcmData.DataType.DoP) {
                    return string.Format(CultureInfo.CurrentCulture, "{0:F1}MHz", (double)mPcmData.SampleRate * 16 / 1000 / 1000);
                }
                return string.Format(CultureInfo.CurrentCulture, "{0}kHz", mPcmData.SampleRate * 0.001);
            }
        }

        public string QuantizationBitRate {
            get {
                if (mPcmData.SampleDataType == PcmDataLib.PcmData.DataType.DoP) {
                    return "1bit";
                }
                if (mPcmData.SampleValueRepresentationType == PcmDataLib.PcmData.ValueRepresentationType.SFloat) {
                    return mPcmData.BitsPerSample.ToString(CultureInfo.CurrentCulture)
                            + "bit (" + Properties.Resources.FloatingPointNumbers + ")";
                }
                return mPcmData.ValidBitsPerSample.ToString(CultureInfo.CurrentCulture) + "bit";
            }
        }

        public string BitRate {
            get {
                if (mPcmData.SampleDataType == PcmDataLib.PcmData.DataType.DoP) {
                    return (mPcmData.SampleRate * 16 * mPcmData.NumChannels / 1000).ToString(CultureInfo.CurrentCulture) + " kbps";
                }
                return ((long)mPcmData.BitsPerSample * mPcmData.SampleRate * mPcmData.NumChannels / 1000).ToString(CultureInfo.CurrentCulture) + " kbps";
            }
        }

        public PcmDataLib.PcmData PcmData() { return mPcmData; }

        public bool ReadSeparaterAfter {
            get { return mReadSeparatorAfter; }
            set {
                mReadSeparatorAfter = value;
                OnPropertyChanged("ReadSeparaterAfter");
            }
        }

        public PlayListItemInfo(PcmDataLib.PcmData pcmData) {
            mPcmData = pcmData;
            mRowId = mNextRowId++;
        }

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
