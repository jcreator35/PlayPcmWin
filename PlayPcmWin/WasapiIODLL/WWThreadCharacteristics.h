#pragma once
// 日本語 UTF-8

#include <Windows.h>
#include <mmsystem.h>

enum WWSchedulerTaskType {
    WWSTTNone,
    WWSTTAudio,
    WWSTTProAudio,
    WWSTTPlayback,

    WWSTTNUM
};

enum WWMMCSSCallType {
    WWMMCSSDisable,
    WWMMCSSEnable,
    WWMMCSSDoNotCall,

    WWMMCSSNUM
};

struct WWThreadCharacteristicsSetupResult {
    HRESULT dwmEnableMMCSSResult;
    bool    avSetMmThreadCharacteristicsResult;

    WWThreadCharacteristicsSetupResult(void) :
            dwmEnableMMCSSResult(S_OK),
            avSetMmThreadCharacteristicsResult(S_OK) { }
};

class WWThreadCharacteristics {
public:
    WWThreadCharacteristics(void) : m_mmcssCallType(WWMMCSSEnable),
            m_schedulerTaskType(WWSTTAudio), m_mmcssHandle(nullptr), m_mmcssTaskIndex(0) { }

    void Set(WWMMCSSCallType ct, WWSchedulerTaskType stt);

    /// Setup()の結果は GetSetupResult()で取得する。
    void Setup(void);
    void Unsetup(void);

    void GetThreadCharacteristicsSetupResult(WWThreadCharacteristicsSetupResult &result) {
        result = m_result;
    }

private:
    WWMMCSSCallType m_mmcssCallType;
    WWSchedulerTaskType m_schedulerTaskType;
    HANDLE  m_mmcssHandle;
    DWORD   m_mmcssTaskIndex;
    WWThreadCharacteristicsSetupResult m_result;
};
