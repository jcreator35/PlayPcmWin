#pragma once 

#include <stdexcept>
#include <assert.h>

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
    }

    ~WWDelay(void) {
        delete [] mDelay;
        mDelay = nullptr;
    }

    int DelaySamples(void) const { return mDelayLength; }

    T Filter(T x) {
        // ���XmDelay[mPos]�ɓ����Ă����l��y�ɕ������Ă���x�ŏ㏑������B
        // ����2�s�͏��Ԃ��d�v��
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
    /// nth�T���v���ߋ��̃T���v���l��߂��B
    /// </summary>
    /// <param name="nth">0: �ŐV�̃T���v���A1: 1�T���v���ߋ��̃T���v���B</param>
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
