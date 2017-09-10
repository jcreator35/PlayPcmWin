// 日本語

#include <complex>

class WWDftCpu {
public:
    /// <summary>
    /// 1次元DFT。要素数Nとして、N分の1した結果を戻す。
    /// </summary>
    /// <param name="from">入力。</param>
    /// <param name="to">出力DFT結果</param>
    static void Dft1d(const std::complex<double> * from, int count, std::complex<double> * to);

    /// <summary>
    /// 1次元IDFT。要素数で割ったりはしない。Dft1dとペアで使用すると値が元に戻る。
    /// </summary>
    /// <param name="from">入力</param>
    /// <param name="to">出力DFT結果</param>
    static void Idft1d(const std::complex<double> * from, int count, std::complex<double> * to);
};
