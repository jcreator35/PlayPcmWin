#pragma once

#include "WWIIRFilterGraph.h"

class WWIIRFilterParallel : public WWIIRFilterGraph {
public:
    WWIIRFilterParallel(int nBlocks)
        : WWIIRFilterGraph(nBlocks) {
    }

    // 入力値xを受け取ると、出力yが出てくる。
    // mOsr倍ZOHオーバーサンプルする。
    // buffInはn要素、buffOutはn*mOsr要素。
    void Filter(int nIn, const double *buffIn, double *buffOut) {
        int writePos = 0;
        for (int readPos=0; readPos<nIn; ++readPos) {
            for (int os=0; os<mOsr; ++os) {
                double x = buffIn[readPos];
                double y = 0;

                for (int j=0; j<mCount; ++j) {
                    y += mFilterBlockArray[j].Filter(x);
                }

                buffOut[writePos++] = y;
            }
        }
    }
};
