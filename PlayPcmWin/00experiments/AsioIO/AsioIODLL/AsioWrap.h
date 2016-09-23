/* ASIO wrapper API
 * DLL functions are imported from AsioCS.cs
 * if you add new function, you must also add in AsioIODLL.def
 */

#ifndef H_AsioWrap
#define H_AsioWrap

extern "C" __declspec(dllexport) void  __stdcall AsioWrap_init(void);
extern "C" __declspec(dllexport) void  __stdcall AsioWrap_term(void);

extern "C" __declspec(dllexport) int   __stdcall AsioWrap_getDriverNum(void);
extern "C" __declspec(dllexport) bool  __stdcall AsioWrap_getDriverName(int n, char *name_return, int size);

extern "C" __declspec(dllexport) bool  __stdcall AsioWrap_loadDriver(int n);
extern "C" __declspec(dllexport) void  __stdcall AsioWrap_unloadDriver(void);

struct ASIOTimeStamp;

extern "C" __declspec(dllexport) double __stdcall AsioTimeStampToDouble(ASIOTimeStamp &a);

struct ASIOSamples;
extern "C" __declspec(dllexport) double __stdcall AsioSamplesToDouble(ASIOSamples &a);

extern "C" __declspec(dllexport) int   __stdcall AsioWrap_setup(int sampleRate);
extern "C" __declspec(dllexport) void  __stdcall AsioWrap_unsetup(void);

extern "C" __declspec(dllexport) int   __stdcall AsioWrap_getInputChannelsNum(void);
extern "C" __declspec(dllexport) int   __stdcall AsioWrap_getOutputChannelsNum(void);

extern "C" __declspec(dllexport) bool  __stdcall AsioWrap_getInputChannelName(int n, char *name_return, int size);
extern "C" __declspec(dllexport) bool  __stdcall AsioWrap_getOutputChannelName(int n, char *name_return, int size);

extern "C" __declspec(dllexport) void  __stdcall AsioWrap_setOutput(int outputChannel, int *data, int samples, bool repeat);
extern "C" __declspec(dllexport) void  __stdcall AsioWrap_setInput(int inputChannel, int samples);

extern "C" __declspec(dllexport) int   __stdcall AsioWrap_start(void);
extern "C" __declspec(dllexport) bool  __stdcall AsioWrap_run(void);
extern "C" __declspec(dllexport) void  __stdcall AsioWrap_stop(void);

extern "C" __declspec(dllexport) void  __stdcall AsioWrap_getRecordedData(int inputChannel, int recordedData_return[], int samples);

extern "C" __declspec(dllexport) int   __stdcall AsioWrap_controlPanel(void);

extern "C" __declspec(dllexport) int   __stdcall AsioWrap_getClockSourceNum(void);
extern "C" __declspec(dllexport) bool  __stdcall AsioWrap_getClockSourceName(int n, char *name_return, int size);
extern "C" __declspec(dllexport) int   __stdcall AsioWrap_setClockSource(int idx);


#endif /* H_AsioWrap */
