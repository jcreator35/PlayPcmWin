#pragma once

class WWSettingsImpl;

class WWSettings {
public:
    WWSettings(void) : mImpl(nullptr) { }
    ~WWSettings(void);

    int Init(const wchar_t *programName);
    void Term(void);

    int GetInt(const wchar_t *key, int defaultValue);
    void SetInt(const wchar_t *key, int v);

private:
    const static int PATH_COUNT = 512;
    wchar_t mFolderPath[PATH_COUNT];
    wchar_t mFilePath[PATH_COUNT];

    WWSettingsImpl * mImpl;
};
