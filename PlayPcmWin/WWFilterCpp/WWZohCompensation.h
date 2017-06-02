#pragma once

#include "WWDelay.h"

class WWZohCompensation {
public:
    WWZohCompensation(void);
    ~WWZohCompensation(void);

    void Filter(int count, const double * inPcm, double *outPcm);

private:
    WWDelay<double> mDelay;

    double Convolution(void);
};

