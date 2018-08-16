/* 日本語UTF-8
define
"FIELD_W" = "1024" (一例)
"FIELD_H" = "1024" (一例)
"THREAD_W" = "16" (固定)
"THREAD_H" = "16" (固定)

上記設定の場合、Dispatchは (FIELD_W/THREAD_W, FIELD_H/THREAD_W, 1)
で呼び出す。FIELD_?はTHREAD_?で割り切れる。

*/

// UAV
RWStructuredBuffer<float>  gPin            : register(u1);
RWTexture2D<float2>        gPout           : register(u2);

[numthreads(THREAD_W, THREAD_H, 1)]
void CSCopyP(uint3 tid: SV_DispatchThreadID)
{
    int x = tid.x;
    int y = tid.y;

    gPout[tid.xy] = gPin[x + y * FIELD_W];
}

