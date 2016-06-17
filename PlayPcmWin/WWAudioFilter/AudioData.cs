using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    /// <summary>
    /// 保存ファイルフォーマットの種類。
    /// </summary>
    enum FileFormatType {
        FLAC,
        DSF,
    }

    struct AudioData {
        public WWFlacRWCS.Metadata meta;
        public List<AudioDataPerChannel> pcm;
        public byte[] picture;
        public FileFormatType preferredSaveFormat;
    };
}
