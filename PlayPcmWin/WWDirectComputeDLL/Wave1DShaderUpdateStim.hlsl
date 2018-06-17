/* 日本語UTF-8
define
"STIM_COUNT" == "4"
で
THREAD_X==1でDispatchする。

*/

#define PI (3.14159265358979f)
#define STIM_GAUSSIAN 0
#define STIM_SINE     1

// UAV
RWStructuredBuffer<float> gV            : register(u0);
RWStructuredBuffer<float> gP            : register(u1);

struct Stim {
    int type; //< STIM_GAUSSIAN or STIM_SINE
    int counter;
    int posX;
    float magnitude;
    float halfPeriod;
    float width;
    float freq;
    int dummy1;
};


// Shader Constant。16バイトの倍数のサイズの構造体。
cbuffer consts {
    // stimの有効要素数。
    int nStim;

    int dummy0;
    int dummy1;
    int dummy2;

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

                    float omega = 2.0f * PI * stim[i].freq;
                    float a = sin(omega * c);

                    gP[x] = prevP + stim[i].magnitude * a;
                }
                break;
            }
        }
    }
}

