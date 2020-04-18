using System;

namespace WWMath {

    public class WWSegmentPointDistance {
        /// <summary>
        /// Distance from point to a line segment
        /// </summary>
        public static double SegmentPointDistance(WWVectorD2 edgeP1, WWVectorD2 edgeP2, WWVectorD2 point, double margin) {
            System.Diagnostics.Debug.Assert(margin < WWVectorD2.Distance(edgeP1, edgeP2));

            // 直線の方程式 ax + by + c = 0
            double dx = edgeP2.X - edgeP1.X;
            double dy = edgeP2.Y - edgeP1.Y;
            double a =  dy;
            double b = -dx;
            double c = edgeP2.X * edgeP1.Y - edgeP2.Y * edgeP1.X;

            // pointと最短距離の直線上の点の座標。
            var nearestPointOnLine = new WWVectorD2(
                    (b * (+b * point.X - a * point.Y) - a * c) / (a * a + b * b),
                    (a * (-b * point.X + a * point.Y) - b * c) / (a * a + b * b));

            if (       (margin + nearestPointOnLine.X < edgeP1.X && margin + nearestPointOnLine.X < edgeP2.X) //< 最短地点のx座標がedgeP1,edgeP2よりも左。
                    || (margin + nearestPointOnLine.Y < edgeP1.Y && margin + nearestPointOnLine.Y < edgeP2.Y) //< 最短地点のy座標がedgeP1,edgeP2よりも下。
                    || (margin + edgeP1.X < nearestPointOnLine.X && margin + edgeP2.X < nearestPointOnLine.X) //< 最短地点のx座標がedgeP1,edgeP2よりも右。
                    || (margin + edgeP1.Y < nearestPointOnLine.Y && margin + edgeP2.Y < nearestPointOnLine.Y) //< 最短地点のy座標がedgeP1,edgeP2よりも上。
                    ) {
                // 点と最短距離の直線上の点が線分の範囲外。
                // edgeP1とedgeP2のうち、近い方が最短距離の線分上の点。
                double d1 = WWVectorD2.Distance(edgeP1, point);
                double d2 = WWVectorD2.Distance(edgeP2, point);
                return (d1 < d2) ? d1 : d2;
            }

            // 直線と点の距離distance。
            double distance = Math.Abs(a * point.X + b * point.Y + c) / Math.Sqrt(a * a + b * b);
            return distance;
        }
    }
}
