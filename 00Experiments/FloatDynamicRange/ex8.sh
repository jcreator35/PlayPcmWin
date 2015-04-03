#!/bin/bash -x

./FloatDynamicRange -generate32 25 original.bin

./FloatDynamicRange -convert32 25 original.bin 1 1048576 M120dB.bin
./FloatDynamicRange -convert32 25 M120dB.bin   1 1048576 M241dB.bin
./FloatDynamicRange -convert32 25 M241dB.bin   1 1048576 M361dB.bin
./FloatDynamicRange -convert32 25 M361dB.bin   1 1048576 M482dB.bin
./FloatDynamicRange -convert32 25 M482dB.bin   1 1048576 M602dB.bin
./FloatDynamicRange -convert32 25 M602dB.bin   1 1048576 M722dB.bin
./FloatDynamicRange -convert32 25 M722dB.bin   1 1048576 M843dB.bin

./FloatDynamicRange -convert32 25 M722dB.bin       1048576 1 M722dBP120dB.bin
./FloatDynamicRange -convert32 25 M722dBP120dB.bin 1048576 1 M722dBP241dB.bin
./FloatDynamicRange -convert32 25 M722dBP241dB.bin 1048576 1 M722dBP361dB.bin
./FloatDynamicRange -convert32 25 M722dBP361dB.bin 1048576 1 M722dBP482dB.bin
./FloatDynamicRange -convert32 25 M722dBP482dB.bin 1048576 1 M722dBP602dB.bin
./FloatDynamicRange -convert32 25 M722dBP602dB.bin 1048576 1 M722dBP722dB.bin

diff -s original.bin M722dBP722dB.bin

./FloatDynamicRange -convert32 25 M843dB.bin       1048576 1 M843dBP120dB.bin
./FloatDynamicRange -convert32 25 M843dBP120dB.bin 1048576 1 M843dBP241dB.bin
./FloatDynamicRange -convert32 25 M843dBP241dB.bin 1048576 1 M843dBP361dB.bin
./FloatDynamicRange -convert32 25 M843dBP361dB.bin 1048576 1 M843dBP482dB.bin
./FloatDynamicRange -convert32 25 M843dBP482dB.bin 1048576 1 M843dBP602dB.bin
./FloatDynamicRange -convert32 25 M843dBP602dB.bin 1048576 1 M843dBP722dB.bin
./FloatDynamicRange -convert32 25 M843dBP722dB.bin 1048576 1 M843dBP843dB.bin

diff -s original.bin M843dBP843dB.bin

# -722dBして+722dBすると元に戻る。
# -843dBして+843dBすると元に戻らない。
#
#  0.5倍 == 20 * log10(0.5) = -6.0205999dB
#
# 120 * -6.0205999 == 722dB
# 140 * -6.0205999 == 843dB
# 130 * -6.0205999 == 783dB

./FloatDynamicRange -convert32 25 M722dB.bin  1 1024 M783dB.bin

./FloatDynamicRange -convert32 25 M783dB.bin       1048576 1 M783dBP120dB.bin
./FloatDynamicRange -convert32 25 M783dBP120dB.bin 1048576 1 M783dBP241dB.bin
./FloatDynamicRange -convert32 25 M783dBP241dB.bin 1048576 1 M783dBP361dB.bin
./FloatDynamicRange -convert32 25 M783dBP361dB.bin 1048576 1 M783dBP482dB.bin
./FloatDynamicRange -convert32 25 M783dBP482dB.bin 1048576 1 M783dBP602dB.bin
./FloatDynamicRange -convert32 25 M783dBP602dB.bin 1048576 1 M783dBP722dB.bin
./FloatDynamicRange -convert32 25 M783dBP722dB.bin 1024    1 M783dBP783dB.bin

diff -s original.bin M783dBP783dB.bin

# -722dBして+722dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 120 * -6.0205999 == 722dB
# 130 * -6.0205999 == 783dB
# 125 * -6.0205999 == 753dB

./FloatDynamicRange -convert32 25 M722dB.bin  1 32 M753dB.bin

./FloatDynamicRange -convert32 25 M753dB.bin       1048576 1 M753dBP120dB.bin
./FloatDynamicRange -convert32 25 M753dBP120dB.bin 1048576 1 M753dBP241dB.bin
./FloatDynamicRange -convert32 25 M753dBP241dB.bin 1048576 1 M753dBP361dB.bin
./FloatDynamicRange -convert32 25 M753dBP361dB.bin 1048576 1 M753dBP482dB.bin
./FloatDynamicRange -convert32 25 M753dBP482dB.bin 1048576 1 M753dBP602dB.bin
./FloatDynamicRange -convert32 25 M753dBP602dB.bin 1048576 1 M753dBP722dB.bin
./FloatDynamicRange -convert32 25 M753dBP722dB.bin 32      1 M753dBP753dB.bin

diff -s original.bin M753dBP753dB.bin

# -753dBして+753dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 125 * -6.0205999 == 753dB
# 130 * -6.0205999 == 783dB
# 127 * -6.0205999 == 765dB

./FloatDynamicRange -convert32 25 M722dB.bin  1 128 M765dB.bin

./FloatDynamicRange -convert32 25 M765dB.bin       1048576 1 M765dBP120dB.bin
./FloatDynamicRange -convert32 25 M765dBP120dB.bin 1048576 1 M765dBP241dB.bin
./FloatDynamicRange -convert32 25 M765dBP241dB.bin 1048576 1 M765dBP361dB.bin
./FloatDynamicRange -convert32 25 M765dBP361dB.bin 1048576 1 M765dBP482dB.bin
./FloatDynamicRange -convert32 25 M765dBP482dB.bin 1048576 1 M765dBP602dB.bin
./FloatDynamicRange -convert32 25 M765dBP602dB.bin 1048576 1 M765dBP722dB.bin
./FloatDynamicRange -convert32 25 M765dBP722dB.bin 128     1 M765dBP765dB.bin

diff -s original.bin M765dBP765dB.bin

# -753dBして+753dBすると元に戻る。
# -765dBして+765dBすると元に戻らない。

# 125 * -6.0205999 == 753dB
# 127 * -6.0205999 == 765dB
# 126 * -6.0205999 == 759dB

./FloatDynamicRange -convert32 25 M722dB.bin  1 64 M759dB.bin

./FloatDynamicRange -convert32 25 M759dB.bin       1048576 1 M759dBP120dB.bin
./FloatDynamicRange -convert32 25 M759dBP120dB.bin 1048576 1 M759dBP241dB.bin
./FloatDynamicRange -convert32 25 M759dBP241dB.bin 1048576 1 M759dBP361dB.bin
./FloatDynamicRange -convert32 25 M759dBP361dB.bin 1048576 1 M759dBP482dB.bin
./FloatDynamicRange -convert32 25 M759dBP482dB.bin 1048576 1 M759dBP602dB.bin
./FloatDynamicRange -convert32 25 M759dBP602dB.bin 1048576 1 M759dBP722dB.bin
./FloatDynamicRange -convert32 25 M759dBP722dB.bin 64      1 M759dBP759dB.bin

diff -s original.bin M759dBP759dB.bin

# -753dBして+753dBすると元に戻る。
# -759dBして+759dBすると元に戻らない。

echo "####################################################################"

./FloatDynamicRange -generate32 25 original.bin

./FloatDynamicRange -convert32 25 original.bin 1048576 1 P120dB.bin
./FloatDynamicRange -convert32 25 P120dB.bin   1048576 1 P241dB.bin
./FloatDynamicRange -convert32 25 P241dB.bin   1048576 1 P361dB.bin
./FloatDynamicRange -convert32 25 P361dB.bin   1048576 1 P482dB.bin
./FloatDynamicRange -convert32 25 P482dB.bin   1048576 1 P602dB.bin
./FloatDynamicRange -convert32 25 P602dB.bin   1048576 1 P722dB.bin
./FloatDynamicRange -convert32 25 P722dB.bin   1048576 1 P843dB.bin

./FloatDynamicRange -convert32 25 P722dB.bin       1 1048576 P722dBM120dB.bin
./FloatDynamicRange -convert32 25 P722dBM120dB.bin 1 1048576 P722dBM241dB.bin
./FloatDynamicRange -convert32 25 P722dBM241dB.bin 1 1048576 P722dBM361dB.bin
./FloatDynamicRange -convert32 25 P722dBM361dB.bin 1 1048576 P722dBM482dB.bin
./FloatDynamicRange -convert32 25 P722dBM482dB.bin 1 1048576 P722dBM602dB.bin
./FloatDynamicRange -convert32 25 P722dBM602dB.bin 1 1048576 P722dBM722dB.bin

diff -s original.bin P722dBM722dB.bin

./FloatDynamicRange -convert32 25 P843dB.bin       1 1048576 P843dBM120dB.bin
./FloatDynamicRange -convert32 25 P843dBM120dB.bin 1 1048576 P843dBM241dB.bin
./FloatDynamicRange -convert32 25 P843dBM241dB.bin 1 1048576 P843dBM361dB.bin
./FloatDynamicRange -convert32 25 P843dBM361dB.bin 1 1048576 P843dBM482dB.bin
./FloatDynamicRange -convert32 25 P843dBM482dB.bin 1 1048576 P843dBM602dB.bin
./FloatDynamicRange -convert32 25 P843dBM602dB.bin 1 1048576 P843dBM722dB.bin
./FloatDynamicRange -convert32 25 P843dBM722dB.bin 1 1048576 P843dBM843dB.bin

diff -s original.bin P843dBM843dB.bin

# +722dBして-722dBすると元に戻る。
# +843dBして-843dBすると元に戻らない。
#
#  0.5倍 == 20 * log10(0.5) = -6.0205999dB
#
# 120 * -6.0205999 == 722dB
# 140 * -6.0205999 == 843dB
# 130 * -6.0205999 == 783dB

./FloatDynamicRange -convert32 25 P722dB.bin  1024 1 P783dB.bin

./FloatDynamicRange -convert32 25 P783dB.bin       1 1048576 P783dBM120dB.bin
./FloatDynamicRange -convert32 25 P783dBM120dB.bin 1 1048576 P783dBM241dB.bin
./FloatDynamicRange -convert32 25 P783dBM241dB.bin 1 1048576 P783dBM361dB.bin
./FloatDynamicRange -convert32 25 P783dBM361dB.bin 1 1048576 P783dBM482dB.bin
./FloatDynamicRange -convert32 25 P783dBM482dB.bin 1 1048576 P783dBM602dB.bin
./FloatDynamicRange -convert32 25 P783dBM602dB.bin 1 1048576 P783dBM722dB.bin
./FloatDynamicRange -convert32 25 P783dBM722dB.bin 1 1024    P783dBM783dB.bin

diff -s original.bin P783dBM783dB.bin

# -722dBして+722dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 120 * -6.0205999 == 722dB
# 130 * -6.0205999 == 783dB
# 125 * -6.0205999 == 753dB

./FloatDynamicRange -convert32 25 P722dB.bin  32 1 P753dB.bin

./FloatDynamicRange -convert32 25 P753dB.bin       1 1048576 P753dBM120dB.bin
./FloatDynamicRange -convert32 25 P753dBM120dB.bin 1 1048576 P753dBM241dB.bin
./FloatDynamicRange -convert32 25 P753dBM241dB.bin 1 1048576 P753dBM361dB.bin
./FloatDynamicRange -convert32 25 P753dBM361dB.bin 1 1048576 P753dBM482dB.bin
./FloatDynamicRange -convert32 25 P753dBM482dB.bin 1 1048576 P753dBM602dB.bin
./FloatDynamicRange -convert32 25 P753dBM602dB.bin 1 1048576 P753dBM722dB.bin
./FloatDynamicRange -convert32 25 P753dBM722dB.bin 1 32      P753dBM753dB.bin

diff -s original.bin P753dBM753dB.bin

# -753dBして+753dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 125 * -6.0205999 == 753dB
# 130 * -6.0205999 == 783dB
# 127 * -6.0205999 == 765dB

./FloatDynamicRange -convert32 25 P722dB.bin  128 1 P765dB.bin

./FloatDynamicRange -convert32 25 P765dB.bin       1 1048576 P765dBM120dB.bin
./FloatDynamicRange -convert32 25 P765dBM120dB.bin 1 1048576 P765dBM241dB.bin
./FloatDynamicRange -convert32 25 P765dBM241dB.bin 1 1048576 P765dBM361dB.bin
./FloatDynamicRange -convert32 25 P765dBM361dB.bin 1 1048576 P765dBM482dB.bin
./FloatDynamicRange -convert32 25 P765dBM482dB.bin 1 1048576 P765dBM602dB.bin
./FloatDynamicRange -convert32 25 P765dBM602dB.bin 1 1048576 P765dBM722dB.bin
./FloatDynamicRange -convert32 25 P765dBM722dB.bin 1 128     P765dBM765dB.bin

diff -s original.bin P765dBM765dB.bin

# -765dBして+765dBすると元に戻る。
# -783dBして+783dBすると元に戻らない。

# 127 * -6.0205999 == 765dB
# 130 * -6.0205999 == 783dB
# 128 * -6.0205999 == 771dB

./FloatDynamicRange -convert32 25 P722dB.bin  256 1 P771dB.bin

./FloatDynamicRange -convert32 25 P771dB.bin       1 1048576 P771dBM120dB.bin
./FloatDynamicRange -convert32 25 P771dBM120dB.bin 1 1048576 P771dBM241dB.bin
./FloatDynamicRange -convert32 25 P771dBM241dB.bin 1 1048576 P771dBM361dB.bin
./FloatDynamicRange -convert32 25 P771dBM361dB.bin 1 1048576 P771dBM482dB.bin
./FloatDynamicRange -convert32 25 P771dBM482dB.bin 1 1048576 P771dBM602dB.bin
./FloatDynamicRange -convert32 25 P771dBM602dB.bin 1 1048576 P771dBM722dB.bin
./FloatDynamicRange -convert32 25 P771dBM722dB.bin 1 256     P771dBM771dB.bin

diff -s original.bin P771dBM771dB.bin

# -765dBして+765dBすると元に戻る。
# -771dBして+771dBすると元に戻らない。

