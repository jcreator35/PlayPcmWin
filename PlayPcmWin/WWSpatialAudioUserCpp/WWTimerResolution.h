#pragma once
// 日本語 UTF-8

#include <Windows.h>

class WWTimerResolution
{
public:
    WWTimerResolution(void)
            : m_beforeHns(0),
            m_desiredHns(10000),
            m_setHns(0) { }

    void SetTimePeriodHundredNanosec(int hnanosec);
    int  GetTimePeriodHundredNanosec(void) const { return m_setHns; }

    DWORD Setup(void);
    void Unsetup(void);

private:
    ULONG        m_beforeHns;
    ULONG        m_desiredHns;
    ULONG        m_setHns;
};
