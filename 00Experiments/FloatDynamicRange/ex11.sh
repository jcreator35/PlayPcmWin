#!/bin/bash -x

./FloatDynamicRange -generate32 24 original.bin

./FloatDynamicRange -convert32 24 original.bin 1  10 D10.bin
./FloatDynamicRange -convert32 24 D10.bin      10 1  D10M10.bin

diff -s original.bin D10M10.bin
