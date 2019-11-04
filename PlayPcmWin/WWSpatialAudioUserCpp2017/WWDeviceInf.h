// 日本語
#pragma once

#include <stdlib.h>
#include <string.h>

#define WW_DEVICE_NAME_COUNT (256)

struct WWDeviceInf {
    int id;
    wchar_t name[WW_DEVICE_NAME_COUNT];

    WWDeviceInf(void) {
        id = -1;
        name[0] = 0;
    }

    WWDeviceInf(int id, const wchar_t * name) {
        this->id = id;
        wcsncpy_s(this->name, _countof(this->name), name, _TRUNCATE);
    }
};
