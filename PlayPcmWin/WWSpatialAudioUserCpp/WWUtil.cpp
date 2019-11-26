// 日本語
#include "WWUtil.h"
#include <Windows.h>
#include <stdio.h>
#include <assert.h>
#include <functiondiscoverykeys.h>

static const char *
AudClntErrorMsg(HRESULT hr) {
    switch (hr) {
    case 0x800700AA: return "Resource is in use";
    case 0x88890001: return "AUDCLNT_E_NOT_INITIALIZED";
    case 0x88890002: return "AUDCLNT_E_ALREADY_INITIALIZED";
    case 0x88890003: return "AUDCLNT_E_WRONG_ENDPOINT_TYPE";
    case 0x88890004: return "AUDCLNT_E_DEVICE_INVALIDATED";
    case 0x88890005: return "AUDCLNT_E_NOT_STOPPED";

    case 0x88890006: return "AUDCLNT_E_BUFFER_TOO_LARGE";
    case 0x88890007: return "AUDCLNT_E_OUT_OF_ORDER";
    case 0x88890008: return "AUDCLNT_E_UNSUPPORTED_FORMAT";
    case 0x88890009: return "AUDCLNT_E_INVALID_SIZE";
    case 0x8889000a: return "AUDCLNT_E_DEVICE_IN_USE";

    case 0x8889000b: return "AUDCLNT_E_BUFFER_OPERATION_PENDING";
    case 0x8889000c: return "AUDCLNT_E_THREAD_NOT_REGISTERED";
    case 0x8889000e: return "AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED";
    case 0x8889000f: return "AUDCLNT_E_ENDPOINT_CREATE_FAILED";
    case 0x88890010: return "AUDCLNT_E_SERVICE_NOT_RUNNING";

    case 0x88890011: return "AUDCLNT_E_EVENTHANDLE_NOT_EXPECTED";
    case 0x88890012: return "AUDCLNT_E_EXCLUSIVE_MODE_ONLY";
    case 0x88890013: return "AUDCLNT_E_BUFDURATION_PERIOD_NOT_EQUAL";
    case 0x88890014: return "AUDCLNT_E_EVENTHANDLE_NOT_SET";
    case 0x88890015: return "AUDCLNT_E_INCORRECT_BUFFER_SIZE";

    case 0x88890016: return "AUDCLNT_E_BUFFER_SIZE_ERROR";
    case 0x88890017: return "AUDCLNT_E_CPUUSAGE_EXCEEDED";
    case 0x88890018: return "AUDCLNT_E_BUFFER_ERROR";
    case 0x88890019: return "AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED";
    case 0x88890020: return "AUDCLNT_E_INVALID_DEVICE_PERIOD";

    case 0x88890021: return "AUDCLNT_E_INVALID_STREAM_FLAG";
    case 0x88890022: return "AUDCLNT_E_ENDPOINT_OFFLOAD_NOT_CAPABLE";
    case 0x88890023: return "AUDCLNT_E_OUT_OF_OFFLOAD_RESOURCES";
    case 0x88890024: return "AUDCLNT_E_OFFLOAD_MODE_ONLY";
    case 0x88890025: return "AUDCLNT_E_NONOFFLOAD_MODE_ONLY";

    case 0x88890026: return "AUDCLNT_E_RESOURCES_INVALIDATED";
    case 0x88890027: return "AUDCLNT_E_RAW_MODE_UNSUPPORTED";
    case 0x88890028: return "AUDCLNT_E_ENGINE_PERIODICITY_LOCKED";
    case 0x88890029: return "AUDCLNT_E_ENGINE_FORMAT_LOCKED";

    case 0x88890100: return "SPTLAUDCLNT_E_DESTROYED";
    case 0x88890101: return "SPTLAUDCLNT_E_OUT_OF_ORDER";
    case 0x88890102: return "SPTLAUDCLNT_E_RESOURCES_INVALIDATED";
    case 0x88890103: return "SPTLAUDCLNT_E_NO_MORE_OBJECTS";
    case 0x88890104: return "SPTLAUDCLNT_E_PROPERTY_NOT_SUPPORTED";

    case 0x88890105: return "SPTLAUDCLNT_E_ERRORS_IN_OBJECT_CALLS";
    case 0x88890106: return "SPTLAUDCLNT_E_METADATA_FORMAT_NOT_SUPPORTED";
    case 0x88890107: return "SPTLAUDCLNT_E_STREAM_NOT_AVAILABLE";
    case 0x88890108: return "SPTLAUDCLNT_E_INVALID_LICENSE";

    case 0x8889010a: return "SPTLAUDCLNT_E_STREAM_NOT_STOPPED";
    case 0x8889010b: return "SPTLAUDCLNT_E_STATIC_OBJECT_NOT_AVAILABLE";
    case 0x8889010c: return "SPTLAUDCLNT_E_OBJECT_ALREADY_ACTIVE";
    case 0x8889010d: return "SPTLAUDCLNT_E_INTERNAL";

    default:
        return nullptr;
    }
}

void
WWErrorDescription(HRESULT hr)
{
    if (FACILITY_WINDOWS == HRESULT_FACILITY(hr)) {
        hr = HRESULT_CODE(hr);
    }

    if (AudClntErrorMsg(hr)) {
        // 自前のエラー文字列が見つかったので表示。
        printf("    0x%08x is: %s\n", hr, AudClntErrorMsg(hr));
        return;
    }

    // OSのエラー文字列を探して表示。

    char* szErrMsg = nullptr;

    if (!FormatMessageA(
            FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
            nullptr, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
            (LPSTR)&szErrMsg, 0, nullptr) != 0) {
        //_tprintf(TEXT("unknown HRESULT %#x\n"), hr);
        return;
    }

    printf("    0x%08x is: %s\n", hr, szErrMsg);
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
WWDeviceIdStrGet(
    IMMDeviceCollection *dc, UINT id, wchar_t *devIdStr, size_t idStrBytes)
{
    HRESULT hr = 0;

    IMMDevice *device = nullptr;
    LPWSTR deviceId = nullptr;

    assert(dc);
    assert(devIdStr);
    devIdStr[0] = 0;
    assert(0 < idStrBytes);

    HRG(dc->Item(id, &device));
    HRG(device->GetId(&deviceId));
    wcsncpy_s(devIdStr, idStrBytes / sizeof devIdStr[0], deviceId, _TRUNCATE);

end:
    if (nullptr != deviceId) {
        CoTaskMemFree(deviceId);
    }
    return hr;
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

