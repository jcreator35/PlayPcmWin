// 日本語。


namespace WWSpatialAudioPlayer {
    enum VirtualSpeakerMotionType {
        Static,
        UserSpecifiedPosition,
        RotateAroundYourHead,
    };

    class VirtualSpeakerProperty {
        public int Channel { get; set; }

        public string ChannelName { get; set; }
        public VirtualSpeakerMotionType Type { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public VirtualSpeakerProperty(int ch, string chName, VirtualSpeakerMotionType vt, float x, float y, float z) {
            Channel = ch;
            ChannelName = chName;
            Type = vt;
            PosX = x;
            PosY = y;
            PosZ = z;
        }
    }
}
