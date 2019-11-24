// 日本語 UTF-8

#pragma once

#include "WWPcm.h"

#define NUM_CHANNELS (12)

class WWPcmStore {
public:
    WWPcmStore(void) {
    }

    void Clear(void) {
        for (int i = 0; i < NUM_CHANNELS; ++i) {
            mPcm[i].Clear();
        }
    }

    WWPcm mPcm[NUM_CHANNELS];
};
