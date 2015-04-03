using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWRoom {
        // やや大きめにすると良いらしい。
        public const double LISTENER_HEAD_RADIUS_DEFAULT = 0.16;

        public const int NUM_OF_SPEAKERS = 2;
        public WW3DModel ListenerModel { get; set; }
        public WW3DModel SpeakerModel { get; set; }
        public WW3DModel RoomModel { get; set; }
        private WWPosture[] mSpeakerPos = new WWPosture[NUM_OF_SPEAKERS];
        public Point3D ListenerPos { get; set; }
        public double ListenerHeadRadius { get; set; }

        public WWRoom() {
            for (int i=0; i<NUM_OF_SPEAKERS; ++i) {
                mSpeakerPos[i] = new WWPosture();
            }
            ListenerHeadRadius = LISTENER_HEAD_RADIUS_DEFAULT;
        }

        public Point3D SpeakerPos(int idx) {
            return mSpeakerPos[idx].Pos;
        }
        public Vector3D SpeakerDir(int idx) {
            return mSpeakerPos[idx].Dir;
        }

        public void SetSpeakerPos(int idx, Point3D pos) {
            mSpeakerPos[idx].Pos = pos;
        }

        public void SetSpeakerDir(int idx, Vector3D dir) {
            mSpeakerPos[idx].Dir = dir;
        }

        public Point3D ListenerEarPos(int ch) {
            switch (ch) {
            case 0:
                return ListenerPos + new Vector3D(-ListenerHeadRadius, 0.0, 0.0);
            case 1:
                return ListenerPos + new Vector3D(ListenerHeadRadius, 0.0, 0.0);
            default:
                System.Diagnostics.Debug.Assert(false);
                return ListenerPos;
            }
        }

        public Vector3D ListenerEarDir(int ch) {
            switch (ch) {
            case 0:
                return new Vector3D(-1.0, 0.0, 0.0);
            case 1:
                return new Vector3D(1.0, 0.0, 0.0);
            default:
                System.Diagnostics.Debug.Assert(false);
                return new Vector3D(1.0, 0.0, 0.0);
            }
        }

        public bool RayIntersection(Point3D rayPos, Vector3D rayDir, out Point3D hitPos, out Vector3D hitSurfaceNormal, out double rayLength) {
            // 部屋との当たり
            // 部屋のモデルはワールド座標に置いてあるのでそのまま当たり判定する
            if (!WWIntersection.ModelRayIntersection(RoomModel, rayPos, rayDir, out hitPos, out hitSurfaceNormal, out rayLength)) {
                return false;
            }
            return true;
        }
    }
}
