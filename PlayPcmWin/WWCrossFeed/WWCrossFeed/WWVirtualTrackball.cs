using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWVirtualTrackball {
        Point mPressPosXY;
        public double SphereRadius { get; set; }
        public Size ScreenWH { get; set; }
        Quaternion mRotationAnchor = new Quaternion();
        Quaternion mRotationCurrent = new Quaternion();

        public void Reset() {
            mRotationAnchor = new Quaternion();
            mRotationCurrent = new Quaternion();
        }

        /// <summary>
        /// ボタンがMouseDownしたら呼ぶ。
        /// </summary>
        /// <param name="xy">スクリーン座標系</param>
        public void Down(Point xy) {
            mPressPosXY = xy;
        }

        private bool PosOnScreenToPosOnSphere(Point xyScreen, out Vector3D xyz) {
            var xy = new Vector(-xyScreen.X + ScreenWH.Width/2, -xyScreen.Y + ScreenWH.Height/2);
            if (SphereRadius <= xy.Length) {
                xyz = new Vector3D(xy.X * SphereRadius / xy.Length, xy.Y * SphereRadius / xy.Length, 0);
            } else {
                double z = Math.Sqrt(SphereRadius * SphereRadius - xy.LengthSquared);
                xyz = new Vector3D(xy.X, xy.Y, z);
            }

            xyz.Normalize();
            return true;
        }

        /// <summary>
        /// ボタンが押されつつMouseMoveしたら呼ぶ。
        /// </summary>
        /// <param name="xy">スクリーン座標系</param>
        public void Move(Point xy) {
            var delta = new Vector(xy.X - mPressPosXY.X, xy.Y - mPressPosXY.Y);
            if (delta.LengthSquared < float.Epsilon) {
                return;
            }

            Vector3D xyzFrom;
            Vector3D xyzTo;

            if (!PosOnScreenToPosOnSphere(mPressPosXY, out xyzFrom) ||
                !PosOnScreenToPosOnSphere(xy, out xyzTo)) {
                return;
            }

            Vector3D axis = Vector3D.CrossProduct(xyzFrom, xyzTo);
            double angle = Math.Acos(Vector3D.DotProduct(xyzFrom, xyzTo));

            var q = new Quaternion(axis, angle * 180.0 / Math.PI);

            mRotationCurrent = mRotationAnchor * q;
        }

        /// <summary>
        /// ボタンが離されたら呼ぶ。
        /// </summary>
        public void Up() {
            mRotationAnchor = mRotationCurrent;
        }

        /// <summary>
        /// 回転行列を取得。
        /// </summary>
        /// <returns></returns>
        public Matrix3D RotationMatrix() {
            var mat = new Matrix3D();
            mat.Rotate(mRotationCurrent);
            return mat;
        }

    }
}
