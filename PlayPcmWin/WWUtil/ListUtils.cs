using System;
using System.Collections.Generic;

namespace WWUtil {
    public class ListUtils<T> {
        public static T[] ArrayListToArray(List<T[]> al) {
            int count = 0;
            foreach (var item in al) {
                count += item.Length;
            }

            var r = new T[count];
            int pos = 0;
            foreach (var item in al) {
                Array.Copy(item, 0, r, pos, item.Length);
                pos += item.Length;
            }

            return r;
        }
    }
}
