#pragma once
#include "WWIIRFilterBlock.h"

class WWIIRFilter {
public:
    enum FilterType {
        WWIIRFilterTypeSerial,
        WWIIRFilterTypeParallel,
    };

    WWIIRFilter(FilterType t) : mFilterType(t) { }


private:
    FilterType mFilterType;
    WWIIRFilterBlock *mFilterArray;
};
