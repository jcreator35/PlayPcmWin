/*
define
"LENGTH" = "256"
"REPEAT" = "1" (更新処理の繰り返し回数)
"gSc"    = Scのfloat値
"gC0"    = C0のfloat値

THREAD_X==LENGTHでDispatchする。
*/

Texture1D                 gLoss         : register(t0);
Texture1D                 gRoh          : register(t1);
Texture1D                 gCr           : register(t2);

Texture1D                 gV            : register(u0);
Texture1D                 gP            : register(u1);

void UpdateV(uint i)
{
    if (i==LENGTH-1) {
        // ABC for V (Schneider17, pp.53)
        gV[LENGTH-1] = gV[LENGTH-2];
    } else {
        // Update V (Schneider17, pp.328)
        float lastV = gV[i];
        float loss = gLoss[i];
        float Cv = 2.0f * gSc / ((gRoh[i]+gRoh[i+1])*gC0);
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
        float Cp = gRoh[i] * gCr[i] * gCr[i] * gC0 * gSc;
        gP[i] = (1.0f - loss) / (1.0f + loss) * lastP - (Cp / (1.0f + loss)) * (gV[i] - gV[i - 1]);
    }
}

[numthreads(LENGTH, 1, 1)]
void CSMain(uint tid: SV_GroupIndex)
{
    for (int i=0; i<REPEAT; ++i) {
        UpdateV(tid);
        UpdateP(tid);
    }
}

