/* 日本語UTF-8
define
"FIELD_W" = "1024" (一例)
"FIELD_H" = "1024" (一例)
"THREAD_W" = "16" (固定)
"THREAD_H" = "16" (固定)
"SC" == mC0 * mΔt / mΔx
"C0" == 334 (m/s)

上記設定の場合、Dispatchは (FIELD_W/THREAD_W, FIELD_H/THREAD_W, 1)
で呼び出す。FIELD_?はTHREAD_?で割り切れる。

*/

// SRV
StructuredBuffer<float>          gLoss         : register(t0);
StructuredBuffer<float>          gRoh          : register(t1);
StructuredBuffer<float>          gCr           : register(t2);

// UAV
RWStructuredBuffer<float2> gVin            : register(u0);
RWStructuredBuffer<float>  gPin            : register(u1);
RWStructuredBuffer<float>  gPout           : register(u2);

[numthreads(THREAD_W, THREAD_H, 1)]
void CSUpdateP(uint3 tid: SV_DispatchThreadID)
{
    int x = tid.x;
    int y = tid.y;

    // Update P (Schneider17, pp.325)
    if (x == 0 || y == 0) {
    } else {
        int pos = x + y * FIELD_W;
        float loss = gLoss[pos];
        float2 v = gVin[pos];
        float2 vL = gVin[pos-1];
        float2 vU = gVin[x + (y-1) * FIELD_W];
        float Cp = gRoh[pos] * gCr[pos] * gCr[pos] * C0 * SC;

        gPout[pos] = (1.0f - loss) * rcp(1.0f + loss) * gPin[pos]
            - (Cp * rcp(1.0f + loss))
            * (v.x - vL.x + v.y - vU.y);
    }
}

