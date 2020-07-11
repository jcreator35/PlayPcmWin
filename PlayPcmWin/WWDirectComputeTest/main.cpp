// 日本語。
// データをGPUメモリーに送ってGPUで計算。
// 結果をCPUメモリーに戻して確認する。
// DirectX11 ComputeShader 5.0 float-precision supportが必要。

#include "Test1.h"
#include "TestWWUpsample.h"
#include "TestTexture.h"
#include "TestWave1D.h"
#include <assert.h>
#include <crtdbg.h>
#include <stdint.h>


int
main(void)
{
#if defined(DEBUG) || defined(_DEBUG)
    _CrtSetDbgFlag( _CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF );
#endif

    //TestTexture();

    //TestWave1D();

    TestWWUpsample();

    return 0;
}
