using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWFlacRWCS {
    public struct FlacCuesheetTrackIndex {
        public int indexNr;
        public long offsetSamples;
    }

    public class FlacCuesheetTrack {
        public int trackNr;
        public long offsetSamples;
        public List<FlacCuesheetTrackIndex> indices = new List<FlacCuesheetTrackIndex>();
    };
}
