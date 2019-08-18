// 日本語

#pragma once

#include <stdexcept>
#include <string.h>

/// <summary>
/// Building block of IIR filter. used by WWIIRFilter class
/// </summary>
class WWIIRFilterBlock {
private:
    /// <summary>
    /// フィードバックの係数
    /// </summary>
    double mA[3];

    /// <summary>
    /// フィードフォワードの係数 (伝達関数の分子。)
    /// </summary>
    double mB[3];

    /// <summary>
    /// ディレイ
    /// </summary>
    double mV[2];

public:
    WWIIRFilterBlock(void) { }
    
    ~WWIIRFilterBlock(void) { }

    void Initialize(int aCount, const double *a, int bCount, const double *b);

    void Finalize(void);

    double Filter(double x) {
        double y = 0;

        // Transposed Direct form 2 structure
        // Discrete-time signal processing 3rd edition pp.427 figure 6.26 and equation 6.44a-d

        // equation 6.44a and 6.44b
        y = mB[0] * x + mV[0];

        // equation 6.44c
        mV[0] = mA[1] * y + mB[1] * x + mV[1];
        mV[1] = mA[2] * y + mB[2] * x;

        return y;
    }
};

