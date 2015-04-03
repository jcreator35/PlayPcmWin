using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWIntersection {
        public static bool TriangleRayIntersect(Point3D p0, Point3D p1, Point3D p2, Point3D rayOrig, Vector3D rayDir, out Point3D hitPos, out Vector3D surfaceNormal, out double distance) {
            var edge01 = p1 - p0;
            var edge02 = p2 - p0;

            surfaceNormal = Vector3D.CrossProduct(edge01, edge02);
            surfaceNormal.Normalize();
            hitPos = new Point3D();
            distance = double.MaxValue;

            var p = Vector3D.CrossProduct(rayDir, edge02);
            var det = Vector3D.DotProduct(edge01, p);
            if (det < float.Epsilon) {
                // レイとトライアングルが平行 or backface
                return false;
            }

            var tvec = rayOrig - p0;
            var u = Vector3D.DotProduct(tvec,p);
            if (u < 0 || det < u) {
                return false;
            }

            var qvec = Vector3D.CrossProduct(tvec, edge01);
            var v = Vector3D.DotProduct(rayDir, qvec);
            if (v < 0 || det < u+v) {
                return false;
            }

            distance = Vector3D.DotProduct(edge02, qvec) / det;
            hitPos = rayOrig + distance * rayDir;
            return true;
        }

        public static bool ModelRayIntersection(WW3DModel model, Point3D rayOrig, Vector3D rayDir, out Point3D hitPos, out Vector3D hitSurfaceNormal, out double rayLength) {
            rayLength = double.MaxValue;
            hitPos = new Point3D();
            hitSurfaceNormal = new Vector3D();

            var points = model.TriangleList();
            var indices = model.IndexList();

            for (int i = 0; i < indices.Length/3; ++i) {
                Point3D pos;
                Vector3D surfaceNormal;
                double distance;
                if (!TriangleRayIntersect(points[indices[i * 3 + 0]], points[indices[i * 3 + 1]], points[indices[i * 3 + 2]], rayOrig, rayDir, out pos, out surfaceNormal, out distance)) {
                    continue;
                }
                if (distance < rayLength) {
                    hitPos = pos;
                    hitSurfaceNormal = surfaceNormal;
                    rayLength = distance;
                }
            }

            return rayLength != double.MaxValue;
        }
    }
}
