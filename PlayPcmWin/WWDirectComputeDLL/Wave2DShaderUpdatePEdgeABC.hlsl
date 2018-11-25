/* 日本語UTF-8

2次元 1次 Absorbing Boundary Condition

gPDelayIn/Out = (p.fieldW + p.fieldH)*4; //< 各点あたり2個、上端分と下端分で計4倍(1次ABC)

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
StructuredBuffer<float>    gCr             : register(t0);

// UAV
RWStructuredBuffer<float>  gPin            : register(u0);
RWStructuredBuffer<float>  gPout           : register(u1);
RWStructuredBuffer<float>  gPdelayIn       : register(u2);
RWStructuredBuffer<float>  gPdelayOut      : register(u3);

[numthreads(THREAD_W, THREAD_H, 1)]
void CSUpdate(uint3 tid: SV_DispatchThreadID)
{
    int x = tid.x;
    int y = tid.y;
    int pos = x + y * FIELD_W;
    float ScPrime = SC * gCr[pos];

    if (x == 0) {
        // 左端。
        int delayOffs = 0;

        // m:位置、q:時刻
        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + y * 2 + 0];
        float p1_1 = gPdelayIn[delayOffs + y * 2 + 1];

        float p1_0 = gPin[x + 1 + y * FIELD_W];
        
        float p0_0 = p1_1 + (ScPrime - 1) / (ScPrime + 1) * (p1_0 - p0_1);
        gPout[pos] = p0_0;

        gPdelayOut[delayOffs + y * 2 + 0] = p0_0;
        gPdelayOut[delayOffs + y * 2 + 1] = p1_0;
    } else if (x == FIELD_W-1) {
        // 右端。
        int delayOffs = FIELD_H * 2;

        // m:位置、q:時刻
        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + y * 2 + 0];
        float p1_1 = gPdelayIn[delayOffs + y * 2 + 1];

        float p1_0 = gPin[x - 1 + y * FIELD_W];

        float p0_0 = p1_1 + (ScPrime - 1) / (ScPrime + 1) * (p1_0 - p0_1);
        gPout[pos] = p0_0;

        gPdelayOut[delayOffs + y * 2 + 0] = p0_0;
        gPdelayOut[delayOffs + y * 2 + 1] = p1_0;
    } else if (y == 0) {
        // 上端。
        int delayOffs = FIELD_H * 4;

        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + x * 2 + 0];
        float p1_1 = gPdelayIn[delayOffs + x * 2 + 1];

        float p1_0 = gPin[x + 1 * FIELD_W];

        float p0_0 = p1_1 + (ScPrime - 1) / (ScPrime + 1) * (p1_0 - p0_1);
        gPout[pos] = p0_0;
        
        gPdelayOut[delayOffs + x * 2 + 0] = p0_0;
        gPdelayOut[delayOffs + x * 2 + 1] = p1_0;
    } else if (y == FIELD_H-1) {
        // 下端。
        int delayOffs = FIELD_H * 4 + FIELD_W * 2;

        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + x * 2 + 0];
        float p1_1 = gPdelayIn[delayOffs + x * 2 + 1];

        float p1_0 = gPin[x + (FIELD_H - 2) * FIELD_W];

        float p0_0 = p1_1 + (ScPrime - 1) / (ScPrime + 1) * (p1_0 - p0_1);
        gPout[pos] = p0_0;
        
        gPdelayOut[delayOffs + x * 2 + 0] = p0_0;
        gPdelayOut[delayOffs + x * 2 + 1] = p1_0;
    } else {
        gPout[pos] = gPin[pos];
    }
}

