using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWLineSegment {
        public Point3D StartPos { get; set; }

        /// <summary>
        /// 方向ベクトル。長さは1に正規化する。
        /// </summary>
        public Vector3D Direction { get; set; }
        public double Length { get; set; }

        /// <summary>
        /// 強さ 1.0がMAX
        /// </summary>
        public double Intensity { get; set; }

        public WWLineSegment(Point3D startPos, Vector3D dirNormalized, double length, double intensity) {
            StartPos = startPos;
            Direction = dirNormalized;
            Length = length;
            Intensity = intensity;
        }
    }
}
