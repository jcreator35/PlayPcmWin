Open Visual Studio x64 Win64 command prompt and enter following commands to create snk files.

> cd \work\BpsConvWin2
 sn -k FlacIntegrityCheck\FlacIntegrityCheck.snk
 sn -k PcmData\PcmDataLib.snk
 sn -k PlayPcmWin\PlayPcmWin.snk
 sn -k PlayPcmWinAlbum\PlayPcmWinAlbum.snk
 sn -k RecPcmWin\RecPcmWin.snk
 sn -k TimerResolutionMonitor\TimerResolutionMonitor.snk
 sn -k WasapiBitmatchChecker\WasapiBitmatchChecker.snk
 sn -k WasapiCS\WasapiCS.snk
 sn -k WasapiPcmUtil\WasapiPcmUtil.snk
 sn -k WavRWLib2\WavRWLib2.snk
 sn -k WWAudioFilter\WWAudioFilter.snk
 sn -k WWFlacRWCS\WWFlacRWCS.snk
 sn -k WWUtil\WWUtil.snk
 sn -k WWXmlRW\WWXmlRW.snk

 sn -k WWAnalogFilterDesign\WWAnalogFilterDesign.snk
 sn -k WWFilterDesign\WWFilterDesign.snk
 sn -k WWIIRFilterDesign\WWIIRFilterDesign.snk
 sn -k WWMath\WWMath.snk
 sn -k WWOfflineResampler\WWOfflineResampler.snk
 sn -k WWUserControls\WWUserControls.snk

Open PlayPcmWin\PlayPcmWin.sln to build/run PlayPcmWin

Open PlayPcmWinAlbum\PPWA.sln to build/run PlayPcmWinAlbum

Open RecPcmWin\RecPcmWin.sln to build/run RecPcmWin

Open WWAudioFilter\WWAudioFiltervs2010.sln to build/run WWAudioFilter (choose x64 build target)

Open WWOfflineResampler\WWOfflineResampler.sln to build/run WWOfflineResampler (choose x64 build target)

