using System;
using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWMatrixUtil {
        public static Matrix3D CalculateLookAt(Vector3D eye, Vector3D at, Vector3D up) {
            var zaxis = (at - eye);
            zaxis.Normalize();
            var xaxis = Vector3D.CrossProduct(up, zaxis);
            xaxis.Normalize();
            var yaxis = Vector3D.CrossProduct(zaxis, xaxis);

            return new Matrix3D(
                xaxis.X, yaxis.X, zaxis.X, 0,
                xaxis.Y, yaxis.Y, zaxis.Y, 0,
                xaxis.Z, yaxis.Z, zaxis.Z, 0,
                Vector3D.DotProduct(xaxis, -eye), Vector3D.DotProduct(yaxis, -eye), Vector3D.DotProduct(zaxis, -eye), 1);
        }

        public static Matrix3D CreatePerspectiveProjectionMatrix(double zNear, double zFar, double fovDegree, double aspectRatio) {
            // near screen size = 2x2

            double hFoV = fovDegree * Math.PI / 180.0;
            double xScale = 1.0 / Math.Tan(hFoV / 2.0);
            double yScale = aspectRatio * xScale;
            double a = (zFar + zNear) / (zNear - zFar);
            double b = 2.0 * zNear / (zNear - zFar);
            return new Matrix3D(
                    xScale, 0, 0, 0,
                    0, yScale, 0, 0,
                    0, 0, a, -1,
                    0, 0, b, 0);
        }


    }
}
