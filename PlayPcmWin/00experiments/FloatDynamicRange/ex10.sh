#!/bin/bash -x

# 24bit signed int PCM を格納する[-1.0, 1.0)の32bit floatについて、DC成分xを加算した後xを減算し、元の値に戻るか調べる。

./FloatDynamicRange -generate32 24 original.bin

./FloatDynamicRange -add32 original.bin 1 1 A1.bin
./FloatDynamicRange -add32 A1.bin      -1 1 A1S1.bin

diff -s original.bin A1S1.bin

./FloatDynamicRange -add32 original.bin 2 1 A2.bin
./FloatDynamicRange -add32 A2.bin      -2 1 A2S2.bin

diff -s original.bin A2S2.bin

