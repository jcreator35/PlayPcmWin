using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class WWVectorF2 {

        private float[] v = new float[2];
        public WWVectorF2() {
        }

        public WWVectorF2(float x, float y) {
            v[0] = x;
            v[1] = y;
        }

        public float X {
            get {
                return v[0];
            }
        }

        public float Y {
            get { 
                return v[1];
            }
        }
    }
}
