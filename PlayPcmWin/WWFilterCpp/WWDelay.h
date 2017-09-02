// 日本語

#pragma once 

#include <stdexcept>
#include <assert.h>
#include <stdlib.h>

template <typename T>
class WWDelay {
private:
    // ring buffer
    int mPos;
    int mDelayLength;
    T * mDelay;

public:
    /// <summary>
    /// n samples delay
    /// </summary>
    WWDelay(int n) {
        if (n < 1) {
            throw std::invalid_argument("n");
        }

        mPos = 0;
        mDelayLength = n;
        mDelay = new T[n];
        memset(mDelay, 0, sizeof(T)*n);
    }

    ~WWDelay(void) {
        delete [] mDelay;
        mDelay = nullptr;
    }

    int DelaySamples(void) const { return mDelayLength; }

    T Filter(T x) {
        // 元々mDelay[mPos]に入っていた値をyに複製してからxで上書きする。
        // この2行は順番が重要だ
        T y = mDelay[mPos];
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
    T GetNth(int nth) {
        int pos = mPos - 1 - nth;

        if (pos < 0) {
            pos += mDelayLength;
        }

        assert(0 <= pos && pos < mDelayLength);
        return mDelay[pos];
    }

    void FillZeroes(void) {
        Fill(0);
    }

    void Fill(T v) {
        for (int i = 0; i < mDelayLength; ++i) {
            mDelay[i] = v;
        }
        mPos = 0;
    }
};
