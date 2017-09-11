using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWAudioFilter;

namespace FftTest {
    class Program {
        static void Print(string s, WWComplex[] v) {

            var sb = new StringBuilder();
            sb.AppendFormat("{0}: ", s);
            if (v.Length < 1) {
                sb.Append("Empty");
            } else {
                sb.AppendFormat("{0}", v[0]);
                for (int i = 1; i < v.Length; ++i) {
                    sb.AppendFormat(", {0}", v[i]);
                }
            }
            Console.WriteLine(sb.ToString());
        }

        static void Ex01() {
            var hp = new WWComplex[8];
            hp[0].real = 1.0;
            hp[1].real = -1.0;

            var fft = new WWRadix2Fft(8);
            
            var H = fft.ForwardFft(hp);

            Print("hp", hp);
            Print("H ", H);

            var xp = new WWComplex[8];

            xp[0].real = 1.0;
            xp[1].real = 2.0;
            xp[2].real = 3.0;
            xp[3].real = 1.0;

            var X = fft.ForwardFft(xp);

            Print("xp", xp);
            Print("X ", X);

            var Y = WWComplex.Mul(H, X);

            Print("Y ", Y);

            var y = fft.InverseFft(Y);

            Print("y ", y);
        }

        static void Ex02() {
            var h = new WWComplex[] {
                new WWComplex(1, 0),
                new WWComplex(-1, 0)
            };

            var x = new WWComplex[] {
                new WWComplex(1, 0),
                new WWComplex(2, 0),
                new WWComplex(3, 0),
                new WWComplex(1, 0),
                new WWComplex(-2, 0),
                new WWComplex(1, 0),
                new WWComplex(-1, 0),
                new WWComplex(-2, 0),
                new WWComplex(1, 0),
                new WWComplex(3, 0),
            };

            var conv = new WWConvolution();

            var y = conv.ConvolutionBruteForce(h, x);

            Print("h ", h);
            Print("x ", x);
            Print("y ", y);

            var y2 = conv.ConvolutionFft(h, x);
            Print("y2", y2);

            Console.WriteLine("distance(y, y2)={0}", WWComplex.AverageDistance(y, y2));

            var y3 = conv.ConvolutionContinuousFft(h, x);
            Print("y3", y3);

            Console.WriteLine("distance(y, y3)={0}", WWComplex.AverageDistance(y, y3));
        }

        static void Main(string[] args) {
            Ex02();
        }
    }
}
