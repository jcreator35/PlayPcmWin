#include "WWPrivilegeControl.h"
#include <stdio.h>
#include <assert.h>
#pragma comment(lib, "advapi32.lib")


WWPrivilegeControl::~WWPrivilegeControl(void)
{
    assert(hToken == nullptr);
}

bool
WWPrivilegeControl::Init(void)
{
    assert(hToken == nullptr);

    if(!OpenThreadToken(GetCurrentThread(), TOKEN_ADJUST_PRIVILEGES|TOKEN_QUERY, FALSE, &hToken) &&
            !OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES|TOKEN_QUERY, &hToken)) {
        printf("OpenProcessToken failed\n");
        return false;
    }

    return true;
}

void
WWPrivilegeControl::Term(void)
{
    if (hToken == nullptr) {
        return;
    }

    CloseHandle(hToken);
    hToken = nullptr;
}

bool
WWPrivilegeControl::SetPrivilege(LPCTSTR lpszPrivilege, BOOL bEnablePrivilege)
{
    assert(hToken != nullptr);

    TOKEN_PRIVILEGES tp;
    LUID luid;

    if (!LookupPrivilegeValue(
            nullptr,       // lookup privilege on local system
            lpszPrivilege, // privilege to lookup 
            &luid)) {      // receives LUID of privilege
        printf("LookupPrivilegeValue error: %u\n", GetLastError() ); 
        return false; 
    }

    tp.PrivilegeCount = 1;
    tp.Privileges[0].Luid = luid;
    if (bEnablePrivilege) {
        tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    } else {
        tp.Privileges[0].Attributes = 0;
    }

    if (!AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof tp, nullptr, nullptr)) {
          printf("AdjustTokenPrivileges error: %u\n", GetLastError() );
          return false;
    }

    if (GetLastError() == ERROR_NOT_ALL_ASSIGNED) {
        printf("The token does not have the specified privilege. \n");
        return false;
    }

    return true;
}
