// 日本語 UTF-8

#include "WWTimerResolution.h"
#include "WWUtil.h"
#include <assert.h>

// 100 nanosec * ONE_MILLISEC == one millisec
#define ONE_MILLISEC (10000)

// ntdll.lib
extern "C" {
NTSYSAPI NTSTATUS NTAPI
NtSetTimerResolution(
        IN  ULONG   desiredResolution,
        IN  BOOLEAN setResolution,
        OUT PULONG  currentResolution);

NTSYSAPI NTSTATUS NTAPI
NtQueryTimerResolution(
        OUT PULONG minimumResolution,
        OUT PULONG maximumResolution,
        OUT PULONG currentResolution);
}; /* extern "C" */

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
        ULONG minResolution = 0;
        ULONG maxResolution = 0;
        ULONG desiredResolution = m_desiredHns;
        m_setHns = 0;

        HRG(NtQueryTimerResolution(&minResolution, &maxResolution, &m_beforeHns));
        if (desiredResolution < maxResolution) {
            desiredResolution = maxResolution;
        }

        HRG(NtSetTimerResolution(desiredResolution, TRUE, &m_setHns));
    } else if (ONE_MILLISEC <= m_desiredHns) {
        timeBeginPeriod(m_desiredHns/ONE_MILLISEC);
        m_setHns = (m_desiredHns/ONE_MILLISEC)*ONE_MILLISEC;
    } else {
        // タイマー解像度を設定しない。
    }

end:
    return hr;
}

void
WWTimerResolution::Unsetup(void)
{
    if (0 < m_desiredHns && m_desiredHns < ONE_MILLISEC) {
        NtSetTimerResolution(m_beforeHns, TRUE, &m_setHns);
    } else if (ONE_MILLISEC <= m_desiredHns) {
        timeEndPeriod(m_desiredHns/ONE_MILLISEC);
    } else {
        // タイマー解像度を設定しない。
    }
}
