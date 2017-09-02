// 日本語

#pragma once

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <stdexcept>

template <typename T>
class WWLoopFilterCRFB {
public:
    /// @param order CRFBフィルターの次数
    /// @param a 係数a 要素数はorder個。
    /// @param b 係数b 要素数はorder+1個。
    /// @param g 係数g 要素数はorder/2個。
    WWLoopFilterCRFB(int order, const T * a, const T * b, const T *g, T gain) {
        if (order < 2 || 9 < order) {
            throw std::domain_error("order");
        }
        mOrder = order;

        assert(a);
        assert(b);
        assert(g);

        mA = new T[order];
        for (int i=0; i<order; ++i) {
            mA[i] = a[i];
        }

        mB = new T[order+1];
        for (int i=0; i<order+1; ++i) {
            mB[i] = b[i];
        }

        mG = new T[order/2];
        for (int i=0; i<order/2; ++i) {
            mG[i] = b[i];
        }

        // ディレイmZは要素の値を零初期化する。
        mZ = new T[order]();

        mGain = gain;
    }

    ~WWLoopFilterCRFB(void) {
        delete[] mZ;
        mZ = nullptr;

        delete[] mG;
        mG = nullptr;

        delete[] mB;
        mB = nullptr;

        delete[] mA;
        mA = nullptr;
    }

    void Reset(void) {
        memset(mZ, 0, mOrder*sizeof(T));
    }

    /// ストリーム buffInを入力し、フィルター処理、量子化して1bitのbuffOutを出力する。
    /// 1ビットデータのバイト内の並び順はリトルエンディアンビットオーダー。
    /// @param n buffInの要素数(出力ビット数。buffOutのバイト数はn/8になる)。
    void Filter(int n, const T *buffIn, uint8_t *buffOut) {
        // 出力が1ビットデータで、8個集まらないとuint8が出来ないので
        // 入力データの個数は8の倍数である必要がある。
        assert((n%8)==0);

        int readPos = 0;
        int writePos = 0;

        switch (mOrder) {
        case 5:
            // 5次の場合だけ少しだけ最適化した。
            for (int i=0; i<n; i+=8) {
                uint8_t sdm = 0;

                for (int j = 0; j < 8; ++j) {
                    T u = buffIn[readPos++];

                    u *= mGain;

                    T y = mZ[4] + mB[5] * u;
                    int v = (0 <= y) ? 1 : -1;

                    mZ[3] += mZ[2] + mB[3] * u - mA[3] * v - mG[1] * mZ[4];
                    mZ[4] += mZ[3] + mB[4] * u - mA[4] * v;

                    mZ[1] += mZ[0] + mB[1] * u - mA[1] * v - mG[0] * mZ[2];
                    mZ[2] += mZ[1] + mB[2] * u - mA[2] * v;

                    mZ[0] += mB[0] * u - mA[0] * v;

                    sdm += ((0<v) << j);
                }

                buffOut[writePos++]=sdm;
            }
            break;
        case 4:
            // 4次の場合。
            for (int i=0; i<n; i+=8) {
                uint8_t sdm = 0;

                for (int j = 0; j < 8; ++j) {
                    T u = buffIn[readPos++];

                    u *= mGain;

                    T y = mZ[mOrder-1] + mB[mOrder] * u;
                    int v = (0 <= y) ? 1 : -1;

                    mZ[2] += mZ[1] + mB[2] * u - mA[2] * v - mG[1] * mZ[3];
                    mZ[3] += mZ[2] + mB[3] * u - mA[3] * v;

                    mZ[0] += mB[0] * u - mA[0] * v - mG[0] * mZ[1];
                    mZ[1] += mZ[0] + mB[1] * u - mA[1] * v;

                    sdm += ((0<v) << j);
                }

                buffOut[writePos++]=sdm;
            }
            break;
        default:
            // その他の場合。
            for (int i=0; i<n; i+=8) {
                uint8_t sdm = 0;

                for (int j = 0; j < 8; ++j) {
                    int b = FilterN(buffIn[readPos++]);
                    sdm += ((0<b) << j);
                }

                buffOut[writePos++]=sdm;
            }
            break;
        }
    }

    int Order(void) const { return mOrder; }

private:
    int mOrder;
    T *mA;
    T *mB;
    T *mG;
    T *mZ;
    T mGain;

    int FilterN(T u) {
        u *= mGain;

        int odd = (mOrder & 1) == 1 ? 1 : 0;
        // CRFB構造。
        // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.97

        // 最終出力v。
        T y = mZ[mOrder-1] + mB[mOrder] * u;
        int v = (0 <= y) ? 1 : -1;

        if (odd == 1) {
            // 奇数次のCRFB。

            for (int i = mOrder - 2; 1 <= i; i -= 2) {
                // 無遅延積分器のディレイmZ[i]
                mZ[i    ] += mZ[i - 1] + mB[i    ] * u - mA[i    ] * v - mG[i / 2] * mZ[i + 1];
                // 遅延積分器のディレイmZ[i+1]
                mZ[i + 1] += mZ[i    ] + mB[i + 1] * u - mA[i + 1] * v;
            }

            // 奇数次の時最初に遅延積分器がある。mZ[0]の値を更新する。
            mZ[0] += mB[0] * u - mA[0] * v;
        } else {
            // 偶数次のCRFB。

            for (int i = mOrder - 2; 2 <= i; i -= 2) {
                // 無遅延積分器のディレイmZ[i]
                mZ[i] += mZ[i - 1] + mB[i] * u - mA[i] * v - mG[i / 2] * mZ[i + 1];
                // 遅延積分器のディレイmZ[i+1]
                mZ[i + 1] += mZ[i] + mB[i + 1] * u - mA[i + 1] * v;
            }

            // 0番の共振器は1個前の共振器からの入力mZ[-1]が無い。
            // 無遅延積分器のディレイmZ[0]
            mZ[0] += mB[0] * u - mA[0] * v - mG[0] * mZ[1];
            // 遅延積分器のディレイmZ[1]
            mZ[1] += mZ[0] + mB[1] * u - mA[1] * v;
        }

        return v;
    }
};
