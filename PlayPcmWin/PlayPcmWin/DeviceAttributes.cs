using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PlayPcmWin {
    class DeviceAttributes {
        public int Idx { get; set; }
        public string Name { get; set; }
        public string DeviceIdStr { get; set; }

        public DeviceAttributes(int idx, string name, string deviceIdStr) {
            Idx = idx;
            Name = name;
            DeviceIdStr = deviceIdStr;
        }

        public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", Idx, Name);
        }
    }


}
