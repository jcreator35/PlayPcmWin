using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayPcmWinAlbum {
    public class TiledItemContent {
        public string DisplayName { get; set; }
        public byte[] ImageBytes { get; set; }

        public TiledItemContent(string displayName, byte[] imageBytes) {
            DisplayName = displayName;
            ImageBytes = imageBytes;
        }
    };

}
