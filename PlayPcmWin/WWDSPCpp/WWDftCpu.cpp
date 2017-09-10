// 日本語

#include "WWDftCpu.h"

#define _USE_MATH_DEFINES
#include <math.h>

void
WWDftCpu::Dft1d(const std::complex<double> * from, int count, std::complex<double> * to)
{
    /*
        * W=e^(j*2pi/N)
        * Gp = (1/N) * seriesSum(Sk * W^(k*p), k, 0, N-1) (p=0,1,2,…,(N-1))
        * 
        * from == Sk
        * to   == Gp
        */

    // 要素数N
    const int n = count;
    const double recipN = 1.0 / n;

    // Wのテーブル
    auto *w = new std::complex<double>[n];
    for (int i=0; i < n; ++i) {
        double re = cos(-i * 2.0 * M_PI / n);
        double im = sin(-i * 2.0 * M_PI / n);
        w[i] = std::complex<double>(re,im);
    }

    for (int p=0; p < n; ++p) {
        double gr = 0.0;
        double gi = 0.0;
        for (int k=0; k < n; ++k) {
            int posSr = k;
            int posWr = ((p * k) % n);
            double sR = from[posSr].real();
            double sI = from[posSr].imag();
            double wR = w[posWr].real();
            double wI = w[posWr].imag();
            gr += sR * wR - sI * wI;
            gi += sR * wI + sI * wR;
        }
        double re = gr * recipN;
        double im = gi * recipN;
        to[p] = std::complex<double>(re, im);
    }

    delete [] w;
    w = nullptr;
}

void
WWDftCpu::Idft1d(const std::complex<double> * from, int count, std::complex<double> * to)
{
    /*
        * W=e^(-j*2pi/N)
        * Sk= seriesSum([ Gp * W^(-k*p) ], p, 0, N-1) (k=0,1,2,…,(N-1))
        * 
        * from == Gp
        * to   == Sk
        */

    // 要素数N
    int n = count;


    // Wのテーブル
    auto *w = new std::complex<double>[n];
    for (int i=0; i < n; ++i) {
        double re = cos(i * 2.0 * M_PI / n);
        double im = sin(i * 2.0 * M_PI / n);
        w[i] = std::complex<double>(re, im);
    }

    // IDFT実行。
    for (int k=0; k < n; ++k) {
        double sr = 0.0;
        double si = 0.0;
        for (int p=0; p < n; ++p) {
            int posGr = p;
            int posWr = (p * k) % n;
            double gR = from[posGr].real();
            double gI = from[posGr].imag();
            double wR = w[posWr].real();
            double wI = w[posWr].imag();
            sr += gR * wR - gI * wI;
            si += gR * wI + gI * wR;
        }
        to[k] = std::complex<double>(sr, si);
    }

    delete [] w;
    w = nullptr;
}
