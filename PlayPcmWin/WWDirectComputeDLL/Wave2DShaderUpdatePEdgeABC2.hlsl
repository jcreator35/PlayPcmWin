/* 日本語UTF-8

2次元 2次 Absorbing Boundary Condition

gPDelayIn/Out = (p.fieldW + p.fieldH)*12; //< 各点あたり6個、上端分と下端分で計12倍(2次ABC)

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

    float k = -1.0f / (1.0f/ScPrime + 2.0f + ScPrime);
    float k0 = k * (1.0f/ScPrime -2.0f + ScPrime);
    float k1 = 2.0f * k * (ScPrime - 1.0f / ScPrime);
    float k2 = -4.0f * k * (1.0f/ScPrime + ScPrime);

    if (x == 0) {
        // 左端。
        int delayOffs = 0;

        // m:位置、q:時刻
        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + y * 6 + 0];
        float p1_1 = gPdelayIn[delayOffs + y * 6 + 1];
        float p2_1 = gPdelayIn[delayOffs + y * 6 + 2];
        float p0_2 = gPdelayIn[delayOffs + y * 6 + 3];
        float p1_2 = gPdelayIn[delayOffs + y * 6 + 4];
        float p2_2 = gPdelayIn[delayOffs + y * 6 + 5];

        float p1_0 = gPin[1 + y * FIELD_W];
        float p2_0 = gPin[2 + y * FIELD_W];
        
        float p0_0 = k0 * (p2_0 + p0_2) + k1 * (p0_1 + p2_1 - p1_0 - p1_2)
                    + k2 * p1_1 - p2_2;

        gPout[pos] = p0_0;

        gPdelayOut[delayOffs + y * 6 + 0] = p0_0;
        gPdelayOut[delayOffs + y * 6 + 1] = p1_0;
        gPdelayOut[delayOffs + y * 6 + 2] = p2_0;
        gPdelayOut[delayOffs + y * 6 + 3] = p0_1;
        gPdelayOut[delayOffs + y * 6 + 4] = p1_1;
        gPdelayOut[delayOffs + y * 6 + 5] = p2_1;
    } else if (x == FIELD_W-1) {
        // 右端。
        int delayOffs = FIELD_H * 6;

        // m:位置、q:時刻
        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + y * 6 + 0];
        float p1_1 = gPdelayIn[delayOffs + y * 6 + 1];
        float p2_1 = gPdelayIn[delayOffs + y * 6 + 2];
        float p0_2 = gPdelayIn[delayOffs + y * 6 + 3];
        float p1_2 = gPdelayIn[delayOffs + y * 6 + 4];
        float p2_2 = gPdelayIn[delayOffs + y * 6 + 5];

        float p1_0 = gPin[(FIELD_W - 2) + y * FIELD_W];
        float p2_0 = gPin[(FIELD_W - 3) + y * FIELD_W];

        float p0_0 = k0 * (p2_0 + p0_2) + k1 * (p0_1 + p2_1 - p1_0 - p1_2)
                    + k2 * p1_1 - p2_2;

        gPout[pos] = p0_0;

        gPdelayOut[delayOffs + y * 6 + 0] = p0_0;
        gPdelayOut[delayOffs + y * 6 + 1] = p1_0;
        gPdelayOut[delayOffs + y * 6 + 2] = p2_0;
        gPdelayOut[delayOffs + y * 6 + 3] = p0_1;
        gPdelayOut[delayOffs + y * 6 + 4] = p1_1;
        gPdelayOut[delayOffs + y * 6 + 5] = p2_1;
    } else if (y == 0) {
        // 上端。
        int delayOffs = FIELD_H * 12;

        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + x * 6 + 0];
        float p1_1 = gPdelayIn[delayOffs + x * 6 + 1];
        float p2_1 = gPdelayIn[delayOffs + x * 6 + 2];
        float p0_2 = gPdelayIn[delayOffs + x * 6 + 3];
        float p1_2 = gPdelayIn[delayOffs + x * 6 + 4];
        float p2_2 = gPdelayIn[delayOffs + x * 6 + 5];

        float p1_0 = gPin[x + 1 * FIELD_W];
        float p2_0 = gPin[x + 2 * FIELD_W];

        float p0_0 = k0 * (p2_0 + p0_2) + k1 * (p0_1 + p2_1 - p1_0 - p1_2)
                    + k2 * p1_1 - p2_2;

        gPout[pos] = p0_0;
        
        gPdelayOut[delayOffs + x * 6 + 0] = p0_0;
        gPdelayOut[delayOffs + x * 6 + 1] = p1_0;
        gPdelayOut[delayOffs + x * 6 + 2] = p2_0;
        gPdelayOut[delayOffs + x * 6 + 3] = p0_1;
        gPdelayOut[delayOffs + x * 6 + 4] = p1_1;
        gPdelayOut[delayOffs + x * 6 + 5] = p2_1;
    } else if (y == FIELD_H-1) {
        // 下端。
        int delayOffs = FIELD_H * 12 + FIELD_W * 6;

        //     m q
        //    p0_0をこれから計算する。
        float p0_1 = gPdelayIn[delayOffs + x * 6 + 0];
        float p1_1 = gPdelayIn[delayOffs + x * 6 + 1];
        float p2_1 = gPdelayIn[delayOffs + x * 6 + 2];
        float p0_2 = gPdelayIn[delayOffs + x * 6 + 3];
        float p1_2 = gPdelayIn[delayOffs + x * 6 + 4];
        float p2_2 = gPdelayIn[delayOffs + x * 6 + 5];

        float p1_0 = gPin[x + (FIELD_H - 2) * FIELD_W];
        float p2_0 = gPin[x + (FIELD_H - 3) * FIELD_W];

        float p0_0 = k0 * (p2_0 + p0_2) + k1 * (p0_1 + p2_1 - p1_0 - p1_2)
                    + k2 * p1_1 - p2_2;

        gPout[pos] = p0_0;
        
        gPdelayOut[delayOffs + x * 6 + 0] = p0_0;
        gPdelayOut[delayOffs + x * 6 + 1] = p1_0;
        gPdelayOut[delayOffs + x * 6 + 2] = p2_0;
        gPdelayOut[delayOffs + x * 6 + 3] = p0_1;
        gPdelayOut[delayOffs + x * 6 + 4] = p1_1;
        gPdelayOut[delayOffs + x * 6 + 5] = p2_1;
    } else {
        gPout[pos] = gPin[pos];
    }
}

