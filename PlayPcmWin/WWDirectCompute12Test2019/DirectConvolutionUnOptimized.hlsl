/*

Direct convolution on GPU.
Not optimized version.

Requires Direct3D Feature Level 11_0, ComputeShader 5_0
if ELEM_TYPE is double DoublePrecisionFloatShaderOps feature is necessary.


■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

#define these constants and compile the shader:

"ELEM_TYPE" : input/output array type. float or double
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
ELEM_TYPE gInputAry[]          : SRV input data of INPUT_COUNT elements.
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

/// @param inoutCenterPos 0 <= inoutCenterPos < Dispatch x == gInputAry size
/// @param i CONV_START <= i <= CONV_END
inline CONV_TYPE
Conv1(int inoutCenterPos, int i)
{
    CONV_TYPE r = 0;

    int convIdx = CONV_HALF_LEN + i;
    int inIdx = inoutCenterPos + i;

    if (0 <= inIdx && inIdx < INPUT_COUNT && convIdx < CONV_COUNT) {
        CONV_TYPE a = gInputAry[inIdx];
        CONV_TYPE c = gConvCoeffsAryFlip[convIdx];
        r = a * c;
    }

    return r;
}

[numthreads(1, 1, 1)]
void
CSMain(
    uint  tid:         SV_GroupIndex, // Always zero on this code.
    uint3 groupIdXYZ : SV_GroupID)    // 0 <= groupIdXYZ.x < Dispatch x
{
    uint inoutCenterPos = cSampleStartPos + groupIdXYZ.x;

    CONV_TYPE acc = 0;

    for (int i = CONV_START; i <= CONV_END; ++i) {
        acc += Conv1(inoutCenterPos, i);
    }

    gOutputAry[inoutCenterPos] = (ELEM_TYPE)acc;
}

