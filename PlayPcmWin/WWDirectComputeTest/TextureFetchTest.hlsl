/*
"GROUP_THREAD_COUNT" = 16
‚ðdefine‚µ‚ÄƒRƒ“ƒpƒCƒ‹‚·‚é
*/

Texture1D                 g_Texture      : register(t0);
RWStructuredBuffer<float> g_OutputBuffer : register(u0);

[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
        uint  tid:        SV_GroupIndex,
        uint3 groupIdXYZ: SV_GroupID)
{
    float f = g_Texture[tid];


    g_OutputBuffer[tid] = f;
}

