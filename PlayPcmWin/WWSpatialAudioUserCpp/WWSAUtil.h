// 日本語
#pragma once

#include <Windows.h>
#include <comdef.h>
#include <MMDeviceAPI.h>
#include <stdint.h>

void WWErrorDescription(HRESULT hr);

/// q[0] == x, q[1] ==y, q[2] == z, q[3] == w
void WWQuaternionToRowMajorRotMat(const float q[4], float m_return[9]);

HRESULT
WWDeviceIdStrGet(
    IMMDeviceCollection *dc, UINT id, wchar_t *devIdStr, size_t idStrBytes);

HRESULT
WWDeviceNameGet(
    IMMDeviceCollection *dc, UINT id, wchar_t *name, size_t nameBytes);

int
WWCountNumberOf1s(uint64_t v);
