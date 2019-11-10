// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include "WWDynAudioHrtfObject.h"
#include <SpatialAudioHrtf.h>

class WWSpatialAudioHrtfUser :
    public WWSpatialAudioUserTemplate<
        ISpatialAudioObjectRenderStreamForHrtf,
        WWDynAudioHrtfObject>
{
public:
    HRESULT Init(void) override;

    /// @param staticObjectTypeMask 1つもスタティックなオブジェクトが無いときはNone。Dynamicにするとエラーが起きた。
    HRESULT ActivateAudioStream(int maxDynObjectCount,
        int staticObjectTypeMask) override;

private:
    static DWORD RenderEntry(LPVOID lpThreadParameter);
    HRESULT RenderMain(void);
    HRESULT Render1(void);
};

