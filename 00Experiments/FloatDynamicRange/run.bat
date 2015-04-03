x64\Release\FloatDynamicRange -generate32 24 original.bin
x64\Release\FloatDynamicRange -convert32 24 original.bin 1 1048576 M120dB.bin
x64\Release\FloatDynamicRange -convert32 24 M120dB.bin   1 1048576 M241dB.bin
x64\Release\FloatDynamicRange -convert32 24 M241dB.bin   1 1048576 M361dB.bin
x64\Release\FloatDynamicRange -convert32 24 M361dB.bin   1 1048576 M482dB.bin
x64\Release\FloatDynamicRange -convert32 24 M482dB.bin   1 1048576 M602dB.bin
x64\Release\FloatDynamicRange -convert32 24 M602dB.bin   1 1048576 M722dB.bin
x64\Release\FloatDynamicRange -convert32 24 M722dB.bin   1 1048576 M843dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dB.bin   1 1048576 M963dB.bin
x64\Release\FloatDynamicRange -convert32 24 M963dB.bin   1 1048576 M1084dB.bin

x64\Release\FloatDynamicRange -convert32 24 M722dB.bin        1048576 1 M722dBP120dB.bin
x64\Release\FloatDynamicRange -convert32 24 M722dBP120dB.bin  1048576 1 M722dBP241dB.bin
x64\Release\FloatDynamicRange -convert32 24 M722dBP241dB.bin  1048576 1 M722dBP361dB.bin
x64\Release\FloatDynamicRange -convert32 24 M722dBP361dB.bin  1048576 1 M722dBP482dB.bin
x64\Release\FloatDynamicRange -convert32 24 M722dBP482dB.bin  1048576 1 M722dBP602dB.bin
x64\Release\FloatDynamicRange -convert32 24 M722dBP602dB.bin  1048576 1 M722dBP722dB.bin

x64\Release\FloatDynamicRange -convert32 24 M843dB.bin        1048576 1 M843dBP120dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dBP120dB.bin  1048576 1 M843dBP241dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dBP241dB.bin  1048576 1 M843dBP361dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dBP361dB.bin  1048576 1 M843dBP482dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dBP482dB.bin  1048576 1 M843dBP602dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dBP602dB.bin  1048576 1 M843dBP722dB.bin
x64\Release\FloatDynamicRange -convert32 24 M843dBP722dB.bin  1048576 1 M843dBP843dB.bin

pause