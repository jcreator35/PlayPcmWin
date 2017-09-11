using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PolynomialVisualize {
    class Util {
        /// <summary>
        /// HSV to BGRA color conversion (A=255)
        /// </summary>
        /// <param name="h">0 <= h < 360 </param>
        /// <param name="s">0 to 1</param>
        /// <param name="v">0 to 1</param>
        /// <returns></returns>
        public static int HsvToBgra(double h, double s, double v) {
            while (h < 0) {
                h += 360.0;
            }
            while (360 < h) {
                h -= 360;
            }

            // create HSV color then convert it to RGB
            double c = v * s;
            double x = c * (1.0 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double rp = 0;
            double gp = 0;
            double bp = 0;

            if (h < 60) {
                rp = c;
                gp = x;
            } else if (h < 120) {
                rp = x;
                gp = c;
            } else if (h < 180) {
                gp = c;
                bp = x;
            } else if (h < 240) {
                gp = x;
                bp = c;
            } else if (h < 300) {
                rp = x;
                bp = c;
            } else {
                rp = c;
                bp = x;
            }

            byte r = (byte)((rp + m) * 255);
            byte g = (byte)((gp + m) * 255);
            byte b = (byte)((bp + m) * 255);

            return b + (g<<8) + (r<<16) + (0xff<<24);
        }
    }
}
