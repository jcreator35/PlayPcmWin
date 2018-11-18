// 日本語。

#pragma once

struct WWUsbVendorIdStr {
    int vendorId;
    const wchar_t *vendorStr;
};

extern WWUsbVendorIdStr gUsbVendorIdStr[];
int WWUsbVendorIdNum(void);
