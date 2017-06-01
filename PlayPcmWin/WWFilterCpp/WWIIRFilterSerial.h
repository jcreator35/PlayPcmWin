#pragma once

#include "WWIIRFilterGraph.h"

class WWIIRFilterSerial : public WWIIRFilterGraph {
public:
    WWIIRFilterSerial(int nBlocks)
        : WWIIRFilterGraph(nBlocks) {
    }

    // ���͒lx���󂯎��ƁA�o��y���o�Ă���B
    // mOsr�{ZOH�I�[�o�[�T���v������B
    // buffIn��n�v�f�AbuffOut��n*mOsr�v�f�B
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
