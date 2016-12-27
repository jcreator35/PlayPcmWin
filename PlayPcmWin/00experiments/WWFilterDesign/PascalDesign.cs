using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWAudioFilter {
    class PascalDesign : ApproximationBase {
        /// H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.80
        /// Table 2.4 Characteristic values of Pa(N, Ω)
        struct CharacteristicValue {
            public int N;
            public double Ωmax;
            public double Pmax;
            public double ΩD;
            public CharacteristicValue(int aN, double aΩmax, double aPmax, double aΩD) {
                N = aN;
                Ωmax = aΩmax;
                Pmax = aPmax;
                ΩD = aΩD;
            }
        };

        struct PoleLocation {
            public int N;
            public double C;
            public WWComplex[] poleList;
            public PoleLocation(int aN, double aC, WWComplex [] aPoleList) {
                N = aN;
                C = aC;
                poleList = aPoleList;
            }
        };

        static Dictionary<int, CharacteristicValue> mCharacteristicValues = new Dictionary<int, CharacteristicValue>();
        static Dictionary<int, PoleLocation> mPoleTable0_01 = new Dictionary<int, PoleLocation>();
        static Dictionary<int, PoleLocation> mPoleTable0_1 = new Dictionary<int, PoleLocation>();
        static Dictionary<int, PoleLocation> mPoleTable0_5 = new Dictionary<int, PoleLocation>();
        static Dictionary<int, PoleLocation> mPoleTable1 = new Dictionary<int, PoleLocation>();
        static Dictionary<int, PoleLocation> mPoleTable1_25 = new Dictionary<int, PoleLocation>();
        static Dictionary<int, PoleLocation> mPoleTable1_5 = new Dictionary<int, PoleLocation>();

        static PascalDesign() {
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.80
            // Table 2.4
            mCharacteristicValues.Add(2,  new CharacteristicValue(2,  0.00000000, -0.12500000, 0.47140452));
            mCharacteristicValues.Add(3,  new CharacteristicValue(3,  0.28867513, +0.06415003, 0.57735029));
            mCharacteristicValues.Add(4,  new CharacteristicValue(4,  0.44721360, -0.04166667, 0.65289675));
            mCharacteristicValues.Add(5,  new CharacteristicValue(5,  0.54814429, +0.03026194, 0.70639006));
            mCharacteristicValues.Add(6,  new CharacteristicValue(6,  0.61812758, -0.02347346, 0.74582512));
            mCharacteristicValues.Add(7,  new CharacteristicValue(7,  0.66950940, +0.01901625, 0.77599290));
            mCharacteristicValues.Add(8,  new CharacteristicValue(8,  0.70882772, -0.01588792, 0.79978194));
            mCharacteristicValues.Add(9,  new CharacteristicValue(9,  0.73987600, +0.01358345, 0.81900877));
            /*
            mCharacteristicValues.Add(10, new CharacteristicValue(10, 0.76500826, -0.01182234, 0.83486553));
            mCharacteristicValues.Add(11, new CharacteristicValue(11, 0.78576311, +0.01043707, 0.84816452));
            mCharacteristicValues.Add(12, new CharacteristicValue(12, 0.80318872, -0.00932176, 0.85947728));
            mCharacteristicValues.Add(13, new CharacteristicValue(13, 0.81802376, +0.00840640, 0.86921746));
            mCharacteristicValues.Add(14, new CharacteristicValue(14, 0.83080378, -0.00764299, 0.87769147));
            mCharacteristicValues.Add(15, new CharacteristicValue(15, 0.84192645, +0.00699753, 0.88513110));
            */

            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.95
            // Table 2.8 αmax = 0.01
            mPoleTable0_01.Add(
                2, new PoleLocation(2, 10.413869, new WWComplex[] {
                new WWComplex(-2.227764,2.337292),
                }));
            mPoleTable0_01.Add(
                3, new PoleLocation(3, 5.206936, new WWComplex[] {
                new WWComplex(-0.794685, 1.626215),
                new WWComplex(-1.589371, 0),
                })
            );
            mPoleTable0_01.Add(
                4, new PoleLocation(4, 2.934297, new WWComplex[] {
                new WWComplex(-0.429585, 1.376373),
                new WWComplex(-1.044855, 0.565886),
                }));
            mPoleTable0_01.Add(
                5, new PoleLocation(5, 1.769666, new WWComplex[] {
                new WWComplex(-0.279541, 1.259329),
                new WWComplex(-0.744100, 0.768710),
                new WWComplex(-0.929117, 0),
                }));
            mPoleTable0_01.Add(
                6, new PoleLocation(6, 1.112555, new WWComplex[] {
                new WWComplex(-0.201501,1.193811),
                new WWComplex(-0.564546,0.860564),
                new WWComplex(-0.787056,0.311634),
                }));
            mPoleTable0_01.Add(
                7, new PoleLocation(7, 0.719074, new WWComplex[] {
                new WWComplex(-0.154870,1.152746),
                new WWComplex(-0.448356,0.908832),
                new WWComplex(-0.667122,0.497552),
                new WWComplex(-0.747272, 0),
                }));
            mPoleTable0_01.Add(
                8, new PoleLocation(8, 0.473977, new WWComplex[] {
                new WWComplex(-0.124349,1.124942),
                new WWComplex(-0.368266,0.936914),
                new WWComplex(-0.571780,0.616491),
                new WWComplex(-0.685759,0.214871),
                }));
            mPoleTable0_01.Add(
                9, new PoleLocation(9, 0.317021, new WWComplex[] {
                new WWComplex(-0.103048,1.105038),
                new WWComplex(-0.310304,0.954500),
                new WWComplex(-0.496350,0.696974),
                new WWComplex(-0.622821,0.367348),
                new WWComplex(-0.667452,0),
                }));

            // Table 2.8 αmax = 0.1
            mPoleTable0_1.Add(
                2, new PoleLocation(2, 3.276101, new WWComplex[] {
                new WWComplex(-1.186178, 1.380948),
                }));
            mPoleTable0_1.Add(
                3, new PoleLocation(3, 1.638051, new WWComplex[] {
                new WWComplex(-0.484703,1.206155),
                new WWComplex(-0.969406,0),
                }));
            mPoleTable0_1.Add(
                4, new PoleLocation(4, 0.923101, new WWComplex[] {
                new WWComplex(-0.278303,1.131224),
                new WWComplex(-0.687948,0.457626),
                }));
            mPoleTable0_1.Add(
                5, new PoleLocation(5, 0.556720, new WWComplex[] {
                new WWComplex(-0.187406,1.093326),
                new WWComplex(-0.513602,0.655532),
                new WWComplex(-0.652393,0),
                }));
            mPoleTable0_1.Add(
                6, new PoleLocation(6, 0.349999, new WWComplex[] {
                new WWComplex(-0.138125,1.071182),
                new WWComplex(-0.402354,0.758765),
                new WWComplex(-0.576657,0.272841),
                }));
            mPoleTable0_1.Add(
                7, new PoleLocation(7, 0.226213, new WWComplex[] {
                new WWComplex(-0.107841,1.056909),
                new WWComplex(-0.327070,0.819700),
                new WWComplex(-0.503758,0.445436),
                new WWComplex(-0.569057,0),
                }));
            mPoleTable0_1.Add(
                8, new PoleLocation(8, 0.149108, new WWComplex[] {
                new WWComplex(-0.087613,1.047051),
                new WWComplex(-0.273459,0.858899),
                new WWComplex(-0.441631,0.561055),
                new WWComplex(-0.536721,0.194964),
                }));
            mPoleTable0_1.Add(
                9, new PoleLocation(9, 0.099732, new WWComplex[] {
                new WWComplex(-0.073275,1.039887),
                new WWComplex(-0.233680,0.885749),
                new WWComplex(-0.390185,0.642298),
                new WWComplex(-0.497623,0.337401),
                new WWComplex(-0.535685,0),
                }));

            // Table 2.8 αmax = 0.5
            mPoleTable0_5.Add(
                2, new PoleLocation(2, 1.431388, new WWComplex[] {
                new WWComplex(-0.712812,1.004043),
                }));
            mPoleTable0_5.Add(
                3, new PoleLocation(3, 0.715694, new WWComplex[] {
                new WWComplex(-0.313228,1.021928),
                new WWComplex(-0.626457,0),
                }));
            mPoleTable0_5.Add(
                4, new PoleLocation(4, 0.403319, new WWComplex[] {
                new WWComplex(-0.185811,1.018171),
                new WWComplex(-0.473019,0.399956),
                }));
            mPoleTable0_5.Add(
                5, new PoleLocation(5, 0.243241, new WWComplex[] {
                new WWComplex(-0.127497,1.014480),
                new WWComplex(-0.367266,0.591875),
                new WWComplex(-0.479538,0),
                }));
            mPoleTable0_5.Add(
                6, new PoleLocation(6, 0.152921, new WWComplex[] {
                new WWComplex(-0.095139,1.011785),
                new WWComplex(-0.295576,0.699381),
                new WWComplex(-0.441502,0.250192),
                }));
            mPoleTable0_5.Add(
                7, new PoleLocation(7, 0.098837, new WWComplex[] {
                new WWComplex(-0.074938,1.009831),
                new WWComplex(-0.245068,0.766314),
                new WWComplex(-0.396560,0.414208),
                new WWComplex(-0.452860,0),
                }));
            mPoleTable0_5.Add(
                8, new PoleLocation(8, 0.065148, new WWComplex[] {
                new WWComplex(-0.061288,1.008378),
                new WWComplex(-0.208034,0.811221),
                new WWComplex(-0.354805,0.527186),
                new WWComplex(-0.438240,0.182751),
                }));
            mPoleTable0_5.Add(
                9, new PoleLocation(9, 0.043575, new WWComplex[] {
                new WWComplex(-0.051527,1.007266),
                new WWComplex(-0.179934,0.843059),
                new WWComplex(-0.318420,0.608385),
                new WWComplex(-0.413955,0.318731),
                new WWComplex(-0.447883,0),
                }));

            // Table 2.8 αmax = 1.0
            mPoleTable1.Add(
                2, new PoleLocation(2, 0.982613, new WWComplex[] {
                new WWComplex(-0.548867,0.895129),
                }));
            mPoleTable1.Add(
                3, new PoleLocation(3, 0.491307, new WWComplex[] {
                new WWComplex(-0.247085,0.965999),
                new WWComplex(-0.494171,0),
                }));
            mPoleTable1.Add(
                4, new PoleLocation(4, 0.276869, new WWComplex[] {
                new WWComplex(-0.148193,0.983002),
                new WWComplex(-0.385603,0.377784),
                }));
            mPoleTable1.Add(
                5, new PoleLocation(5, 0.166979, new WWComplex[] {
                new WWComplex(-0.102338,0.989590),
                new WWComplex(-0.305853,0.566511),
                new WWComplex(-0.407030,0),
                }));
            mPoleTable1.Add(
                6, new PoleLocation(6, 0.104977, new WWComplex[] {
                new WWComplex(-0.076689,0.992848),
                new WWComplex(-0.249813,0.675152),
                new WWComplex(-0.383801,0.241065),
                }));
            mPoleTable1.Add(
                7, new PoleLocation(7, 0.067849, new WWComplex[] {
                new WWComplex(-0.060590,0.994713),
                new WWComplex(-0.209387,0.744156),
                new WWComplex(-0.350205,0.401443),
                new WWComplex(-0.402818,0),
                }));
            mPoleTable1.Add(
                8, new PoleLocation(8, 0.044723, new WWComplex[] {
                new WWComplex(-0.049668,0.995890),
                new WWComplex(-0.179237,0.791172),
                new WWComplex(-0.316890,0.513188),
                new WWComplex(-0.395505,0.177699),
                }));
            mPoleTable1.Add(
                9, new PoleLocation(9, 0.029913, new WWComplex[] {
                new WWComplex(-0.041834,0.996687),
                new WWComplex(-0.156064,0.824921),
                new WWComplex(-0.286839,0.594246),
                new WWComplex(-0.377413,0.310934),
                new WWComplex(-0.409609,0),
                }));

            // Table 2.8 αmax = 1.25
            mPoleTable1_25.Add(
                2, new PoleLocation(2, 0.865781, new WWComplex[] {
                new WWComplex(-0.499894,0.865964),
                }));
            mPoleTable1_25.Add(
                3, new PoleLocation(3, 0.432891, new WWComplex[] {
                new WWComplex(-0.226566,0.950787),
                new WWComplex(-0.453133,0),
                }));
            mPoleTable1_25.Add(
                4, new PoleLocation(4, 0.243950, new WWComplex[] {
                new WWComplex(-0.136308,0.973361),
                new WWComplex(-0.357779,0.370834),
                }));
            mPoleTable1_25.Add(
                5, new PoleLocation(5, 0.147125, new WWComplex[] {
                new WWComplex(-0.094300,0.982733),
                new WWComplex(-0.286033,0.558421),
                new WWComplex(-0.383466,0),
                }));
            mPoleTable1_25.Add(
                6, new PoleLocation(6, 0.092495, new WWComplex[] {
                new WWComplex(-0.070751,0.987613),
                new WWComplex(-0.234913,0.667338),
                new WWComplex(-0.364884,0.238135),
                }));
            mPoleTable1_25.Add(
                7, new PoleLocation(7, 0.059782, new WWComplex[] {
                new WWComplex(-0.055947,0.990524),
                new WWComplex(-0.197697,0.736954),
                new WWComplex(-0.334920,0.397324),
                new WWComplex(-0.386340,0),
                }));
            mPoleTable1_25.Add(
                8, new PoleLocation(8, 0.039405, new WWComplex[] {
                new WWComplex(-0.045892,0.992424),
                new WWComplex(-0.169760,0.784617),
                new WWComplex(-0.304335,0.508654),
                new WWComplex(-0.381389,0.176063),
                }));
            mPoleTable1_25.Add(
                9, new PoleLocation(9, 0.026356, new WWComplex[] {
                new WWComplex(-0.038673,0.993746),
                new WWComplex(-0.148181,0.818964),
                new WWComplex(-0.276347,0.589650),
                new WWComplex(-0.365311,0.308401),
                new WWComplex(-0.396944,0),
                }));

            // Table 2.8 αmax = 1.5
            mPoleTable1_5.Add(
                2, new PoleLocation(2, 0.778464, new WWComplex[] {
                new WWComplex(-0.461089,0.844158),
                }));
            mPoleTable1_5.Add(
                3, new PoleLocation(3, 0.389232, new WWComplex[] {
                new WWComplex(-0.210056,0.939346),
                new WWComplex(-0.420112,0),
                }));
            mPoleTable1_5.Add(
                4, new PoleLocation(4, 0.219346, new WWComplex[] {
                new WWComplex(-0.126673,0.966086),
                new WWComplex(-0.335093,0.365204),
                }));
            mPoleTable1_5.Add(
                5, new PoleLocation(5, 0.132287, new WWComplex[] {
                new WWComplex(-0.087756,0.977549),
                new WWComplex(-0.269761,0.551805),
                new WWComplex(-0.364010,0),
                }));
            mPoleTable1_5.Add(
                6, new PoleLocation(6, 0.083166, new WWComplex[] {
                new WWComplex(-0.065902,0.983651),
                new WWComplex(-0.222628,0.660911),
                new WWComplex(-0.349191,0.235725),
                }));
            mPoleTable1_5.Add(
                7, new PoleLocation(7, 0.053753, new WWComplex[] {
                new WWComplex(-0.052147,0.987350),
                new WWComplex(-0.188031,0.731006),
                new WWComplex(-0.322204,0.393930),
                new WWComplex(-0.372639,0),
                }));
            mPoleTable1_5.Add(
                8, new PoleLocation(8, 0.035431, new WWComplex[] {
                new WWComplex(-0.042796,0.989795),
                new WWComplex(-0.161907,0.779188),
                new WWComplex(-0.293868,0.504909),
                new WWComplex(-0.369634,0.174714),
                }));
            mPoleTable1_5.Add(
                9, new PoleLocation(9, 0.023698, new WWComplex[] {
                new WWComplex(-0.036079,0.991514),
                new WWComplex(-0.141638,0.814018),
                new WWComplex(-0.267586,0.585850),
                new WWComplex(-0.355221,0.306307),
                new WWComplex(-0.386388,0),
                }));
        }

        private Dictionary<int, PoleLocation> mPoleTable = null;

        public PascalDesign(double h0, double hc, double hs, double ωc, double ωs, ApproximationBase.BetaType bt) {
            if (h0 <= 0) {
                throw new System.ArgumentOutOfRangeException("h0");
            }
            if (hc <= 0 || h0 <= hc) {
                throw new System.ArgumentOutOfRangeException("hc");
            }
            if (hs <= 0 || hc <= hs) {
                throw new System.ArgumentOutOfRangeException("hs");
            }
            if (ωs <= ωc) {
                throw new System.ArgumentOutOfRangeException("ωs");
            }

            if (bt != BetaType.BetaMax) {
                throw new System.ArgumentOutOfRangeException("This version of Pascal approximation only supports betaMax");
            }

            mH0 = h0;
            mHc = hc;
            mHs = hs;
            mωc = ωc;
            mΩs = ωs / ωc;

            double αmax = 20.0 * Math.Log10(mH0 / mHc);
            if (αmax < 0.01) {
                throw new System.ArgumentOutOfRangeException("H0:Hc gain difference is too small");
            } else if (αmax < 0.1) {
                mPoleTable = mPoleTable0_01;
            } else if (αmax < 0.5) {
                mPoleTable = mPoleTable0_1;
            } else if (αmax < 1.0) {
                mPoleTable = mPoleTable0_5;
            } else if (αmax < 1.25) {
                mPoleTable = mPoleTable1;
            } else if (αmax < 1.5) {
                mPoleTable = mPoleTable1_25;
            } else {
                mPoleTable = mPoleTable1_5;
            }

            for (mN = 2; mN<9; ++mN) {
                double stopbandGain = H(new WWComplex(0,mΩs)).Magnitude();
                Console.WriteLine("n={0} G={1}", mN, stopbandGain);
                if (stopbandGain < mHs) {
                    // フィルター完成。
                    return;
                }
            }

            throw new System.ArgumentOutOfRangeException("Filter is too steep to design");
        }

        public override WWComplex PoleNth(int nth) {
            // H. G. Dimopoulos, Analog Electronic Filters: theory, design amd synthesis, Springer, 2012. pp.87
            if (mPoleTable[mN].poleList.Count() <= nth) {
                return WWComplex.ComplexConjugate(mPoleTable[mN].poleList[mN-1-nth]);
            }
            return new WWComplex(mPoleTable[mN].poleList[nth]);
        }

        public override double TransferFunctionConstant() { return mPoleTable[mN].C; }
    };
}
