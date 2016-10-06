using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWAudioFilter;

namespace PolynomialVisualize {
    class ImageToFreqResponse {

        struct GraphPositions {
            public int upper;
            public int lower;
            public override string ToString() {
                return string.Format("upper={0}, lower={1}", upper, lower);
            }
        };

        public WWComplex[] Run(string path) {
            System.Drawing.Bitmap bm = new System.Drawing.Bitmap(path);

            int width = bm.Width;
            int height = bm.Height;

            // 0を表す中央の線の位置とグラフの線の位置を調べる。
            var positions = new GraphPositions[width];
            var histogram = new int[height];

            for (int x = 0; x < width; ++x) {
                // 上から調べる
                for (int y = 0; y < height; ++y) {
                   var c = bm.GetPixel(x, y);
                   float b = c.GetBrightness();
                   if (b < 0.5f) {
                       positions[x].upper = y;
                       ++histogram[y];
                       break;
                   }
                }

                // 下から調べる
                for (int y = height-1; 0<=y; --y) {
                    var c = bm.GetPixel(x, y);
                    float b = c.GetBrightness();
                    if (b < 0.5f) {
                        positions[x].lower = y;
                        ++histogram[y];
                        break;
                    }
                }
            }

            int minValue = -1;
            for (int y = 0; y < height; ++y) {
                if (0 < histogram[y]) {
                    minValue = y;
                    break;
                }
            }

            int maxValue = height;
            for (int y = height - 1; 0 <= y; --y) {
                if (0 < histogram[y]) {
                    maxValue = y;
                    break;
                }
            }

            int zeroPosition = 0;
            int histogramMax = 0;
            for (int y = 0; y < height; ++y) {
                if (histogramMax < histogram[y]) {
                    zeroPosition = y;
                    histogramMax = histogram[y];
                }
            }

            // 中央の線の位置 = zeroPosition
            // 最大値 maxValue
            // 最小値 minValue

            double center = (maxValue + minValue)/2.0;
            double magnitude = (maxValue - minValue) / 2.0;

            var waveForm = new WWComplex[width];

            for (int x = 0; x < width; ++x) {
                int iv = positions[x].upper;
                if (zeroPosition == iv) {
                    iv = positions[x].lower;
                }

                double v = (center - iv) / magnitude;

                waveForm[x] = new WWComplex(v, 0);
                //Console.WriteLine("{0} {1}", x, v);
            }

            WWComplex [] freqResponse;
            WWDftCpu.Dft1d(waveForm, out freqResponse);

            return freqResponse;
        }
    }
}
