// 日本語。

#include "TestCopyShader.h"
#include "TestSincConvolution.h"
#include "framework.h"

int main(void)
{
    HRESULT hr = S_OK;

    //hr = TestCopyShader();

    hr = TestSincConvolution();

    // 成功: 0
    // 失敗: 1
    return FAILED(hr);
}
