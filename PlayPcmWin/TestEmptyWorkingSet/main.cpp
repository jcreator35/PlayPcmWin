#include <Windows.h>
#include <Psapi.h>
#include <stdio.h>

static void ShowMemoryStat(void)
{
    MEMORYSTATUSEX st = {0};
    st.dwLength = sizeof (st);
    GlobalMemoryStatusEx (&st);

    printf("%.1f GB free / %.1f GB total.\r",
            st.ullAvailPhys / 1024.0 / 1024.0 / 1024.0,
            st.ullTotalPhys / 1024.0 / 1024.0 / 1024.0);
}

int main(void)
{
    int MAX_PROCESS = 65536;
    DWORD *processIdArray = new DWORD[MAX_PROCESS];

    HANDLE hSelf = GetCurrentProcess();

    printf("Press CTRL+C to stop.\n");

    while (true) {
        Sleep(10* 1000);

        ShowMemoryStat();

        DWORD nProcBytes = 0;
        int nProc = 0;
        BOOL b = EnumProcesses(processIdArray, MAX_PROCESS*sizeof(DWORD), &nProcBytes);
        if (!b) {
            printf("Error: EnumProcesses failed\n");
            goto end;
        }

        int nSuccess = 0;
        nProc = nProcBytes / sizeof(DWORD);
        for (int p=0; p<nProc; ++p) {
            DWORD pid = processIdArray[p];

            HANDLE h = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA,
                    FALSE, pid);
            if (h == nullptr) {
                //printf("Error: OpenProcess(pid=%d) failed\n", pid);
            } else {
                if (hSelf == h) {
                    // This process.
                } else {
                    b= EmptyWorkingSet(h);
                    if (!b) {
                        printf("Error: EmptyWorkingSet pid=%d failed\n", pid);
                    } else {
                        ++nSuccess;
                    }
                }

                CloseHandle(h);
                h = nullptr;
            }
        }

        if (nSuccess == 0) {
            printf("Error: All %d attempts of EmptyWorkingSet failed!\n", nProc);
            goto end;
        } else {
        }
    }

end:
    delete [] processIdArray;
    processIdArray = nullptr;

    return 0;
}