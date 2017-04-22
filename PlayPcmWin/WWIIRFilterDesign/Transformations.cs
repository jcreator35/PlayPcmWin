using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWIIRFilterDesign {
    public class Transformations {

        public static FirstOrderComplexRationalPolynomial LowpassToHighpass(FirstOrderComplexRationalPolynomial p) {
            /*
             z^-1 → -z^-1に置き換える。
             
              n1 z^-1 + n0     -n1 z^-1 + n0
             ────────────── → ───────────────
              d1 z^-1 + d0     -d1 z^-1 + d0
             
             */

            var r = new FirstOrderComplexRationalPolynomial(
                p.N(1).Minus(), p.N(0),
                p.D(1).Minus(), p.D(0));
            return r;
        }
    }
}
