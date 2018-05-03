using System.Collections.Generic;

namespace PlayPcmWin {
    enum RemoteCommandType {
        PlaylistWant,
        PlaylistSend,
        Play,
        Pause,
        Stop,
        Seek,
        Exit
    };

    class RemoteCommandPlayListItem {
        public int durationMillsec;
        public string artistName;
        public string titleName;
        public byte[] albumCoverArt;
        public RemoteCommandPlayListItem(int aDurationMilliSec, string aArtistName,
                string aTitleName, byte[] aAlbumCoverArt) {
            durationMillsec = aDurationMilliSec;
            artistName = aArtistName;
            titleName = aTitleName;
            albumCoverArt = aAlbumCoverArt;
        }
    };

    class RemoteCommand {
        public const int FOURCC_PPWR          = 0x52575050; // "PPWR"
        public const int FOURCC_PLAYLIST_WANT = 0x574c4c50; // "PLLW"
        public const int FOURCC_PLAYLIST_SEND = 0x534c4c50; // "PLLS"
        public const int FOURCC_EXIT          = 0x54495845; // "EXIT"
        public const int FOURCC_PLAY          = 0x59414c50; // "PLAY"
        public const int FOURCC_PAUSE         = 0x53554150; // "PAUS"
        public const int FOURCC_STOP          = 0x504f5453; // "STOP"
        public const int FOURCC_SEEK          = 0x4b454553; // "SEEK"

        public RemoteCommandType cmd;
        public int trackIdx;
        public int positionMillisec;
        public List<RemoteCommandPlayListItem> playlist = new List<RemoteCommandPlayListItem>();

        public RemoteCommand(RemoteCommandType t) {
            cmd = t;
        }

        public RemoteCommand(int header, int bytes, byte[] payload) {
            cmd = RemoteCommandType.Exit;

            switch (header) {
            case FOURCC_PLAYLIST_WANT:
                cmd = RemoteCommandType.PlaylistWant;
                break;
            case FOURCC_EXIT:
            default:
                cmd = RemoteCommandType.Exit;
                break;
            }
        }
    };
}
