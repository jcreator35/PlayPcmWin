// 日本語UTF-8

/*

OutputBuffer[t+convolutionN] = Σ[sample[t+x] * sinc(πx + XBuffer[t])]
CONV_START <= x < CONV_END
を計算する

convolutionN = 256
sampleN = 100
の場合

CONV_START = -256
CONV_END   =  256
CONV_COUNT =  512
SAMPLE_N   =  100
GROUP_THREAD_COUNT 2の乗数
を#defineしてCS5.0 DirectCompute シェーダーとしてコンパイルする。

// シェーダー定数を渡す
shaderParams.c_convOffs = 0
shaderParams.c_dispatchCount = convolutionN*2/GROUP_THREAD_COUNT;
ComputeShaderのrun(shaderParams, sampleN, 1, 1);

する。

用意するデータ

①SampleDataBuffer…前後を水増しされたサンプルデータsample[t]
SampleDataBuffer[0]～SampleDataBuffer[convolutionN-1]…0を詰める
SampleDataBuffer[convolutionN]～SampleDataBuffer[convolutionN + sampleN-1]…サンプルデータsample[t]
SampleDataBuffer[convolutionN+SampleN]～SampleDataBuffer[convolutionN*2 + sampleN-1]…0を詰める

②SinxBuffer リサンプル地点のsin(x) 適当に作る
SinxBuffer[0]～SinxBuffer[sampleN-1] sin(x)の値

③XBuffer リサンプル地点x
XBuffer[0]～XBuffer[sampleN-1] xの値

④出力バッファー
OutputBuffer[0]～OutputBuffer[sampleN-1]
OutputBuffer[]はsampleN個用意する

*/
#ifdef HIGH_PRECISION
// 主にdouble精度

StructuredBuffer<float>   g_SampleDataBuffer : register(t0);
StructuredBuffer<double>  g_SinxBuffer       : register(t1);
StructuredBuffer<float>   g_XBuffer          : register(t2);
RWStructuredBuffer<float> g_OutputBuffer     : register(u0);

/// 定数。16バイトの倍数のサイズの構造体。
cbuffer consts {
    /// 畳み込み要素オフセット値。n * GROUP_THREAD_COUNTの飛び飛びの値が渡る。
    uint c_convOffs;
    /// Dispatch繰り返し回数。
    uint c_dispatchCount;
    uint c_reserved1;
    uint c_reserved2;
};

inline double
SincF(double sinx, float x)
{
    if (-0.000000001f < x && x < 0.000000001f) {
        return 1.0;
    } else {
        // 割り算ができないので、ここで精度落ちる。残念。
        return sinx * rcp(x);
    }
}

#define PI_F 3.141592653589793238462643f

// TGSM
groupshared double s_scratch[GROUP_THREAD_COUNT];
groupshared double s_sinX;
groupshared float  s_xOffs;

/// 畳み込み計算要素1回実行。
/// sample[t+x] * sinc(πx + XBuffer[t])
inline double
ConvolutionElemValue(uint pos, uint convOffs)
{
    const int offs = c_convOffs + convOffs;
    const float x = mad(PI_F, offs + CONV_START, s_xOffs);
    return ((double)g_SampleDataBuffer[offs + pos]) * SincF(s_sinX, x);
}

// スレッドグループとTGSMを使用して、GPUメモリからの読み出し回数を減らす最適化。

// groupIdXYZはDispatch()のパラメータXYZ=(nx,1,1)の場合(0,0,0)～(nx-1, 0, 0)。
// スレッドグループが作られ、tid==0～groupDim_x-1までのtidを持ったスレッドが同時に走る。
[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
        uint  tid:        SV_GroupIndex,
        uint3 groupIdXYZ: SV_GroupID)
{
    uint offs = tid;

    if (tid == 0) {
        s_xOffs = g_XBuffer[groupIdXYZ.x];
        s_sinX  = g_SinxBuffer[groupIdXYZ.x];
    }
    s_scratch[tid] = 0;

    GroupMemoryBarrierWithGroupSync();

    do {
        s_scratch[tid] +=
            ConvolutionElemValue(groupIdXYZ.x, offs) +
            ConvolutionElemValue(groupIdXYZ.x, offs + GROUP_THREAD_COUNT);
        offs += GROUP_THREAD_COUNT * 2;
    } while (offs < CONV_COUNT);

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
    //GroupMemoryBarrierWithGroupSync(); // これ以降要らないらしい。2260_GTC2010.pdf参照。
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
   // GroupMemoryBarrierWithGroupSync();
#endif

#if 4 <= GROUP_THREAD_COUNT
    if (tid < 2) { s_scratch[tid] += s_scratch[tid + 2]; }
    //GroupMemoryBarrierWithGroupSync();
#endif

    if (tid == 0) {
        s_scratch[0] += s_scratch[1];
        g_OutputBuffer[groupIdXYZ.x] = (float)s_scratch[0];
    }
}

#else

// 主にfloat精度

StructuredBuffer<float>   g_SampleDataBuffer : register(t0);
StructuredBuffer<float>   g_SinxBuffer       : register(t1);
StructuredBuffer<float>   g_XBuffer          : register(t2);
RWStructuredBuffer<float> g_OutputBuffer     : register(u0);

/// 定数。16バイトの倍数のサイズの構造体。
cbuffer consts {
    /// 畳み込み要素オフセット値。n * GROUP_THREAD_COUNTの飛び飛びの値が渡る。
    uint c_convOffs;
    /// Dispatch繰り返し回数。
    uint c_dispatchCount;
    uint c_reserved1;
    uint c_reserved2;
};

inline float
SincF(float sinx, float x)
{
    if (-0.000000001f < x && x < 0.000000001f) {
        return 1.0f;
    } else {
        // どちらでも同じだった。
#if 1
        return sinx * rcp(x);
#else
        return sinx / x;
#endif
    }
}

#define PI_F 3.141592653589793238462643f

// TGSM
groupshared float s_scratch[GROUP_THREAD_COUNT];
groupshared float s_sinX;
groupshared float s_xOffs;

/// 畳み込み計算要素1回実行。
/// sample[t+x] * sinc(πx + XBuffer[t])
inline float
ConvolutionElemValue(uint pos, uint convOffs)
{
    const int offs = c_convOffs + convOffs;
    const float x = mad(PI_F, offs + CONV_START, s_xOffs);
    return g_SampleDataBuffer[offs + pos] * SincF(s_sinX, x);
}

// スレッドグループとTGSMを使用して、GPUメモリからの読み出し回数を減らす最適化。

// groupIdXYZはDispatch()のパラメータXYZ=(nx,1,1)の場合(0,0,0)～(nx-1, 0, 0)。
// スレッドグループが作られ、tid==0～groupDim_x-1までのtidを持ったスレッドが同時に走る。
[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
        uint  tid:        SV_GroupIndex,
        uint3 groupIdXYZ: SV_GroupID)
{
    uint offs = tid;

    if (tid == 0) {
        s_xOffs = g_XBuffer[groupIdXYZ.x];
#if 1
        // 計算精度良好。
        s_sinX  = g_SinxBuffer[groupIdXYZ.x];
#else
        // こうすると精度が落ちる。GPUのsin()の精度に問題あり。
        s_sinX  = sin(s_xOffs);
#endif
    }
    s_scratch[tid] = 0;

    GroupMemoryBarrierWithGroupSync();

    do {
        s_scratch[tid] +=
            ConvolutionElemValue(groupIdXYZ.x, offs) +
            ConvolutionElemValue(groupIdXYZ.x, offs + GROUP_THREAD_COUNT);
        offs += GROUP_THREAD_COUNT * 2;
    } while (offs < CONV_COUNT);

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
    //GroupMemoryBarrierWithGroupSync(); // これ以降要らないらしい。2260_GTC2010.pdf参照。
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
   // GroupMemoryBarrierWithGroupSync();
#endif

#if 4 <= GROUP_THREAD_COUNT
    if (tid < 2) { s_scratch[tid] += s_scratch[tid + 2]; }
    //GroupMemoryBarrierWithGroupSync();
#endif

    if (tid == 0) {
        s_scratch[0] += s_scratch[1];
        g_OutputBuffer[groupIdXYZ.x] = s_scratch[0];
    }
}

#if 0
// 最適化前
[numthreads(1, 1, 1)]
void
CSMain(uint3 groupIdXYZ  : SV_GroupID,
       uint threadIdx : SV_GroupIndex)
{
    int   i;
    float sinx  = SinxBuffer[c_pos];
    float xOffs = XBuffer[c_pos];
    float r = 0.0f;

    for (i=CONV_START; i<CONV_END; ++i) {
        float x = mad(PI, i, xOffs);
        r = mad(SampleDataBuffer[c_pos+i+CONV_N], SincF(sinx, x), r);
    }

    OutputBuffer[c_pos] = r;
}
#endif // before optimization

#endif // HIGH_PRECISION
