// 日本語 UTF-8

#include "WWWasapiIOUtil.h"
#include "WWCommonUtil.h"
#include <stdint.h>
#include <assert.h>
#include <string.h>
#include <string>

void
WWWaveFormatDebug(WAVEFORMATEX *v)
{
    (void)v;

    dprintf(
        "  cbSize=%d\n"
        "  nAvgBytesPerSec=%d\n"
        "  nBlockAlign=%d\n"
        "  nChannels=%d\n"
        "  nSamplesPerSec=%d\n"
        "  wBitsPerSample=%d\n"
        "  wFormatTag=0x%x\n",
        v->cbSize,
        v->nAvgBytesPerSec,
        v->nBlockAlign,
        v->nChannels,
        v->nSamplesPerSec,
        v->wBitsPerSample,
        v->wFormatTag);
}

void
WWWFEXDebug(WAVEFORMATEXTENSIBLE *v)
{
    (void)v;

    dprintf(
        "  dwChannelMask=0x%x\n"
        "  Samples.wValidBitsPerSample=%d\n"
        "  SubFormat=%08x-%04x-%04x-%02x%02x%02x%02x%02x%02x%02x%02x\n",
        v->dwChannelMask,
        v->Samples.wValidBitsPerSample,
        v->SubFormat.Data1,
        v->SubFormat.Data2,
        v->SubFormat.Data3,
        v->SubFormat.Data4[0],
        v->SubFormat.Data4[1],
        v->SubFormat.Data4[2],
        v->SubFormat.Data4[3],
        v->SubFormat.Data4[4],
        v->SubFormat.Data4[5],
        v->SubFormat.Data4[6],
        v->SubFormat.Data4[7]
        );
}

enum State {
    SStart,
    SInToken,
    SInEscapedToken,
    SSkipWhiteSpace,
};

/// white spaceで区切られたトークン列から、トークンの配列を取り出す。
void
WWSplit(std::wstring s, std::vector<std::wstring> & result)
{
    result.clear();

    if (s[0] == 0) {
        return;
    }

    std::wstring sb;
    State state = SStart;

    for (uint32_t i=0; i<s.length(); ++i) {
        switch (state) {
        case SStart:
            if (s[i] == L'\"') {
                state = SInEscapedToken;
            } else {
                state = SInToken;
                sb += s[i];
            }
            break;
        case SInToken:
            if (s[i] == L' ') {
                if (0 < sb.length()) {
                    result.push_back(sb);
                    sb = L"";
                }
                state = SSkipWhiteSpace;
            } else {
                sb += s[i];
            }
            break;
        case SInEscapedToken:
            if (s[i] == L'\\') {
                ++i;
                if (i < s.length()) {
                    sb += s[i];
                }
            } else if (s[i] == L'\"') {
                if (0 < sb.length()) {
                    result.push_back(sb);
                    sb = L"";
                }
                state = SSkipWhiteSpace;
            } else {
                sb += s[i];
            }
            break;
        case SSkipWhiteSpace:
            if (s[i] == L'\"') {
                state = SInEscapedToken;
            } else if (s[i] != L' ') {
                state = SInToken;
                sb.push_back(s[i]);
            } else {
                // do nothing
            }
            break;
        }
    }

    if (state == SInToken) {
        if (0 < sb.length()) {
            result.push_back(sb);
            sb = L"";
        }
    }
}

/// comma separated numberから、フラグ配列をセット。
/// flagCount==8のとき
/// 例: "1"     → 0,1,0,0,0,0,0,0
/// 例: "1,3,4" → 0,1,0,1,1,0,0,0
/// 例: "-1"    → 1,1,1,1,1,1,1,1
void
WWCommaSeparatedIdxToFlagArray(const std::wstring sIn, bool *flagAry_out, const int flagCount)
{
    assert(sIn.length() < 512);
    assert(flagAry_out);
    assert(0 < flagCount);

    wchar_t s[512];
    wcsncpy_s(s, sIn.c_str(), 511);
    s[511] = 0;

    // reset all flags
    for (int i=0; i<flagCount; ++i) {
        flagAry_out[i] = 0;
    }

    wchar_t *tokenCtx = nullptr;
    wchar_t *p = wcstok_s(s, L",", &tokenCtx);
    while (p != nullptr) {
        // 存在する。
        int idx = 0;
        int rv = swscanf_s(p, L"%d", &idx);
        if (rv == 1) {
            // 成功。
            if (idx == -1) {
                for (int i=0; i<flagCount; ++i) {
                    flagAry_out[i] = true;
                }
            } else {
                if (0 <= idx && idx < flagCount) {
                    flagAry_out[idx] = true;
                } else {
                    // 範囲外の値。
                    printf("D: flagAry out of range value %d %d\n", idx, flagCount);
                }
            }
        }

        p = wcstok_s(nullptr, L",", &tokenCtx);
    }
}

