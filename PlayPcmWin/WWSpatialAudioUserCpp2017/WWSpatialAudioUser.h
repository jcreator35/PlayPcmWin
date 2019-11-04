// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include <SpatialAudioClient.h>
#include "WWDynAudioObject.h"

class WWSpatialAudioUser :
    public WWSpatialAudioUserTemplate<ISpatialAudioObjectRenderStream, WWDynAudioObject>
{
public:
    HRESULT Init(void) override;
    //void Term(void) override;
    HRESULT ActivateAudioStream(int maxDynObjectCount);

private:

    static DWORD RenderEntry(LPVOID lpThreadParameter);
    HRESULT RenderMain(void);
    HRESULT Render1(void);
};

