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
#include <d3dx11.h>

// We should extern this function to call it for some reason! Please refer imgui_imple_win32.h
extern LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

static const wchar_t *gProgramName = L"WWWaveSimDX";
static const int WINDOW_W = 1280;
static const int WINDOW_H = 1080;
static const int GRID_W = 512;
static const int GRID_H = 512;
static const float SIM_WINDOW_W = 1024.0f;
static const float SIM_WINDOW_H = 1024.0f;
static const int BUTTON_EDGE_PIXEL = 4;

static const float C0     = 1.0f; // 334.0f;             // 334 (m/s)
static const float DeltaT = 1.0f; // 1.0e-5f;            // 1x10^-5 (s)
//private float mΔx = 1.0f; // 334.0f * 1.0e-5f;   // 334 * 10^-5 (m)
static const float Sc     = 1.0f / sqrt(2.0f) / 10; // クーラント数は1.0/sqrt(2)     c0 * Δt / Δx;

static const int DEFAULT_DPI          = 96;
static const int DEFAULT_REPEAT_COUNT = 32;

static const wchar_t *SETTINGS_DPI = L"DPI";
static const wchar_t *SETTINGS_GRID_W = L"GridW";
static const wchar_t *SETTINGS_GRID_H = L"GridH";

//                                 R     G     B     A
static ImVec4 gClearColor = ImVec4(0.2f, 0.2f, 0.2f, 1.00f);

static const int WindowDpiToDpi(WindowDpi a)
{
    assert(0 <= (int)a && (int)a < WD_NUM);

    const int table[] = { 96, 120, 144, 168, 192, 240 };
    return table[(int)a];
}

static WindowDpi DpiToWindowDpi(int iDpi)
{
    switch (iDpi) {
    case 96:
    default:
        return WD_96;
    case 120: return WD_120;
    case 144: return WD_144;
    case 168: return WD_168;
    case 192: return WD_192;
    case 240: return WD_240;
    }
}

MainWindow * MainWindow::mInstance = nullptr;

static LRESULT WINAPI
SWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    return MainWindow::Instance()->WndProc(hWnd, msg, wParam, lParam);
}

MainWindow::MainWindow(void)
    : mD3DDevice(nullptr), mD3DDeviceCtx(nullptr), mSwapChain(nullptr),
      mMainRTV(nullptr), mHWnd(nullptr), mDpi(WD_96), mFrameCount(0), mGridW(GRID_W), mGridH(GRID_H)
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

    // ALT+Enterでフルスクリーンに切り替わる。
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
    if (mSwapChain) {
        mSwapChain->SetFullscreenState(FALSE, nullptr);
    }
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

HRESULT
MainWindow::Setup(void)
{
    assert(mHWnd == nullptr);
    assert(mWC.hInstance == nullptr);

    // 設定を読み出します。
    mSettings.Init(gProgramName);

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

    // Enable Keyboard Controls
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;

    io.MouseDrawCursor = true;

    ImGui_ImplWin32_Init(mHWnd);
    ImGui_ImplDX11_Init(mD3DDevice, mD3DDeviceCtx);
    ImGui::StyleColorsDark();

    {
        int iDpi = mSettings.GetInt(SETTINGS_DPI, DEFAULT_DPI);
        mDpi = DpiToWindowDpi(iDpi);

        mGridW = mSettings.GetInt(SETTINGS_GRID_W, GRID_W);
        mGridH = mSettings.GetInt(SETTINGS_GRID_H, GRID_H);
    }

    hr = mWaveSim2D.Init(mD3DDeviceCtx, mD3DDevice, mGridW, mGridH, C0, DeltaT, Sc);
    if (FAILED(hr)) {
        printf("Error: mWaveSim2D.Init failed!\n");
        return hr;
    }

    return hr;
}

void
MainWindow::Unsetup(void)
{
    mWaveSim2D.Term();

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

    // 設定を保存する。
    mSettings.SetInt(SETTINGS_DPI, WindowDpiToDpi(mDpi));
    mSettings.Term();
}

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

        mWaveSim2D.Update(DEFAULT_REPEAT_COUNT);

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        UpdateGUI();

        ImGui::Render();
        mD3DDeviceCtx->OMSetRenderTargets(1, &mMainRTV, nullptr);
        mD3DDeviceCtx->ClearRenderTargetView(mMainRTV, (float*)&gClearColor);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

        // VSYNCに同期(待機)してフロントバッファーとバックバッファーをスワップする。
        // これによりScreen tearingを防ぐ。
        HRGS(mSwapChain->Present(1, 0));

        ++mFrameCount;
    }

end:
    return hr;
}

void
MainWindow::UpdateGUI(void)
{
    {   // シミュレーション画面。
        ImGui::Begin("Simulation Window");

        auto leftTop = ImGui::GetCursorScreenPos();
        leftTop.x += BUTTON_EDGE_PIXEL;
        leftTop.y += BUTTON_EDGE_PIXEL;
        if (ImGui::ImageButton(mWaveSim2D.GetResultTexSRV(), ImVec2(SIM_WINDOW_W, SIM_WINDOW_H),
                ImVec2(0,0), ImVec2(1,1), BUTTON_EDGE_PIXEL)) {
            // clicked
            auto xyAbs = ImGui::GetMousePos();
            ImVec2 xyRel(xyAbs.x - leftTop.x, xyAbs.y - leftTop.y);

            // 枠の部分の4 pixにも当たるのでマイナス値等範囲外の値が戻ることがある。
            if (xyRel.x < 0) { xyRel.x = 0; }
            if (SIM_WINDOW_W <= xyRel.x) { xyRel.x = SIM_WINDOW_W-1; }
            if (xyRel.y < 0) { xyRel.y = 0; }
            if (SIM_WINDOW_H <= xyRel.y) { xyRel.y = SIM_WINDOW_H-1; }

            printf("pressed (%d %d)\n", (int)xyRel.x, (int)xyRel.y);

            int x = (int)(xyRel.x * mGridW / SIM_WINDOW_W);
            int y = (int)(xyRel.y * mGridH / SIM_WINDOW_H);

            int pos1D = x + y * mGridW;
            WWWave1DStim stim = { 0, 2121, pos1D, 5, 1060, 0.001, 1704 };
            mWaveSim2D.AddStimulus(stim);
        }

        ImGui::End();
    }

    {   // 設定画面。
        ImGui::Begin("Settings");
        ImGui::BulletText("ALT-Enter to switch Fullscreen mode");
        ImGui::Text("%.1f Frames/s", ImGui::GetIO().Framerate);

        UpdateDpi();

        if (ImGui::Button("Update")) {

        }

        ImGui::End();
    }
}

void
MainWindow::UpdateDpi(void)
{
    if (mFrameCount == 0) {
        ChangeDpi();
    }

    if (ImGui::TreeNode("UI Scaling")) {
        // DPIの設定。
        for (int i=0; i<WD_NUM; ++i) {
            bool bDpi = (int)mDpi == i;

            char s[256];
            sprintf_s(s, "%d dpi", WindowDpiToDpi((WindowDpi)i));

            if (ImGui::Checkbox(s, &bDpi) && ((int)mDpi != i)) {
                // DPI変更。
                mDpi = (WindowDpi)i;
                ChangeDpi();
            }
        }

        ImGui::TreePop();
    }
}

void
MainWindow::ChangeDpi(void)
{
    int dpi = WindowDpiToDpi(mDpi);

    float scale = (float)dpi / DEFAULT_DPI;
    ImGui::SetWindowFontScale(scale);
    ImGui::GetStyle().MouseCursorScale = scale;
}

