using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWDistortionNoise {

    /// <summary>
    /// Refer FilterFactory.Create()
    /// </summary>
    public enum FilterType {
        Gain,
        JitterAdd,
    }

    public class FilterBase {
        public FilterType FilterType { get; set; }

        private static int msFilterId = 0;

        public int FilterId { get; set; }

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
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public long NumSamples { get; set; }

        public PcmFormat(int channel, int sampleRate, long numSamples) {
            Channels = channel;
            SampleRate = sampleRate;
            NumSamples = numSamples;
        }
        public PcmFormat(PcmFormat rhs) {
            Channels = rhs.Channels;
            SampleRate = rhs.SampleRate;
            NumSamples = rhs.NumSamples;
        }
    };
}
