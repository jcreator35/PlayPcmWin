/*

Requires DirectX 11_1, ComputeShader 5_0 with DoublePrecisionFloatShaderOps feature.

basically, calc following code on GPU

for (int toPos=0; toPos<sampleTotalTo; ++toPos) {
        int    fromPos  = resamplePosArray[toPos];
        double fraction = fractionArray[toPos];
        double sinPreCompute = sinPreComputeArray[toPos];

        double v = 0.0;

        for (int convOffs=CONV_START; convOffs < CONV_END; ++convOffs) {
            int pos = convOffs + fromPos;
            if (0 <= pos && pos < sampleTotalFrom) {
                double x = PI_D * (convOffs - fraction);

                double sinX = sinPreCompute;
                if (convOffs & 1) {
                    sinX *= -1.0;
                }

                double sinc =  SincD(sinX, x);

                v += sampleData[pos] * sinc;
            }
        }
        outputTo[toPos] = (float)v;
    }

"CONV_START"   = -convolutionN
"CONV_END"     = convolutionN
"CONV_COUNT"   = convolutionN*2
"SAMPLE_TOTAL_FROM" = sampleTotalFrom
"SAMPLE_TOTAL_TO"   = sampleTotalTo

"SAMPLE_RATE_FROM"   = sampleRateFrom
"SAMPLE_RATE_TO"     = sampleRateTo

"ITERATE_N"          = convolutionN*2/GROUP_THREAD_COUNT
"GROUP_THREAD_COUNT" = 1024

GROUP_THREAD_COUNT is constant value and cannot increase
because TGSM resource size is limited

set
shaderParams.c_convOffs = 0
shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;

and run(shaderParams, sampleN, 1, 1);

prepare following input data:
float SampleFromBuffer    : sampleFrom[] of SAMPLE_TOTAL_FROM elements
uint ResamplePosBuffer resample position array of SAMPLE_TOTAL_TO elements
float FractionBuffer resample position fraction number of SAMPLE_TOTAL_TO elements
float SinPreComputeBuffer sin(-fractionBuffer * PI) of SAMPLE_TOTAL_TO elements

output buffer:
float OutputBuffer[0] to OutputBuffer[sampleN-1]

*/

#define PI_F 3.141592653589793238462643f
#define PI_D 3.141592653589793238462643

/// constants need to be multiple of 16
cbuffer consts {
    /// convolution position offset. Should be multiple of n * GROUP_THREAD_COUNT
    uint c_convOffs;

    uint c_dispatchCount;

    /// add this value to toPos
    uint c_sampleToStartPos;

    /// pad
    uint c_reserved2;
};

inline double
SincD(double sinx, double x)
{
    if (-2.2204460492503131e-016 < x && x < 2.2204460492503131e-016) {
        return 1.0;
    } else {
#if 1
        // DirectX 11_1 or later
        return sinx * rcp(x);
#else
        // DirectX 11_0: do not have rcp(double) !
        float xf = 1.0f / (float)x;
        return sinx * xf;
#endif
    }
}

/* Read optimization using thread group and TGSM
 * in order to output 1 sample, need to calc following
 * ・g_ResamplePosBuffer   1 read
 * ・g_FractionBuffer      1 read
 * ・g_SinPreComputeBuffer 1 read
 * store those values on TGSM
 * each thread calc respective convolution position calculation and store result to s_scratch
 */

 // use doubleprec if possible

 // GPU memory
StructuredBuffer<float>   g_SampleFromBuffer    : register(t0);
StructuredBuffer<int>     g_ResamplePosBuffer   : register(t1);
StructuredBuffer<double>  g_FractionBuffer      : register(t2);
StructuredBuffer<double>  g_SinPreComputeBuffer : register(t3);
RWStructuredBuffer<float> g_OutputBuffer        : register(u0);

// TGSM, 32KB available ?
groupshared double s_scratch[GROUP_THREAD_COUNT];
groupshared int    s_fromPos;
groupshared double s_fraction;
groupshared double s_sinPreCompute;

inline double
ConvolutionElemValue(int convOffs)
{
    double r = 0.0;

    int pos = convOffs + s_fromPos;
    if (0 <= pos && pos < SAMPLE_TOTAL_FROM) {
        double x = PI_D * ((double)convOffs - s_fraction);

        double sinX = s_sinPreCompute;
        if (convOffs & 1) {
            sinX *= -1.0;
        }

        double sinc = SincD(sinX, x);

        r = g_SampleFromBuffer[pos] * sinc;
    }

    return r;
}

#if 1
[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
    uint  tid:        SV_GroupIndex,
    uint3 groupIdXYZ : SV_GroupID)
{
    if (tid == 0) {
        uint toPos = c_sampleToStartPos + groupIdXYZ.x;
        s_fromPos = g_ResamplePosBuffer[toPos];
        s_fraction = g_FractionBuffer[toPos];
        s_sinPreCompute = g_SinPreComputeBuffer[toPos];
    }
    s_scratch[tid] = 0;
    int offs = (int)tid + CONV_START;

    GroupMemoryBarrierWithGroupSync();

    do {
        s_scratch[tid] +=
            ConvolutionElemValue(offs) +
            ConvolutionElemValue(offs + GROUP_THREAD_COUNT);
        offs += GROUP_THREAD_COUNT * 2;
    } while (offs < CONV_END);

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

        uint toPos = c_sampleToStartPos + groupIdXYZ.x;
        g_OutputBuffer[toPos] = (float)s_scratch[0];
    }
}
#endif

#if 0

// Before optimization. Painfully slow because only 1 GPU unit is used!

inline double
Sinc(double sinx, float x)
{
    if (-1.192092896e-07F < x && x < 1.192092896e-07F) {
        return 1.0;
    } else {
        return sinx * rcp(x);
    }
}

[numthreads(1, 1, 1)]
void
CSMain(
    uint  tid:        SV_GroupIndex,
    uint3 groupIdXYZ : SV_GroupID)
{
    uint toPos = c_sampleToStartPos + groupIdXYZ.x;

    s_fromPos = g_ResamplePosBuffer[toPos];
    s_fraction = g_FractionBuffer[toPos];
    s_sinPreCompute = g_SinPreComputeBuffer[toPos];

    double v = 0.0;

    for (int convOffs = CONV_START; convOffs < CONV_END; ++convOffs) {
        int pos = convOffs + fromPos;
        if (0 <= pos && pos < SAMPLE_TOTAL_FROM) {
            float x = PI_F * (convOffs - fraction);

            double sinX = s_sinPreCompute;
            if (convOffs & 1) {
                sinX *= -1.0;
            }

            double sinc = Sinc(sinX, x);

            v += g_SampleFromBuffer[pos] * sinc;
        }
    }
    g_OutputBuffer[toPos] = (float)v;
}
#endif

