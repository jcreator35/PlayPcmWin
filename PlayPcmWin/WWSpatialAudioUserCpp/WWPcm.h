// 日本語 UTF-8

#pragma once

#include <vector>
#include <stdint.h>

class WWPcm {
public:
    void Clear(void) {
        pcm.clear();
    }

    std::vector<float> pcm;
};

