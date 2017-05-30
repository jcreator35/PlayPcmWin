#include "stdafx.h"
#include "WWLoopFilterCRFB.h"
#include <stdlib.h>
#include <assert.h>
#include <stdexcept>

WWLoopFilterCRFB::WWLoopFilterCRFB(int order, const double * a,
        const double * b, const double *g, double gain)
{
    if (order < 2 || 9 < order) {
        throw std::domain_error("order");
    }
    mOrder = order;

    assert(a);
    assert(b);
    assert(g);

    mA = new double[order];
    for (int i=0; i<order; ++i) {
        mA[i] = a[i];
    }

    mB = new double[order+1];
    for (int i=0; i<order+1; ++i) {
        mB[i] = b[i];
    }

    mG = new double[order/2];
    for (int i=0; i<order/2; ++i) {
        mG[i] = b[i];
    }

    // �f�B���CmZ�͗v�f�̒l��돉��������B
    mZ = new double[order]();

    mGain = gain;
}

WWLoopFilterCRFB::~WWLoopFilterCRFB(void)
{
    delete[] mZ;
    mZ = nullptr;

    delete[] mG;
    mG = nullptr;

    delete[] mB;
    mB = nullptr;

    delete[] mA;
    mA = nullptr;
}

void
WWLoopFilterCRFB::Reset(void)
{
    memset(mZ, 0, mOrder*sizeof(double));
}

int
WWLoopFilterCRFB::FilterN(double u)
{
    u *= mGain;

    int odd = (mOrder & 1) == 1 ? 1 : 0;
    // CRFB�\���B
    // R. Schreier and G. Temes, �����^�A�i���O/�f�W�^���ϊ������,�ۑP,2007, pp.97

    // �ŏI�o��v�B
    double y = mZ[mOrder-1] + mB[mOrder] * u;
    int v = (0 <= y) ? 1 : -1;

    if (odd == 1) {
        // �����CRFB�B

        for (int i = mOrder - 2; 1 <= i; i -= 2) {
            // ���x���ϕ���̃f�B���CmZ[i]
            mZ[i    ] += mZ[i - 1] + mB[i    ] * u - mA[i    ] * v - mG[i / 2] * mZ[i + 1];
            // �x���ϕ���̃f�B���CmZ[i+1]
            mZ[i + 1] += mZ[i    ] + mB[i + 1] * u - mA[i + 1] * v;
        }

        // ����̎��ŏ��ɒx���ϕ��킪����BmZ[0]�̒l���X�V����B
        mZ[0] += mB[0] * u - mA[0] * v;
    } else {
        // ��������CRFB�B

        for (int i = mOrder - 2; 2 <= i; i -= 2) {
            // ���x���ϕ���̃f�B���CmZ[i]
            mZ[i] += mZ[i - 1] + mB[i] * u - mA[i] * v - mG[i / 2] * mZ[i + 1];
            // �x���ϕ���̃f�B���CmZ[i+1]
            mZ[i + 1] += mZ[i] + mB[i + 1] * u - mA[i + 1] * v;
        }

        // 0�Ԃ̋��U���1�O�̋��U�킩��̓���mZ[-1]�������B
        // ���x���ϕ���̃f�B���CmZ[0]
        mZ[0] += mB[0] * u - mA[0] * v - mG[0] * mZ[1];
        // �x���ϕ���̃f�B���CmZ[1]
        mZ[1] += mZ[0] + mB[1] * u - mA[1] * v;
    }

    return v;
}

int
WWLoopFilterCRFB::Filter5(double u)
{
    assert(mOrder==5);
    u *= mGain;

    double y = mZ[4] + mB[5] * u;
    int v = (0 <= y) ? 1 : -1;

    mZ[3] += mZ[2] + mB[3] * u - mA[3] * v - mG[1] * mZ[4];
    mZ[4] += mZ[3] + mB[4] * u - mA[4] * v;

    mZ[1] += mZ[0] + mB[1] * u - mA[1] * v - mG[0] * mZ[2];
    mZ[2] += mZ[1] + mB[2] * u - mA[2] * v;

    mZ[0] += mB[0] * u - mA[0] * v;

    return v;
}

void
WWLoopFilterCRFB::Filter(int n, const double *buffIn, uint8_t *buffOut)
{
    int readPos = 0;
    int writePos = 0;

    if (mOrder==5) {
        // 5���̏ꍇ�������������œK�������B
        for (int i=0; i<n; i+=8) {
            uint8_t sdm = 0;

            for (int j = 0; j < 8; ++j) {
                int b = Filter5(buffIn[readPos++]);
                sdm += ((0<b) << j);
            }

            buffOut[writePos++]=sdm;
        }
    } else {
        for (int i=0; i<n; i+=8) {
            uint8_t sdm = 0;

            for (int j = 0; j < 8; ++j) {
                int b = FilterN(buffIn[readPos++]);
                sdm += ((0<b) << j);
            }

            buffOut[writePos++]=sdm;
        }
    }
}

