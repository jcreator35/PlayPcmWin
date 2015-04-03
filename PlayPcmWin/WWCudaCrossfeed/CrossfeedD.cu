// 日本語
#include "CrossfeedD.h"
#include "Util.h"

#include <assert.h>
#include "WWFlacRW.h"
#include <vector>

#define WW_CROSSOVER_COEFF_LENGTH (49)

// 44.1kHz用 1kHz以下を取り出すLPF。
static double gLpfD[WW_CROSSOVER_COEFF_LENGTH] = {
        0.005228327, 0.003249754, 0.004192373, 0.005265026,
        0.006468574, 0.007797099, 0.009237486, 0.010779043,
        0.012417001, 0.014132141, 0.01589555, 0.017701121,
        0.019508703, 0.021304869, 0.023059883,0.024747905,
        0.02634363, 0.027823228, 0.029158971, 0.030331066,
        0.031319484, 0.032104039, 0.032676435, 0.033022636,
        0.033138738, 0.033022636, 0.032676435, 0.032104039,
        0.031319484, 0.030331066, 0.029158971, 0.027823228,
        0.02634363, 0.024747905, 0.023059883, 0.021304869,
        0.019508703, 0.017701121, 0.01589555, 0.014132141,
        0.012417001, 0.010779043, 0.009237486, 0.007797099,
        0.006468574, 0.005265026, 0.004192373, 0.003249754,
        0.005228327 };

// 44.1kHz用 1kHz以上を取り出すHPF。LPFとコンプリメンタリーになっている。
static double gHpfD[WW_CROSSOVER_COEFF_LENGTH] = {
        -0.005228327,-0.003249754,-0.004192373,-0.005265026,
        -0.006468574,-0.007797099,-0.009237486,-0.010779043,
        -0.012417001,-0.014132141,-0.01589555,-0.017701121,
        -0.019508703,-0.021304869,-0.023059883,-0.024747905,
        -0.02634363,-0.027823228,-0.029158971,-0.030331066,
        -0.031319484,-0.032104039,-0.032676435,-0.033022636,
        0.966861262,-0.033022636,-0.032676435,-0.032104039,
        -0.031319484,-0.030331066,-0.029158971,-0.027823228,
        -0.02634363,-0.024747905,-0.023059883,-0.021304869,
        -0.019508703,-0.017701121,-0.01589555,-0.014132141,
        -0.012417001,-0.010779043,-0.009237486,-0.007797099,
        -0.006468574,-0.005265026,-0.004192373,-0.003249754,
        -0.005228327};

struct CrossfeedParamD {
    int numChannels;
    double *coeffs[CROSSFEED_COEF_NUM];
    cufftDoubleComplex *spectra[CROSSFEED_COEF_NUM];

    int sampleRate;
    int coeffSize;
    int fftSize;

    CrossfeedParamD(void) {
        numChannels = 0;
        sampleRate = 0;
        coeffSize = 0;

        for (int i=0; i<CROSSFEED_COEF_NUM; ++i) {
            coeffs[i]  = NULL;
            spectra[i] = NULL;
        }
    }
    void Term(void) {
        for (int i=0; i<CROSSFEED_COEF_NUM; ++i) {
            delete [] coeffs[i];
            coeffs[i] = NULL;

            CHK_CUDAFREE(spectra[i], fftSize * sizeof(cufftDoubleComplex));
        }
    }
};

struct PcmSamplesPerChannelD {
    size_t totalSamples;
    double *inputPcm;
    double *outputPcm;
    cufftDoubleComplex *spectrum;
    int fftSize;

    void Init(void) {
        inputPcm = NULL;
        outputPcm = NULL;
        spectrum = NULL;
    }

    void Term(void) {
        delete [] inputPcm;
        inputPcm = NULL;

        delete [] outputPcm;
        outputPcm = NULL;

        CHK_CUDAFREE(spectrum, fftSize * sizeof(cufftDoubleComplex));
    }
};

static bool
ReadCrossfeeedParamsFromFileD(const wchar_t *path, CrossfeedParamD *param_return)
{
    assert(param_return);

    char buff[256];
    bool result = false;
    FILE *fp;
    errno_t ercd = _wfopen_s(&fp, path, L"rb");
    if (NULL == fp || 0 != ercd) {
        return false;
    }

    CHECKED(ReadOneLine(fp, buff, sizeof buff));
    CHECKED(0 == strncmp(buff, "CFD2", 4));

    param_return->numChannels = 2;

    CHECKED(ReadOneLine(fp, buff, sizeof buff));
    sscanf(buff, "%d", &param_return->sampleRate);

    CHECKED(ReadOneLine(fp, buff, sizeof buff));
    sscanf(buff, "%d", &param_return->coeffSize);

    CHECKED(0 < param_return->coeffSize);

    // コメント行。スキップする。
    CHECKED(ReadOneLine(fp, buff, sizeof buff));

    for (int ch=0; ch<CROSSFEED_COEF_NUM; ++ch) {
        param_return->coeffs[ch] = new double[param_return->coeffSize];
    }

    for (int i=0; i<param_return->coeffSize; ++i) {
#if CROSSFEED_COEF_NUM != 8
#  error
#endif
        double v[CROSSFEED_COEF_NUM];

        CHECKED(ReadOneLine(fp, buff, sizeof buff));
        CHECKED(8 == sscanf(buff, "%lf, %lf, %lf, %lf, %lf, %lf, %lf, %lf",
                &v[0], &v[1], &v[2], &v[3], &v[4], &v[5], &v[6], &v[7]));

        for (int ch=0; ch<CROSSFEED_COEF_NUM; ++ch) {
            param_return->coeffs[ch][i] = v[ch];
        }
    }

    result = true;

END:
    fclose(fp);
    fp = NULL;
    return result;
}

static void
SetInputPcmSamplesD(uint8_t *buff, int bitsPerSample, PcmSamplesPerChannelD *ppc_return)
{
    assert(ppc_return);

    switch (bitsPerSample) {
    case 16:
        for (size_t samplePos=0; samplePos<ppc_return->totalSamples; ++samplePos) {
            short v = (short)(buff[samplePos*2] + (buff[samplePos*2+1]<<8));
            ppc_return->inputPcm[samplePos] = double(v) * (1.0 / 32768.0);
        }
        break;
    case 24:
        for (size_t samplePos=0; samplePos<ppc_return->totalSamples; ++samplePos) {
            int v = (int)((buff[samplePos*3]<<8) + (buff[samplePos*3+1]<<16) + (buff[samplePos*3+2]<<24));
            ppc_return->inputPcm[samplePos] = double(v) * (1.0 / 2147483648.0);
        }
        break;
    default:
        assert(!"not supported");
        break;
    }
}

__global__ void
ElementWiseMulCudaD(cufftDoubleComplex *C, cufftDoubleComplex *A, cufftDoubleComplex *B)
{
    int offs = threadIdx.x + WW_NUM_THREADS_PER_BLOCK * (blockIdx.x + WW_BLOCK_X * blockIdx.y);
    C[offs].x = A[offs].x * B[offs].x - A[offs].y * B[offs].y;
    C[offs].y = A[offs].x * B[offs].y + A[offs].y * B[offs].x;
}

__global__ void
ElementWiseAddCudaD(cufftDoubleReal *C, cufftDoubleReal *A, cufftDoubleReal *B)
{
    int offs = threadIdx.x + WW_NUM_THREADS_PER_BLOCK * (blockIdx.x + WW_BLOCK_X * blockIdx.y);
    C[offs] = A[offs] + B[offs];
}

static void
CudaElementWiseMulD(int count, cufftDoubleComplex *dest, cufftDoubleComplex *from0, cufftDoubleComplex *from1)
{
    dim3 threads(1);
    dim3 blocks(1);

    GetBestBlockThreadSize(count, threads, blocks);
    cudaDeviceSynchronize();
    ElementWiseMulCudaD<<<blocks, threads>>>(dest, from0, from1);
    cudaDeviceSynchronize();
}

static void
CudaElementWiseAddD(int count, cufftDoubleReal *dest, cufftDoubleReal *from0, cufftDoubleReal *from1)
{
    dim3 threads(1);
    dim3 blocks(1);

    GetBestBlockThreadSize(count, threads, blocks);
    cudaDeviceSynchronize();
    ElementWiseAddCudaD<<<blocks, threads>>>(dest, from0, from1);
    cudaDeviceSynchronize();
}

static cufftDoubleComplex *
CreateSpectrumD(double *timeDomainData, int numSamples, int fftSize)
{
    cufftDoubleReal *cuFromT = NULL;
    cudaError_t ercd;
    cufftResult fftResult;
    cufftDoubleComplex *spectrum;
    cufftHandle plan = 0;

    CHK_CUDAMALLOC((void**)&cuFromT, sizeof(cufftDoubleReal)*fftSize);
    CHK_CUDAERROR(cudaMemset((void*)cuFromT, 0, sizeof(cufftDoubleReal)*fftSize));
    CHK_CUDAERROR(cudaMemcpy(cuFromT, timeDomainData, numSamples * sizeof(cufftDoubleReal), cudaMemcpyHostToDevice));
    CHK_CUDAMALLOC((void**)&spectrum, sizeof(cufftDoubleComplex)*fftSize);

    CHK_CUFFT(cufftPlan1d(&plan, fftSize, CUFFT_D2Z, 1));
    CHK_CUFFT(cufftExecD2Z(plan, cuFromT, spectrum));

    cufftDestroy(plan);
    plan = 0;

    CHK_CUDAFREE(cuFromT, sizeof(cufftDoubleReal)*fftSize);
    return spectrum;
}

static double *
FirFilterD(double *firCoeff, size_t firCoeffNum, PcmSamplesPerChannelD &input, PcmSamplesPerChannelD *pOutput)
{
    size_t fftSize = (firCoeffNum < input.totalSamples) ? input.totalSamples: firCoeffNum;
    fftSize = NextPowerOf2(fftSize);
    if (fftSize == 0) {
        return NULL;
    }

    cudaError_t ercd;
    cufftResult fftResult;
    cufftDoubleReal *coefTime = NULL;
    cufftDoubleReal *pcmTime = NULL;
    cufftDoubleReal *resultTime = NULL;
    cufftDoubleComplex *coefFreq = NULL;
    cufftDoubleComplex *pcmFreq = NULL;
    cufftDoubleComplex *resultFreq = NULL;
    cufftHandle plan = 0;

    CHK_CUDAMALLOC((void**)&coefTime, sizeof(cufftDoubleReal)*fftSize);
    CHK_CUDAERROR(cudaMemset((void*)coefTime, 0, sizeof(cufftDoubleReal)*fftSize));
    CHK_CUDAERROR(cudaMemcpy(coefTime, firCoeff, firCoeffNum * sizeof(cufftDoubleReal), cudaMemcpyHostToDevice));
    CHK_CUDAMALLOC((void**)&coefFreq, sizeof(cufftDoubleComplex)*fftSize);

    CHK_CUFFT(cufftPlan1d(&plan, fftSize, CUFFT_D2Z, 1));
    CHK_CUFFT(cufftExecD2Z(plan, coefTime, coefFreq));

    CHK_CUDAFREE(coefTime, sizeof(cufftDoubleReal)*fftSize);

    CHK_CUDAMALLOC((void**)&pcmTime, sizeof(cufftDoubleReal)*fftSize);
    CHK_CUDAERROR(cudaMemset((void*)pcmTime, 0, sizeof(cufftDoubleReal)*fftSize));
    CHK_CUDAERROR(cudaMemcpy(pcmTime, input.inputPcm, input.totalSamples * sizeof(cufftDoubleReal), cudaMemcpyHostToDevice));
    CHK_CUDAMALLOC((void**)&pcmFreq, sizeof(cufftDoubleComplex)*fftSize);

    CHK_CUFFT(cufftExecD2Z(plan, pcmTime, pcmFreq));

    cufftDestroy(plan);
    plan = 0;

    CHK_CUDAFREE(pcmTime, sizeof(cufftDoubleReal)*fftSize);

    CHK_CUDAMALLOC((void**)&resultFreq, sizeof(cufftDoubleComplex)*fftSize);
    CudaElementWiseMulD(fftSize, resultFreq, coefFreq, pcmFreq);

    CHK_CUDAFREE(coefFreq, sizeof(cufftDoubleComplex)*fftSize);
    CHK_CUDAFREE(pcmFreq, sizeof(cufftDoubleComplex)*fftSize);

    CHK_CUDAMALLOC((void**)&resultTime, sizeof(cufftDoubleReal)*fftSize);

    CHK_CUFFT(cufftPlan1d(&plan, fftSize, CUFFT_Z2D, 1));
    CHK_CUFFT(cufftExecZ2D(plan, resultFreq, resultTime));

    cufftDestroy(plan);
    plan = 0;

    CHK_CUDAFREE(resultFreq, sizeof(cufftDoubleComplex)*fftSize);

    CHK_CUDAERROR(cudaMemcpy(pOutput->inputPcm, resultTime, input.totalSamples * sizeof(cufftDoubleReal), cudaMemcpyDeviceToHost));
    CHK_CUDAFREE(resultTime, sizeof(cufftDoubleReal)*fftSize);

    return pOutput->inputPcm;
}

static double *
CrossfeedMixD(cufftDoubleComplex *inPcmSpectra[PCT_NUM], cufftDoubleComplex *coeffLo[2],
        cufftDoubleComplex *coeffHi[2], int nFFT, int pcmSamples)
{
    cudaError_t ercd;
    cufftResult fftResult;
    cufftHandle plan = 0;
    cufftDoubleComplex *cuFreq = NULL;
    cufftDoubleReal *cuTime[PCT_NUM] = {NULL, NULL, NULL, NULL};
    cufftDoubleReal *cuTimeMixedLo = NULL;
    cufftDoubleReal *cuTimeMixedHi = NULL;
    cufftDoubleReal *cuTimeMixed = NULL;

    CHK_CUDAMALLOC((void**)&cuFreq, sizeof(cufftDoubleComplex)*nFFT);
    CHK_CUFFT(cufftPlan1d(&plan, nFFT, CUFFT_Z2D, 1));

    for (int ch=0; ch<2; ++ch) {
        CudaElementWiseMulD(nFFT, cuFreq, inPcmSpectra[ch*2], coeffLo[ch]);

        CHK_CUDAMALLOC((void**)&cuTime[ch*2], sizeof(cufftDoubleReal)*nFFT);
        CHK_CUFFT(cufftExecZ2D(plan, cuFreq, cuTime[ch*2]));

        CudaElementWiseMulD(nFFT, cuFreq, inPcmSpectra[ch*2+1], coeffHi[ch]);

        CHK_CUDAMALLOC((void**)&cuTime[ch*2+1], sizeof(cufftDoubleReal)*nFFT);
        CHK_CUFFT(cufftExecZ2D(plan, cuFreq, cuTime[ch*2+1]));
    }

    cufftDestroy(plan);
    plan = 0;

    CHK_CUDAFREE(cuFreq, sizeof(cufftDoubleComplex)*nFFT);

    CHK_CUDAMALLOC((void**)&cuTimeMixedLo, sizeof(cufftDoubleReal)*nFFT);
    CHK_CUDAMALLOC((void**)&cuTimeMixedHi, sizeof(cufftDoubleReal)*nFFT);
    CHK_CUDAMALLOC((void**)&cuTimeMixed, sizeof(cufftDoubleReal)*nFFT);

    CudaElementWiseAddD(nFFT, cuTimeMixedLo, cuTime[0], cuTime[2]);
    CudaElementWiseAddD(nFFT, cuTimeMixedHi, cuTime[1], cuTime[3]);
    CudaElementWiseAddD(nFFT, cuTimeMixed, cuTimeMixedLo, cuTimeMixedHi);

    for (int i=0; i<PCT_NUM; ++i) {
        CHK_CUDAFREE(cuTime[i], sizeof(cufftDoubleReal)*nFFT);
    }
    CHK_CUDAFREE(cuTimeMixedLo, sizeof(cufftDoubleReal)*nFFT);
    CHK_CUDAFREE(cuTimeMixedHi, sizeof(cufftDoubleReal)*nFFT);

    double *result = new double[pcmSamples];
    CHK_CUDAERROR(cudaMemcpy(result, cuTimeMixed, pcmSamples * sizeof(cufftDoubleReal), cudaMemcpyDeviceToHost));

    CHK_CUDAFREE(cuTimeMixed, sizeof(cufftDoubleReal)*nFFT);

    return result;
}

static void
NormalizeOutputPcmD(std::vector<PcmSamplesPerChannelD> & pcmSamples)
{
    double minV = DBL_MAX;
    double maxV = DBL_MIN;

    for (size_t ch=0; ch<pcmSamples.size(); ++ch) {
        if (pcmSamples[ch].outputPcm == NULL) {
            continue;
        }

        for (size_t i=0; i<pcmSamples[ch].totalSamples; ++i) {
            if (maxV < pcmSamples[ch].outputPcm[i]) {
                maxV = pcmSamples[ch].outputPcm[i];
            }
            if (pcmSamples[ch].outputPcm[i] < minV) {
                minV = pcmSamples[ch].outputPcm[i];
            }
        }
    }

    double absMax = (fabs(minV) < fabs(maxV)) ? fabs(maxV) : fabs(minV);
    double scale = 1.0;
    if ((8388607.0 / 8388608.0) < absMax) {
        scale = (8388607.0 / 8388608.0) / absMax;
    }

    for (size_t ch=0; ch<pcmSamples.size(); ++ch) {
        if (pcmSamples[ch].outputPcm == NULL) {
            continue;
        }
        for (size_t i=0; i<pcmSamples[ch].totalSamples; ++i) {
            pcmSamples[ch].outputPcm[i] *= scale;
        }
    }
}

static bool
WriteFlacFileD(const WWFlacMetadata &meta, const uint8_t *picture,
        std::vector<PcmSamplesPerChannelD> &pcmSamples, const wchar_t *path)
{
    bool result = false;
    int rv;
    int pictureBytes = meta.pictureBytes;

    int id = WWFlacRW_EncodeInit(meta);
    if (id < 0) {
        return false;
    }

    if (0 < pictureBytes) {
        rv = WWFlacRW_EncodeSetPicture(id, picture, pictureBytes);
        if (rv < 0) {
            goto END;
        }
    }

    for (int ch=0; ch<meta.channels; ++ch) {
        uint8_t *pcmDataUint8 = new uint8_t[(size_t)(meta.totalSamples * 3)];
        for (int i=0; i<meta.totalSamples; ++i) {
            int v = (int)(8388608.0 * pcmSamples[ch].outputPcm[i]);
            pcmDataUint8[i*3+0] = v&0xff;
            pcmDataUint8[i*3+1] = (v>>8)&0xff;
            pcmDataUint8[i*3+2] = (v>>16)&0xff;
        }

        rv = WWFlacRW_EncodeAddPcm(id, ch, pcmDataUint8, meta.totalSamples*3);
        if (rv < 0) {
            goto END;
        }
        delete [] pcmDataUint8;
        pcmDataUint8 = NULL;
    }

    rv = WWFlacRW_EncodeRun(id, path);
    if (rv < 0) {
        goto END;
    }

    result = true;
END:

    WWFlacRW_EncodeEnd(id);
    return result;
}

int
WWRunCrossfeedD(const wchar_t *coeffPath, const wchar_t *fromPath, const wchar_t *toPath)
{
    int result = 1;
    int ercd;
    int id = -1;
    size_t nFFT;
    CrossfeedParamD crossfeedParam;
    WWFlacMetadata meta;
    uint8_t * picture = NULL;
    cufftDoubleComplex * inPcmSpectra[PCT_NUM];

    std::vector<PcmSamplesPerChannelD> pcmSamples;

    if (!ReadCrossfeeedParamsFromFileD(coeffPath, &crossfeedParam)) {
        printf("Error: could not read crossfeed param file %S\n", coeffPath);
        goto END;
    }

    id = WWFlacRW_DecodeAll(fromPath);
    if (id < 0) {
        printf("Error: Read failed %S\n", fromPath);
        goto END;
    }

    ercd = WWFlacRW_GetDecodedMetadata(id, meta);
    if (ercd < 0) {
        printf("Error: Read meta failed %S\n", fromPath);
        goto END;
    }

    if (0 < meta.pictureBytes) {
        picture = new uint8_t[meta.pictureBytes];
        ercd = WWFlacRW_GetDecodedPicture(id, picture, meta.pictureBytes);
        if (ercd < 0) {
            printf("Error: Read meta failed %S\n", fromPath);
            goto END;
        }
    }

    if (meta.channels != crossfeedParam.numChannels) {
        printf("Error: channel count mismatch. FLAC ch=%d, crossfeed ch=%d\n", meta.channels, crossfeedParam.numChannels);
        goto END;
    }

    if (meta.channels != crossfeedParam.numChannels) {
        printf("Error: samplerate mismatch. FLAC=%d, crossfeed=%d\n", meta.sampleRate, crossfeedParam.sampleRate);
        goto END;
    }

    for (int ch=0; ch<meta.channels; ++ch) {
        size_t bytes = (size_t)(meta.totalSamples * (meta.bitsPerSample/8));
        uint8_t *buff = new uint8_t[bytes];
        WWFlacRW_GetDecodedPcmBytes(id, ch, 0, buff, bytes);

        PcmSamplesPerChannelD ppc;
        ppc.Init();
        ppc.totalSamples = (size_t)meta.totalSamples;
        ppc.inputPcm = new double[(size_t)(meta.totalSamples * sizeof(double))];
        SetInputPcmSamplesD(buff, meta.bitsPerSample, &ppc);

        delete [] buff;
        buff = NULL;

        {
            // 低音域
            PcmSamplesPerChannelD lowFreq;
            lowFreq.Init();
            lowFreq.totalSamples = ppc.totalSamples;
            lowFreq.inputPcm = new double[ppc.totalSamples];
            if (NULL == FirFilterD(gLpfD, sizeof gLpfD/sizeof gLpfD[0], ppc, &lowFreq)) {
                goto END;
            }
            pcmSamples.push_back(lowFreq);
        }

        {
            // 高音域
            PcmSamplesPerChannelD highFreq;
            highFreq.Init();
            highFreq.totalSamples = ppc.totalSamples;
            highFreq.inputPcm = new double[ppc.totalSamples];
            if (NULL == FirFilterD(gHpfD, sizeof gHpfD/sizeof gHpfD[0], ppc, &highFreq)) {
                goto END;
            }
            pcmSamples.push_back(highFreq);
        }
        ppc.Term();
    }

    WWFlacRW_DecodeEnd(id);
    id = -1;

    nFFT = (size_t)((crossfeedParam.coeffSize < meta.totalSamples) ? meta.totalSamples : crossfeedParam.coeffSize);
    nFFT = NextPowerOf2(nFFT);

    for (int i=0; i<CROSSFEED_COEF_NUM; ++i) {
        crossfeedParam.spectra[i] = CreateSpectrumD(crossfeedParam.coeffs[i], crossfeedParam.coeffSize, nFFT);
        if (crossfeedParam.spectra[i] == NULL) {
            goto END;
        }
        crossfeedParam.fftSize = nFFT;
    }
    for (int i=0; i<pcmSamples.size(); ++i) {
        pcmSamples[i].spectrum = CreateSpectrumD(pcmSamples[i].inputPcm, pcmSamples[i].totalSamples, nFFT);
        if (pcmSamples[i].spectrum == NULL) {
            goto END;
        }
        pcmSamples[i].fftSize = nFFT;
        inPcmSpectra[i] = pcmSamples[i].spectrum;
    }

    pcmSamples[0].outputPcm = CrossfeedMixD(inPcmSpectra,
            &crossfeedParam.spectra[0], &crossfeedParam.spectra[4], nFFT, pcmSamples[0].totalSamples);
    if (pcmSamples[0].outputPcm == NULL) {
        goto END;
    }
    pcmSamples[1].outputPcm = CrossfeedMixD(inPcmSpectra,
            &crossfeedParam.spectra[2], &crossfeedParam.spectra[6], nFFT, pcmSamples[0].totalSamples);
    if (pcmSamples[1].outputPcm == NULL) {
        goto END;
    }

    NormalizeOutputPcmD(pcmSamples);

    // 出力bit depth == 24bit
    meta.bitsPerSample = 24;
    if (!WriteFlacFileD(meta, picture, pcmSamples, toPath)) {
        printf("Error: WriteFlac(%S) failed\n", toPath);
        goto END;
    }

    result = 0;

END:
    delete [] picture;
    picture = NULL;

    for (size_t i=0; i<pcmSamples.size(); ++i) {
        pcmSamples[i].Term();
    }
    pcmSamples.clear();

    crossfeedParam.Term();

    printf("    maximum allocated CUDA memory: %lld Mbytes\n", gCudaMaxBytes / 1024/ 1024);
    if (result != 0) {
        printf("Failed!\n");
    } else {
        printf("Succeeded to write %S.\n", toPath);
        assert(gCudaAllocatedBytes == 0);
    }

    return result;
}

