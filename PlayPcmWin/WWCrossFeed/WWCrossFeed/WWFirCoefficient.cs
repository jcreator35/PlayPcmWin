using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWFirCoefficient {
        public double DelaySecond { get; set; }
        public Vector3D SoundDirection { get; set; }
        public double Gain { get; set; }
        public bool IsDirect { get; set; }

        public WWFirCoefficient(double delaySecond, Vector3D soundDir, double gain, bool isDirect) {
            DelaySecond = delaySecond;
            SoundDirection = soundDir;
            Gain = gain;
            IsDirect = isDirect;
        }
    }
}
