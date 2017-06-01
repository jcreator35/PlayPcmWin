#pragma once

#include "WWIIRFilterGraph.h"

class WWIIRFilterParallel : public WWIIRFilterGraph {
public:
    WWIIRFilterParallel(int nBlocks)
        : WWIIRFilterGraph(nBlocks) {
    }

    // ���͒lx���󂯎��ƁA�o��y���o�Ă���B
    // mOsr�{ZOH�I�[�o�[�T���v������B
    // buffIn��n�v�f�AbuffOut��n*mOsr�v�f�B
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
