/*

Direct convolution on GPU.
Not optimized version.

Requires Direct3D Feature Level 11_0, ComputeShader 5_0 with DoublePrecisionFloatShaderOps feature.

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

#define these constants and compile the shader:

"INPUT_COUNT" : g_InputBuf elem count

"CONV_COUNT"    = convolution coeffs count (odd number)
"CONV_START"    = -(CONV_COUNT-1)/2
"CONV_END"      = (CONV_COUNT-1)/2
"CONV_HALF_LEN" = (CONV_COUNT-1)/2

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

/// @param inoutCenterPos 0 <= inoutCenterPos < Dispatch x == gInputAry size
/// @param i CONV_START <= i <= CONV_END
inline double
Conv1(int inoutCenterPos, int i)
{
    double r = 0.0;

    int convIdx = CONV_HALF_LEN + i;
    int inIdx = inoutCenterPos + i;

    if (0 <= inIdx && inIdx < INPUT_COUNT && convIdx < CONV_COUNT) {
        double a = gInputAry[inIdx];
        double c = gConvCoeffsAryFlip[convIdx];
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

    double acc = 0.0;

    for (int i = CONV_START; i <= CONV_END; ++i) {
        acc += Conv1(inoutCenterPos, i);
    }

    gOutputAry[inoutCenterPos] = (float)acc;
}

