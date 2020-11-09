#pragma once

// 日本語 UTF-8

#include "WWAudioFilterFIR.h"

class WWAudioFilterDeEmphasis : public WWAudioFilterFIR {
public:
    WWAudioFilterDeEmphasis(void);

    virtual ~WWAudioFilterDeEmphasis(void) { }
};

