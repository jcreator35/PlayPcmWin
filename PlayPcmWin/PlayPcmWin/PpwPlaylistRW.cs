using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace PlayPcmWin {
    /// <summary>
    /// Class name and member name and its type should not be changed.
    /// These names are used as XML tag name
    /// </summary>
    public class PlaylistItemSave {
        public string Title { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public string PathName { get; set; }
        public int CueSheetIndex { get; set; }
        public int StartTick { get; set; }
        public int EndTick { get; set; }
        public bool ReadSeparaterAfter { get; set; }
        //public long LastWriteTime { get; set; }

        public PlaylistItemSave() {
            Reset();
        }

        public void Reset() {
            Title = string.Empty;
            AlbumName = string.Empty;
            ArtistName = string.Empty;
            PathName = string.Empty;
            CueSheetIndex = 1;
            StartTick = 0;
            EndTick = -1;
            ReadSeparaterAfter = false;
            //LastWriteTime = -1;
        }

        public PlaylistItemSave Set(
                string title,
                string albumName,
                string artistName,
                string pathName,
                int cueSheetIndex,
                int startTick,
                int endTick,
                bool readSeparatorAfter,
                long lastWriteTime) {
            Title = title;
            AlbumName = albumName;
            ArtistName = artistName;
            PathName = pathName;
            CueSheetIndex = cueSheetIndex;
            StartTick = startTick;
            EndTick = endTick;
            ReadSeparaterAfter = readSeparatorAfter;
            //LastWriteTime = lastWriteTime;
            return this;
        }
    }

    public class PlaylistSave : WWXmlRW.SaveLoadContents {
        // SaveLoadContents IF
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public static readonly int CurrentVersion = 1;
        public int Version { get; set; }
        public int ItemNum { get { return Items.Count(); } }
        private List<PlaylistItemSave> items = new List<PlaylistItemSave>();
        public Collection<PlaylistItemSave> Items {
            get { return new Collection<PlaylistItemSave>(items); }
        }

        public void Reset() {
            Version = CurrentVersion;
            items.Clear();
        }

        public PlaylistSave() {
            Reset();
        }

        public void Add(PlaylistItemSave item) {
            items.Add(item);
        }
    }

    /// <summary>
    ///  @todo PreferenceStoreクラスと同様なので、1個にまとめる。
    /// </summary>
    class PpwPlaylistRW {
        private PpwPlaylistRW() {
        }

        private const string m_fileName = "PlayPcmWinPlayList.xml";

        private static void OverwritePlaylist(PlaylistSave p) {
            // TODO: ロード後に、強制的に上書きしたいパラメータがある場合はここで上書きする。
        }

        public static PlaylistSave Load() {
            var xmlRW = new WWXmlRW.XmlRW<PlaylistSave>(m_fileName, true);
            PlaylistSave p = xmlRW.Load();

            OverwritePlaylist(p);

            return p;
        }

        public static PlaylistSave LoadFrom(string path) {
            var xmlRW = new WWXmlRW.XmlRW<PlaylistSave>(path, false);
            PlaylistSave p = xmlRW.Load();

            OverwritePlaylist(p);

            return p;
        }

        public static bool Save(PlaylistSave p) {
            var xmlRW = new WWXmlRW.XmlRW<PlaylistSave>(m_fileName, true);
            return xmlRW.Save(p);
        }

        public static bool SaveAs(PlaylistSave p, string path) {
            var xmlRW = new WWXmlRW.XmlRW<PlaylistSave>(path, false);
            return xmlRW.Save(p);
        }
    }
}
