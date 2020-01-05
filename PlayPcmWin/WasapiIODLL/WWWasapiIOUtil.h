#pragma once
// 日本語 UTF-8

#include <Windows.h>
#include <mmsystem.h>
#include <MMReg.h>
#include <string>
#include <vector>

void
WWWaveFormatDebug(WAVEFORMATEX *v);

void
WWWFEXDebug(WAVEFORMATEXTENSIBLE *v);

/// white spaceで区切られたトークン列から、トークンの配列を取り出す。
void
WWSplit(std::wstring s, std::vector<std::wstring> & result);

/// comma separated numberから、フラグ配列をセット。
/// flagCount==8のとき
/// 例: "1"     → 0,1,0,0,0,0,0,0
/// 例: "1,3,4" → 0,1,0,1,1,0,0,0
/// 例: "-1"    → 1,1,1,1,1,1,1,1
void
WWCommaSeparatedIdxToFlagArray(const std::wstring sIn, bool *flagAry_out, const int flagCount);
