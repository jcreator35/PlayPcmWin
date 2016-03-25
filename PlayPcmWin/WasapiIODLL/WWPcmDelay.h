#pragma once

// 日本語 UTF-8

#include "WWPcmData.h"
#include <assert.h>

/// nサンプルのディレイ
class WWPcmDelay {
public:
        WWPcmDelay(void) : mStoreSamples(0), mPos(0), mDelay(nullptr) {
        }

        ~WWPcmDelay(void) {
            Term();
        }

        void Init(int n);

        void Term(void);

        float Filter(float x) {
            mDelay[mPos] = x;

            // advance the position
            ++mPos;
            if (mStoreSamples <= mPos) {
                mPos = 0;
            }

            float y = mDelay[mPos];
            return y;
        }

        /// <summary>
        /// nthサンプル過去のサンプル値を戻す。
        /// </summary>
        /// <param name="nth">0: 最新のサンプル、1: 1サンプル過去のサンプル。</param>
        float GetNthDelayedSampleValue(int nth) const {
            int pos = mPos - 1 - nth;

            if (pos < 0) {
                pos += mStoreSamples;
            }

            assert(0 <= pos && pos < mStoreSamples);
            return mDelay[pos];
        }

        void FillZeroes(void);

        int DelaySamples(void) const { return mStoreSamples-1; }
        int StoreSamples(void) const { return mStoreSamples; }

private:
    int mStoreSamples;
    int mPos;
    float *mDelay;
};
