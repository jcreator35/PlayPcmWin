#pragma once

/// @param dst must be aligned by 16 bytes
/// @param src must be aligned by 16 bytes
/// @param bytes must be multiply of 128
extern "C" void MyMemcpy64(char *dst, const char *src, int bytes);

extern "C" void MyMemcpy64a(char *dst, const char *src, int bytes);
