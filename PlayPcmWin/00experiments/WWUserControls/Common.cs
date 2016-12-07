using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WWMath;

namespace WWUserControls {
    public class Common {
        public delegate WWComplex TransferFunctionDelegate(WWComplex s);
        public delegate double TimeDomainResponseFunctionDelegate(double t);
    }
}
