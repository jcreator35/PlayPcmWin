using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWCrossFeed {
    class WWRoute {
        List<WWLineSegment> mRoute = new List<WWLineSegment>();

        public int EarCh { get; set; }

        public WWRoute(int earCh) {
            EarCh = earCh;
        }

        public void Add(WWLineSegment line) {
            mRoute.Add(line);
        }

        public void Clear() {
            mRoute.Clear();
        }

        public WWLineSegment GetNth(int idx) {
            return mRoute[idx];
        }

        public int Count() {
            return mRoute.Count;
        }
    }
}
