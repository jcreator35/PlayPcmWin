﻿using System;

namespace DesignFrequencySamplingFIRFilter {
    class Program {
        void Run(int M, double[] filterGain) {
            // calculated by Excel
            var c = new CalcFrequencySamplingFilterCoeffs();
            var h = c.Calc(M, filterGain);

            Console.WriteLine("M={0} FIR filter coeffs:", M);
            for (int i = 0; i < h.Length; ++i) {
                Console.WriteLine("{0:G17}, ", h[i]);
            }

            {
                // calc frequency response
                Console.WriteLine("M={0} Frequency(kHz),M={0} Gain(dB)", M);
                double fs = 44100;
                for (double f = 0; f < fs / 2; f += 1000) {
                    var fr = new CalcFIRFilterFrequencyResponse();
                    double ω = 2.0 * Math.PI * f / fs;
                    var gain = fr.Calc(h, ω);
                    double gainDB = 20.0 * Math.Log10(gain.Magnitude());

                    Console.WriteLine("{0},{1}", f / 1000, gainDB);
                }

                Console.WriteLine("");
            }
            {
                // calc frequency response, semitone step
                Console.WriteLine("M={0} Frequency(Hz),M={0} Gain(dB)", M);
                double fs = 44100;
                {
                    var fr = new CalcFIRFilterFrequencyResponse();
                    double f = 0;
                    double ω = 0;
                    var gain = fr.Calc(h, ω);
                    double gainDB = 20.0 * Math.Log10(gain.Magnitude());

                    Console.WriteLine("{0},{1}", f, gainDB);
                }
                //for (double f = 10; f < fs / 2; f *= Math.Pow(2, 1.0 / 12.0)) {
                for (double f = 10; f < fs / 2; f +=10) {
                    var fr = new CalcFIRFilterFrequencyResponse();
                    double ω = 2.0 * Math.PI * f / fs;
                    var gain = fr.Calc(h, ω);
                    double gainDB = 20.0 * Math.Log10(gain.Magnitude());

                    Console.WriteLine("{0},{1}", f, gainDB);
                }

                Console.WriteLine("");
            }

        }

        static void Main(string[] args) {
            var self = new Program();

            // Pre-emphasis P(f) M=27 -------------

            self.Run(27, new double[] {
                1.00000000000000000E+00,
                1.11088384722943000E+00,
                1.36946695218957000E+00,
                1.66654567679067000E+00,
                1.94415250456232000E+00,
                2.18211486467795000E+00,
                2.37798009431373000E+00,
                2.53625062976637000E+00,
                2.66334193034791000E+00,
                2.76546457294939000E+00,
                2.84790344718652000E+00,
                2.91490384068686000E+00,
                2.96978410166178000E+00,
                3.01510739727585000E+00,
            });


            // T(f) -------------------------------

            self.Run(9, new double[] {
                1,
                0.60004356,
                0.420524967,
                0.361602897,
                0.336724814,
            });

            self.Run(15, new double[] {
                1,
                0.762278521,
                0.54427685,
                0.441434182,
                0.390022558,
                0.361602897,
                0.344522557,
                0.333557132,
            });

            self.Run(17, new double[] {
                1.00000000000000000E+00,
                7.98007232143326000E-01,
                5.82114881704065000E-01,
                4.69486330836685000E-01,
                4.10203663902539000E-01,
                3.76419000241381000E-01,
                3.55711428471783000E-01,
                3.42239118771433000E-01,
                3.33039936592509000E-01,
            });

            self.Run(19, new double[] {
                1.00000000000000000E+00,
                8.27106831883030000E-01,
                6.17275277292737000E-01,
                4.97285498957717000E-01,
                4.30946926355020000E-01,
                3.92005677799586000E-01,
                3.67668244675991000E-01,
                3.51619621723856000E-01,
                3.40555098655984000E-01,
                3.32639840701821000E-01,
            });

            self.Run(21, new double[] {
                1,
                0.850900638,
                0.649610191,
                0.524462917,
                0.451954766,
                0.408154844,
                0.380251988,
                0.361602897,
                0.348619035,
                0.33926269,
                0.332321138,
            });

            self.Run(23, new double[] {
                1.00000000000000000E+00,
                8.70469075246059000E-01,
                6.79129410348967000E-01,
                5.50756334252722000E-01,
                4.72983787150043000E-01,
                4.24683730353270000E-01,
                3.93332645675986000E-01,
                3.72097470718362000E-01,
                3.57167022187219000E-01,
                3.46327766330894000E-01,
                3.38239941583536000E-01,
                3.32061293287020000E-01,
            });

            self.Run(25, new double[] {
                1.00000000000000000E+00,
                8.86671703532858000E-01,
                7.05941221213926000E-01,
                5.75987500371901000E-01,
                4.93838166836648000E-01,
                4.41434181595776000E-01,
                4.06792222748518000E-01,
                3.83017360721430000E-01,
                3.66136565267598000E-01,
                3.53789333493937000E-01,
                3.44522556895152000E-01,
                3.37410656227241000E-01,
                3.31845386222291000E-01,
            });

            self.Run(27, new double[] {
                1,
                0.900184121,
                0.730211122,
                0.60004356,
                0.514362941,
                0.458271018,
                0.420524967,
                0.3942828,
                0.375468125,
                0.361602897,
                0.3511355,
                0.34306449,
                0.336724814,
                0.331663144,
            });

            // 8th polynomial fit

            self.Run(43, new double[] {
                0.999848093,
                0.956932104,
                0.855466357,
                0.748662531,
                0.657444499,
                0.585677303,
                0.5308857,
                0.489209709,
                0.457186271,
                0.432183477,
                0.412347235,
                0.39640805,
                0.383481497,
                0.372913161,
                0.364184311,
                0.356875737,
                0.35067272,
                0.345379733,
                0.340898375,
                0.337106162,
                0.333557023,
                0.328912405,
            });

            self.Run(33, new double[] {
                0.999848093,
                0.929156731,
                0.789499991,
                0.66492378,
                0.572764063,
                0.508014835,
                0.462420702,
                0.429533807,
                0.405167292,
                0.386748523,
                0.372623976,
                0.361608183,
                0.352815394,
                0.34567669,
                0.339924137,
                0.335195893,
                0.329782874,
            });

            self.Run(27, new double[] {
                0.999848093,
                0.899578889,
                0.730331405,
                0.600029145,
                0.514121908,
                0.45823346,
                0.420617775,
                0.394320687,
                0.375457405,
                0.361608183,
                0.351099304,
                0.342959484,
                0.336713901,
                0.330542293,
            });

            // 6th poly -----------------------

            self.Run(43, new double[] {
                1,
                0.947639329,
                0.851584921,
                0.748982995,
                0.658117048,
                0.584703682,
                0.528230716,
                0.485836634,
                0.454190091,
                0.430261165,
                0.411592965,
                0.396376901,
                0.383446317,
                0.372213629,
                0.362548032,
                0.354591823,
                0.348518311,
                0.344228271,
                0.340967459,
                0.336852293,
                0.328378045,
                0.310252444, });

            self.Run(37, new double[] {
                1,
                0.933804354,
                0.817805776,
                0.702748867,
                0.608463391,
                0.537734328,
                0.486826395,
                0.450566267,
                0.424255982,
                0.404258088,
                0.388119104,
                0.37451066,
                0.363028693,
                0.353842729,
                0.34719908,
                0.342770511,
                0.338806735,
                0.331098821,
                0.3122303,
            });

            self.Run(33, new double[] {
                1,
                0.921039852,
                0.788655492,
                0.665682072,
                0.571387511,
                0.504844831,
                0.459288011,
                0.427760316,
                0.404797932,
                0.386789274,
                0.371897453,
                0.359695479,
                0.350505498,
                0.344446911,
                0.340151227,
                0.333070099,
                0.313866027,
            });

            self.Run(31, new double[] {
                1,
                0.913144837,
                0.771526701,
                0.645060549,
                0.551794814,
                0.488212371,
                0.445758947,
                0.416589435,
                0.395039165,
                0.377816059,
                0.363703603,
                0.352840753,
                0.345574868,
                0.34086211,
                0.334096209,
                0.314806629,
            });

            self.Run(29, new double[] {
                1,
                0.90394611,
                0.752386614,
                0.622991679,
                0.531635407,
                0.471622392,
                0.432455867,
                0.405491799,
                0.385121127,
                0.368700031,
                0.355832482,
                0.34701442,
                0.341629011,
                0.335143895,
                0.315845362,
            });

            self.Run(27, new double[] {
                1,
                0.893144653,
                0.730973788,
                0.599479869,
                0.511066124,
                0.455207449,
                0.419371436,
                0.394337263,
                0.374976523,
                0.359695479,
                0.348902998,
                0.342495451,
                0.33620803,
                0.316997354,
            });

            self.Run(25, new double[] {
                1,
                0.880354336,
                0.707010472,
                0.574589409,
                0.490291858,
                0.439078084,
                0.406416637,
                0.382966565,
                0.364719751,
                0.351437316,
                0.343534498,
                0.337283705,
                0.318280599,
            });

            self.Run(23, new double[]{
                1,
                0.865072057,
                0.680218547,
                0.54847062,
                0.469558334,
                0.423277197,
                0.393400041,
                0.371310149,
                0.354899373,
                0.344867611,
                0.338368919,
                0.319716443,
            });

            self.Run(21, new double[] {
                1,
                0.846637142,
                0.650350788,
                0.52139031,
                0.449121072,
                0.407710956,
                0.380076664,
                0.359695479,
                0.346695706,
                0.339470909,
                0.321329981,
            });

            self.Run(19, new double[] {
                1,
                0.824177354,
                0.617247694,
                0.493758418,
                0.429166096,
                0.392085172,
                0.366421275,
                0.349350606,
                0.340620471,
                0.323150119,
            });

            self.Run(17, new double[] {
                1,
                0.796541033,
                0.580934801,
                0.466125049,
                0.409651925,
                0.376000318,
                0.353380586,
                0.341903906,
                0.325208692,
            });

            self.Run(9, new double[] {
                1,
                0.599479869,
                0.419371436,
                0.359695479,
                0.33620803,
            });

#if false
            var h = c.Calc(15, new double[] { 1, 1, 1, 1, 0.4, 0, 0, 0 });
            for (int i=0;i<h.Length;++i) {
                Console.WriteLine("{0}, ", h[i]);
            }
#endif
        }
    }
}
