using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wasapi;
using System.Globalization;

namespace RecPcmWin {
    class WasapiControl {
        private WasapiCS mWasapi = new WasapiCS();
        List<WasapiCS.DeviceAttributes> mDeviceAttributeList = new List<WasapiCS.DeviceAttributes>();
        private byte[] mCapturedPcmData;
        private int mNextWritePos = 0;
        private Wasapi.WasapiCS.CaptureCallback mCaptureCallback;
        private WasapiCS.SampleFormatType mSampleFormat;
        private int mSampleRate;
        private int mNumChannels;

        struct InspectFormat {
            public int sampleRate;
            public WasapiCS.SampleFormatType sampleFormat;
            public InspectFormat(int sr, WasapiCS.SampleFormatType sf) {
                sampleRate = sr;
                sampleFormat = sf;
            }
        };

        public readonly int[] mSampleRateList = {
                44100,48000,88200,96000,176400,192000};
        public readonly WasapiCS.SampleFormatType[] mSampleFormatList = {
                WasapiCS.SampleFormatType.Sint16,
                WasapiCS.SampleFormatType.Sint24,
                WasapiCS.SampleFormatType.Sint32V24,
                WasapiCS.SampleFormatType.Sint32};

        public bool AllocateCaptureMemory(int bytes) {
            try {
                mCapturedPcmData = null;
                mCapturedPcmData = new byte[bytes];
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return false;
            }

            mNextWritePos = 0;
            return true;
        }

        public void ReleaseCaptureMemory() {
            mCapturedPcmData = null;
            mNextWritePos = 0;
        }

        /// mCapturedPcmData is resized: you must call ReleaseCaptureMemory() and AllocateCaptureMemory() after this call
        public byte[] GetCapturedData() {
            Array.Resize(ref mCapturedPcmData, mNextWritePos);
            return mCapturedPcmData;
        }

        private void CaptureCallback(byte[] pcmData) {
            if (pcmData == null || pcmData.Length == 0) {
                return;
            }

            if (mCapturedPcmData.Length <= mNextWritePos + pcmData.Length) {
                return;
            }

            Array.Copy(pcmData, 0, mCapturedPcmData, mNextWritePos, pcmData.Length);
            mNextWritePos += pcmData.Length;
        }

        public int GetPosFrame() {
            return mNextWritePos / WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / mNumChannels * 8;
        }

        public int GetNumFrames() {
            return mCapturedPcmData.Length / WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / mNumChannels * 8;
        }

        public bool IsRunning() {
            return mCapturedPcmData != null;
        }

        public int Init() {
            int hr = mWasapi.Init();
            if (hr < 0) {
                return hr;
            }
            
            mCaptureCallback = new WasapiCS.CaptureCallback(CaptureCallback);
            mWasapi.RegisterCaptureCallback(mCaptureCallback);

            return hr;
        }

        public void Term() {
            mWasapi.Stop();
            mWasapi.Unsetup();
            mWasapi.Term();
            ReleaseCaptureMemory();
            mWasapi = null;
        }

        public int EnumerateRecDeviceNames(List<string> deviceNamesList) {
            mDeviceAttributeList.Clear();
            deviceNamesList.Clear();

            int hr = mWasapi.EnumerateDevices(WasapiCS.DeviceType.Rec);
            if (hr < 0) {
                return hr;
            }

            int nDevices = mWasapi.GetDeviceCount();
            for (int i = 0; i < nDevices; ++i) {
                var att = mWasapi.GetDeviceAttributes(i);
                mDeviceAttributeList.Add(att);
                deviceNamesList.Add(att.Name);
            }

            return hr;
        }

        public int Setup(int deviceIdx, WasapiCS.DataFeedMode dfm, int wasapiBufferSize, int sampleRate, WasapiCS.SampleFormatType sampleFormatType, int numChannels) {
            int hr = mWasapi.Setup(deviceIdx, WasapiCS.DeviceType.Rec,
                WasapiCS.StreamType.PCM, sampleRate, sampleFormatType, numChannels, WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive, dfm, wasapiBufferSize, 0, 10000);
            mSampleFormat = sampleFormatType;
            mSampleRate = sampleRate;
            mNumChannels = numChannels;
            return hr;
        }

        public void Unsetup() {
            mWasapi.Stop();
            mWasapi.Unsetup();
        }

        public void Stop() {
            mWasapi.Stop();
        }

        public bool Run(int millisec) {
            return mWasapi.Run(millisec);
        }

        public string InspectDevice(int deviceIdx, int numChannels) {
            if (deviceIdx < 0 || mDeviceAttributeList.Count <= deviceIdx) {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            string dn = mDeviceAttributeList[deviceIdx].Name;
            string did = mDeviceAttributeList[deviceIdx].DeviceIdString;

            sb.AppendFormat(CultureInfo.InvariantCulture, "wasapi.InspectDevice()\r\nDeviceFriendlyName={0}\r\nDeviceIdString={1}\r\nTested numChannels={2}",
                    dn, did, numChannels);
            foreach (int sr in mSampleRateList) {
                sb.AppendFormat("\r\n{0}Hz: ", sr);
                foreach (var fmt in mSampleFormatList) {
                    int hr = mWasapi.InspectDevice(deviceIdx, sr, fmt, numChannels);
                    string resultStr = "OK";
                    if (hr < 0) {
                        resultStr = string.Format("{0:X8}", hr);
                    }
                    sb.AppendFormat("{0}={1}, ", fmt, resultStr);
                }
            }

            sb.Append("\r\nDone.\r\n");

            return sb.ToString();
        }

        public int StartRecording() {
            int hr = mWasapi.StartRecording();
            return hr;
        }

        public long GetCaptureGlitchCount() {
            return mWasapi.GetCaptureGlitchCount();
        }
    }
}
