#include <wchar.h>
#include <string.h>
#include <stdio.h>

int main(void)
{
    const wchar_t *testString = L"This is a pen.";
    wchar_t s[256];

    wcscpy_s(s, testString);

    wchar_t *p = wcstok(s, L" ");
    while (p != nullptr) {
        printf("%S\n", p);
        p = wcstok(nullptr, L" ");
    }

    return 0;
}
