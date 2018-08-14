// 日本語

#pragma once
#include <d3d11.h>


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

    HRESULT CreateDeviceAndSwapChain(void);
    void DestroyDeviceAndSwapChain(void);

    HRESULT CreateRenderTarget(void);
    void DestroyRenderTarget(void);

    HRESULT SetupD3D(void);
    void UnsetupD3D(void);

    void UpdateGUI(void);
};

