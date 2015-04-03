using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {
    public class FilterState {
        public int Channel { get; set; }

        public FilterState(int channel) {
            Channel = channel;
        }
    }
}
