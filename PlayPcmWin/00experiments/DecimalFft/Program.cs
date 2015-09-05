using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DecimalFft {
    class Program {
        const decimal SAMPLERATE = 44100;
        const int LENGTH = 1024;

        void FftSpectrum(WWDecimalComplex[] signalTime) {
            WWDecimalFft fft = new WWDecimalFft(LENGTH);
            var signalFreq = fft.ForwardFft(signalTime, 1M/LENGTH);

            var result = new Dictionary<double, double>();

            Parallel.For(0, LENGTH / 2, i => {
                decimal magnitude = signalFreq[i].Magnitude();
                double db = (double)(20M * WWDecimalMath.Log10(magnitude));
                lock (result) {
                    result.Add((double)(SAMPLERATE * i / LENGTH), db);
                }
            });

            foreach (var item in result) {
                Console.WriteLine("{0},{1}", item.Key, item.Value);
            }
        }

        enum ValueType
        {
            VT_Int16, // 16bit signed integer
            VT_Int24, // 24bit signed integer
            VT_Int32, // 32bit signed integer
            VT_Float32, // 32bit float
            VT_Float64, // 64bit float
            VT_Decimal, // Decimal
            VT_Int64,   // 64bit signed integer
        }

        void Run(ValueType t) {
            // 1024 samples of 44100Hz PCM contains 23 periods of 998.5Hz sine wave
            decimal frequency = 23 * WWDecimalMath.M_2PI;

            var signalTime = new WWDecimalComplex[LENGTH];

            for (int i=0; i < LENGTH; ++i) {
                decimal r = WWDecimalMath.Cos(frequency * i / LENGTH);

                switch (t) {
                case ValueType.VT_Int16:
                    r = ((int)(r * 32767))/32767M;
                    break;

                case ValueType.VT_Int24:
                    r = (decimal)((int)(r * 8388607) / 8388607M);
                    break;

                case ValueType.VT_Int32:
                    r = (decimal)((int)(r * Int32.MaxValue) / ((decimal)(Int32.MaxValue)));
                    break;

                case ValueType.VT_Int64:
                    r = (decimal)(((long)(r * Int64.MaxValue)) / ((decimal)(Int64.MaxValue)));
                    break;

                case ValueType.VT_Float32:
                    r = (decimal)((float)r);
                    break;

                case ValueType.VT_Float64:
                    r = (decimal)((double)r);
                    break;

                case ValueType.VT_Decimal:
                    break;
                }

                signalTime[i] = new WWDecimalComplex(r, 0);
            }

            FftSpectrum(signalTime);

            Console.WriteLine("Done.");
        }

        static void PrintUsage() {
            Console.WriteLine("please specify valueType parameter.");
            Console.WriteLine("  0 : 16bit signed integer");
            Console.WriteLine("  1 : 24bit signed integer");
            Console.WriteLine("  2 : 32bit signed integer");
            Console.WriteLine("  3 : 32bit float");
            Console.WriteLine("  4 : 64bit float");
            Console.WriteLine("  5 : 128bit decimal float");
            Console.WriteLine("  6 : 64bit signed integer");
        }

        static void Main(string[] args) {
            // Test.TestAll();

            if (args.Length < 1) {
                PrintUsage();
                return;
            }
            int valueTypeInt = 0;
            if (!Int32.TryParse(args[0], out valueTypeInt)) {
                PrintUsage();
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var p = new Program();
            p.Run((ValueType)valueTypeInt);

            Console.WriteLine("{0}  {1} seconds", (ValueType)valueTypeInt, sw.ElapsedMilliseconds * 0.001M);
        }
    }
}
