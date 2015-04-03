#!/bin/bash

./FloatDynamicRange -generate32 24 original.bin

for i in `seq 1 100`;do
    echo -n $i " : "
    ./FloatDynamicRange -convert32 24 original.bin 1  $i div.bin
    ./FloatDynamicRange -convert32 24 div.bin      $i 1  divmul.bin
    diff -s original.bin divmul.bin
done

