// 日本語 UTF-8

#pragma once

#include <vector>
#include <stdint.h>

class WWPcm {
public:
    void Clear(void) {
        pos = 0;
        pcm.clear();
    }

    int64_t pos = 0;
    std::vector<float> pcm;
};

