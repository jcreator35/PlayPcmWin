#include <Windows.h>
#include <stdio.h>

/* ntdll.dll is included in Windows Driver Kit.
 * You may need to update VS user library path to
 * i386:  $(LibraryPath);C:\WinDDK\7600.16385.1\lib\win7\i386
 * amd64: $(LibraryPath);C:\WinDDK\7600.16385.1\lib\win7\amd64
 * respectively.
 */
#pragma comment(lib, "ntdll")

#define TEST_MMTIMER

#ifdef TEST_MMTIMER
# include <mmsystem.h>
# pragma comment(lib, "winmm")
#endif /* TEST_MMTIMER */

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

int main(void)
{
    ULONG minResolution  = 0U;
    ULONG maxResolution  = 0U;
    ULONG curResolution  = 0U;
    ULONG origResolution = 0U;
    NTSTATUS stat = 0;
    MMRESULT mmr  = 0;

    stat = NtQueryTimerResolution(&minResolution, &maxResolution, &curResolution);
    printf("NtQueryTimerResolution %x min=%u max=%u cur=%u\n",
            stat, minResolution, maxResolution, curResolution);
    origResolution = curResolution;

    stat = NtSetTimerResolution(maxResolution, TRUE, &curResolution);
    printf("NtSetTimerResolution %x set=%u cur=%u\n",
            stat, maxResolution, curResolution);

    stat = NtQueryTimerResolution(&minResolution, &maxResolution, &curResolution);
    printf("NtQueryTimerResolution %x min=%u max=%u cur=%u\n",
            stat, minResolution, maxResolution, curResolution);

    stat = NtSetTimerResolution(origResolution, TRUE, &curResolution);
    printf("NtSetTimerResolution %x set=%u cur=%u\n",
            stat, origResolution, curResolution);

    stat = NtQueryTimerResolution(&minResolution, &maxResolution, &curResolution);
    printf("NtQueryTimerResolution %x min=%u max=%u cur=%u\n",
            stat, minResolution, maxResolution, curResolution);

#ifdef TEST_MMTIMER
    printf("\n");

    mmr = timeBeginPeriod(1);
    printf("timeBeginPeriod(1) %x\n", mmr);

    stat = NtQueryTimerResolution(&minResolution, &maxResolution, &curResolution);
    printf("NtQueryTimerResolution %x min=%u max=%u cur=%u\n",
            stat, minResolution, maxResolution, curResolution);

    mmr = timeEndPeriod(1);
    printf("timeEndPeriod(1) %x\n", mmr);

    stat = NtQueryTimerResolution(&minResolution, &maxResolution, &curResolution);
    printf("NtQueryTimerResolution %x min=%u max=%u cur=%u\n",
            stat, minResolution, maxResolution, curResolution);
#endif /* TEST_MMTIMER */
}
