using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWAudioFilter {

    /// <summary>
    /// Refer FilterFactory.Create()
    /// </summary>
    public enum FilterType {
        Gain,
        ZohUpsampler,
        LowPassFilter,
        FftUpsampler,
        Mash2,

        NoiseShaping,
        NoiseShaping4th,
        TagEdit,
        Downsampler,
        CicFilter,

        InsertZeroesUpsampler,
        HalfbandFilter,
        Crossfeed,
        JitterAdd,
        GaussianNoise,

        DynamicRangeCompression,
        UnevenBitDac,
        Normalize,
        AddFundamentals,
        LineDrawUpsampler,
        CubicHermiteSplineUpsampler,
        ReduceBitDepth,
        FirstOrderAllPassIIR,
        SecondOrderAllPassIIR,
        WindowedSincUpsampler,
        SubsonicFilter,
        TimeReversal,
        ZohNosdacCompensation
    }

    public struct TagData {
        /// <summary>
        ///  @todo FLACのメタデータを持ってきていてアレだ
        /// </summary>
        public WWFlacRWCS.Metadata Meta { get; set; }
        public byte [] Picture { get;set; }
    };

    public class FilterBase {
        public FilterType FilterType { get; set; }

        private static int msFilterId = 0;

        public int FilterId { get; set; }

        /// <summary>
        /// 必要に応じて派生クラスでoverrideする。
        /// </summary>
        /// <returns>true: すべてのチャンネルの入力PCMが揃うまでFilterDo()を呼び出さない。</returns>
        public virtual bool WaitUntilAllChannelDataAvailable() {
            return false;
        }

        /// <summary>
        /// WaitUntilAllChannelDataAvailable()==trueの時呼び出される。
        /// 全てのチャンネルについてSetChannelPcm()が呼び出されたあとFilterDo()が呼び出される。
        /// </summary>
        public virtual void SetChannelPcm(int ch, double[] inPcm) {
        }

        // 物置
        private double [] mPreviousProcessRemains;
        public double[] GetPreviousProcessRemains() {
            return mPreviousProcessRemains;
        }
        public void SetPreviousProcessRemains(double[] remains) {
            mPreviousProcessRemains = remains;
        }
        public FilterBase(FilterType type) {
            FilterType = type;
            FilterId = msFilterId++;
        }

        public virtual FilterBase CreateCopy() {
            return null;
        }

        public virtual string ToDescriptionText() {
            return "Do nothing.";
        }

        public virtual string ToSaveText() {
            return "";
        }

        /// <summary>
        /// perform setup task, set pcm format and returns output format
        /// </summary>
        /// <param name="inputFormat">input pcm format</param>
        /// <returns>output pcm format</returns>
        public virtual PcmFormat Setup(PcmFormat inputFormat) {
            return new PcmFormat(inputFormat);
        }

        public virtual TagData TagEdit(TagData tagData) {
            return tagData;
        }

        public virtual void FilterStart() {
            mPreviousProcessRemains = null;
        }

        public virtual void FilterEnd() {
            mPreviousProcessRemains = null;
        }

        /// </summary>
        /// <returns>num of samples needed to start next signal processing</returns>
        public virtual long NumOfSamplesNeeded() {
            return 4096;
        }

        public virtual double [] FilterDo(double [] inPcm) {
            long num = NumOfSamplesNeeded();
            if (inPcm.LongLength != num) {
                throw new ArgumentOutOfRangeException("inPcm");
            }

            double [] outPcm = new double[num];
            Array.Copy(inPcm, 0, outPcm, 0, num);
            return outPcm;
        }

        protected static bool IsPowerOfTwo(int length) {
            return (0 < length) && ((length & (length - 1)) == 0);
        }
    }

    public class PcmFormat {
        public int NumChannels { get; set; }
        public int ChannelId { get; set; }
        public int SampleRate { get; set; }
        public long NumSamples { get; set; }

        public PcmFormat(int numChannels, int channelId, int sampleRate, long numSamples) {
            NumChannels = numChannels;
            ChannelId   = channelId;
            SampleRate  = sampleRate;
            NumSamples  = numSamples;
        }
        public PcmFormat(PcmFormat rhs) {
            NumChannels = rhs.NumChannels;
            ChannelId   = rhs.ChannelId;
            SampleRate  = rhs.SampleRate;
            NumSamples  = rhs.NumSamples;
        }
    };
}
