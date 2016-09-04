using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayPcmWinAlbum {
    public class TiledItemContent {
        public string DisplayName { get; set; }
        public byte[] ImageBytes { get; set; }
        public object Tag { get; set; }

        public TiledItemContent(string displayName, byte[] imageBytes, object tag) {
            DisplayName = displayName;
            ImageBytes = imageBytes;
            Tag = tag;
        }
    };

}
