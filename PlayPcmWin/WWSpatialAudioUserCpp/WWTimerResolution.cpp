// 日本語 UTF-8

#include "WWTimerResolution.h"
#include "WWSAUtil.h"
#include <assert.h>
#include "WWCommonUtil.h"

// 100 nanosec * ONE_MILLISEC == one millisec
#define ONE_MILLISEC (10000)

void
WWTimerResolution::SetTimePeriodHundredNanosec(int hnanosec)
{
    assert(0 <= hnanosec);
    m_desiredHns = (ULONG)hnanosec;
}

DWORD
WWTimerResolution::Setup(void)
{
    HRESULT hr = 0;

    if (0 < m_desiredHns && m_desiredHns < ONE_MILLISEC) {
        dprintf("E: WWTimerResolution::Setup() does not support hns=%d\n", m_desiredHns);
        assert(0);
    } else if (ONE_MILLISEC <= m_desiredHns) {
        MMRESULT mr = timeBeginPeriod(m_desiredHns/ONE_MILLISEC);
        if (mr == TIMERR_NOERROR) {
            printf("D: Timer resolution set to %d ms\n", m_desiredHns / ONE_MILLISEC);
            hr = 0;
        } else {
            dprintf("E: WWTimerResolution::Setup() timeBeginPeriod(%d) failed %x\n", m_desiredHns, mr);
            hr = E_FAIL;
        }

        m_setHns = (m_desiredHns/ONE_MILLISEC)*ONE_MILLISEC;
    } else {
        // タイマー解像度を設定しない。
    }

    return hr;
}

void
WWTimerResolution::Unsetup(void)
{
    if (0 < m_desiredHns && m_desiredHns < ONE_MILLISEC) {
        dprintf("E: WWTimerResolution::Unsetup() does not support hns=%d\n", m_desiredHns);
        assert(0);
    } else if (ONE_MILLISEC <= m_desiredHns) {
        timeEndPeriod(m_desiredHns/ONE_MILLISEC);
        dprintf("D: WWTimerResolution::Unetup() timeEndPeriod(%d)\n", m_desiredHns / ONE_MILLISEC);
    } else {
        // タイマー解像度を設定しない。
    }
}
