// 日本語 UTF-8

#include "WWThreadCharacteristics.h"
#include "WWSAUtil.h"
#include <avrt.h>
#include <assert.h>
#include <Dwmapi.h>
#include "WWCommonUtil.h"

static const wchar_t*
WWSchedulerTaskTypeToStr(WWSchedulerTaskType t)
{
    switch (t) {
    case WWSTTNone: return L"None";
    case WWSTTAudio: return L"Audio";
    case WWSTTProAudio: return L"Pro Audio";
    case WWSTTPlayback: return L"Playback";
    default: assert(0); return L"";
    }
}

static const wchar_t *
WWMMThreadPriorityTypeToStr(WWMMThreadPriorityType t)
{
    switch (t) {
    case WWTPNone:      return L"None";
    case WWTPLow:       return L"Low";
    case WWTPNormal:    return L"Normal";
    case WWTPHigh:      return L"High";
    case WWTPCritical:  return L"Critical";
    default: assert(0); return L"";
    }
};

static AVRT_PRIORITY
WWMMThreadPriorityTypeToAvrtPriority(WWMMThreadPriorityType t)
{
    switch (t) {
    case WWTPLow:       return AVRT_PRIORITY_LOW;
    case WWTPNormal:    return AVRT_PRIORITY_NORMAL;
    case WWTPHigh:      return AVRT_PRIORITY_HIGH;
    case WWTPCritical:  return AVRT_PRIORITY_CRITICAL;
    default: assert(0); return AVRT_PRIORITY_NORMAL;
    }
};

void
WWThreadCharacteristics::Set(WWMMCSSCallType ct, WWMMThreadPriorityType tp, WWSchedulerTaskType stt)
{
    assert(0 <= ct && ct < WWMMCSSNUM);
    assert(0 <= tp && tp < WWTPNUM);
    assert(0 <= stt&& stt < WWSTTNUM);
    dprintf("D: %s() ct=%d tp=%d stt=%d\n", __FUNCTION__, (int)ct, (int)tp, (int)stt);
    m_mmcssCallType = ct;
    m_threadPriority = tp;
    m_schedulerTaskType = stt;
}

void
WWThreadCharacteristics::Setup(void)
{
    HRESULT hr = S_OK;

    if (WWSTTNone != m_schedulerTaskType) {
        // マルチメディアクラススケジューラーサービスのスレッド特性設定。
        m_mmcssHandle =
                AvSetMmThreadCharacteristics(WWSchedulerTaskTypeToStr(m_schedulerTaskType), &m_mmcssTaskIndex);
        if (nullptr == m_mmcssHandle) {
            dprintf("Failed to enable MMCSS on render thread: 0x%08x\n", GetLastError());
            m_mmcssTaskIndex = 0;
            m_result.avSetMmThreadCharacteristicsResult = false;
        } else {
            printf("D: AvSetMmThreadCharacteristics(%S) success.\n",
                WWSchedulerTaskTypeToStr(m_schedulerTaskType));
            m_result.avSetMmThreadCharacteristicsResult = true;
        }

        if (m_result.avSetMmThreadCharacteristicsResult && WWTPNone != m_threadPriority) {
            // スレッド優先度設定。

            assert(m_mmcssHandle != nullptr);

            m_result.avSetMmThreadPriorityResult =
                    !!AvSetMmThreadPriority(m_mmcssHandle, WWMMThreadPriorityTypeToAvrtPriority(m_threadPriority));
            printf("D: AvSetMmThreadPriority(%S) %d\n",
                WWMMThreadPriorityTypeToStr(m_threadPriority), (int)m_result.avSetMmThreadCharacteristicsResult);
        }
    }

    if (WWMMCSSDoNotCall != m_mmcssCallType) {
        // MMCSSの有効、無効の設定。
        hr = DwmEnableMMCSS(m_mmcssCallType==WWMMCSSEnable);
        printf("D: DwmEnableMMCSS(%d) 0x%08x\n", (int)(m_mmcssCallType==WWMMCSSEnable), hr);
        // 失敗することがあるが、続行する。
        m_result.dwmEnableMMCSSResult = hr;
    }

}

void
WWThreadCharacteristics::Unsetup(void)
{
    HRESULT hr = S_OK;

    if (nullptr != m_mmcssHandle) {
        AvRevertMmThreadCharacteristics(m_mmcssHandle);
        m_mmcssHandle = nullptr;
        m_mmcssTaskIndex = 0;
    }

    if (WWMMCSSEnable == m_mmcssCallType) {
        hr = DwmEnableMMCSS(false);
        dprintf("D: %s() DwmEnableMMCSS(%d) 0x%08x\n", __FUNCTION__, false, hr);
    }
}
