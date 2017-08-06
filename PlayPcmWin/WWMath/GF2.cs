
namespace WWMath {
    /// <summary>
    /// GF(2)
    /// </summary>
    public class GF2 {
        public int Val {
            get {
                return mValue;
            }
        }

        public static GF2 Zero {
            get {
                return sInstance[0];
            }
        }

        public static GF2 One {
            get {
                return sInstance[1];
            }
        }

        public static GF2 Add(GF2 a, GF2 b) {
            // GF(2)の加算: xor
            int v = a.Val ^ b.Val;
            return sInstance[v];
        }

        public static GF2 Mul(GF2 a, GF2 b) {
            // GF(2)の乗算: and
            int v = a.Val & b.Val;
            return sInstance[v];
        }

        /// <summary>
        /// 自分自身は変更しない。
        /// 戻り値 := this + a
        /// </summary>
        public GF2 Add(GF2 a) {
            return GF2.Add(this, a);
        }

        /// <summary>
        /// 自分自身は変更しない。
        /// 戻り値 := this + a
        /// </summary>
        public GF2 Mul(GF2 a) {
            return GF2.Mul(this, a);
        }

        public override string ToString() {
            return ( mValue == 0 ) ? "0" : "1";
        }

        private int mValue;

        // flyweight pattern
        private GF2(int val) {
            mValue = val;
        }

        private static GF2[] sInstance = new GF2[] {
            new GF2(0),
            new GF2(1),
        };
    }
}
