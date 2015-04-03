using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WW3DModel {
        /// <summary>
        /// triangle vertex list
        /// </summary>
        private Point3D [] mTriangleList;

        /// <summary>
        /// triangle index list
        /// </summary>
        private int[] mIndexList;

        public Point3D[] TriangleList() {
            return mTriangleList;
        }

        public int[] IndexList() {
            return mIndexList;
        }

        public WW3DModel(Point3D[] triangleList, int[] indexList) {
            mTriangleList = triangleList;
            mIndexList = indexList;
        }
    }
}
