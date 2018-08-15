#include "WWSettings.h"
#include "SimpleIni/SimpleIni.h"
#include <ShlObj.h>
#include <assert.h>

class WWSettingsImpl {
public:
    CSimpleIni mSI;
};

static const wchar_t * DEFAULT_SECTION = L"Common";

// ������������������������������������������������������������������������������������������������������������������������������

WWSettings::~WWSettings(void)
{
    assert(mImpl == nullptr); //< Term���ĂіY���Ɣ�������B
}

int
WWSettings::Init(const wchar_t *programName)
{
    assert(mImpl == nullptr);

    PWSTR localAppFolder = nullptr;
    assert(programName);

    HRESULT hr = SHGetKnownFolderPath(FOLDERID_LocalAppData,
            0, nullptr, &localAppFolder);
    if (FAILED(hr)) {
        printf("Error: SHGetKnownFolderPath failed %08x\n", hr);
        return hr;
    }

    swprintf_s(mFolderPath, L"%s\\%s", localAppFolder, programName);
    CoTaskMemFree(localAppFolder);
    localAppFolder = nullptr;

    // �f�B���N�g�����B
    hr = CreateDirectory(mFolderPath, nullptr);
    if (hr == ERROR_ALREADY_EXISTS) {
        hr = S_OK;
    }
    if (FAILED(hr)) {
        printf("Error: CreateDirectory failed %08x\n", hr);
        return hr;
    }

    // �t�@�C�����B
    swprintf_s(mFilePath, L"%s\\%s.ini", mFolderPath, programName);

    mImpl = new WWSettingsImpl();
    SI_Error er = mImpl->mSI.LoadFile(mFilePath);
    if (er != SI_OK) {
        printf("LoadFile %08x\n", er);
    }

    return S_OK;
}

void
WWSettings::Term(void)
{
    assert(mImpl);

    SI_Error er = mImpl->mSI.SaveFile(mFilePath);
    if (er != SI_OK) {
        printf("SaveFile %08x\n", er);
    }

    delete mImpl;
    mImpl = nullptr;
}

int
WWSettings::GetInt(const wchar_t *key, int defaultValue)
{
    assert(mImpl);

    int v = mImpl->mSI.GetLongValue(DEFAULT_SECTION, key, defaultValue, false);
    return v;
}

void
WWSettings::SetInt(const wchar_t *key, int v)
{
    assert(mImpl);

    SI_Error er = mImpl->mSI.SetLongValue(DEFAULT_SECTION, key, v, nullptr, false, false);
    if (er != SI_OK) {
        printf("SetLongValue %08x\n", er);
    }
}

