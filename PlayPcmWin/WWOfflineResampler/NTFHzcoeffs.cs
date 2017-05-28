using System;
using WWMath;

namespace WWOfflineResampler {
    /// <summary>
    /// poles and zeros of H = synthesizeNTF(osr==64,opt==1)
    /// R.Schreier and G.Temes, Understanding Delta-Sigma Data Converters, §8
    ///
    ///                 k-1   z - ZeroNth(i)
    /// k次のNTFのH(z) =  Π  ─────────────────
    ///                 i=0   z - PoleNth(i)
    ///
    ///                  k-1
    ///                    Σ N(i)z^i
    ///                  i=0
    ///               = ───────────────
    ///                  k-1
    ///                    Σ D(i)z^i
    ///                  i=0
    /// </summary>
    public class NTFHzcoeffs {
        private int mOrder;

        private RealPolynomial mNumer;
        private RealPolynomial mDenom;

        public int Order {
            get { return mOrder; }
        }

        public NTFHzcoeffs(int order) {
            if (order < 2 || 9 < order) {
                throw new ArgumentOutOfRangeException("order");
            }

            mOrder = order;

            {
                if (0 == (order & 1)) {
                    mNumer = new RealPolynomial(new double[] { 1.0 });
                } else {
                    mNumer = new RealPolynomial(new double[] { ZeroNth(mOrder / 2).Minus().real, 1 });
                }
                for (int i = mOrder / 2 - 1; 0 <= i; --i) {
                    var coeff = new WWComplex[] { ZeroNth(i).Minus(), WWComplex.Unity() };
                    var pair = WWPolynomial.MulComplexConjugatePair(new ComplexPolynomial(coeff));
                    mNumer = WWPolynomial.Mul(mNumer, pair);
                }
            }
            {
                if (0 == (order & 1)) {
                    mDenom = new RealPolynomial(new double[] { 1.0 });
                } else {
                    mDenom = new RealPolynomial(new double[] { PoleNth(mOrder / 2).Minus().real, 1 });
                }
                for (int i = mOrder / 2-1; 0 <= i; --i) {
                    var coeff = new WWComplex[] { PoleNth(i).Minus(), WWComplex.Unity() };
                    var pair = WWPolynomial.MulComplexConjugatePair(new ComplexPolynomial(coeff));
                    mDenom = WWPolynomial.Mul(mDenom, pair);
                }
            }
        }

        public double N(int n) {
            return mNumer.C(n);
        }

        public double D(int n) {
            return mDenom.C(n);
        }

        public WWComplex ZeroNth(int n) {
            switch (mOrder) {
            case 2:
                return mZero2[n];
            case 3:
                return mZero3[n];
            case 4:
                return mZero4[n];
            case 5:
                return mZero5[n];
            case 6:
                return mZero6[n];
            case 7:
                return mZero7[n];
            case 8:
                return mZero8[n];
            case 9:
                return mZero9[n];
            default:
                throw new ArgumentOutOfRangeException("n");
            }
        }

        public WWComplex PoleNth(int n) {
            switch (mOrder) {
            case 2:
                return mPole2[n];
            case 3:
                return mPole3[n];
            case 4:
                return mPole4[n];
            case 5:
                return mPole5[n];
            case 6:
                return mPole6[n];
            case 7:
                return mPole7[n];
            case 8:
                return mPole8[n];
            case 9:
                return mPole9[n];
            default:
                throw new ArgumentOutOfRangeException("n");
            }
        }

        private readonly static WWComplex[] mZero2 = new WWComplex[] {
            new WWComplex(0.99959843164790452,-0.028336821399895074),
            new WWComplex(0.99959843164790452,+0.028336821399895074),
        };

        private readonly static WWComplex[] mPole2 = new WWComplex[] {
            new WWComplex(0.61239792049008979,-0.25749599646682375),
            new WWComplex(0.61239792049008979,+0.25749599646682375),
        };

        private readonly static WWComplex[] mZero3 = new WWComplex[] {
            new WWComplex(0.99927721567022176,-0.038013763854283067),
            new WWComplex(1,0),
            new WWComplex(0.99927721567022176,+0.038013763854283067),
        };

        private readonly static WWComplex[] mPole3 = new WWComplex[] {
            new WWComplex(0.7652022012394929,-0.27949784299477298),
            new WWComplex(0.66916385038998427,0),
            new WWComplex(0.7652022012394929,+0.27949784299477298),
        };

        private readonly static WWComplex[] mZero4 = new WWComplex[] {
            new WWComplex(0.99910671726894029,-0.042258342467277693),
            new WWComplex(0.99986074553561111,-0.016688005781757095),
            new WWComplex(0.99986074553561111,+0.016688005781757095),
            new WWComplex(0.99910671726894029,+0.042258342467277693),
        };

        private readonly static WWComplex[] mPole4 = new WWComplex[] {
            new WWComplex(0.85073692167013315,-0.25115084455367426),
            new WWComplex(0.74599389446009401,-0.088115291323076994),
            new WWComplex(0.74599389446009401,+0.088115291323076994),
            new WWComplex(0.85073692167013315,+0.25115084455367426),
        };

        private readonly static WWComplex[] mZero5 = new WWComplex[] {
            new WWComplex(0.99901083899123211,-0.044467331582122752),
            new WWComplex(0.99965069369221016,-0.026428972751189283),
            new WWComplex(1,                  0),
            new WWComplex(0.99965069369221016,+0.026428972751189283),
            new WWComplex(0.99901083899123211,+0.044467331582122752),
        };

        private readonly static WWComplex[] mPole5 = new WWComplex[] {
            new WWComplex(0.89868121129605072,-0.21884241162067616),
            new WWComplex(0.80745323549041315,-0.11936322897692095),
            new WWComplex(0.77872275371010113,0),
            new WWComplex(0.80745323549041315,+0.11936322897692095),
            new WWComplex(0.89868121129605072,+0.21884241162067616),
        };

        private readonly static WWComplex[] mZero6 = new WWComplex[] {
            new WWComplex(0.99895262226016712,-0.04575651297395765),
            new WWComplex(0.9994733157346799, -0.032451365677345759),
            new WWComplex(0.99993140054479901,-0.011712992978597386),

            new WWComplex(0.99993140054479901,+0.011712992978597386),
            new WWComplex(0.9994733157346799, +0.032451365677345759),
            new WWComplex(0.99895262226016712,+0.04575651297395765),
        };

        private readonly static WWComplex[] mPole6 = new WWComplex[] {
            new WWComplex(0.92730202307588183,-0.19114212738876282),
            new WWComplex(0.85220750147188917,-0.12734836589094306),
            new WWComplex(0.81530056334839318,-0.044170176371949685),

            new WWComplex(0.81530056334839318,+0.044170176371949685),
            new WWComplex(0.85220750147188917,+0.12734836589094306),
            new WWComplex(0.92730202307588183,+0.19114212738876282),
        };

        private readonly static WWComplex[] mZero7 = new WWComplex[] {
            new WWComplex(0.99891491845731339,-0.046572370386516161),
            new WWComplex(0.99933760016539919,-0.036391769614305548),
            new WWComplex(0.99980156737524672,-0.019920488799223011),
            new WWComplex(1,                  0),
            new WWComplex(0.99980156737524672,+0.019920488799223011),
            new WWComplex(0.99933760016539919,+0.036391769614305548),
            new WWComplex(0.99891491845731339,+0.046572370386516161),
        };

        private readonly static WWComplex[] mPole7 = new WWComplex[] {
            new WWComplex(0.94550675650246885,-0.1685918179206885),
            new WWComplex(0.88430530362135007,-0.12573890748261188),
            new WWComplex(0.84713481037601968,-0.066384893359361263),
            new WWComplex(0.83481209475345819,0),
            new WWComplex(0.84713481037601968,+0.066384893359361263),
            new WWComplex(0.88430530362135007,+0.12573890748261188),
            new WWComplex(0.94550675650246885,+0.1685918179206885),
        };

        private readonly static WWComplex[] mZero8 = new WWComplex[] {
            new WWComplex(0.9988892044462494, -0.047120666811271417),
            new WWComplex(0.9992354464041886, -0.039096325267496908),
            new WWComplex(0.99966727422552282,-0.025794201722739683),
            new WWComplex(0.99995946024013505,-0.0090043254193575457),

            new WWComplex(0.99995946024013505,+0.0090043254193575457),
            new WWComplex(0.99966727422552282,+0.025794201722739683),
            new WWComplex(0.9992354464041886, +0.039096325267496908),
            new WWComplex(0.9988892044462494, +0.047120666811271417),
        };

        private readonly static WWComplex[] mPole8 = new WWComplex[] {
            new WWComplex(0.9577219279013468, -0.15031047003303374),
            new WWComplex(0.90757212269318754,-0.12033898114671127),
            new WWComplex(0.87305024424099287,-0.076976344186339696),
            new WWComplex(0.85566579486125016,-0.026402580880934043),

            new WWComplex(0.85566579486125016,+0.026402580880934043),
            new WWComplex(0.87305024424099287,+0.076976344186339696),
            new WWComplex(0.90757212269318754,+0.12033898114671127),
            new WWComplex(0.9577219279013468, +0.15031047003303374),
        };

        private readonly static WWComplex[] mZero9 = new WWComplex[] {
            new WWComplex(0.99887092573062408,-0.047506565125739642),
            new WWComplex(0.9991580361900011, -0.041027048601384584),
            new WWComplex(0.99954676561830269,-0.030104208044555814),
            new WWComplex(0.99987333305151704,-0.015915962190522496),
            new WWComplex(1,                  0),
            new WWComplex(0.99987333305151704,+0.015915962190522496),
            new WWComplex(0.99954676561830269,+0.030104208044555814),
            new WWComplex(0.9991580361900011, +0.041027048601384584),
            new WWComplex(0.99887092573062408,+0.047506565125739642),
        };

        private readonly static WWComplex[] mPole9 = new WWComplex[] {
            new WWComplex(0.96628215460154498,-0.1353632930185984),
            new WWComplex(0.92475384380873382,-0.11366837560354798),
            new WWComplex(0.89372288460352667,-0.081261840816732128),
            new WWComplex(0.87475522797274419,-0.042196160337686694),
            new WWComplex(0.86839586271196278,0),
            new WWComplex(0.87475522797274419,-0.042196160337686694),
            new WWComplex(0.89372288460352667,-0.081261840816732128),
            new WWComplex(0.92475384380873382,-0.11366837560354798),
            new WWComplex(0.96628215460154498,-0.1353632930185984),
        };
    }
}
