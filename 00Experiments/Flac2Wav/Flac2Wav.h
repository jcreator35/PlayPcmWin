// 日本語UTF-8
#pragma once

enum Flac2WavResultType {
    F2WRT_Success = 0,
    F2WRT_WriteOpenFailed,
    F2WRT_FlacStreamDecoderNewFailed,
    F2WRT_FlacStreamDecoderInitFailed,
    F2WRT_LostSync,
    F2WRT_BadHeader,
    F2WRT_FrameCrcMismatch,
    F2WRT_Unparseable,
    F2WRT_OtherError
};

/// FLACファイルを読み込んで、WAVファイルを出力する。
/// @return 0 成功。1以上: エラー。Flac2WavResultType参照。
extern "C" __declspec(dllexport)
int __stdcall
Flac2Wav(const char *fromFlacPath, const char *toWavPath);
