// 日本語

// original code: ImGui 1.62 example_win32_directx11/main.cpp

#include "MainWindow.h"
#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"
#include <d3d11.h>
#define DIRECTINPUT_VERSION 0x0800
#include <dinput.h>
#include <tchar.h>
#include <assert.h>
#include "WWWinUtil.h"

// We should extern this function to call it for some reason! Please refer imgui_imple_win32.h
extern LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

static const wchar_t *gProgramName = L"DirectXTest";
static const int WINDOW_W = 1280;
static const int WINDOW_H = 1080;

//                                 R     G     B     A
static ImVec4 clear_color = ImVec4(0.2f, 0.2f, 0.2f, 1.00f);

MainWindow * MainWindow::mInstance = nullptr;

MainWindow::MainWindow(void)
    : mD3DDevice(nullptr), mD3DDeviceCtx(nullptr), mSwapChain(nullptr),
      mMainRTV(nullptr), mHWnd(nullptr)
{
    assert(mInstance == nullptr);
    mInstance = this;

    memset(&mWC, 0, sizeof mWC);
}

MainWindow::~MainWindow(void)
{
    assert(mInstance != nullptr);
    mInstance = nullptr;
}


HRESULT
MainWindow::CreateRenderTarget(void)
{
    assert(mMainRTV == nullptr);

    HRESULT hr = S_OK;
    ID3D11Texture2D* pBackBuffer = nullptr;

    HRG(mSwapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (LPVOID*)&pBackBuffer));
    HRG(mD3DDevice->CreateRenderTargetView(pBackBuffer, nullptr, &mMainRTV));
end:
    // 成功しても失敗してもpBackBufferはReleaseする。
    SAFE_RELEASE(pBackBuffer);

    return hr;
}

void
MainWindow::DestroyRenderTarget(void)
{
    SAFE_RELEASE(mMainRTV);
}

HRESULT
MainWindow::CreateDeviceAndSwapChain(void)
{
    HRESULT hr = S_OK;
    D3D_FEATURE_LEVEL featureLevel;
    const D3D_FEATURE_LEVEL fLvCandidates[] = { D3D_FEATURE_LEVEL_11_0 };

    UINT createDeviceFlags = 0;

    // これは、うまくいかない感じがする。
    //createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;

    DXGI_SWAP_CHAIN_DESC sd;
    memset(&sd, 0, sizeof sd);
    sd.BufferCount = 2;
    sd.BufferDesc.Width = 0;
    sd.BufferDesc.Height = 0;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferDesc.RefreshRate.Numerator = 60;
    sd.BufferDesc.RefreshRate.Denominator = 1;
    sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = mHWnd;
    sd.SampleDesc.Count = 1;
    sd.SampleDesc.Quality = 0;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    HRG(D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags,
            fLvCandidates, sizeof fLvCandidates/sizeof fLvCandidates[0], D3D11_SDK_VERSION,
            &sd, &mSwapChain, &mD3DDevice, &featureLevel, &mD3DDeviceCtx));
end:
    return hr;
}

void
MainWindow::DestroyDeviceAndSwapChain(void)
{
    SAFE_RELEASE(mSwapChain);
    SAFE_RELEASE(mD3DDeviceCtx);
    SAFE_RELEASE(mD3DDevice);
}

HRESULT
MainWindow::SetupD3D(void)
{
    HRESULT hr = S_OK;

    HRG(CreateDeviceAndSwapChain());
    HRG(CreateRenderTarget());
end:
    return hr;
}

void
MainWindow::UnsetupD3D(void)
{
    DestroyRenderTarget();
    DestroyDeviceAndSwapChain();
}

static LRESULT WINAPI
SWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    return MainWindow::Instance()->WndProc(hWnd, msg, wParam, lParam);
}

// ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

LRESULT
MainWindow::WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam)) {
        return true;
    }

    switch (msg) {
    case WM_SIZE:
        if (mD3DDevice != nullptr && wParam != SIZE_MINIMIZED) {
            UINT newW = (UINT)LOWORD(lParam);
            UINT newH = (UINT)HIWORD(lParam);

            dprintf("WM_SIZE %u x %u\n", newW, newH);

            ImGui_ImplDX11_InvalidateDeviceObjects();
            DestroyRenderTarget();
            mSwapChain->ResizeBuffers(0, newW, newH, DXGI_FORMAT_UNKNOWN, 0);
            CreateRenderTarget();
            ImGui_ImplDX11_CreateDeviceObjects();
        }
        return 0;
    case WM_SYSCOMMAND:
        // Disable ALT application menu
        if ((wParam & 0xfff0) == SC_KEYMENU) {
            return 0;
        }
        break;
    case WM_KEYDOWN:
        switch (wParam) {
        case VK_ESCAPE:
            dprintf("WM_KEYDOWN ESC\n");
            // 上品に終了する。
            PostMessage(hWnd, WM_CLOSE, 0, 0);
            break;
        default:
            break;
        }
        break;

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProc(hWnd, msg, wParam, lParam);
}

HRESULT
MainWindow::Setup(void)
{
    assert(mHWnd == nullptr);
    assert(mWC.hInstance == nullptr);

    HRESULT hr = S_OK;
    WNDCLASSEX mWC = { sizeof(WNDCLASSEX), CS_CLASSDC, SWndProc, 0L, 0L,
            GetModuleHandle(nullptr), nullptr, nullptr, nullptr, nullptr, gProgramName, nullptr };

    RegisterClassEx(&mWC);

    mHWnd = CreateWindow(gProgramName, gProgramName, WS_OVERLAPPEDWINDOW,
            100, 100, WINDOW_W, WINDOW_H, nullptr, nullptr, mWC.hInstance, nullptr);

    hr = SetupD3D();
    if (FAILED(hr)) {
        UnsetupD3D();
        UnregisterClass(gProgramName, mWC.hInstance);
        mWC.hInstance = nullptr;
        return hr;
    }

    ShowWindow(mHWnd, SW_SHOWDEFAULT);
    UpdateWindow(mHWnd);

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    (void)io;

    // Enable Keyboard Controls
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;

    ImGui_ImplWin32_Init(mHWnd);
    ImGui_ImplDX11_Init(mD3DDevice, mD3DDeviceCtx);
    ImGui::StyleColorsDark();

    // Load Fonts
    // - If no fonts are loaded, dear imgui will use the default font. You can also load multiple fonts and use ImGui::PushFont()/PopFont() to select them. 
    // - AddFontFromFileTTF() will return the ImFont* so you can store it if you need to select the font among multiple. 
    // - If the file cannot be loaded, the function will return nullptr. Please handle those errors in your application (e.g. use an assertion, or display an error and quit).
    // - The fonts will be rasterized at a given size (w/ oversampling) and stored into a texture when calling ImFontAtlas::Build()/GetTexDataAsXXXX(), which ImGui_ImplXXXX_NewFrame below will call.
    // - Read 'misc/fonts/README.txt' for more instructions and details.
    // - Remember that in C/C++ if you want to include a backslash \ in a string literal you need to write a double backslash \\ !
    //io.Fonts->AddFontDefault();
    //io.Fonts->AddFontFromFileTTF("../../misc/fonts/Roboto-Medium.ttf", 16.0f);
    //io.Fonts->AddFontFromFileTTF("../../misc/fonts/Cousine-Regular.ttf", 15.0f);
    //io.Fonts->AddFontFromFileTTF("../../misc/fonts/DroidSans.ttf", 16.0f);
    //io.Fonts->AddFontFromFileTTF("../../misc/fonts/ProggyTiny.ttf", 10.0f);
    //ImFont* font = io.Fonts->AddFontFromFileTTF("c:\\Windows\\Fonts\\ArialUni.ttf", 18.0f, nullptr, io.Fonts->GetGlyphRangesJapanese());
    //IM_ASSERT(font != nullptr);

    //ImFont* font = io.Fonts->AddFontFromFileTTF("../../misc/mplus/mplus-1m-regular.ttf", 16.0f);
    //IM_ASSERT(font != nullptr);

    return hr;
}

void
MainWindow::Unsetup(void)
{
    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();

    UnsetupD3D();
    
    if (mHWnd != nullptr) {
        DestroyWindow(mHWnd);
        mHWnd = nullptr;
    }

    if (mWC.hInstance != nullptr) {
        UnregisterClass(gProgramName, mWC.hInstance);
        mWC.hInstance = nullptr;
    }
}

HRESULT
MainWindow::Loop(void)
{
    HRESULT hr = S_OK;
    MSG msg;
    ZeroMemory(&msg, sizeof(msg));

    while (msg.message != WM_QUIT) {
        // Poll and handle messages (inputs, window resize, etc.)
        // You can read the io.WantCaptureMouse, io.WantCaptureKeyboard flags to tell if dear imgui wants to use your inputs.
        // - When io.WantCaptureMouse is true, do not dispatch mouse input data to your main application.
        // - When io.WantCaptureKeyboard is true, do not dispatch keyboard input data to your main application.
        // Generally you may always pass all inputs to dear imgui, and hide them from your application based on those two flags.
        if (PeekMessage(&msg, nullptr, 0U, 0U, PM_REMOVE)) {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
            continue;
        }

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        UpdateGUI();

        ImGui::Render();
        mD3DDeviceCtx->OMSetRenderTargets(1, &mMainRTV, nullptr);
        mD3DDeviceCtx->ClearRenderTargetView(mMainRTV, (float*)&clear_color);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

        // VSYNCに同期(待機)してフロントバッファーとバックバッファーをスワップする。
        // これによりScreen tearingを防ぐ。
        HRGS(mSwapChain->Present(1, 0));
    }

end:
    return hr;
}

void
MainWindow::UpdateGUI(void)
{
    ImGui::Begin("Settings");
    ImGui::Text("%.1f Frames/s", ImGui::GetIO().Framerate);
    
    if (ImGui::Button("Update")) {

    }

    ImGui::End();
}

