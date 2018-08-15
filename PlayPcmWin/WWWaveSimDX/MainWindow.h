// 日本語

#pragma once
#include <d3d11.h>

enum WindowDpi {
    WD_96,  //< 1x
    WD_120, //< 1.25x
    WD_144, //< 1.5x
    WD_168, //< 1.75x
    WD_192, //< 2.0x
    WD_240, //< 2.5x
    WD_NUM
};

class MainWindow {
public:
    MainWindow(void);
    ~MainWindow(void);

    HRESULT Setup(void);
    HRESULT Loop(void);
    void Unsetup(void);

    LRESULT WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

    static MainWindow *Instance(void) { return mInstance; }
private:
    static MainWindow        *mInstance;
    ID3D11Device*            mD3DDevice;
    ID3D11DeviceContext*     mD3DDeviceCtx;
    IDXGISwapChain*          mSwapChain;
    ID3D11RenderTargetView*  mMainRTV;
    HWND                     mHWnd;
    WNDCLASSEX               mWC;
    WindowDpi                mDpi;

    HRESULT CreateDeviceAndSwapChain(void);
    void DestroyDeviceAndSwapChain(void);

    HRESULT CreateRenderTarget(void);
    void DestroyRenderTarget(void);

    HRESULT SetupD3D(void);
    void UnsetupD3D(void);

    void UpdateGUI(void);

    void UpdateDpi(void);
};

