#pragma once

#include <stdint.h>

class WWLoopFilterCRFB {
public:
    /// @param order CRFBフィルターの次数
    /// @param a 係数a 要素数はorder個。
    /// @param b 係数b 要素数はorder+1個。
    /// @param g 係数g 要素数はorder/2個。
    WWLoopFilterCRFB(int order, const double * a, const double * b,
            const double *g, double gain);

    ~WWLoopFilterCRFB(void);

    void Reset(void);

    /// ストリーム buffInを入力し、フィルター処理、量子化して1bitのbuffOutを出力する。
    /// 1ビットデータのバイト内の並び順はリトルエンディアンビットオーダー。
    /// @param n buffInの要素数(出力ビット数。buffOutのバイト数はn/8になる)。
    void Filter(int n, const double *buffIn, uint8_t *buffOut);

    int Order(void) const { return mOrder; }

private:
    int mOrder;
    double *mA;
    double *mB;
    double *mG;
    double *mZ;
    double mGain;

    int FilterN(double u);
};
