using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWShowUSBDeviceTree {
    public class TerminalProperty {
        public enum TermType {
            InputTerminal,
            OutputTerminal
        }

        public TermType type;
        public int id;

        public TerminalProperty(int aId, TermType aType) {
            id = aId;
            type = aType;
        }
    }
}
