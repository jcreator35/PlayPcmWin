// 日本語。

#include "TestCopyShader.h"
#include "framework.h"

int main(void)
{
    HRESULT hr = S_OK;

    hr = TestCopyShader();

    // 成功: 0
    // 失敗: 1
    return FAILED(hr);
}
