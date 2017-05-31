#pragma once

#include <stdint.h>

class WWLoopFilterCRFB {
public:
    /// @param order CRFB�t�B���^�[�̎���
    /// @param a �W��a �v�f����order�B
    /// @param b �W��b �v�f����order+1�B
    /// @param g �W��g �v�f����order/2�B
    WWLoopFilterCRFB(int order, const double * a, const double * b,
            const double *g, double gain);

    ~WWLoopFilterCRFB(void);

    void Reset(void);

    /// �X�g���[�� buffIn����͂��A�t�B���^�[�����A�ʎq������1bit��buffOut���o�͂���B
    /// 1�r�b�g�f�[�^�̃o�C�g���̕��я��̓��g���G���f�B�A���r�b�g�I�[�_�[�B
    /// @param n buffIn�̗v�f��(�o�̓r�b�g���BbuffOut�̃o�C�g����n/8�ɂȂ�)�B
    void Filter(int n, const double *buffIn, uint8_t *buffOut);

    int Order(void) const { return mOrder; }

private:
    int mOrder;
    double *mA;
    double *mB;
    double *mG;
    double *mZ;
    double mGain;

    int FilterN(double u);
};
