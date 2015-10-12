#pragma once

#include <Windows.h>

class WWPrivilegeControl {
public:
    WWPrivilegeControl(void) : hToken(nullptr) { }
    ~WWPrivilegeControl(void);
    bool Init(void);
    void Term(void);

    bool SetPrivilege(LPCTSTR lpszPrivilege, BOOL bEnablePrivilege);

private:
    HANDLE hToken;
};
