//　日本語

#pragma once

enum WWStimType {
    WWSTIM_Gaussian,
    WWSTIM_Sine,
    WWSTIM_Pulse,
};

struct WWWave1DStim {
    int type; //< WWSTIM_Gaussian or WWSTIM_Sine
    int counter;
    int pos;
    float magnitude;
    float halfPeriod;
    float width;
    float freq;
    float sinePeriod;
};

