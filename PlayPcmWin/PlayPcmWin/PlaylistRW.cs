namespace PlayPcmWin {

    class PlaylistTrackInfo {
        public string path;
        public string title;
        public int    trackId;   // TRACK 10 ==> 10 (CUE sheets)
        public int    startTick; // *75 seconds 0: start of the file
        public int    endTick;   // -1: till the end of file

        public int    indexId;   // INDEX 00 ==> 0, INDEX 01 ==> 1
        public string performer;
        public bool readSeparatorAfter;

        public string albumTitle;
    }
    
    interface PlaylistReader {
        bool ReadFromFile(string path);
        PlaylistTrackInfo GetTrackInfo(int nth);
        int GetTrackInfoCount();
    }
}
