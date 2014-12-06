cd bin\x64\Release
copy ..\..\..\locbaml_x64.exe locbaml.exe
locbaml /parse en-US\PlayPcmWin.resources.dll /out:..\..\..\PlayPcmWin.en-US.csv
del locbaml.exe
cd ..\..\..

pause

