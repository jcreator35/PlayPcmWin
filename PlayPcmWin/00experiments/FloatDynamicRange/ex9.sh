#!/bin/bash -x

# FTZフラグを立てて、subnormal numberを0にしたときの
# 24bit signed int PCM を格納する[-1.0, 1.0)の32bit floatデータのダイナミックレンジを調べる。

./FloatDynamicRange -generate32 24 original.bin

./FloatDynamicRange -convert32 -ftz 24 original.bin 1 1048576 M120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M120dB.bin   1 1048576 M241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M241dB.bin   1 1048576 M361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M361dB.bin   1 1048576 M482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M482dB.bin   1 1048576 M602dB.bin
./FloatDynamicRange -convert32 -ftz 24 M602dB.bin   1 1048576 M722dB.bin

./FloatDynamicRange -convert32 -ftz 24 M602dB.bin       1048576 1 M602dBP120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M602dBP120dB.bin 1048576 1 M602dBP241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M602dBP241dB.bin 1048576 1 M602dBP361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M602dBP361dB.bin 1048576 1 M602dBP482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M602dBP482dB.bin 1048576 1 M602dBP602dB.bin

diff -s original.bin M602dBP602dB.bin

./FloatDynamicRange -convert32 -ftz 24 M722dB.bin       1048576 1 M722dBP120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M722dBP120dB.bin 1048576 1 M722dBP241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M722dBP241dB.bin 1048576 1 M722dBP361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M722dBP361dB.bin 1048576 1 M722dBP482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M722dBP482dB.bin 1048576 1 M722dBP602dB.bin
./FloatDynamicRange -convert32 -ftz 24 M722dBP602dB.bin 1048576 1 M722dBP722dB.bin

diff -s original.bin M722dBP722dB.bin

# -602dBして+602dBすると元に戻る。
# -722dBして+722dBすると元に戻らない。
#
#  0.5倍 == 20 * log10(0.5) = -6.0205999dB
#
# 100 * -6.0205999 == 602dB
# 120 * -6.0205999 == 722dB
# 110 * -6.0205999 == 662dB

./FloatDynamicRange -convert32 -ftz 24 M602dB.bin  1 1024 M662dB.bin

./FloatDynamicRange -convert32 -ftz 24 M662dB.bin       1048576 1 M662dBP120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M662dBP120dB.bin 1048576 1 M662dBP241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M662dBP241dB.bin 1048576 1 M662dBP361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M662dBP361dB.bin 1048576 1 M662dBP482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M662dBP482dB.bin 1048576 1 M662dBP602dB.bin
./FloatDynamicRange -convert32 -ftz 24 M662dBP602dB.bin 1024    1 M662dBP662dB.bin

diff -s original.bin M662dBP662dB.bin

# -602dBして+602dBすると元に戻る。
# -662dBして+662dBすると元に戻らない。

# 100 * -6.0205999 == 602dB
# 110 * -6.0205999 == 662dB
# 105 * -6.0205999 == 632dB

./FloatDynamicRange -convert32 -ftz 24 M602dB.bin  1 32 M632dB.bin

./FloatDynamicRange -convert32 -ftz 24 M632dB.bin       1048576 1 M632dBP120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M632dBP120dB.bin 1048576 1 M632dBP241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M632dBP241dB.bin 1048576 1 M632dBP361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M632dBP361dB.bin 1048576 1 M632dBP482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M632dBP482dB.bin 1048576 1 M632dBP602dB.bin
./FloatDynamicRange -convert32 -ftz 24 M632dBP602dB.bin 32      1 M632dBP632dB.bin

diff -s original.bin M632dBP632dB.bin

# -602dBして+602dBすると元に戻る。
# -632dBして+632dBすると元に戻らない。

# 100 * -6.0205999 == 602dB
# 105 * -6.0205999 == 632dB
# 103 * -6.0205999 == 620dB

./FloatDynamicRange -convert32 -ftz 24 M602dB.bin  1 8 M620dB.bin

./FloatDynamicRange -convert32 -ftz 24 M620dB.bin       1048576 1 M620dBP120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M620dBP120dB.bin 1048576 1 M620dBP241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M620dBP241dB.bin 1048576 1 M620dBP361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M620dBP361dB.bin 1048576 1 M620dBP482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M620dBP482dB.bin 1048576 1 M620dBP602dB.bin
./FloatDynamicRange -convert32 -ftz 24 M620dBP602dB.bin 8       1 M620dBP620dB.bin

diff -s original.bin M620dBP620dB.bin

# -620dBして+620dBすると元に戻る。
# -632dBして+632dBすると元に戻らない。

# 103 * -6.0205999 == 620dB
# 105 * -6.0205999 == 632dB
# 104 * -6.0205999 == 626dB

./FloatDynamicRange -convert32 -ftz 24 M602dB.bin  1 16 M626dB.bin

./FloatDynamicRange -convert32 -ftz 24 M626dB.bin       1048576 1 M626dBP120dB.bin
./FloatDynamicRange -convert32 -ftz 24 M626dBP120dB.bin 1048576 1 M626dBP241dB.bin
./FloatDynamicRange -convert32 -ftz 24 M626dBP241dB.bin 1048576 1 M626dBP361dB.bin
./FloatDynamicRange -convert32 -ftz 24 M626dBP361dB.bin 1048576 1 M626dBP482dB.bin
./FloatDynamicRange -convert32 -ftz 24 M626dBP482dB.bin 1048576 1 M626dBP602dB.bin
./FloatDynamicRange -convert32 -ftz 24 M626dBP602dB.bin 16      1 M626dBP626dB.bin

diff -s original.bin M626dBP626dB.bin

# -620dBして+620dBすると元に戻る。
# -626dBして+626dBすると元に戻らない。

echo "#########################################################################"

./FloatDynamicRange -generate32 24 original.bin

./FloatDynamicRange -convert32 -ftz 24 original.bin 1048576 1 P120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P120dB.bin   1048576 1 P241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P241dB.bin   1048576 1 P361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P361dB.bin   1048576 1 P482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P482dB.bin   1048576 1 P602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P602dB.bin   1048576 1 P722dB.bin
./FloatDynamicRange -convert32 -ftz 24 P722dB.bin   1048576 1 P843dB.bin

./FloatDynamicRange -convert32 -ftz 24 P722dB.bin       1 1048576 P722dBM120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P722dBM120dB.bin 1 1048576 P722dBM241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P722dBM241dB.bin 1 1048576 P722dBM361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P722dBM361dB.bin 1 1048576 P722dBM482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P722dBM482dB.bin 1 1048576 P722dBM602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P722dBM602dB.bin 1 1048576 P722dBM722dB.bin

diff -s original.bin P722dBM722dB.bin

./FloatDynamicRange -convert32 -ftz 24 P843dB.bin       1 1048576 P843dBM120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P843dBM120dB.bin 1 1048576 P843dBM241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P843dBM241dB.bin 1 1048576 P843dBM361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P843dBM361dB.bin 1 1048576 P843dBM482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P843dBM482dB.bin 1 1048576 P843dBM602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P843dBM602dB.bin 1 1048576 P843dBM722dB.bin
./FloatDynamicRange -convert32 -ftz 24 P843dBM722dB.bin 1 1048576 P843dBM843dB.bin

diff -s original.bin P843dBM843dB.bin

# +722dBして-722dBすると元に戻る。
# +843dBして-843dBすると元に戻らない。
#
#  0.5倍 == 20 * log10(0.5) = -6.0205999dB
#
# 120 * -6.0205999 == 722dB
# 140 * -6.0205999 == 843dB
# 130 * -6.0205999 == 783dB

./FloatDynamicRange -convert32 -ftz 24 P722dB.bin  1024 1 P783dB.bin

./FloatDynamicRange -convert32 -ftz 24 P783dB.bin       1 1048576 P783dBM120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P783dBM120dB.bin 1 1048576 P783dBM241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P783dBM241dB.bin 1 1048576 P783dBM361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P783dBM361dB.bin 1 1048576 P783dBM482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P783dBM482dB.bin 1 1048576 P783dBM602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P783dBM602dB.bin 1 1048576 P783dBM722dB.bin
./FloatDynamicRange -convert32 -ftz 24 P783dBM722dB.bin 1 1024    P783dBM783dB.bin

diff -s original.bin P783dBM783dB.bin

# -722dBして+722dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 120 * -6.0205999 == 722dB
# 130 * -6.0205999 == 783dB
# 125 * -6.0205999 == 753dB

./FloatDynamicRange -convert32 -ftz 24 P722dB.bin  32 1 P753dB.bin

./FloatDynamicRange -convert32 -ftz 24 P753dB.bin       1 1048576 P753dBM120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P753dBM120dB.bin 1 1048576 P753dBM241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P753dBM241dB.bin 1 1048576 P753dBM361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P753dBM361dB.bin 1 1048576 P753dBM482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P753dBM482dB.bin 1 1048576 P753dBM602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P753dBM602dB.bin 1 1048576 P753dBM722dB.bin
./FloatDynamicRange -convert32 -ftz 24 P753dBM722dB.bin 1 32      P753dBM753dB.bin

diff -s original.bin P753dBM753dB.bin

# -753dBして+753dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 125 * -6.0205999 == 753dB
# 130 * -6.0205999 == 783dB
# 127 * -6.0205999 == 765dB

./FloatDynamicRange -convert32 -ftz 24 P722dB.bin  128 1 P765dB.bin

./FloatDynamicRange -convert32 -ftz 24 P765dB.bin       1 1048576 P765dBM120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P765dBM120dB.bin 1 1048576 P765dBM241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P765dBM241dB.bin 1 1048576 P765dBM361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P765dBM361dB.bin 1 1048576 P765dBM482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P765dBM482dB.bin 1 1048576 P765dBM602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P765dBM602dB.bin 1 1048576 P765dBM722dB.bin
./FloatDynamicRange -convert32 -ftz 24 P765dBM722dB.bin 1 128     P765dBM765dB.bin

diff -s original.bin P765dBM765dB.bin

# -765dBして+765dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 127 * -6.0205999 == 765dB
# 130 * -6.0205999 == 783dB
# 128 * -6.0205999 == 771dB

./FloatDynamicRange -convert32 -ftz 24 P722dB.bin  256 1 P771dB.bin

./FloatDynamicRange -convert32 -ftz 24 P771dB.bin       1 1048576 P771dBM120dB.bin
./FloatDynamicRange -convert32 -ftz 24 P771dBM120dB.bin 1 1048576 P771dBM241dB.bin
./FloatDynamicRange -convert32 -ftz 24 P771dBM241dB.bin 1 1048576 P771dBM361dB.bin
./FloatDynamicRange -convert32 -ftz 24 P771dBM361dB.bin 1 1048576 P771dBM482dB.bin
./FloatDynamicRange -convert32 -ftz 24 P771dBM482dB.bin 1 1048576 P771dBM602dB.bin
./FloatDynamicRange -convert32 -ftz 24 P771dBM602dB.bin 1 1048576 P771dBM722dB.bin
./FloatDynamicRange -convert32 -ftz 24 P771dBM722dB.bin 1 256     P771dBM771dB.bin

diff -s original.bin P771dBM771dB.bin

# -765dBして+765dBすると元に戻る。
# -771dBして+771dBすると元に戻らない。


