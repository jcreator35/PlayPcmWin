// This Compute shader is called from TestSandboxShader.cpp

RWStructuredBuffer<float> g_output   : register(u0);

[numthreads(3, 1, 1)]
void
CSMain(
    uint  tid:        SV_GroupIndex, //< 0 <= tid < 3
    uint3 groupIdXYZ : SV_GroupID)   //< 0 <= groupIdXyz.x < 4 when Dispatch() xyz=(4,1,1)
{
    int idx = tid + groupIdXYZ.x * 5;
    g_output[idx] = 1;
}

/*

When this function is called with Dispatch(4,1,1)

CSMain is called 12 times with those different args:
    CSMain(tid=0, groupIdXYZ=0,0,0)
    CSMain(tid=1, groupIdXYZ=0,0,0)
    CSMain(tid=2, groupIdXYZ=0,0,0)

    CSMain(tid=0, groupIdXYZ=1,0,0)
    CSMain(tid=1, groupIdXYZ=1,0,0)
    CSMain(tid=2, groupIdXYZ=1,0,0)

    CSMain(tid=0, groupIdXYZ=2,0,0)
    CSMain(tid=1, groupIdXYZ=2,0,0)
    CSMain(tid=2, groupIdXYZ=2,0,0)

    CSMain(tid=0, groupIdXYZ=3,0,0)
    CSMain(tid=1, groupIdXYZ=3,0,0)
    CSMain(tid=2, groupIdXYZ=3,0,0)

And g_output becomes like this:

1.000000
1.000000
1.000000
0.000000
0.000000
1.000000
1.000000
1.000000
0.000000
0.000000
1.000000
1.000000
1.000000
0.000000
0.000000
1.000000
1.000000
1.000000
0.000000
0.000000
0.000000
0.000000
0.000000
0.000000
0.000000

*/