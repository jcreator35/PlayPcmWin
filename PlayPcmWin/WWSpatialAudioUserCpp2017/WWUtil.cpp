// 日本語
#include "WWUtil.h"
#include <Windows.h>
#include <stdio.h>
#include <assert.h>
#include <functiondiscoverykeys.h>

void
WWErrorDescription(HRESULT hr)
{
    if (FACILITY_WINDOWS == HRESULT_FACILITY(hr)) {
        hr = HRESULT_CODE(hr);
    }

    char* szErrMsg = nullptr;

    if (!FormatMessageA(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
        nullptr, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPSTR)&szErrMsg, 0, nullptr) != 0) {
        //_tprintf(TEXT("unknown HRESULT %#x\n"), hr);
        return;
    }

    printf("    0x%08x is: %s", hr, szErrMsg);
    LocalFree(szErrMsg);
}

// matrixのRow-Majorの説明: https://en.wikipedia.org/wiki/Row-_and_column-major_order
void WWQuaternionToRowMajorRotMat(const float q[4], float m_return[9])
{
    float X = q[0];
    float Y = q[1];
    float Z = q[2];
    float W = q[3];

    float xx = X * X;
    float xy = X * Y;
    float xz = X * Z;
    float xw = X * W;

    float yy = Y * Y;
    float yz = Y * Z;
    float yw = Y * W;

    float zz = Z * Z;
    float zw = Z * W;

    float m00 = 1 - 2 * (yy + zz);
    float m01 = 2 * (xy - zw);
    float m02 = 2 * (xz + yw);

    float m10 = 2 * (xy + zw);
    float m11 = 1 - 2 * (xx + zz);
    float m12 = 2 * (yz - xw);

    float m20 = 2 * (xz - yw);
    float m21 = 2 * (yz + xw);
    float m22 = 1 - 2 * (xx + yy);

    float m03 = 0;
    float m13 = 0;
    float m23 = 0;
    float m30 = 0;
    float m31 = 0;
    float m32 = 0;
    float m33 = 1;

    m_return[0] = m00;
    m_return[1] = m01;
    m_return[2] = m02;

    m_return[3] = m10;
    m_return[4] = m11;
    m_return[5] = m12;

    m_return[6] = m20;
    m_return[7] = m21;
    m_return[8] = m22;

}

HRESULT
WWDeviceNameGet(
    IMMDeviceCollection *dc, UINT id, wchar_t *name, size_t nameBytes)
{
    HRESULT hr = 0;

    IMMDevice *device = nullptr;
    LPWSTR deviceId = nullptr;
    IPropertyStore *ps = nullptr;
    PROPVARIANT pv;

    assert(dc);
    assert(name);

    name[0] = 0;

    assert(0 < nameBytes);

    PropVariantInit(&pv);

    HRR(dc->Item(id, &device));
    HRR(device->GetId(&deviceId));
    HRR(device->OpenPropertyStore(STGM_READ, &ps));

    HRG(ps->GetValue(PKEY_Device_FriendlyName, &pv));
    SafeRelease(&ps);

    wcsncpy_s(name, nameBytes / sizeof name[0], pv.pwszVal, _TRUNCATE);

end:
    PropVariantClear(&pv);
    CoTaskMemFree(deviceId);
    SafeRelease(&ps);
    return hr;
}

int
WWCountNumberOf1s(uint64_t v)
{
    int acc = 0;
    for (int i = 0; i < 64; ++i) {
        uint64_t bit = 1LLU << i;
        if (v & bit) {
            ++acc;
        }
    }

    return acc;
}

