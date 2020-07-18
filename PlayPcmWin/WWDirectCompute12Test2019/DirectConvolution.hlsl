/*

Direct convolution on GPU.

Requires DirectX 11_0, ComputeShader 5_0 with DoublePrecisionFloatShaderOps feature.

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

#define these constants and compile the shader:

"INPUT_COUNT" : g_InputBuf elem count

"CONV_COUNT"   = convolution coeffs count (odd number)
"CONV_START"   = -CONV_HALF_LEN
"CONV_END"     = CONV_HALF_LEN

"ITERATE_N"          = convolutionN*2/GROUP_THREAD_COUNT
"GROUP_THREAD_COUNT" = 1024

■■■ Note: GROUP_THREAD_COUNT cannot be increased because TGSM size is limited. ■■■

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

Prepare following input data (data sent from CPU to GPU: those values are placed on GPU memory):
float gInputAry[]           : SRV input data of INPUT_COUNT elements.
double gConvCoeffsAryFlip[] : SRV convolution coeffs buffer of CONV_COUNT elements.
■■■ NOTE: Convolution coefficient should be flipped: store end to start to buffer!　■■■

Ex. On triangle conv coeffs, CONV_HALF_LEN=4, CONV_COUNT==8 :
CONV idx                  : -4 -3 -2 -1 +0 +1 +2 +3
Conv coeff buf array index:  0  1  2  3  4  5  6  7
Conv values               :  1  2  3  4  3  2  1  0

Prepare output buffer (Calculation result store on GPU memory):
float gOutputAry[] : UAV output buffer of INPUT_COUNT elements

■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

Set
shaderParams.c_convOffs      = 0
shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
And run(consts=shaderParams, x=INPUT_COUNT, y=1, z=1);

*/

// GPU memory
StructuredBuffer<float>    gInputAry          : register(t0);
StructuredBuffer<double>   gConvCoeffsAryFlip : register(t1);
RWStructuredBuffer<float>  gOutputAry         : register(u0);

/// shader constants.
cbuffer consts {
    /// convolution position offset. Should be multiple of n * GROUP_THREAD_COUNT
    uint cConvOffs;
    uint cDispatchCount;
    uint cSampleStartPos;

    /// pad
    uint cReserved[61];
};

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

