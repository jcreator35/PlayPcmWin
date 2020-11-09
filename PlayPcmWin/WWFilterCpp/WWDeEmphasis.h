// 日本語

#pragma once

#include "WWFIRFilter.h"

class WWDeEmphasis {
public:
    WWDeEmphasis(void);
    ~WWDeEmphasis(void);

    void Filter(int count, const double * inPcm, double *outPcm);

private:
    WWFIRFilter mFIRFilter;
};

