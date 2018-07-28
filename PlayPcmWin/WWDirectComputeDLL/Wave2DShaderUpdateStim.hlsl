/* 日本語UTF-8
define
"STIM_COUNT" == "4"
"SC" == mC0 * mΔt / mΔx
"DELTA_T" == mΔt
で
THREAD_X==1でDispatchする。

*/

#define STIM_GAUSSIAN 0
#define STIM_SINE     1

// UAV
RWStructuredBuffer<float2> gV            : register(u0);
RWStructuredBuffer<float>  gP            : register(u1);

struct Stim {
    int type; //< STIM_GAUSSIAN or STIM_SINE
    int counter;
    int pos;          //< 配列の要素番号。pos = x + y * W
    float magnitude;
    float halfPeriod;
    float width;
    float omega;
    float sinePeriod;
};


// Shader Constant。16バイトの倍数のサイズの構造体。
cbuffer consts {
    // stimの有効要素数。
    int nStim;

    int cDummy0;
    int cDummy1;
    int cDummy2;

    Stim stim[STIM_COUNT];
};

[numthreads(1, 1, 1)]
void CSUpdateStim()
{
    for (int i=0; i<nStim; ++i) {
        if (0 < stim[i].counter) {
            switch (stim[i].type) {
            case STIM_GAUSSIAN:
                {
                    int x = stim[i].posX;
                    float prevP = gP[x];

                    int c = stim[i].counter;

                    float period = (float)c - stim[i].halfPeriod;
                    float t = -period * period * stim[i].width;
                    float fr = exp( t );
                    gP[x] = prevP + stim[i].magnitude * fr;
                }
                break;
            case STIM_SINE:
                {
                    int x = stim[i].posX;
                    float prevP = gP[x];

                    int c = stim[i].counter;

                    float elapsedTime = ((int)(stim[i].sinePeriod/SC) - c) * DELTA_T;
                    float omega = stim[i].omega * elapsedTime;
                    float a = sin(omega);

                    gP[x] = prevP + stim[i].magnitude * a;
                }
                break;
            }
        }
    }
}

