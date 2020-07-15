/*

Copy compute shader.

Calc following code on GPU:

for (int i=0; i<c_count; ++i) {
    float v = g_input[i];
    g_output[i] = v;
}

In order to run this shader,

set shaderParams.c_count

and run(shaderParams, count, 1, 1);

prepare following input buffer (SRV):
float g_input[count]

and output buffer (UAV):
float g_output[count];

define "GROUP_THREAD_COUNT" = 1024
when compiling shader

*/

cbuffer consts {
    uint c_count;

    // Constant buffers are 256-byte aligned.
    uint c_reserved[63];
};

// GPU memory
StructuredBuffer<float>   g_input    : register(t0);
RWStructuredBuffer<float> g_output   : register(u0);

[numthreads(GROUP_THREAD_COUNT, 1, 1)]
void
CSMain(
    uint  tid:        SV_GroupIndex,
    uint3 groupIdXYZ : SV_GroupID)
{
    int offs = (int)tid;
    if (c_count <= offs) {
        return;
    }

    float v = g_input[offs];
    g_output[offs] = v;
}
