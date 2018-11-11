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
