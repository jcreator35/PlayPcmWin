using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWModeler {
        public enum NormalDirection {
            Inward,
            Outward
        }

        public static WW3DModel GenerateCuboid(Size3D wdh, Vector3D offset, NormalDirection nd) {
            var points = new List<Point3D>();
            points.Add(new Point3D(-wdh.X / 2 + offset.X, -wdh.Y / 2 + offset.Y, wdh.Z / 2 + offset.Z));
            points.Add(new Point3D(-wdh.X / 2 + offset.X, -wdh.Y / 2 + offset.Y, -wdh.Z / 2 + offset.Z));
            points.Add(new Point3D(wdh.X / 2 + offset.X, -wdh.Y / 2 + offset.Y, -wdh.Z / 2 + offset.Z));
            points.Add(new Point3D(wdh.X / 2 + offset.X, -wdh.Y / 2 + offset.Y, wdh.Z / 2 + offset.Z));

            points.Add(new Point3D(-wdh.X / 2 + offset.X, wdh.Y / 2 + offset.Y, wdh.Z / 2 + offset.Z));
            points.Add(new Point3D(-wdh.X / 2 + offset.X, wdh.Y / 2 + offset.Y, -wdh.Z / 2 + offset.Z));
            points.Add(new Point3D(wdh.X / 2 + offset.X, wdh.Y / 2 + offset.Y, -wdh.Z / 2 + offset.Z));
            points.Add(new Point3D(wdh.X / 2 + offset.X, wdh.Y / 2 + offset.Y, wdh.Z / 2 + offset.Z));

            int[] indices = null;

            switch (nd) {
            case NormalDirection.Outward:
                indices = new int[] {
                    5, 1, 0, 6, 2, 1, 7, 3, 2, 4, 0, 3,
                    1, 2, 3, 6, 5, 4, 4, 5, 0, 5, 6, 1,
                    6, 7, 2, 7, 4, 3, 0, 1, 3, 7, 6, 4 };
                break;
            case NormalDirection.Inward:
                indices = new int[] {
                    0,1,5,1,2,6,2,3,7,3,0,4,
                    3,2,1,4,5,6,0,5,4,1,6,5,
                    2,7,6,3,4,7,3,1,0,4,6,7};
                break;
            }

            return new WW3DModel(points.ToArray(), indices);
        }
    }
}
