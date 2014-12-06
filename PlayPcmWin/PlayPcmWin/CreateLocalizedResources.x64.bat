call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86

echo on

rem en-US resources ############################################
resgen .\Properties\Resources.resx
copy .\Properties\Resources.resources bin\x64\Release\en-US

rem ja-JP resources ############################################
resgen .\Properties\Resources.ja-JP.resx
copy .\Properties\Resources.ja-JP.resources bin\x64\Release\ja-JP
cd .\bin\x64\Release\ja-JP
copy ..\PlayPcmWin.exe .
copy ..\..\..\..\LocBaml.exe .
locbaml /generate ..\..\..\..\obj\x64\Release\PlayPcmWin.g.en-US.resources /tran:..\..\..\..\PlayPcmWin.ja_JP.csv /cul:ja-JP /out:.
ren Resources.ja-JP.resources PlayPcmWin.Properties.Resources.ja-JP.resources
al /template:"..\PlayPcmWin.exe" /embed:PlayPcmWin.g.ja-JP.resources /embed:PlayPcmWin.Properties.Resources.ja-JP.resources /culture:ja-JP /out:PlayPcmWin.resources.dll
cd ..\..\..\..

pause

