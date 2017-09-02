// 日本語

#pragma once

/// カイザー窓。
/// @param alpha カイザー窓のパラーメーター α
/// @param window_r [out] 窓の係数。要素数はlength個。
void WWKaiserWindow(double alpha, int length, double *window_r);
