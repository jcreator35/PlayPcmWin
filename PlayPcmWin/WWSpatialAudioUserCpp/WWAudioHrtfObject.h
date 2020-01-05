// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include <SpatialAudioHrtf.h>
#include "WWSAUtil.h"
#include <assert.h>
#include "WWAudioObjectTemplate.h"

class WWAudioHrtfObject : public WWAudioObjectTemplate<ISpatialAudioObjectForHrtf> {
public:
    // SetOrientation(QuatToMat(orientation))
    //                       qx qy qz qw
    float orientation[4] = { 0,0,0,1 };

    // SetEnvironment()
    SpatialAudioHrtfEnvironmentType env = SpatialAudioHrtfEnvironment_Average;

    // SetDistanceDecay()
    SpatialAudioHrtfDistanceDecay dd = { SpatialAudioHrtfDistanceDecay_NaturalDecay ,
        3.98f /* maxGain */,  float(1.58439 * pow(10, -5)) /* minGain */, 1.0f /* unityGainDist */, 100.0f /* cutoffDist */ };

    // SetDirectivity()
    SpatialAudioHrtfDirectivityUnion directivity = { SpatialAudioHrtfDirectivity_OmniDirectional , 1.0f /* scaling */ };
};
