using WWDigitalFilter;
using WWUtil;

namespace WWOfflineResampler {
    class DsfWrite {
        private const int FILTER_ORDER = 5;

        private WWDsfRW.WWDsfWriter mDsfW;

        private LoopFilter mLoopFilter = new LoopFilter();

        class SampleData1ch {
            public LargeArray<byte> sdmData;
            public long pos;
            public SampleData1ch(long size) {
                sdmData = new LargeArray<byte>(size);
                pos = 0;

                // 念のため無音をセットする。
                for (long i = 0; i < size; ++i) {
                    sdmData.Set(i, 0x69);
                }
            }
        };

        private SampleData1ch[] mSampleData;

        public void Setup(WWFlacRWCS.Metadata metaW, byte[] picture) {
            mDsfW = new WWDsfRW.WWDsfWriter();
            mDsfW.EncodeInit(metaW);
            if (picture != null) {
                mDsfW.EncodeSetPicture(picture);
            }

            // サンプルデータ置き場。
            mSampleData = new SampleData1ch[metaW.channels];
            for (int ch = 0; ch < metaW.channels; ++ch) {
                mSampleData[ch] = new SampleData1ch((metaW.totalSamples + 7) / 8);
            }

            // ノイズシェイピングフィルターmLoopFiltersを作る。
            mLoopFilter.Design(FILTER_ORDER, metaW.channels);
        }

        public int AddSampleArray(int ch, double [] sampleArray) {
            int rv = 0;

            // 8で割り切れる。
            System.Diagnostics.Debug.Assert((sampleArray.Length & 7) == 0);

            var buffOut = mLoopFilter.Filter(ch, sampleArray);
            mSampleData[ch].sdmData.CopyFrom(buffOut, 0, mSampleData[ch].pos, buffOut.Length);
            mSampleData[ch].pos += sampleArray.Length / 8;

            return rv;
        }

        public int OutputFile(string path) {
            int rv;

            for (int ch=0; ch<mDsfW.NumChannels; ++ch) {
                mDsfW.EncodeAddPcm(ch, mSampleData[ch].sdmData);
            }

            rv = mDsfW.EncodeRun(path);

            // 修了処理。

            mDsfW.EncodeEnd();

            mLoopFilter.Dispose();

            return rv;
        }
    }
}
