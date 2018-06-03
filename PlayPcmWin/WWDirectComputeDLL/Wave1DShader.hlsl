/*
define
"LENGTH" = "256"

THREAD_X==LENGTH��Dispatch����B
*/

// SRV
Texture1D                 gLoss         : register(t0);
Texture1D                 gRoh          : register(t1);
Texture1D                 gCr           : register(t2);

// UAV
RWTexture1D<float>        gV            : register(u0);
RWTexture1D<float>        gP            : register(u1);

// Shader Constant�B16�o�C�g�̔{���̃T�C�Y�̍\���́B
cbuffer consts {
    // �X�V�����̌J��Ԃ��񐔁B
    int cRepeat;

    // �p�����[�^Sc
    float cSc;
    
    // �p�����[�^C0
    float cC0;

    int cStimCounter;
    int cStimPosX;
    float cStimMagnitude;
    float cStimHalfPeriod;
    float cStimWidth;
};

int gCounter;

void InitGaussianStim()
{
    gCounter = cStimCounter;
}

void UpdateGaussianStim()
{
    if (gCounter < 0) {
        return;
    }
    --gCounter;

    float fr = exp(-(gCounter - cStimHalfPeriod) * (gCounter - cStimHalfPeriod) * cStimWidth);

    gP[gStimPosX] += cStimMagnitude * fr;
}

void UpdateV(uint i)
{
    if (i==LENGTH-1) {
        // ABC for V (Schneider17, pp.53)
        gV[LENGTH-1] = gV[LENGTH-2];
    } else {
        // Update V (Schneider17, pp.328)
        float lastV = gV[i];
        float loss = gLoss[i];
        float Cv = 2.0f * cSc / ((gRoh[i]+gRoh[i+1])*cC0);
        gV[i] = (1.0f - loss) / (1.0f + loss) * lastV - (Cv / (1.0f + loss)) * (gP[i + 1] - gP[i]);
    }
}

void UpdateP(uint i)
{
    if (i==0) {
        // ABC for P (Schneider17, pp.53)
        gP[0] = gP[1];
    } else {
        // Update P (Schneider17, pp.325)
        float lastP = gP[i];
        float loss = gLoss[i];
        float Cp = gRoh[i] * gCr[i] * gCr[i] * cC0 * cSc;
        gP[i] = (1.0f - loss) / (1.0f + loss) * lastP - (Cp / (1.0f + loss)) * (gV[i] - gV[i - 1]);
    }
}

[numthreads(LENGTH, 1, 1)]
void CSMain(uint tid: SV_GroupIndex)
{
    if (tid == 0) {
        InitGaussianStim();
    }

    for (int i=0; i<cRepeat; ++i) {
        if (tid == 0) {
            UpdateGaussianStim();
        }
        GroupMemoryBarrierWithGroupSync();

        UpdateV(tid);
        GroupMemoryBarrierWithGroupSync();
        UpdateP(tid);
        GroupMemoryBarrierWithGroupSync();
    }
}

