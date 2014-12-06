call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86

echo "updating uid..."

msbuild /t:updateuid PlayPcmWin.csproj

pause