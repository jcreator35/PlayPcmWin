// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include "WWAudioObject.h"
#include <SpatialAudioClient.h>
#include "WWThreadCharacteristics.h"
#include "WWTimerResolution.h"

class WWSpatialAudioUser :
    public WWSpatialAudioUserTemplate<
        ISpatialAudioObjectRenderStream,
        WWAudioObject>
{
public:
    HRESULT Init(void) override;

    void Term(void) override;

    /// @param staticObjectTypeMask 1つもスタティックなオブジェクトが無いときはNone。Dynamicにするとエラーが起きた。
    HRESULT ActivateAudioStream(int maxDynObjectCount, int staticObjectTypeMask) override;

private:
    WWThreadCharacteristics mThreadCharacteristics;
    WWTimerResolution mTimerResolution;

    static DWORD RenderEntry(LPVOID lpThreadParameter);
    HRESULT RenderMain(void);
    HRESULT Render1(void);
};

