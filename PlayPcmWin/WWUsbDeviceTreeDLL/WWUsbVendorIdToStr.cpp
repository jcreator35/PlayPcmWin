#include "WWUsbVendorIdToStr.h"
#include "WWUsbVendorIdStr.h"

#include <map>

static std::map<int, const wchar_t *> mTable;

void
WWUsbVendorIdToStrInit(void)
{
    mTable.clear();
    for (int i = 0; i < WWUsbVendorIdNum(); ++i) {
        WWUsbVendorIdStr & v = gUsbVendorIdStr[i];
        mTable[v.vendorId] = v.vendorStr;
    }
}

void
WWUsbVendorIdToStrTerm(void)
{
    mTable.clear();
}

const wchar_t *
WWUsbVendorIdToStr(int vendorId)
{
    auto v = mTable.find(vendorId);
    if (v == mTable.end()) {
        return L"";
    }

    return v->second;
}

