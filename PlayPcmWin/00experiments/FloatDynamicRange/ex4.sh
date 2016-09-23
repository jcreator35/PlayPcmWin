#!/bin/bash -x

./FloatDynamicRange -generate32 24 original.bin

./FloatDynamicRange -convert32 24 original.bin 1 1048576 M120dB.bin
./FloatDynamicRange -convert32 24 M120dB.bin   1 1048576 M241dB.bin
./FloatDynamicRange -convert32 24 M241dB.bin   1 1048576 M361dB.bin
./FloatDynamicRange -convert32 24 M361dB.bin   1 1048576 M482dB.bin
./FloatDynamicRange -convert32 24 M482dB.bin   1 1048576 M602dB.bin
./FloatDynamicRange -convert32 24 M602dB.bin   1 1048576 M722dB.bin
./FloatDynamicRange -convert32 24 M722dB.bin   1 1048576 M843dB.bin

./FloatDynamicRange -convert32 24 M722dB.bin       1048576 1 M722dBP120dB.bin
./FloatDynamicRange -convert32 24 M722dBP120dB.bin 1048576 1 M722dBP241dB.bin
./FloatDynamicRange -convert32 24 M722dBP241dB.bin 1048576 1 M722dBP361dB.bin
./FloatDynamicRange -convert32 24 M722dBP361dB.bin 1048576 1 M722dBP482dB.bin
./FloatDynamicRange -convert32 24 M722dBP482dB.bin 1048576 1 M722dBP602dB.bin
./FloatDynamicRange -convert32 24 M722dBP602dB.bin 1048576 1 M722dBP722dB.bin

diff -s original.bin M722dBP722dB.bin

./FloatDynamicRange -convert32 24 M843dB.bin       1048576 1 M843dBP120dB.bin
./FloatDynamicRange -convert32 24 M843dBP120dB.bin 1048576 1 M843dBP241dB.bin
./FloatDynamicRange -convert32 24 M843dBP241dB.bin 1048576 1 M843dBP361dB.bin
./FloatDynamicRange -convert32 24 M843dBP361dB.bin 1048576 1 M843dBP482dB.bin
./FloatDynamicRange -convert32 24 M843dBP482dB.bin 1048576 1 M843dBP602dB.bin
./FloatDynamicRange -convert32 24 M843dBP602dB.bin 1048576 1 M843dBP722dB.bin
./FloatDynamicRange -convert32 24 M843dBP722dB.bin 1048576 1 M843dBP843dB.bin

diff -s original.bin M843dBP843dB.bin

# -722dBして+722dBすると元に戻る。
# -843dBして+843dBすると元に戻らない。
#
#  0.5倍 == 20 * log10(0.5) = -6.0205999dB
#
# 120 * -6.0205999 == 722dB
# 140 * -6.0205999 == 843dB
# 130 * -6.0205999 == 783dB

./FloatDynamicRange -convert32 24 M722dB.bin  1 1024 M783dB.bin

./FloatDynamicRange -convert32 24 M783dB.bin       1048576 1 M783dBP120dB.bin
./FloatDynamicRange -convert32 24 M783dBP120dB.bin 1048576 1 M783dBP241dB.bin
./FloatDynamicRange -convert32 24 M783dBP241dB.bin 1048576 1 M783dBP361dB.bin
./FloatDynamicRange -convert32 24 M783dBP361dB.bin 1048576 1 M783dBP482dB.bin
./FloatDynamicRange -convert32 24 M783dBP482dB.bin 1048576 1 M783dBP602dB.bin
./FloatDynamicRange -convert32 24 M783dBP602dB.bin 1048576 1 M783dBP722dB.bin
./FloatDynamicRange -convert32 24 M783dBP722dB.bin 1024    1 M783dBP783dB.bin

diff -s original.bin M783dBP783dB.bin

# -722dBして+722dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 120 * -6.0205999 == 722dB
# 130 * -6.0205999 == 783dB
# 125 * -6.0205999 == 753dB

./FloatDynamicRange -convert32 24 M722dB.bin  1 32 M753dB.bin

./FloatDynamicRange -convert32 24 M753dB.bin       1048576 1 M753dBP120dB.bin
./FloatDynamicRange -convert32 24 M753dBP120dB.bin 1048576 1 M753dBP241dB.bin
./FloatDynamicRange -convert32 24 M753dBP241dB.bin 1048576 1 M753dBP361dB.bin
./FloatDynamicRange -convert32 24 M753dBP361dB.bin 1048576 1 M753dBP482dB.bin
./FloatDynamicRange -convert32 24 M753dBP482dB.bin 1048576 1 M753dBP602dB.bin
./FloatDynamicRange -convert32 24 M753dBP602dB.bin 1048576 1 M753dBP722dB.bin
./FloatDynamicRange -convert32 24 M753dBP722dB.bin 32      1 M753dBP753dB.bin

diff -s original.bin M753dBP753dB.bin

# -753dBして+753dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 125 * -6.0205999 == 753dB
# 130 * -6.0205999 == 783dB
# 127 * -6.0205999 == 765dB

./FloatDynamicRange -convert32 24 M722dB.bin  1 128 M765dB.bin

./FloatDynamicRange -convert32 24 M765dB.bin       1048576 1 M765dBP120dB.bin
./FloatDynamicRange -convert32 24 M765dBP120dB.bin 1048576 1 M765dBP241dB.bin
./FloatDynamicRange -convert32 24 M765dBP241dB.bin 1048576 1 M765dBP361dB.bin
./FloatDynamicRange -convert32 24 M765dBP361dB.bin 1048576 1 M765dBP482dB.bin
./FloatDynamicRange -convert32 24 M765dBP482dB.bin 1048576 1 M765dBP602dB.bin
./FloatDynamicRange -convert32 24 M765dBP602dB.bin 1048576 1 M765dBP722dB.bin
./FloatDynamicRange -convert32 24 M765dBP722dB.bin 128     1 M765dBP765dB.bin

diff -s original.bin M765dBP765dB.bin

# -753dBして+753dBすると元に戻る。
# -765dBして+765dBすると元に戻らない。

# 125 * -6.0205999 == 753dB
# 127 * -6.0205999 == 765dB
# 126 * -6.0205999 == 759dB

./FloatDynamicRange -convert32 24 M722dB.bin  1 64 M759dB.bin

./FloatDynamicRange -convert32 24 M759dB.bin       1048576 1 M759dBP120dB.bin
./FloatDynamicRange -convert32 24 M759dBP120dB.bin 1048576 1 M759dBP241dB.bin
./FloatDynamicRange -convert32 24 M759dBP241dB.bin 1048576 1 M759dBP361dB.bin
./FloatDynamicRange -convert32 24 M759dBP361dB.bin 1048576 1 M759dBP482dB.bin
./FloatDynamicRange -convert32 24 M759dBP482dB.bin 1048576 1 M759dBP602dB.bin
./FloatDynamicRange -convert32 24 M759dBP602dB.bin 1048576 1 M759dBP722dB.bin
./FloatDynamicRange -convert32 24 M759dBP722dB.bin 64      1 M759dBP759dB.bin

diff -s original.bin M759dBP759dB.bin

# -759dBして+759dBすると元に戻る。
# -765dBして+765dBすると元に戻らない。


