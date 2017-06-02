#pragma once

#include "WWIIRFilterGraph.h"

class WWIIRFilterParallel : public WWIIRFilterGraph {
public:
    WWIIRFilterParallel(int nBlocks)
        : WWIIRFilterGraph(nBlocks) {
    }

    // 入力値xを受け取ると、出力yが出てくる。
    // mOsr倍ZOHオーバーサンプルする。
    void Filter(int nIn, const double *buffIn, int nOut, double *buffOut) {
        int sampleCounter = 0;
        int writePos = 0;
        for (int readPos=0; readPos<nIn; ++readPos) {
            for (int os=0; os<mOsr; ++os) {
                double x = buffIn[readPos];
                double y = 0;

                for (int j=0; j<mCount; ++j) {
                    y += mFilterBlockArray[j].Filter(x);
                }

                if ((sampleCounter % mDecimation)==0 && writePos < nOut) {
                    buffOut[writePos++] = y;
                }
                ++sampleCounter;
            }
        }
    }
};
