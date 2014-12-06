using Wasapi;
using PcmDataLib;

namespace WasapiPcmUtil {
    public enum RenderThreadTaskType {
        None,
        Audio,
        ProAudio,
        Playback
    };

    public enum BitsPerSampleFixType {
        Variable, ///< deprecated
        Sint16,
        Sint32,
        Sfloat32,
        Sint24,

        Sint32V24,
        VariableSint16Sint24,    ///< deprecated
        VariableSint16Sint32V24, ///< deprecated
        AutoSelect
    }

    public enum WasapiSharedOrExclusiveType {
        Shared,
        Exclusive
    };

    public enum WasapiDataFeedModeType {
        EventDriven,
        TimerDriven
    };

    public struct SampleFormatInfo {
        public int bitsPerSample;
        public int validBitsPerSample;
        public WasapiCS.BitFormatType bitFormatType;

        private static WasapiCS.SampleFormatType [] mTryFormat16;
        private static WasapiCS.SampleFormatType [] mTryFormat24;
        private static WasapiCS.SampleFormatType [] mTryFormat32;

        static SampleFormatInfo() {
            mTryFormat16 = new WasapiCS.SampleFormatType [] {
                WasapiCS.SampleFormatType.Sint16,
                WasapiCS.SampleFormatType.Sint24,
            };
 
            mTryFormat24 = new WasapiCS.SampleFormatType [] {
                WasapiCS.SampleFormatType.Sint24,
                WasapiCS.SampleFormatType.Sint32V24,
                WasapiCS.SampleFormatType.Sint16,
            };

            mTryFormat32 = new WasapiCS.SampleFormatType [] {
                WasapiCS.SampleFormatType.Sint32,
                WasapiCS.SampleFormatType.Sint24,
                WasapiCS.SampleFormatType.Sint32V24,
                WasapiCS.SampleFormatType.Sint16,
            };
        }

        public static WasapiCS.BitFormatType
        VrtToBft(PcmData.ValueRepresentationType vrt) {
            return (WasapiCS.BitFormatType)vrt;
        }

        public static PcmData.ValueRepresentationType
        BftToVrt(WasapiCS.BitFormatType bft) {
            return (PcmData.ValueRepresentationType)bft;
        }

        public WasapiCS.SampleFormatType GetSampleFormatType() {
            return WasapiCS.BitAndFormatToSampleFormatType(bitsPerSample, validBitsPerSample, bitFormatType);
        }

        /// <summary>
        /// フォーマット設定から、
        /// Setup()に設定されうるビットフォーマットの候補の数を数えて戻す。
        /// </summary>
        /// <returns>Setup()に設定されうるビットフォーマットの候補の数</returns>
        static public int GetSetupSampleFormatCandidateNum(
                    WasapiSharedOrExclusiveType sharedOrExclusive,
                    BitsPerSampleFixType bitsPerSampleFixType,
                    int validBitsPerSample,
                    PcmDataLib.PcmData.ValueRepresentationType vrt) {
            if (bitsPerSampleFixType != BitsPerSampleFixType.AutoSelect ||
                    sharedOrExclusive == WasapiSharedOrExclusiveType.Shared) {
                // 共有モードの場合 1通り
                // 排他モードで自動選択以外の選択肢の場合 1通り
                return 1;
            }

            // 排他モードのAutoSelect
            switch (validBitsPerSample) {
            case 16:
                return mTryFormat16.Length;
            case 24:
            default:
                return mTryFormat24.Length;
            case 32:
                return mTryFormat32.Length;
            }
        }

        /// <summary>
        /// PcmDataの形式と、(共有・排他)、フォーマット固定設定から、
        /// デバイスに設定されるビットフォーマットを取得。
        /// 
        /// これは、内容的にテーブルなので、テーブルにまとめたほうが良い。
        /// </summary>
        /// <returns>デバイスに設定されるビットフォーマット</returns>
        static public SampleFormatInfo CreateSetupSampleFormat(
                WasapiSharedOrExclusiveType sharedOrExclusive,
                BitsPerSampleFixType bitsPerSampleFixType,
                int bitsPerSample,
                int validBitsPerSample,
                PcmDataLib.PcmData.ValueRepresentationType vrt,
                int candidateId) {
            SampleFormatInfo sf = new SampleFormatInfo();

            if (sharedOrExclusive == WasapiSharedOrExclusiveType.Shared) {
                // 共有モード
                sf.bitsPerSample = bitsPerSample;
                sf.validBitsPerSample = validBitsPerSample;
                sf.bitFormatType = SampleFormatInfo.VrtToBft(vrt);
                return sf;
            }

            // 排他モード
            switch (bitsPerSampleFixType) {
            case BitsPerSampleFixType.Sint16:
                sf.bitFormatType = WasapiCS.BitFormatType.SInt;
                sf.bitsPerSample = 16;
                sf.validBitsPerSample = 16;
                break;
            case BitsPerSampleFixType.Sint24:
                sf.bitFormatType = WasapiCS.BitFormatType.SInt;
                sf.bitsPerSample = 24;
                sf.validBitsPerSample = 24;
                break;
            case BitsPerSampleFixType.Sint32:
                sf.bitFormatType = WasapiCS.BitFormatType.SInt;
                sf.bitsPerSample = 32;
                sf.validBitsPerSample = 32;
                break;
            case BitsPerSampleFixType.Sint32V24:
                sf.bitFormatType = WasapiCS.BitFormatType.SInt;
                sf.bitsPerSample = 32;
                sf.validBitsPerSample = 24;
                break;
            case BitsPerSampleFixType.Sfloat32:
                sf.bitFormatType = WasapiCS.BitFormatType.SFloat;
                sf.bitsPerSample = 32;
                sf.validBitsPerSample = 32;
                break;
            case BitsPerSampleFixType.AutoSelect:
                WasapiCS.SampleFormatType sampleFormat = WasapiCS.SampleFormatType.Sint16;
                switch (validBitsPerSample) {
                case 16:
                    sampleFormat = mTryFormat16[candidateId];
                    break;
                case 24:
                default: /* ? */
                    sampleFormat = mTryFormat24[candidateId];
                    break;
                case 32:
                    sampleFormat = mTryFormat32[candidateId];
                    break;
                }

                sf.bitFormatType      = WasapiCS.BitFormatType.SInt;
                sf.bitsPerSample      = WasapiCS.SampleFormatTypeToUseBitsPerSample(sampleFormat);
                sf.validBitsPerSample = WasapiCS.SampleFormatTypeToValidBitsPerSample(sampleFormat);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            return sf;
        }
    };

    public class PcmUtil {
        private PcmFormatConverter mConv;

        private PcmUtil() {
        }

        public PcmUtil(int numChannels) {
            mConv = new PcmFormatConverter(numChannels);
        }

        public bool IsNoiseShapingOrDitherCapable(PcmData pdFrom, WasapiCS.SampleFormatType toFormat) {
            return mConv.IsConversionNoiseshapingOrDitherCapable(
                    WasapiCS.BitAndFormatToSampleFormatType(pdFrom.BitsPerSample, pdFrom.ValidBitsPerSample,
                        SampleFormatInfo.VrtToBft(pdFrom.SampleValueRepresentationType)), toFormat);
        }

        /// <summary>
        /// 量子化ビット数を、もし必要なら変更する。
        /// </summary>
        /// <param name="pd">入力PcmData</param>
        /// <returns>変更後PcmData</returns>
        public PcmData BitsPerSampleConvAsNeeded(PcmData pd, WasapiCS.SampleFormatType fmt, WasapiPcmUtil.PcmFormatConverter.BitsPerSampleConvArgs args) {
            switch (fmt) {
            case WasapiCS.SampleFormatType.Sfloat:
                // System.Console.WriteLine("Converting to Sfloat32bit...");
                pd = mConv.Convert(pd, WasapiCS.SampleFormatType.Sfloat, args);
                break;
            case WasapiCS.SampleFormatType.Sint16:
                // System.Console.WriteLine("Converting to SInt16bit...");
                pd = mConv.Convert(pd, WasapiCS.SampleFormatType.Sint16, args);
                break;
            case WasapiCS.SampleFormatType.Sint24:
                // System.Console.WriteLine("Converting to SInt24...");
                pd = mConv.Convert(pd, WasapiCS.SampleFormatType.Sint24, args);
                break;
            case WasapiCS.SampleFormatType.Sint32V24:
                // System.Console.WriteLine("Converting to SInt32V24...");
                pd = mConv.Convert(pd, WasapiCS.SampleFormatType.Sint32V24, args);
                break;
            case WasapiCS.SampleFormatType.Sint32:
                // System.Console.WriteLine("Converting to SInt32bit...");
                pd = mConv.Convert(pd, WasapiCS.SampleFormatType.Sint32, args);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
            return pd;
        }
    }
}
