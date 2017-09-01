#pragma once

#include "WWCicDownsampler.h"
#include "WWHalfbandFilterDownsampler.h"
#include <stdint.h>

/** 1�`�����l����SDM�X�g���[������͂���64����1�_�E���T���v������float�^��PCM�o�͂𓾂�B
 * SDM�X�g���[����CIC�_�E���T���v���[��16����1�_�E���T���v������
 * �n�[�t�o���h�t�B���^�[�_�E���T���v���[��2����1�_�E���T���v��
 * �n�[�t�o���h�t�B���^�[�_�E���T���v���[��2����1�_�E���T���v��
 * 16 * 2 * 2 = 64x
 */
class WWSdmToPcm {
public:
    WWSdmToPcm(void) : mHBDS23(23), mHBDS47(47), mTmp1Count(0), mTmp2Count(0),
        mOutPcm(nullptr), mOutCount(0), mTotalOutSamples(0) { }

    ~WWSdmToPcm(void);

    void Start(int totalOutSamples);

    /// Sdm�f�[�^��16�T���v����������B
    /// @param inSdm 1�r�b�g��SDM�f�[�^��16�A�r�b�O�G���f�B�A���r�b�g�I�[�_�[�œ����Ă���B
    void AddInputSamples(const uint16_t inSdm) {
        mTmp1Pcm[mTmp1Count++] = mCicDS.Filter(inSdm);

        if (2 == mTmp1Count) {
            mTmp1Count = 0;

            mHBDS23.Filter(mTmp1Pcm, 2, &mTmp2Pcm[mTmp2Count++]);
            if (2 == mTmp2Count) {
                mTmp2Count = 0;
                mHBDS47.Filter(mTmp2Pcm, 2, &mOutPcm[mOutCount++]);
            }
        }
    }

    // ���ׂđ�������ĂԁB(�t���b�V�����ăf�B���C�ɑؗ����Ă���T���v�����o���B)
    void Drain(void);

    // ���Ɏ����Ă���o�̓o�b�t�@�[�̍ŏ��̏o�̓f�[�^���w���Ă���|�C���^��߂��B
    const float *GetOutputPcm(void) const;

    // ���Ɏ����Ă���o�̓o�b�t�@�[���폜����B
    void End(void);

    int FilterDelay(void) const {
        return 13;
    }

private:
    WWCicDownsampler mCicDS;
    WWHalfbandFilterDownsampler mHBDS23;
    WWHalfbandFilterDownsampler mHBDS47;
    float mTmp1Pcm[2];
    int mTmp1Count;
    float mTmp2Pcm[2];
    int mTmp2Count;
    float *mOutPcm;
    int mOutCount;
    int mTotalOutSamples;
};
