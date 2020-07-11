// 日本語UTF-8

/*

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

を計算する

"CONV_START"   = -convolutionN
"CONV_END"     = convolutionN
"CONV_COUNT"   = convolutionN*2
"SAMPLE_TOTAL_FROM" = sampleTotalFrom
"SAMPLE_TOTAL_TO"   = sampleTotalTo

"SAMPLE_RATE_FROM"   = sampleRateFrom
"SAMPLE_RATE_TO"     = sampleRateTo
"ITERATE_N"          = convolutionN*2/GROUP_THREAD_COUNT
"GROUP_THREAD_COUNT" = 1024

を#defineしてCS5.0 DirectCompute シェーダーとしてコンパイルする。

// シェーダー定数を渡す
shaderParams.c_convOffs = 0
shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
ComputeShaderのrun(shaderParams, sampleN, 1, 1);

する。

用意するデータ

①SampleFromBuffer…サンプルデータsampleFrom[]
float
SAMPLE_TOTAL_FROM要素

②ResamplePosBuffer リサンプル地点配列
uint
SAMPLE_TOTAL_TO要素

③FractionBuffer リサンプル地点の小数点以下
float
SAMPLE_TOTAL_TO要素

④SinPreComputeBuffer sin(-fractionBuffer * π)
float
SAMPLE_TOTAL_TO要素

⑤出力バッファー
OutputBuffer[0]～OutputBuffer[sampleN-1]
OutputBuffer[]はsampleN個用意する

*/

#define PI_F 3.141592653589793238462643f
#define PI_D 3.141592653589793238462643

/// 定数。16バイトの倍数のサイズの構造体。
cbuffer consts {
    /// 畳み込み要素オフセット値。n * GROUP_THREAD_COUNTの飛び飛びの値が渡る。
    uint c_convOffs;
    /// Dispatch繰り返し回数。
    uint c_dispatchCount;

    /// toPosにこの値を足す。
    uint c_sampleToStartPos;
    uint c_reserved2;
};

inline double
Sinc(double sinx, float x)
{
    if (-1.192092896e-07F < x && x < 1.192092896e-07F) {
        return 1.0;
    } else {
        return sinx * rcp(x);
    }
}

inline double
SincD(double sinx, double x)
{
    if (-2.2204460492503131e-016 < x && x < 2.2204460492503131e-016) {
        return 1.0;
    } else {
        float xf = 1.0f / (float)x;
        return sinx * xf;
    }
}

/* スレッドグループとTGSMを使用して、GPUメモリからの読み出し回数を減らす最適化。
 * 1個の出力サンプルを計算するためには、
 * ・g_ResamplePosBuffer   1回読み出し。
 * ・g_FractionBuffer      1回読み出し。
 * ・g_SinPreComputeBuffer 1回読み出し
 * で良いので、TGSMに蓄える。
 * 各スレッドは、自分の担当convolution位置の計算を行ってs_scratchに入れる。
 */

#if HIGH_PRECISION
// できるだけdoubleprec

// GPUメモリー
StructuredBuffer<float>   g_SampleFromBuffer    : register(t0);
StructuredBuffer<int>     g_ResamplePosBuffer   : register(t1);
StructuredBuffer<double>  g_FractionBuffer      : register(t2);
StructuredBuffer<double>  g_SinPreComputeBuffer : register(t3);
RWStructuredBuffer<float> g_OutputBuffer        : register(u0);

// TGSM
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

        double sinc =  SincD(sinX, x);

        r = g_SampleFromBuffer[pos] * sinc;
    }

    return r;
}
#else
 // 主にsingleprec

// GPUメモリー
StructuredBuffer<float>   g_SampleFromBuffer    : register(t0);
StructuredBuffer<int>     g_ResamplePosBuffer   : register(t1);
StructuredBuffer<float>   g_FractionBuffer      : register(t2);
StructuredBuffer<float>   g_SinPreComputeBuffer : register(t3);
RWStructuredBuffer<float> g_OutputBuffer        : register(u0);

// TGSM
groupshared double s_scratch[GROUP_THREAD_COUNT];
groupshared int    s_fromPos;
groupshared float  s_fraction;
groupshared float  s_sinPreCompute;

inline double
ConvolutionElemValue(int convOffs)
{
    double r = 0.0;

    int pos = convOffs + s_fromPos;
    if (0 <= pos && pos < SAMPLE_TOTAL_FROM) {
        double x = PI_D * ((double)convOffs - (double)s_fraction);
                
        double sinX = s_sinPreCompute;
        if (convOffs & 1) {
            sinX *= -1.0;
        }

        double sinc =  SincD(sinX, x);

        r = g_SampleFromBuffer[pos] * sinc;
    }

    return r;
}

#endif // HIGH_PRECISION

#if 1
[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
        uint  tid:        SV_GroupIndex,
        uint3 groupIdXYZ: SV_GroupID)
{
    if (tid == 0) {
        uint toPos = c_sampleToStartPos + groupIdXYZ.x;
        s_fromPos       = g_ResamplePosBuffer[toPos];
        s_fraction      = g_FractionBuffer[toPos];
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
    /* これ以降GroupMemoryBarrierWithGroupSyncは要らないらしい。
     * 2260_GTC2010.pdf参照。
     * だが、動作が怪しくなるのでうまくいかない場合はSyncしてみると良い。
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
// 最適化前。

[numthreads(1, 1, 1)]
void
CSMain(
        uint  tid:        SV_GroupIndex,
        uint3 groupIdXYZ: SV_GroupID)
{
    uint toPos = c_sampleToStartPos + groupIdXYZ.x;

    s_fromPos       = g_ResamplePosBuffer[toPos];
    s_fraction      = g_FractionBuffer[toPos];
    s_sinPreCompute = g_SinPreComputeBuffer[toPos];

    double v = 0.0;

    for (int convOffs=CONV_START; convOffs < CONV_END; ++convOffs) {
        int pos = convOffs + fromPos;
        if (0 <= pos && pos < SAMPLE_TOTAL_FROM) {
            float x = PI_F * (convOffs - fraction);
                
            double sinX = sinPreCompute;
            if (convOffs & 1) {
                sinX *= -1.0;
            }

            double sinc =  Sinc(sinX, x);

            v += g_SampleFromBuffer[pos] * sinc;
        }
    }
    g_OutputBuffer[toPos] = (float)v;
}
#endif

