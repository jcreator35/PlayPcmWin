#!/bin/bash

./FloatDynamicRange -generate32 24 original.bin

for i in `seq 1 100`;do
    echo -n $i " : "
    ./FloatDynamicRange -convert32 24 original.bin $i 1  mul.bin
    ./FloatDynamicRange -convert32 24 mul.bin      1  $i muldiv.bin
    diff -s original.bin muldiv.bin
done

