/*

Direct convolution on GPU.

Requires DirectX 11_0, ComputeShader 5_0 with DoublePrecisionFloatShaderOps feature.

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

#define these constants and compile the shader:

"INPUT_COUNT" : g_InputBuf elem count

"CONV_COUNT"   = convolution coeffs count (odd number)
"CONV_START"   = -(CONV_COUNT-1)/2
"CONV_END"     = (CONV_COUNT-1)/2

"GROUP_THREAD_COUNT" = 1024

■ Note: GROUP_THREAD_COUNT cannot be increased futher because TGSM size is limited (?) ■

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

Prepare following input data (data sent from CPU to GPU: those values are placed on GPU memory):
float gInputAry[]           : SRV input data of INPUT_COUNT elements.
double gConvCoeffsAryFlip[] : SRV convolution coeffs buffer of CONV_COUNT elements. It should be odd number.
■ Note: Convolution coefficient should be flipped: store end to start to buffer!　■

Prepare output buffer (Calculation result store on GPU memory):
float gOutputAry[] : UAV output buffer of INPUT_COUNT elements

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

Set
shaderParams.cSampleStartPos = 0
And run(consts=shaderParams, x=INPUT_COUNT, y=1, z=1);

*/

// GPU memory
StructuredBuffer<float>    gInputAry          : register(t0);
StructuredBuffer<double>   gConvCoeffsAryFlip : register(t1);
RWStructuredBuffer<float>  gOutputAry         : register(u0);

/// shader constants.
cbuffer consts {
    uint cSampleStartPos;

    /// pad
    uint cReserved[63];
};

#if 0

// Not optimized code.
[numthreads(1, 1, 1)]
void
CSMain(
    uint  tid:         SV_GroupIndex, //< always zero on this code.
    uint3 groupIdXYZ : SV_GroupID)
{
    uint samplePos = cSampleStartPos + groupIdXYZ.x;

    double v = 0.0;

    for (int i = CONV_START; i <= CONV_END; ++i) {
        int pos = i + samplePos;
        if (0 <= pos && pos < INPUT_COUNT) {
            v += gInputAry[pos] * gConvCoeffsAryFlip[CONV_HALF_LEN + i];
        }
    }

    gOutputAry[samplePos] = (float)v;
}

#else

// Optimized code.

// TGSM, total 32KB available ?
groupshared double s_scratch[GROUP_THREAD_COUNT];

/// @param offs starts from zero
inline double
Conv1(int inoutCenterPos, int convOffs)
{
    double r = 0.0;

    int convIdx = convOffs + CONV_HALF_LEN;
    int inIdx   = inoutCenterPos + convOffs;

    if (0 <= inIdx && inIdx < INPUT_COUNT && convIdx < CONV_COUNT) {
        double a = gInputAry[inIdx];
        double c = gConvCoeffsAryFlip[convIdx];
        r = a * c;
    }

    return r;
}

[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
    uint  tid:        SV_GroupIndex, //< 0 to GROUP_THREAD_COUNT-1
    uint3 groupIdXYZ : SV_GroupID)   //< 0 <= groupIdXYZ.x < mDC.Run x
{
    s_scratch[tid] = 0;

    GroupMemoryBarrierWithGroupSync();

    int inoutCenterPos = cSampleStartPos + groupIdXYZ.x;

    int convOffs = tid + CONV_START;
    do {
        s_scratch[tid] += 
            Conv1(inoutCenterPos, convOffs) +
            Conv1(inoutCenterPos, convOffs + GROUP_THREAD_COUNT);
        convOffs += GROUP_THREAD_COUNT * 2;
    } while (convOffs <= CONV_END);

    GroupMemoryBarrierWithGroupSync();

#if 1024 <= GROUP_THREAD_COUNT
    if (tid < 512) { s_scratch[tid] += s_scratch[tid + 512]; }
    GroupMemoryBarrierWithGroupSync();
#endif

#if 512 <= GROUP_THREAD_COUNT
    if (tid < 256) { s_scratch[tid] += s_scratch[tid + 256]; }
    GroupMemoryBarrierWithGroupSync();
#endif

#if 256 <= GROUP_THREAD_COUNT
    if (tid < 128) { s_scratch[tid] += s_scratch[tid + 128]; }
    GroupMemoryBarrierWithGroupSync();
#endif

#if 128 <= GROUP_THREAD_COUNT
    if (tid < 64) { s_scratch[tid] += s_scratch[tid + 64]; }
    GroupMemoryBarrierWithGroupSync();
#endif

#if 64 <= GROUP_THREAD_COUNT
    if (tid < 32) { s_scratch[tid] += s_scratch[tid + 32]; }
    /* GroupMemoryBarrierWithGroupSync is not necessary after this stage.
     * refer 2260_GTC2010.pdf
     * but, if result is not correct, try sync
     */
     //GroupMemoryBarrierWithGroupSync(); 
#endif

#if 32 <= GROUP_THREAD_COUNT
    if (tid < 16) { s_scratch[tid] += s_scratch[tid + 16]; }
    //GroupMemoryBarrierWithGroupSync();
#endif

#if 16 <= GROUP_THREAD_COUNT
    if (tid < 8) { s_scratch[tid] += s_scratch[tid + 8]; }
    //GroupMemoryBarrierWithGroupSync();
#endif

#if 8 <= GROUP_THREAD_COUNT
    if (tid < 4) { s_scratch[tid] += s_scratch[tid + 4]; }
    //GroupMemoryBarrierWithGroupSync();
#endif

#if 4 <= GROUP_THREAD_COUNT
    if (tid < 2) { s_scratch[tid] += s_scratch[tid + 2]; }
    //GroupMemoryBarrierWithGroupSync();
#endif

    if (tid == 0) {
        s_scratch[0] += s_scratch[1];

        gOutputAry[inoutCenterPos] = (float)s_scratch[0];
    }
}

#endif
