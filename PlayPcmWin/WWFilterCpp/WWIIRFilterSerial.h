#pragma once

#include "WWIIRFilterGraph.h"

class WWIIRFilterSerial : public WWIIRFilterGraph {
public:
    WWIIRFilterSerial(int nBlocks)
        : WWIIRFilterGraph(nBlocks) {
    }

    // 入力値xを受け取ると、出力yが出てくる。
    // mOsr倍ZOHオーバーサンプルする。
    // buffInはnIn要素、buffOutはnOut要素。
    void Filter(int nIn, const double *buffIn, int nOut, double *buffOut) {
        int sampleCounter = 0;
        int writePos = 0;
        for (int readPos=0; readPos<nIn; ++readPos) {
            double x = buffIn[readPos];
            for (int os=0; os<mOsr; ++os) {
                double y = x;

                for (int j=0; j<mCount; ++j) {
                    y = mFilterBlockArray[j].Filter(y);
                }

                if ((sampleCounter % mDecimation)==0 && writePos < nOut) {
                    buffOut[writePos++] = y;
                }

                ++sampleCounter;
            }
        }
    }
};
