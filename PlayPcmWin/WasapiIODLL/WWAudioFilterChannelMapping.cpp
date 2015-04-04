// 日本語 UTF-8

#include "WWAudioFilterChannelMapping.h"
#include "WWTypes.h"
#include <assert.h>
#include <vector>
#include <string>
#include <string.h>
#include <stdint.h>

enum State {
    SStart,
    SInToken,
    SInEscapedToken,
    SSkipWhiteSpace,
};

static void
Split(std::wstring s, std::vector<std::wstring> & result)
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

struct ChannelMappingItem {
    uint32_t fromCh;
    uint32_t toCh;
};

static bool
ParseChannelMappingItem(std::wstring s, ChannelMappingItem &item_return)
{
    std::wstring::size_type found = s.find(L'>');
    if (found == std::wstring::npos || s.length()-1 <= found) {
        return false;
    }

    s[found] = 0;
    const wchar_t *sc = s.c_str();

    swscanf_s(&sc[0],       L"%u", &item_return.fromCh);
    swscanf_s(&sc[found+1], L"%u", &item_return.toCh);

    return true;
}

WWAudioFilterChannelMapping::WWAudioFilterChannelMapping(PCWSTR args)
{
    memset(mRoutingTable, 0, sizeof mRoutingTable);
    mNumOfChannels = 0;

    std::vector<std::wstring> argVector;
    Split(args, argVector);

    if (WW_CHANNEL_NUM < argVector.size()) {
        // 失敗。
        return;
    }
    
    std::vector<ChannelMappingItem> routingVector;
    for (uint32_t i=0; i<argVector.size(); ++i) {
        ChannelMappingItem item;
        if (ParseChannelMappingItem(argVector[i], item)) {
            if (argVector.size() <= item.fromCh
                    || argVector.size() <= item.toCh) {
                // 失敗。
                assert(0);
                return;
            }
            mRoutingTable[item.fromCh] = item.toCh;
        }
    }

    // 成功。
    mNumOfChannels = (int)argVector.size();
}

void
WWAudioFilterChannelMapping::UpdateSampleFormat(
        WWPcmDataSampleFormatType format,
        WWStreamType streamType, int numChannels)
{
    mManip.UpdateFormat(format, streamType, numChannels);
}

void
WWAudioFilterChannelMapping::Filter(unsigned char *buff, int bytes)
{
    // PCM, DSD共通の処理。
    float sampleValueOfChannel[WW_CHANNEL_NUM];

    if (mNumOfChannels != mManip.NumChannels()) {
        return;
    }

    int nFrames = bytes / (mManip.NumChannels() * mManip.BitsPerSample() / 8);

    for (int i=0; i<nFrames; ++i) {
        // sampleValueOfChannelを準備する。
        // 表に従ってチャンネルをルーティングする。

        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            float v = 0.0f;
            mManip.GetFloatSample(buff, bytes, i, ch, v);
            sampleValueOfChannel[ch] = v;
        }

        for (int ch=0; ch<mManip.NumChannels(); ++ch) {
            mManip.SetFloatSample(buff, bytes, i, ch, sampleValueOfChannel[mRoutingTable[ch]]);
        }
    }
}

