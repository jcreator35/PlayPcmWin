#include <windows.h>
#include <psapi.h>

#include <stdio.h>
#include <string.h>
#include <vector>
#include <map>
#include <stdint.h>

#define BENCHMARK_REPEAT_COUNT (3)
#define MALLOC_SLEEP__REPEAT_COUNT (10)

class NumaTester {
public:
    bool Init(void) {
        mHighestNumaNodeNumber = 0;

        SYSTEM_INFO si;
        memset(&si, 0, sizeof si);

        GetSystemInfo(&si);
        mNumOfProcessors = si.dwNumberOfProcessors;

        ULONG hnn = 0;
        if (!GetNumaHighestNodeNumber(&hnn)) {
            printf("GetNumaHighestNodeNumber failed: %d\n", GetLastError());
            return false;
        }
        mHighestNumaNodeNumber = hnn;

        for (int i = 0; i < mNumOfProcessors; ++i) {
            UCHAR numaNodeId;
            if (!GetNumaProcessorNode(i, &numaNodeId)) {
                printf("GetNumaProcessorNode failed: %d\n", GetLastError());
                return false;
            }
            mProcessorIdToNumaNodeIdTable.push_back(numaNodeId);
        }

        return true;
    }

    void Term(void) {
        mProcessorIdToNumaNodeIdTable.clear();
    }

    int NumOfProcessors(void) const { return mNumOfProcessors; }
    int HighestNumaNodeNumber(void) const { return mHighestNumaNodeNumber; }
    int ProcessorIdToNumaNodeId(int processorId) const {
        return mProcessorIdToNumaNodeIdTable[processorId];
    }

private:
    int mNumOfProcessors;
    int mHighestNumaNodeNumber;
    std::vector<int> mProcessorIdToNumaNodeIdTable;
};

static void
PrintComputerConfiguration(void)
{
    NumaTester nt;

    nt.Init();
    printf("Total logical processors=%d, highest NUMA node id=%d\n",
        nt.NumOfProcessors(), nt.HighestNumaNodeNumber());

    printf("Logical processor id, NUMA node id\n");
        for (int i = 0; i < nt.NumOfProcessors(); ++i) {
        printf("%d, %d\n",
            i, nt.ProcessorIdToNumaNodeId(i));
    }

    printf("\n");

    nt.Term();
}

static void
MemoryWriteTest(char *buff, int64_t bytes)
{
    memset(buff, 0, bytes);
    memset(buff, 255, bytes);
}

static void
MeasureMemorySpeed(int64_t allocBytes)
{
    NumaTester nt;

    nt.Init();

    std::map<int, char *> numaNodeMemoryTable;

    printf("Allocating memory...\n");

    for (int i = 0; i < nt.NumOfProcessors(); i++) {
        int numaNodeId = nt.ProcessorIdToNumaNodeId(i);
        auto it = numaNodeMemoryTable.find(numaNodeId);
        if (it != numaNodeMemoryTable.end()) {
            continue;
        }

        // allocate memory belongs to NUMA node id == i.
        char * buff = (char *)VirtualAllocExNuma(
            GetCurrentProcess(),
            NULL,
            allocBytes,
            MEM_RESERVE | MEM_COMMIT,
            PAGE_READWRITE,
            numaNodeId);
        if (nullptr == buff) {
            printf("Error: VirtualAllocExNuma failed\n");
            return;
        }

        memset(buff, 255, allocBytes);

        numaNodeMemoryTable.insert(
            std::map<int, char*>::value_type(numaNodeId, buff));
    }

    printf("\n");

    for (int repeat = 0; repeat < BENCHMARK_REPEAT_COUNT; ++repeat) {
        printf("Test %d/%d\n", repeat+1, BENCHMARK_REPEAT_COUNT);

        printf("Logical processor id, Memory NUMA node id, elapsed time in millisec\n");
        for (int i = 0; i < nt.NumOfProcessors(); i++) {
            SetThreadAffinityMask(GetCurrentThread(), 1 << i);

            for (auto it = numaNodeMemoryTable.begin(); it != numaNodeMemoryTable.end(); ++it) {
                LARGE_INTEGER freq;
                LARGE_INTEGER before, after;
                int numaNodeId = it->first;
                char *buff = it->second;

                QueryPerformanceFrequency(&freq);
                QueryPerformanceCounter(&before);

                MemoryWriteTest(buff, allocBytes);

                QueryPerformanceCounter(&after);
                printf("%d, %d, %lld\n",
                    i, numaNodeId, (after.QuadPart - before.QuadPart) / (freq.QuadPart / 1000));
            }
        }

        printf("\n");
    }

    // release all allocated memory
    for (auto it = numaNodeMemoryTable.begin(); it != numaNodeMemoryTable.end(); ++it) {
        char *buff = it->second;
        VirtualFreeEx(GetCurrentProcess(), buff, 0, MEM_RELEASE);
    }
    numaNodeMemoryTable.clear();
}

static void
MallocTest(int64_t allocBytes)
{
    printf("Allocating memory...\n");
    char * buff = (char *)malloc(allocBytes);
    if (nullptr == buff) {
        printf("Error: malloc failed\n");
        return;
    }
    memset(buff, 255, allocBytes);

    for (int repeat = 0; repeat < BENCHMARK_REPEAT_COUNT; ++repeat) {
        LARGE_INTEGER freq;
        LARGE_INTEGER before, after;

        QueryPerformanceFrequency(&freq);
        QueryPerformanceCounter(&before);

        MemoryWriteTest(buff, allocBytes);

        QueryPerformanceCounter(&after);
        printf("%lld millisec\n",
            (after.QuadPart - before.QuadPart) / (freq.QuadPart / 1000));
    }

    free(buff);
    buff = nullptr;
}

static void
MallocSleepTest(int64_t allocBytes)
{
    printf("Allocating memory...\n");
    char * buff = (char *)malloc(allocBytes);
    if (nullptr == buff) {
        printf("Error: malloc failed\n");
        return;
    }
    memset(buff, 255, allocBytes);

    for (int repeat = 0; repeat < MALLOC_SLEEP__REPEAT_COUNT; ++repeat) {
        LARGE_INTEGER freq;
        LARGE_INTEGER before, after;

        Sleep(1000);

        QueryPerformanceFrequency(&freq);
        QueryPerformanceCounter(&before);

        MemoryWriteTest(buff, allocBytes);

        QueryPerformanceCounter(&after);
        printf("%lld millisec\n",
            (after.QuadPart - before.QuadPart) / (freq.QuadPart / 1000));
    }

    free(buff);
    buff = nullptr;
}

static void
PrintUsage(const wchar_t *programName)
{
    printf("%s printconfig\n"
        "    print numa configuration\n"
        "%s [benchmark|malloctest|mallocsleeptest] allocationSize\n"
        "    perform benchmarks. allocationSize is number in MB",
        programName, programName);
}

int wmain(int argc, wchar_t *argv[])
{
    if (argc < 2) {
        PrintUsage(argv[0]);
        return 1;
    }

    if (0 == wcscmp(L"printconfig", argv[1])) {
        PrintComputerConfiguration();
        return 0;
    }

    if (argc != 3) {
        PrintUsage(argv[0]);
        return 1;
    }

    wchar_t *stopS = nullptr;
    int64_t allocBytes = wcstol(argv[2], &stopS, 10);
    if (allocBytes == 0) {
        PrintUsage(argv[0]);
        return 1;
    }

    if (0 == wcscmp(L"benchmark", argv[1])) {
        MeasureMemorySpeed(allocBytes * 1024 * 1024);
    }

    if (0 == wcscmp(L"malloctest", argv[1])) {
        MallocTest(allocBytes * 1024 * 1024);
    }

    if (0 == wcscmp(L"mallocsleeptest", argv[1])) {
        MallocSleepTest(allocBytes * 1024 * 1024);
    }

    return 0;
}
