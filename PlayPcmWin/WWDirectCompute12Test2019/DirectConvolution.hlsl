/*

Direct convolution on GPU.
Optimized version.

Requires Direct3D Feature Level 11_0, ComputeShader 5_0
if ELEM_TYPE is double DoublePrecisionFloatShaderOps feature is necessary.

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

#define these constants and compile the shader:

"ELEM_TYPE" : input/output array elem type. float or double
"CONV_TYPE" : conv coeff type. also it is used to internal sum. float or double

"INPUT_COUNT" : g_InputBuf elem count

"CONV_COUNT"    = convolution coeffs count (odd number)
"CONV_START"    = -(CONV_COUNT-1)/2
"CONV_END"      = (CONV_COUNT-1)/2
"CONV_HALF_LEN" = (CONV_COUNT-1)/2

"GROUP_THREAD_COUNT" = 1024

■ Note: GROUP_THREAD_COUNT cannot be increased futher because TGSM size is limited (?) ■

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

Prepare following input data (data sent from CPU to GPU: those values are placed on GPU memory):
ELEM_TYPE gInputAry[]           : SRV input data of INPUT_COUNT elements.
CONV_TYPE gConvCoeffsAryFlip[] : SRV convolution coeffs buffer of CONV_COUNT elements. It should be odd number.
■ Note: Convolution coefficient should be flipped: store end to start to buffer!　■

Prepare output buffer (Calculation result store on GPU memory):
ELEM_TYPE gOutputAry[] : UAV output buffer of INPUT_COUNT elements

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

Set
shaderParams.cSampleStartPos = 0
And run(consts=shaderParams, x=INPUT_COUNT, y=1, z=1);

*/

// GPU memory
StructuredBuffer<ELEM_TYPE>   gInputAry          : register(t0);
StructuredBuffer<CONV_TYPE>   gConvCoeffsAryFlip : register(t1);
RWStructuredBuffer<ELEM_TYPE> gOutputAry         : register(u0);

/// shader constants.
cbuffer consts {
    uint cSampleStartPos;

    /// pad
    uint cReserved[63];
};

// TGSM, total 32KB available ?
groupshared CONV_TYPE s_scratch[GROUP_THREAD_COUNT];

/// @param inoutCenterPos 0 <= inOutCenterPos < Dispatch x
/// @param i CONV_START <= i <= CONV_END
inline CONV_TYPE
Conv1(int inoutCenterPos, int i)
{
    CONV_TYPE r = 0;

    int convIdx = CONV_HALF_LEN + i;
    int inIdx   = inoutCenterPos + i;

    if (0 <= inIdx && inIdx < INPUT_COUNT && convIdx < CONV_COUNT) {
        CONV_TYPE a = gInputAry[inIdx];
        CONV_TYPE c = gConvCoeffsAryFlip[convIdx];
        r = a * c;
    }

    return r;
}

[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
    uint  tid:        SV_GroupIndex, // 0 to GROUP_THREAD_COUNT-1
    uint3 groupIdXYZ : SV_GroupID)   // 0 <= groupIdXYZ.x < Dispatch x
{
    s_scratch[tid] = 0;

    GroupMemoryBarrierWithGroupSync();

    int inoutCenterPos = cSampleStartPos + groupIdXYZ.x;

    int i = tid + CONV_START;
    do {
        s_scratch[tid] += 
            Conv1(inoutCenterPos, i) +
            Conv1(inoutCenterPos, i + GROUP_THREAD_COUNT);
        i += GROUP_THREAD_COUNT * 2;
    } while (i <= CONV_END);

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

        gOutputAry[inoutCenterPos] = (ELEM_TYPE)s_scratch[0];
    }
}

