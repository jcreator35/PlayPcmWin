using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wasapi;

namespace PlayPcmWinAlbum {
    class PlaybackController {
        WasapiCS mWasapi = null;
        private int mBufferSizeMillisec = 170;
        private int mZeroFlushMillisec = 1000;
        private WasapiCS.DataFeedMode mDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
        public delegate void StateChangedCallback(State newState);
        private StateChangedCallback mStateChangedCallback = null;
        private DeviceFormat mFromFormat;
        private long mDecodedPcmOffs = 0;
        private int mLoadedGroupId = -1;
        private WWFlacRWCS.FlacRW mFlac;

        public class DeviceFormat {
            public int NumChannels { get; set; }
            public int SampleRate { get; set; }
            public WasapiCS.SampleFormatType SampleFormat { get; set; }
            public void Set(int numChannels, int sampleRate, WasapiCS.SampleFormatType sampleFormat) {
                NumChannels = numChannels;
                SampleRate = sampleRate;
                SampleFormat = sampleFormat;
            }

            public int BytesPerFrame() {
                return NumChannels * WasapiCS.SampleFormatTypeToUseBitsPerSample(SampleFormat) / 8;
            }
        };

        private DeviceFormat mDeviceFormat = new DeviceFormat();

        public void SetStateChangedCallback(StateChangedCallback cb) {
            mStateChangedCallback = cb;
        }

        public enum State {
            Stopped,
            Loading,
            Playing,
            Paused,
        };

        private State mState = State.Stopped;

        public void ChangeState(State s) {
            mState = s;

            if (mStateChangedCallback != null) {
                mStateChangedCallback(s);
            }
        }

        public State GetState() {
            return mState;
        }


        public PlaybackController() {
        }

        public bool Init() {
            mWasapi = new WasapiCS();
            int ercd = mWasapi.Init();
            if (ercd < 0) {
                Console.WriteLine("PlaybackController::Init() Wasapi.Init() failed {0}", ercd);
                return false;
            }
            return true;
        }

        public bool EnumerateDevices() {
            int ercd = mWasapi.EnumerateDevices(Wasapi.WasapiCS.DeviceType.Play);
            if (ercd < 0) {
                Console.WriteLine("PlaybackController::EnumerateDevices() failed {0}", ercd);
                return false;
            }

            return true;
        }

        public int GetDeviceCount() {
            return mWasapi.GetDeviceCount();
        }

        public WasapiCS.DeviceAttributes GetDeviceAttribute(int nth) {
            return mWasapi.GetDeviceAttributes(nth);
        }

        public void Term() {
            Console.WriteLine("D: PlaybackController.Term()");
            mWasapi.Term();
            mWasapi = null;
        }

        private static readonly WasapiCS.SampleFormatType[] mSampleFormatCandidate16 = new WasapiCS.SampleFormatType[] {
            WasapiCS.SampleFormatType.Sint16,
            WasapiCS.SampleFormatType.Sint24,
            WasapiCS.SampleFormatType.Sint32V24
        };

        private static readonly WasapiCS.SampleFormatType[] mSampleFormatCandidate24 = new WasapiCS.SampleFormatType[] {
            WasapiCS.SampleFormatType.Sint24,
            WasapiCS.SampleFormatType.Sint32V24,
            WasapiCS.SampleFormatType.Sint16,
        };

        private static WasapiCS.SampleFormatType[] SampleFormatCandidates(int bitsPerSample) {
            switch (bitsPerSample) {
            case 16:
                return mSampleFormatCandidate16;
            case 24:
                return mSampleFormatCandidate24;
            default:
                return null;
            }
        }

        public void SetWasapiParams(int bufferSizeMillisec, int zeroFlushMillisec, WasapiCS.DataFeedMode dfm) {
            mBufferSizeMillisec = bufferSizeMillisec;
            mZeroFlushMillisec = zeroFlushMillisec;
            mDataFeedMode = dfm;
        }

        public int GetWasapiBufferSizeMilisec() {
            return mBufferSizeMillisec;
        }

        public WasapiCS.DataFeedMode GetDataFeedMode() {
            return mDataFeedMode;
        }

        public DeviceFormat GetDeviceFormat() {
            return mDeviceFormat;
        }

        /// <returns>WASAPIエラーコード。</returns>
        private int Setup(int deviceId, PcmDataLib.PcmData format) {
            int ercd = 0;

            var mixFormat = mWasapi.GetMixFormat(deviceId);
            var sampleFormatCandidates = SampleFormatCandidates(format.ValidBitsPerSample);

            for (int i=0; i<sampleFormatCandidates.Length; ++i) {
                ercd = mWasapi.Setup(deviceId, WasapiCS.DeviceType.Play, WasapiCS.StreamType.PCM,
                    format.SampleRate, sampleFormatCandidates[i], mixFormat.numChannels,
                    mixFormat.dwChannelMask, WasapiCS.MMCSSCallType.Enable,
                    WasapiCS.MMThreadPriorityType.High, WasapiCS.SchedulerTaskType.ProAudio,
                    WasapiCS.ShareMode.Exclusive, mDataFeedMode, mBufferSizeMillisec, mZeroFlushMillisec, 10000);
                if (ercd < 0) {
                    Console.WriteLine("Wasapi.Setup({0} {1}) failed", format.SampleRate, sampleFormatCandidates[i]);
                } else {
                    mDeviceFormat.Set(mixFormat.numChannels, format.SampleRate, sampleFormatCandidates[i]);
                    break;
                }
            }

            return ercd;
        }

        private byte[] PcmFormatConvert(byte[] from, int fromOffs, int fromBytes,
            DeviceFormat fromFormat, DeviceFormat toFormat) {
            System.Diagnostics.Debug.Assert(fromFormat.SampleRate == toFormat.SampleRate);

            int numFrames = fromBytes / (fromFormat.NumChannels * WasapiCS.SampleFormatTypeToUseBitsPerSample(fromFormat.SampleFormat) / 8);

            var pcmFrom = new PcmDataLib.PcmData();
            pcmFrom.SetFormat(fromFormat.NumChannels, WasapiCS.SampleFormatTypeToUseBitsPerSample(fromFormat.SampleFormat),
                WasapiCS.SampleFormatTypeToValidBitsPerSample(fromFormat.SampleFormat),
                fromFormat.SampleRate, PcmDataLib.PcmData.ValueRepresentationType.SInt, numFrames);
            pcmFrom.SetSampleLargeArray(new WWUtil.LargeArray<byte>(from, fromOffs, fromBytes));

            var conv = new WasapiPcmUtil.PcmFormatConverter(fromFormat.NumChannels);
            var pcmTo = conv.Convert(pcmFrom, toFormat.SampleFormat, new WasapiPcmUtil.PcmFormatConverter.BitsPerSampleConvArgs(WasapiPcmUtil.NoiseShapingType.None));

            // チャンネル数を変更する。
            var toBytes = WasapiPcmUtil.PcmFormatConverter.ChangeChannelCount(
                fromFormat.SampleFormat, WasapiCS.StreamType.PCM,
                    fromFormat.NumChannels, pcmTo.GetSampleLargeArray().ToArray(),
                    toFormat.NumChannels);

            return toBytes;
        }

        private void SetSampleDataToWasapiStart(int idx, WWFlacRWCS.FlacRW flac) {
            WWFlacRWCS.Metadata meta;
            flac.GetDecodedMetadata(out meta);

            mFromFormat = new DeviceFormat();
            mFromFormat.Set(meta.channels, meta.sampleRate,
                    WasapiCS.BitAndFormatToSampleFormatType(meta.bitsPerSample, meta.bitsPerSample, WasapiCS.BitFormatType.SInt));

            long totalBytes = meta.totalSamples * mDeviceFormat.BytesPerFrame();

            mWasapi.AddPlayPcmDataAllocateMemory(idx, totalBytes);

            mDecodedPcmOffs = 0;
        }

        private void SetSampleDataToWasapiOne(int idx, byte[] pcm, int bytes) {
            var toBytes = PcmFormatConvert(pcm, 0, bytes, mFromFormat, mDeviceFormat);

            mWasapi.AddPlayPcmDataSetPcmFragment(idx, mDecodedPcmOffs, toBytes);
            mDecodedPcmOffs += toBytes.Length;
        }

        private void SetSampleDataToWasapiEnd(int idx, WWFlacRWCS.FlacRW flac) {
            mFromFormat = null;
            mDecodedPcmOffs = 0;
        }

        /// <returns>負の場合WASAPIエラーコード。成功の場合0。</returns>
        public int PlaylistCreateStart(int deviceId, ContentList.AudioFile af) {
            int ercd = 0;

            mWasapi.Stop();
            mWasapi.Unsetup();

            ChangeState(State.Loading);

            mWasapi.ClearPlayList();

            // 最初に再生する曲 af
            ercd = Setup(deviceId, af.Pcm);
            if (ercd < 0) {
                Console.WriteLine("E: PlaybackController::Play({0}) failed {1:X8}", deviceId, ercd);
                ChangeState(State.Stopped);
                return ercd;
            }

            mWasapi.AddPlayPcmDataStart();

            return 0;
        }

        public int LoadedGroupId() {
            return mLoadedGroupId;
        }

        /// <returns>負の値: FLACのエラー FlacErrorCode</returns>
        public int LoadAddStart(ContentList.AudioFile af) {
            mFlac = new WWFlacRWCS.FlacRW();
            int ercd = mFlac.DecodeStreamStart(af.Path);
            if (ercd < 0) {
                return ercd;
            }

            SetSampleDataToWasapiStart(af.Idx, mFlac);
            return ercd;
        }

        /// <returns>負: FLACのエラー FlacErrorCode。0: デコード終了。1以上: デコードされて出てきたデータのバイト数。</returns>
        public int LoadAddDo(ContentList.AudioFile af) {
            int ercd = 0;

            byte[] buff;
            ercd = mFlac.DecodeStreamOne(out buff);
            if (0 < ercd) {
                int buffBytes = ercd;
                SetSampleDataToWasapiOne(af.Idx, buff, buffBytes);
            }

            return ercd;
        }

        public void LoadAddEnd(ContentList.AudioFile af) {
            SetSampleDataToWasapiEnd(af.Idx, mFlac);
            mLoadedGroupId = af.GroupId;
            mFlac.DecodeEnd();
            mFlac = null;
        }

        public void PlaylistCreateEnd() {
            mWasapi.AddPlayPcmDataEnd();
        }

        /// <summary>
        /// ロードが完了している状態で、曲Idxを指定して再生開始する。
        /// 別の曲を再生しているときに呼び出すと、再生曲を切り替える。
        /// ロードしていない曲Idを指定して呼び出すと無視する。
        /// </summary>
        public bool Play(int idx) {
            int ercd;

            switch (mState) {
            case State.Paused:
                // ポーズ中の場合。
                mWasapi.UpdatePlayPcmDataById(idx);
                ercd = mWasapi.Unpause();
                if (0 <= ercd) {
                    ChangeState(State.Playing);
                }
                return true;
            case State.Playing:
                // 既に再生中の場合。
                mWasapi.UpdatePlayPcmDataById(idx);
                return true;
            default:
                break;
            }

            ercd = mWasapi.StartPlayback(idx);
            if (0 <= ercd) {
                ChangeState(State.Playing);
            }

            return 0 <= ercd;
        }

        public void Stop() {
            mLoadedGroupId = -1;
            mWasapi.Stop();
            ChangeState(State.Stopped);
        }

        public bool Pause() {
            int ercd = mWasapi.Pause();
            if (0 <= ercd) {
                ChangeState(State.Paused);
            }
            return 0 <= ercd;
        }

        public bool Run(int millisec) {
            bool result = mWasapi.Run(millisec);
            if (result) {
                ChangeState(State.Stopped);
            }

            return result;
        }

        public int GetPcmDataId(WasapiCS.PcmDataUsageType t) {
            return mWasapi.GetPcmDataId(t);
        }

        public WasapiCS.CursorLocation GetCursorLocation(WasapiCS.PcmDataUsageType t) {
            return mWasapi.GetPlayCursorPosition(t);
        }

        public WasapiCS.SessionStatus GetSessionStatus() {
            return mWasapi.GetSessionStatus();
        }

        public void SetPosFrame(long pos) {
            mWasapi.SetPosFrame(pos);
        }
    }
}
