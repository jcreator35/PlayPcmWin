#pragma once

// 日本語 UTF-8

#include "WWAudioFilterFIR.h"

class WWAudioFilterZohNosdacCompensation : public WWAudioFilterFIR {
public:
    WWAudioFilterZohNosdacCompensation(void);

    virtual ~WWAudioFilterZohNosdacCompensation(void) { }
};

