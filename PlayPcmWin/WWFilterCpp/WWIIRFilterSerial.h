#pragma once

#include "WWIIRFilterGraph.h"

class WWIIRFilterSerial : public WWIIRFilterGraph {
public:
    WWIIRFilterSerial(int nBlocks)
        : WWIIRFilterGraph(nBlocks) {
    }

    // 入力値xを受け取ると、出力yが出てくる。
    // mOsr倍ZOHオーバーサンプルする。
    // buffInはn要素、buffOutはn*mOsr要素。
    void Filter(int nIn, const double *buffIn, double *buffOut) {
        int writePos = 0;
        for (int readPos=0; readPos<nIn; ++readPos) {
            double x = buffIn[readPos];
            for (int os=0; os<mOsr; ++os) {
                double y = x;

                for (int j=0; j<mCount; ++j) {
                    y = mFilterBlockArray[j].Filter(y);
                }

                buffOut[writePos++] = y;
            }
        }
    }
};
