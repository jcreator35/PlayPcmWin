using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWWaveSimulatorCS {
    public class WaveSim3D {
        /// <summary>
        /// 気圧P スカラー場
        /// </summary>
        double [] mP;

        /// <summary>
        /// 速度V 3次元ベクトル場
        /// </summary>
        WWVector3 [] mV;

        int mGridW; // x
        int mGridH; // y
        int mGridD; // z

        Params.EdgeType mEdgeType = Params.EdgeType.Absolute;

        public WaveSim3D(int gridW, int gridH, int gridD) {
            mGridW = gridW;
            mGridH = gridH;
            mGridD = gridD;

            mP = new double[gridW * gridH * gridD];
            mV = new WWVector3[gridW * gridH * gridD];
        }

        public void Update() {
        }

        public double[] P() {
            return mP;
        }

        public WWVector3[] V() {
            return mV;
        }

        public double P(int x, int y, int z) {
            int pos = x + mGridW * (y + mGridH*z);
            return mP[pos];
        }

        public WWVector3 V(int x, int y, int z) {
            int pos = x + mGridW * (y + mGridH * z);
            return mV[pos];
        }

        public void UpdateP(int x, int y, int z, double p) {
            int pos = x + mGridW * (y + mGridH * z);
            mP[pos] = p;
        }
        public void UpdateV(int x, int y, int z, WWVector3 v) {
            int pos = x + mGridW * (y + mGridH * z);
            mV[pos] = v;
        }
    }
}
