// 日本語
#pragma once
#include "WWSpatialAudioUserTemplate.h"
#include "WWDynAudioHrtfObject.h"
#include <SpatialAudioHrtf.h>

class WWSpatialAudioHrtfUser :
    public WWSpatialAudioUserTemplate<ISpatialAudioObjectRenderStreamForHrtf, WWDynAudioHrtfObject> {
public:
    HRESULT Init(void) override;
    //void Term(void) override;
    HRESULT ActivateAudioStream(int maxDynObjectCount);

private:
    static DWORD RenderEntry(LPVOID lpThreadParameter);
    HRESULT RenderMain(void);
    HRESULT Render1(void);
};

