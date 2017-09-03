// 日本語

#include "WWWindowFunc.h"
#include <assert.h>
#include <math.h>
#include <stdint.h>

/// <summary>
/// 0以上の整数値vの階乗
/// </summary>
static int64_t Factorial(int v) {
    assert(0 <= v);

    // vが21以上でint64_tがオーバーフローする
    assert(v <= 20);

    if (v <= 1) {
        return 1;
    }

    long rv = 1;
    for (int i=2; i <= v; ++i) {
        rv *= i;
    }
    return rv;
}

/// <summary>
/// 0次の第1種変形ベッセル関数I0(alpha)
/// </summary>
/// <param name="alpha">引数</param>
/// <returns>I0(alpha)</returns>
static double ModifiedBesselI0(double alpha) {
    const int L=15;

    double i0 = 1.0;
    for (int l=1; l < L; ++l) {
        double t = pow(alpha * 0.5, l) / Factorial(l);
        i0 += t * t;
    }
    return i0;
}

void WWKaiserWindow(double alpha, int length, double *window_r) {
    // αは4より大きく9より小さい
    assert(4 <= alpha && alpha <= 9);

    // カイザー窓は両端の値が0にならないので普通に計算する。
    int m = length-1;
    for (int i=0; i < length; ++i) {
        int pos = i;

        // 分母i0d
        double i0d = ModifiedBesselI0(alpha);

        // 分子i0n
        double t2 = (1.0 - 2.0 * pos / m);
        double a = alpha * sqrt(1.0 - t2 * t2);
        double i0n = ModifiedBesselI0(a);

        window_r[i] = i0n / i0d;
    }
}
