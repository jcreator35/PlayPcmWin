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
RWStructuredBuffer<float> gPout           : register(u2);

[numthreads(LENGTH, 1, 1)]
void CSUpdateP(uint i: SV_GroupIndex)
{
    if (i==0) {
        // ABC for P (Schneider17, pp.53)
        gPout[0] = gPin[1];
    } else {
#if 0
        // For testing
        float lastP = gPin[i];
        float V = (gVin[i] - gVin[i-1]);
        gPout[i] = lastP - V;
#else
        // Update P (Schneider17, pp.325)
        float lastP = gPin[i];
        float V = gVin[i] - gVin[i-1];
        float loss = gLoss[i];
        float Cp = gRoh[i] * gCr[i] * gCr[i] * C0 * SC;
        gPout[i] = (1.0f - loss) *rcp(1.0f + loss) * lastP - (Cp *rcp(1.0f + loss)) * V;
#endif
    }
}

