#pragma once

#include <Windows.h>

class WWWave1DGpu {
public:
    WWWave1DGpu(void);
    ~WWWave1DGpu(void);

    void Init(void);

    HRESULT
    Run(int cRepeat, float sc, float c0, int stimCounter,
            int stimPosX, float stimMagnitude, float stimHalfPeriod,
            float stimWidth, int dataCount, float *loss,
            float *roh, float *cr, float *v, float *p);

    // @return �R�s�[�����v�f���B
    int CopyResultV(float *vTo, int count);
    // @return �R�s�[�����v�f���B
    int CopyResultP(float *pTo, int count);

    void Term(void);

private:
    int    mCount;
    float *mV;
    float *mP;
};
