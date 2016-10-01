using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace PlayPcmWin {
    /// <summary>
    /// version 1
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
        }

        public PlaylistItemSave Set(
                string title,
                string albumName,
                string artistName,
                string pathName,
                int cueSheetIndex,
                int startTick,
                int endTick,
                bool readSeparatorAfter) {
            Title = title;
            AlbumName = albumName;
            ArtistName = artistName;
            PathName = pathName;
            CueSheetIndex = cueSheetIndex;
            StartTick = startTick;
            EndTick = endTick;
            ReadSeparaterAfter = readSeparatorAfter;
            return this;
        }
    }


    /// <summary>
    ///  Version 2
    ///  TrackId and LastWriteTime
    /// </summary>
    public class PlaylistItemSave2 {
        public string Title { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public string PathName { get; set; }
        public int CueSheetIndex { get; set; }
        public int StartTick { get; set; }
        public int EndTick { get; set; }
        public bool ReadSeparaterAfter { get; set; }
        public int TrackId { get; set; }
        public long LastWriteTime { get; set; }

        public PlaylistItemSave2() {
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
            LastWriteTime = -1;
            TrackId = 0;
        }

        public PlaylistItemSave2 Set(
                string title,
                string albumName,
                string artistName,
                string pathName,
                int cueSheetIndex,
                int startTick,
                int endTick,
                bool readSeparatorAfter,
                long lastWriteTime,
                int trackId) {
            Title = title;
            AlbumName = albumName;
            ArtistName = artistName;
            PathName = pathName;
            CueSheetIndex = cueSheetIndex;
            StartTick = startTick;
            EndTick = endTick;
            ReadSeparaterAfter = readSeparatorAfter;
            LastWriteTime = lastWriteTime;
            TrackId = trackId;
            return this;
        }
    }

    /// <summary>
    ///  Version 3
    ///  ComposerName
    /// </summary>
    public class PlaylistItemSave3 {
        public string Title { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public string ComposerName { get; set; }
        public string PathName { get; set; }
        public int CueSheetIndex { get; set; }
        public int StartTick { get; set; }
        public int EndTick { get; set; }
        public bool ReadSeparaterAfter { get; set; }
        public int TrackId { get; set; }
        public long LastWriteTime { get; set; }

        public PlaylistItemSave3() {
            Reset();
        }

        public void Reset() {
            Title = string.Empty;
            AlbumName = string.Empty;
            ArtistName = string.Empty;
            ComposerName = string.Empty;
            PathName = string.Empty;
            CueSheetIndex = 1;
            StartTick = 0;
            EndTick = -1;
            ReadSeparaterAfter = false;
            LastWriteTime = -1;
            TrackId = 0;
        }

        public PlaylistItemSave3 Set(
                string title,
                string albumName,
                string artistName,
                string composerName,
                string pathName,
                int cueSheetIndex,
                int startTick,
                int endTick,
                bool readSeparatorAfter,
                long lastWriteTime,
                int trackId) {
            Title = title;
            AlbumName = albumName;
            ArtistName = artistName;
            ComposerName = composerName;
            PathName = pathName;
            CueSheetIndex = cueSheetIndex;
            StartTick = startTick;
            EndTick = endTick;
            ReadSeparaterAfter = readSeparatorAfter;
            LastWriteTime = lastWriteTime;
            TrackId = trackId;
            return this;
        }
    }

    /// <summary>
    /// version 1
    /// </summary>
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
    /// version 2
    /// </summary>
    public class PlaylistSave2 : WWXmlRW.SaveLoadContents {
        // SaveLoadContents IF
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public static readonly int CurrentVersion = 2;
        public int Version { get; set; }
        public int ItemNum { get { return Items.Count(); } }
        private List<PlaylistItemSave2> items = new List<PlaylistItemSave2>();
        public Collection<PlaylistItemSave2> Items {
            get { return new Collection<PlaylistItemSave2>(items); }
        }

        public void Reset() {
            Version = CurrentVersion;
            items.Clear();
        }

        public PlaylistSave2() {
            Reset();
        }

        public void Add(PlaylistItemSave2 item) {
            items.Add(item);
        }

        public static PlaylistSave2 ConvertFrom(PlaylistSave p1) {
            var p2 = new PlaylistSave2();
            p2.Version = CurrentVersion;
            foreach (var item in p1.Items) {
                var item2 = new PlaylistItemSave2();
                item2.Set(item.Title, item.AlbumName, item.ArtistName, item.PathName, item.CueSheetIndex, item.StartTick, item.EndTick, item.ReadSeparaterAfter, -1, 0);
                p2.items.Add(item2);
            }
            return p2;
        }
    }

    /// <summary>
    /// version 3
    /// </summary>
    public class PlaylistSave3 : WWXmlRW.SaveLoadContents {
        // SaveLoadContents IF
        public int GetCurrentVersionNumber() { return CurrentVersion; }
        public int GetVersionNumber() { return Version; }

        public static readonly int CurrentVersion = 3;
        public int Version { get; set; }
        public int ItemNum { get { return Items.Count(); } }
        private List<PlaylistItemSave3> items = new List<PlaylistItemSave3>();
        public Collection<PlaylistItemSave3> Items {
            get { return new Collection<PlaylistItemSave3>(items); }
        }

        public void Reset() {
            Version = CurrentVersion;
            items.Clear();
        }

        public PlaylistSave3() {
            Reset();
        }

        public void Add(PlaylistItemSave3 item) {
            items.Add(item);
        }

        public static PlaylistSave3 ConvertFrom(PlaylistSave2 p2) {
            var p3 = new PlaylistSave3();
            p3.Version = CurrentVersion;
            foreach (var item in p2.Items) {
                var item3 = new PlaylistItemSave3();
                item3.Set(item.Title, item.AlbumName, item.ArtistName, "", item.PathName, item.CueSheetIndex, item.StartTick, item.EndTick, item.ReadSeparaterAfter, -1, 0);
                p3.items.Add(item3);
            }
            return p3;
        }
    }

    class PpwPlaylistRW {
        private PpwPlaylistRW() {
        }

        private const string m_fileName = "PlayPcmWinPlayList.xml";

        private static void OverwritePlaylist(PlaylistSave3 p) {
            // TODO: ロード後に、強制的に上書きしたいパラメータがある場合はここで上書きする。
        }

        public static PlaylistSave3 Load() {
            return LoadFrom(m_fileName, true);
        }

        public static PlaylistSave3 LoadFrom(string path, bool useIsolatedStorage = false) {
            var xmlRW3 = new WWXmlRW.XmlRW<PlaylistSave3>(path, useIsolatedStorage);
            PlaylistSave3 p3 = xmlRW3.Load();
            if (p3.ItemNum == 0) {
                var xmlRW2 = new WWXmlRW.XmlRW<PlaylistSave2>(path, useIsolatedStorage);
                PlaylistSave2 p2 = xmlRW2.Load();
                if (p2.ItemNum == 0) {
                    var xmlRW1 = new WWXmlRW.XmlRW<PlaylistSave>(path, useIsolatedStorage);
                    var p1 = xmlRW1.Load();
                    p2 = PlaylistSave2.ConvertFrom(p1);
                }
                p3 = PlaylistSave3.ConvertFrom(p2);
            }
            OverwritePlaylist(p3);

            return p3;
        }

        public static bool Save(PlaylistSave3 p) {
            var xmlRW = new WWXmlRW.XmlRW<PlaylistSave3>(m_fileName, true);
            return xmlRW.Save(p);
        }

        public static bool SaveAs(PlaylistSave3 p, string path) {
            var xmlRW = new WWXmlRW.XmlRW<PlaylistSave3>(path, false);
            return xmlRW.Save(p);
        }
    }
}
