cd bin\x86\Release
copy ..\..\..\LocBaml_x86.exe locbaml.exe
locbaml.exe /parse en-US\PlayPcmWin.resources.dll /out:..\..\..\PlayPcmWinx86.en-US.csv
del locbaml.exe
cd ..\..\..

pause

