/* 日本語UTF-8
define
"LENGTH" = "256"
"SC" == mC0 * mΔt / mΔx
"C0" == 334 (m/s)
THREAD_X==LENGTHでDispatchする。

*/

#define PI (3.14159265358979f)

// SRV
StructuredBuffer<float>          gLoss         : register(t0);
StructuredBuffer<float>          gRoh          : register(t1);
StructuredBuffer<float>          gCr           : register(t2);

// UAV
RWStructuredBuffer<float> gVin            : register(u0);
RWStructuredBuffer<float> gPin            : register(u1);
RWStructuredBuffer<float> gVout           : register(u2);

[numthreads(LENGTH, 1, 1)]
void CSUpdateV(uint i: SV_GroupIndex)
{
    if (i==LENGTH-1) {
        // ABC for V (Schneider17, pp.53)
        gVout[LENGTH-1] = gVin[LENGTH-2];
    } else {
#if 0
        // For testing
        float lastV = gVin[i];
        float P = (gPin[i + 1] - gPin[i]);
        gVout[i] = lastV - P;
#else
        // Update V (Schneider17, pp.328)
        float lastV = gVin[i];
        float P = (gPin[i + 1] - gPin[i]);
        float loss = gLoss[i];
        float roh = (gRoh[i]+gRoh[i+1])*0.5f;
        float Cv = SC * rcp(roh * C0);
        gVout[i] = (1.0f - loss) * rcp(1.0f + loss) * lastV - (Cv *rcp(1.0f + loss)) * P;
#endif
    }
}

