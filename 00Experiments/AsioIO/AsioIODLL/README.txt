1. ASIO SDKを以下の場所からダウンロードしてきます。展開するとASIOSDK2ディレクトリが出来ます。
http://www.steinberg.net/en/company/3rd_party_developer.html

2. 以下のリポジトリを、svn チェックアウトします。
https://bitspersampleconv2.googlecode.com/svn/trunk

C:\work\BpsConvWin2\ディレクトリにチェックアウトしたとして話を進めます。

3. チェックアウトしたBpsConvWin2ディレクトリの中に
AsioIOディレクトリとASIOSDK2ディレクトリが並列に並ぶように、
エクスプローラでASIOSDK2ディレクトリをコピーします。すると以下のような感じになります：

C:\work\BpsConvWin2\AsioIO\AsioIO.sln 等がある
C:\work\BpsConvWin2\ASIOSDK2\readme.txt 等がある
C:\work\BpsConvWin2\sqwave2\sqwave2.sln 等がある

4. C:\work\BpsConvWin2\AsioIO\AsioIODLL\copyasiofiles.batを実行します。
以下のファイルがC:\work\BpsConvWin2\AsioIO\AsioIODLL\の中にコピーされます:
ASIOSDK2/common/asio.h
ASIOSDK2/common/asio.cpp
ASIOSDK2/common/asiodrivers.h
ASIOSDK2/common/asiodrivers.cpp
ASIOSDK2/common/asiosys.h
ASIOSDK2/common/iasiodrv.h

ASIOSDK2/host/ginclude.h
ASIOSDK2/pc/asiolist.cpp
ASIOSDK2/pc/asiolist.h


5. サンプルプログラム C:\work\BpsConvWin2\AsioIO\AsioTestCUI\AsioTestCui.slnを開いて
リビルド、実行します。これは、Visual C++ Express editionでできると思います。

AsioIO DLLだけをビルドする場合はC:\work\BpsConvWin2\AsioIO\AsioIODLL\AsioIO.vcprojを開いて、
リビルドします。これは、Visual C++ Express editionでできると思います。

C:\work\BpsConvWin2\sqwave2\sqwave2.slnをVS2010で開いて、リビルドします
これはGUIにC#を使っているので、一度に行うためにはVS2010(有料)が必要です。
DLLをVisual C++ 2010 Expressで作って、Sqwave2をVisual C# 2010 Expressで作れば、タダで作れるかも。



ASIO is a trademark and software of Steinberg Media Technologies GmbH