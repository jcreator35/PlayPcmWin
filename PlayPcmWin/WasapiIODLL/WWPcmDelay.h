#pragma once

// 日本語 UTF-8

#include "WWPcmData.h"
#include <assert.h>

/// nサンプルのディレイ
class WWPcmDelay {
public:
        WWPcmDelay(void) : mDelayLength(0), mPos(0), mDelay(nullptr) {
        }

        ~WWPcmDelay(void) {
            Term();
        }

        void Init(int n);

        void Term(void);

        float Filter(float x) {
            // 元々mDelay[mPos]に入っていた値をyに複製してからxで上書きする。
            // この2行は順番が重要だ
            float y = mDelay[mPos];
            mDelay[mPos] = x;

            // advance the position
            ++mPos;
            if (mDelayLength <= mPos) {
                mPos = 0;
            }

            return y;
        }

        /// <summary>
        /// nthサンプル過去のサンプル値を戻す。
        /// </summary>
        /// <param name="nth">0: 最新のサンプル、1: 1サンプル過去のサンプル。</param>
        float GetNthDelayedSampleValue(int nth) {
            int pos = mPos - 1 - nth;

            if (pos < 0) {
                pos += mDelayLength;
            }

            assert(0 <= pos && pos < mDelayLength);
            return mDelay[pos];
        }

        void FillZeroes(void);

        int DelaySamples(void) { mDelayLength; }

private:
    int mDelayLength;
    int mPos;
    float *mDelay;
};
