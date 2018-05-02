using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace PlayPcmWin {
    enum RemoteCommandType {
        PlaylistWant,
        PlaylistSend,
        Play,
        Pause,
        Stop,
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
        public RemoteCommandType cmd;
        public int idx;
        public int positionMillisec;
        public List<RemoteCommandPlayListItem> playlist;

        public RemoteCommand(RemoteCommandType t) {
            cmd = t;
        }
    };
}
