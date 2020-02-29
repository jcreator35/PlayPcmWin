// 日本語。
#include "WWMFVideoReaderCommon.h"
#include <string>

const char *
MFVideoChromaSubsamplingToStr(MFVideoChromaSubsampling t) {
    switch (t) {
    case MFVideoChromaSubsampling_Unknown:
        return "Unknown";
    case MFVideoChromaSubsampling_ProgressiveChroma:
        return "ProgressiveChroma";
    case MFVideoChromaSubsampling_Horizontally_Cosited:
        return "Horizontally_Cosited";
    case MFVideoChromaSubsampling_Vertically_Cosited:
        return "Vertically_Cosited";
    case MFVideoChromaSubsampling_Vertically_AlignedChromaPlanes:
        return "MPEG1 or Vertically_AlignedChromaPlanes";
    case MFVideoChromaSubsampling_MPEG2:
        return "MPEG2";
    case MFVideoChromaSubsampling_DV_PAL:
        return "DV_PAL";
    case MFVideoChromaSubsampling_Cosited:
        return "Cosited";
    default:
        return "Other";
    }
}

const char *
MFVideoInterlaceModeToStr(MFVideoInterlaceMode t) {
    switch (t) {
    case MFVideoInterlace_Unknown:
        return "Unknown";
    case MFVideoInterlace_Progressive:
        return "Progressive";
    case MFVideoInterlace_FieldInterleavedUpperFirst:
        return "FieldInterleavedUpperFirst";
    case MFVideoInterlace_FieldInterleavedLowerFirst:
        return "FieldInterleavedLowerFirst";
    case MFVideoInterlace_FieldSingleUpper:
        return "FieldSingleUpper";
    case MFVideoInterlace_FieldSingleLower:
        return "FieldSingleLower";
    case MFVideoInterlace_MixedInterlaceOrProgressive:
        return "MixedInterlaceOrProgressive";
    default:
        return "Other";
    }
}

const char *
MFVideoTransferFunctionToStr(MFVideoTransferFunction t)
{
    switch (t) {
    case MFVideoTransFunc_Unknown:
        return "Unknown";
    case MFVideoTransFunc_10:
        return "Linear RGB. Gamma=1.0";
    case MFVideoTransFunc_18:
        return "Gamma=1.8";
    case MFVideoTransFunc_20:
        return "Gamma=2.0";
    case MFVideoTransFunc_22:
        return "Gamma=2.2";
    case MFVideoTransFunc_709:
        return "Rec.709";
    case MFVideoTransFunc_240M:
        return "SMPTE 240M";
    case MFVideoTransFunc_sRGB:
        return "sRGB";
    case MFVideoTransFunc_28:
        return "Gamma=2.8 used in ITU-R BT.470-2 SystemB, G(PAL)";
    case MFVideoTransFunc_Log_100:
        return "Logarismic transfer 100:1 used in H.264";
    case MFVideoTransFunc_Log_316:
        return "Log 316.22777:1 used in H.264";
    case MFVideoTransFunc_709_sym:
        return "Rec.709 simmetric";
    case MFVideoTransFunc_2020_const:
        return "Rec.2020 constant luminance";
    case MFVideoTransFunc_2020:
        return "Rec.2020 Non-constant luminance";
    case MFVideoTransFunc_26:
        return "Gamma=2.6";
    case MFVideoTransFunc_2084:
        return "SMPTE ST.2084 also known as PQ.";
    case MFVideoTransFunc_HLG:
        return "Hybrid Log-Gamma, ARIB STD-B67";
    case MFVideoTransFunc_10_rel:
        return "10_rel";
    default:
        return "Other";
    }
}

const char *
MFVideoPrimariesToStr(MFVideoPrimaries t) {
    switch (t) {
    case MFVideoPrimaries_Unknown:
        return "Unknown";
    case MFVideoPrimaries_reserved:
        return "reserved";
    case MFVideoPrimaries_BT709:
        return "Rec.709";
    case MFVideoPrimaries_BT470_2_SysM:
        return "ITU-R BT.470-4 System M (Typically NTSC)";
    case MFVideoPrimaries_BT470_2_SysBG:
        return "ITU-R BT.470-4 System B,G (PAL or SECAM)";
    case MFVideoPrimaries_SMPTE170M:
        return "SMPTE 170M";
    case MFVideoPrimaries_SMPTE240M:
        return "SMPTE 240M";
    case MFVideoPrimaries_EBU3213:
        return "EBU 3213";
    case MFVideoPrimaries_SMPTE_C:
        return "SMPTE C(SMPTE RP 145)";
    case MFVideoPrimaries_BT2020:
        return "Rec.2020";
    case MFVideoPrimaries_XYZ:
        return "CIE 1931 XYZ";
    case  MFVideoPrimaries_DCI_P3:
        return "DCI-P3";
    case  MFVideoPrimaries_ACES:
        return "Academy Color Encoding System";
    default:
        return "Other";
    }
}

const char *
MFVideoTransferMatrixToStr(MFVideoTransferMatrix t)
{
    switch (t) {
    case MFVideoTransferMatrix_Unknown:
        return "Unknown, perhaps Rec.709";
    case MFVideoTransferMatrix_BT709:
        return "Rec.709";
    case MFVideoTransferMatrix_BT601:
        return "Rec.601";
    case MFVideoTransferMatrix_SMPTE240M:
        return "SMPTE 240M";
    case MFVideoTransferMatrix_BT2020_10:
        return "BT2020_10";
    case MFVideoTransferMatrix_BT2020_12:
        return "BT2020_12";
    default:
        return "Other";
    }
}

const char *
MFVideoLightingToStr(MFVideoLighting t)
{
    switch (t) {
    case MFVideoLighting_Unknown:
        return "Unknown";
    case MFVideoLighting_bright:
        return "Bright";
    case MFVideoLighting_office:
        return "Office (Medium brightness)";
    case MFVideoLighting_dim:
        return "Dim";
    case MFVideoLighting_dark:
        return "Dark";
    default:
        return "Other";
    }
}

const char *
MFNominalRangeToStr(MFNominalRange t)
{
    switch (t) {
    case MFNominalRange_Unknown:
        return "Unknown";
    case MFNominalRange_Normal:
        return "0 to 255 for 8bit or 0 to 1023 for 10bit";
    case MFNominalRange_Wide:
        return "16 to 235 for 8bit or 64 to 940 for 10bit";
    case MFNominalRange_48_208:
        return "48 to 208 for 8bit or 64 to 940 for 10bit";
    case MFNominalRange_64_127:
        return "64 to 127 for 8bit or 256 to 508 for 10bit (xRGB)";
    default:
        return "Other";
    }
}

static double
MFOffsetToDouble(const MFOffset &t)
{
    double r = t.value;
    r += t.fract / 65536.0;
    return r;
}

const std::string
MFVideoAreaToStr(const MFVideoArea a)
{
    char s[256];
    s[0] = 0;
    sprintf_s(s, "Offset=(%f,%f),Area=%dx%d",
        MFOffsetToDouble(a.OffsetX),
        MFOffsetToDouble(a.OffsetY),
        a.Area.cx, a.Area.cy);

    return std::string(s);
}

const std::string
MFVideoFlagsToStr(uint64_t t) {
    std::string r = "";

    if (t & MFVideoFlag_PAD_TO_Mask) {
        r += "| PAD_TO_Mask";
    }
    if (t &MFVideoFlag_PAD_TO_None) {
        r += "| PAD_TO_None";
    }
    if (t &MFVideoFlag_PAD_TO_4x3) {
        r += "| PAD_TO_4x3";
    }
    if (t &MFVideoFlag_PAD_TO_16x9) {
        r += "| PAD_TO_16x9";
    }
    if (t &MFVideoFlag_SrcContentHintMask) {
        r += "| SrcContentHintMask";
    }
    if (t &MFVideoFlag_SrcContentHintNone) {
        r += "| SrcContentHintNone";
    }
    if (t &MFVideoFlag_SrcContentHint16x9) {
        r += "| SrcContentHint16x9";
    }
    if (t &MFVideoFlag_SrcContentHint235_1) {
        r += "| SrcContentHint235_1";
    }
    if (t &MFVideoFlag_AnalogProtected) {
        r += "| AnalogProtected";
    }
    if (t &MFVideoFlag_DigitallyProtected) {
        r += "| DigitallyProtected";
    }
    if (t &MFVideoFlag_ProgressiveContent) {
        r += "| ProgressiveCotent";
    }
    if (t &MFVideoFlag_FieldRepeatCountMask) {
        r += "| FieldRepeatCountMask";
    }
    if (t &MFVideoFlag_FieldRepeatCountShift) {
        r += "| FieldRepeatCountShift";
    }
    if (t &MFVideoFlag_ProgressiveSeqReset) {
        r += "| ProgressiveSeqReset";
    }
    if (t &MFVideoFlag_PanScanEnabled) {
        r += "| PanScanEnalbled";
    }
    if (t &MFVideoFlag_LowerFieldFirst) {
        r += "| LowerFieldFirst";
    }
    if (t &MFVideoFlag_BottomUpLinearRep) {
        r += "| BottomUpLinearRep";
    }
    if (t &MFVideoFlags_DXVASurface) {
        r += "| DXVASurface";
    }
    if (t &MFVideoFlags_RenderTargetSurface) {
        r += "| RenderTargetSurface";
    }

    if (r.size() == 0) {
        return std::string("None");
    } else {
        // erase first 2 chars "| " and return.
        return r.erase(0,2);
    }
}

void PrintMFVideoFormat(const MFVIDEOFORMAT *p)
{
    OLECHAR* guidString = nullptr;
    StringFromCLSID(p->guidFormat, &guidString);

    printf("  dwSize = %u\n", p->dwSize);
    printf("  videoInfo\n");
    printf("    dwWidth=%u\n", p->videoInfo.dwWidth);
    printf("    dwHeight=%u\n", p->videoInfo.dwHeight);
    printf("    PixelAspectRatio=%u:%u\n", p->videoInfo.PixelAspectRatio.Numerator, p->videoInfo.PixelAspectRatio.Denominator);
    printf("    SourceChromaSubsampling=%s\n", MFVideoChromaSubsamplingToStr(p->videoInfo.SourceChromaSubsampling));
    printf("    InterlaceMode=%s\n", MFVideoInterlaceModeToStr(p->videoInfo.InterlaceMode));
    printf("    TransferFunction=%s\n",MFVideoTransferFunctionToStr(p->videoInfo.TransferFunction));
    printf("    ColorPrimaries=%s\n", MFVideoPrimariesToStr(p->videoInfo.ColorPrimaries));
    printf("    TransferMatrix=%s\n", MFVideoTransferMatrixToStr(p->videoInfo.TransferMatrix));
    printf("    SourceLighting=%s\n", MFVideoLightingToStr(p->videoInfo.SourceLighting));
    printf("    FramesPerSecond=%u/%u (approx. %.2f fps)\n", p->videoInfo.FramesPerSecond.Numerator, p->videoInfo.FramesPerSecond.Denominator, 
        (double)p->videoInfo.FramesPerSecond.Numerator / p->videoInfo.FramesPerSecond.Denominator);
    printf("    NominalRange=%s\n", MFNominalRangeToStr(p->videoInfo.NominalRange));
    printf("    GeometricAperture=%s\n", MFVideoAreaToStr(p->videoInfo.GeometricAperture).c_str());
    printf("    MinimumDisplayAperture=%s\n", MFVideoAreaToStr(p->videoInfo.MinimumDisplayAperture).c_str());
    printf("    PanScanAperture=%s\n", MFVideoAreaToStr(p->videoInfo.PanScanAperture).c_str());
    printf("    VideoFlags=%s\n", MFVideoFlagsToStr(p->videoInfo.VideoFlags).c_str());
    printf("  guidFormat=%S\n", guidString);
    printf("  compressedInfo\n");
    printf("    AvgBitRate=%.3f Mbps\n", 0.001 * 0.001 * p->compressedInfo.AvgBitrate);
    printf("    AvgBitErrorRate=%lld bps\n", p->compressedInfo.AvgBitErrorRate);
    printf("    MaxKeyFramesSpacing=%u\n", p->compressedInfo.MaxKeyFrameSpacing);
    printf("  surfaceInfo\n");
    printf("    format=%08x (%c%c%c%c)\n",
        p->surfaceInfo.Format,
        p->surfaceInfo.Format & 0xff,
        (p->surfaceInfo.Format >> 8) & 0xff,
        (p->surfaceInfo.Format >> 16) & 0xff,
        (p->surfaceInfo.Format >> 24) & 0xff);
    printf("    PaletteEntries=%u\n", p->surfaceInfo.PaletteEntries);

    CoTaskMemFree(guidString);
}
