using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Asio
{
    /** simple wrapper class of C++ ASIO Wrapper API
     */
    public class AsioCS
    {
        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_init();

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_term();

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_getDriverNum();

        [DllImport("AsioIODLL.dll", CharSet = CharSet.Ansi)]
        private extern static bool AsioWrap_getDriverName(int n, System.Text.StringBuilder name, int size);

        [DllImport("AsioIODLL.dll")]
        private extern static bool AsioWrap_loadDriver(int n);

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_unloadDriver();

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_setup(int sampleRate);

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_unsetup();

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_getInputChannelsNum();

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_getOutputChannelsNum();

        [DllImport("AsioIODLL.dll", CharSet = CharSet.Ansi)]
        private extern static bool AsioWrap_getInputChannelName(int n, System.Text.StringBuilder name_return, int size);

        [DllImport("AsioIODLL.dll", CharSet = CharSet.Ansi)]
        private extern static bool AsioWrap_getOutputChannelName(int n, System.Text.StringBuilder name_return, int size);

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_setOutput(int channel, int[] data, int samples, bool repeat);

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_setInput(int inputChannel, int samples);

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_start();

        [DllImport("AsioIODLL.dll")]
        private extern static bool AsioWrap_run();

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_stop();

        [DllImport("AsioIODLL.dll")]
        private extern static void AsioWrap_getRecordedData(int inputChannel, int [] recordedData_return, int samples);

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_controlPanel();

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_getClockSourceNum();

        [DllImport("AsioIODLL.dll", CharSet = CharSet.Ansi)]
        private extern static bool AsioWrap_getClockSourceName(int idx, System.Text.StringBuilder name_return, int size);

        [DllImport("AsioIODLL.dll")]
        private extern static int AsioWrap_setClockSource(int idx);

        public enum AsioStatus
        {
            PreInit,
            DriverNotLoaded, //< driver unloaded
            DriverLoaded,  //< driver loaded but samplerate is not set
            SampleRateSet, //< samplerate is set but inputchannels and output channels are not set
            Prepared,      //< in/out channels are set but stopped
            Running        //< in/out started
        }

        private AsioStatus state = AsioStatus.PreInit;

        public AsioStatus GetStatus() {
            return state;
        }

        private void ChangeState(AsioStatus newState) {
            Console.WriteLine("AsioWrap.ChangeState() {0} ==> {1}",
                state, newState);
            if (state == newState) {
                return;
            }

            switch (state) {
            case AsioStatus.PreInit:
                System.Diagnostics.Debug.Assert(
                    newState == AsioStatus.DriverNotLoaded);
                break;
            case AsioStatus.DriverNotLoaded:
                System.Diagnostics.Debug.Assert(
                    newState == AsioStatus.DriverLoaded ||
                    newState == AsioStatus.PreInit);
                break;
            case AsioStatus.DriverLoaded:
                System.Diagnostics.Debug.Assert(
                    newState == AsioStatus.DriverNotLoaded ||
                    newState == AsioStatus.SampleRateSet);
                break;
            case AsioStatus.SampleRateSet:
                System.Diagnostics.Debug.Assert(
                    newState == AsioStatus.DriverLoaded ||
                    newState == AsioStatus.Prepared);
                break;
            case AsioStatus.Prepared:
                System.Diagnostics.Debug.Assert(
                    newState == AsioStatus.SampleRateSet ||
                    newState == AsioStatus.Running ||
                    newState == AsioStatus.DriverLoaded);
                break;
            case AsioStatus.Running:
                System.Diagnostics.Debug.Assert(
                    newState == AsioStatus.Prepared);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            state = newState;
        }

        /////////////////////////////////////////////////////////////////////////

        /** Initialize Asio
         */
        public void Init() {
            System.Diagnostics.Debug.Assert(state == AsioStatus.PreInit); 
            AsioWrap_init();
            ChangeState(AsioStatus.DriverNotLoaded);
        }

        /** Terminate asio completely
         */
        public void Term() {
            if (AsioStatus.Running == state) {
                Stop();
            }
            if (AsioStatus.Prepared == state ||
                AsioStatus.SampleRateSet == state) {
                Unsetup();
            }
            if (AsioStatus.DriverLoaded == state) {
                DriverUnload();
            }
            if (AsioStatus.DriverNotLoaded == state) {
                AsioWrap_term();
            }
        }

        /** returns num of Asio driver */
        public int DriverNumGet() {
            return AsioWrap_getDriverNum();
        }

        /** @param n asio driver index (0 ... DriverNumGet()-1)
         **/
        public string DriverNameGet(int n) {
            StringBuilder buf = new StringBuilder(64);
            AsioWrap_getDriverName(n, buf, buf.Capacity);
            return buf.ToString();
        }

        /** load ASIO driver
         * @param n asio driver index (0 ... DriverNumGet()-1)
         */
        public bool DriverLoad(int n) {
            System.Diagnostics.Debug.Assert(state == AsioStatus.DriverNotLoaded);

            bool rv = AsioWrap_loadDriver(n);
            Console.WriteLine("AsioWrap_loadDriver({0}) rv={1}", n, rv);
            if (rv) {
                ChangeState(AsioStatus.DriverLoaded);
            } else {
                Console.WriteLine("AsioWrap_loadDriver({0}) rv={1} FAILED", n, rv);
            }
            return rv;
        }

        /** unload ASIO driver 
         * @param n asio driver index (0 ... DriverNumGet()-1)
         */
        public void DriverUnload() {
            Console.WriteLine("AsioWrap_unloadDriver()");

            switch (state) {
            case AsioStatus.DriverLoaded:
                AsioWrap_unloadDriver();
                ChangeState(AsioStatus.DriverNotLoaded);
                break;
            case AsioStatus.DriverNotLoaded:
                Console.WriteLine("AsioWrap_unloadDriver() not loaded. IGNORED!");
                break;
            default:
                System.Console.WriteLine("E: AsioWrap_unloadDriver() state is wrong {0}", state);
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        /** set samplerate
         * @return 0: success. other: ASIOError
         */
        public int Setup(int sampleRate) {
            System.Diagnostics.Debug.Assert(state == AsioStatus.DriverLoaded);
            int rv = AsioWrap_setup(sampleRate);
            if (rv == 0) {
                ChangeState(AsioStatus.SampleRateSet);
            } else {
                Console.WriteLine("AsioWrap_setup({0}) rv={1} FAILED", sampleRate, rv);
            }
            return rv;
        }

        /** unset samplerate
         */
        public void Unsetup() {
            AsioWrap_unsetup();
            ChangeState(AsioStatus.DriverLoaded);
        }

        /** returns num of input channels
         */
        public int InputChannelsNumGet() {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            return AsioWrap_getInputChannelsNum();
        }

        /** returns input channel name
         * @param n input channel index (0 ... InputChannelsNumGet()-1)
         */
        public string InputChannelNameGet(int n) {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            StringBuilder buf = new StringBuilder(64);
            AsioWrap_getInputChannelName(n, buf, buf.Capacity);
            return buf.ToString();
        }

        /** returns num of output channels
         */
        public int OutputChannelsNumGet() {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            return AsioWrap_getOutputChannelsNum();
        }

        /** returns output channel name
         * @param n output channel index (0 ... OutputChannelsNumGet()-1)
         */
        public string OutputChannelNameGet(int n) {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            StringBuilder buf = new StringBuilder(64);
            AsioWrap_getOutputChannelName(n, buf, buf.Capacity);
            return buf.ToString();
        }

        /** set num of receive input data samples
         * when samples data is retrieved, recording stops.
         * @param channel input channel (0 ... InputChannelsNumGet()-1)
         * @param samples num of samples to retrieve
         */
        public void InputSet(int channel, int samples) {
            System.Diagnostics.Debug.Assert(
                AsioStatus.SampleRateSet == state ||
                AsioStatus.Prepared == state);
            AsioWrap_setInput(channel, samples);
            ChangeState(AsioStatus.Prepared);
        }

        /** send output sample data
         * @param channel output channel (0 ... OutputChannelsNumGet()-1)
         * @param outputData output sample data
         * @param repeat true: repeats. false: not repeat(when data is end, play stops)
         */
        public void OutputSet(int channel, int[] outputData, bool repeat) {
            System.Diagnostics.Debug.Assert(
                AsioStatus.SampleRateSet == state ||
                AsioStatus.Prepared == state);
            AsioWrap_setOutput(channel, outputData, outputData.Length, repeat);
            ChangeState(AsioStatus.Prepared);
        }

        /** receive input data
         * @param channel input channel (0 ... InputChannelsNumGet()-1)
         * @param samples num of samples to retrieve
         */
        public int[] RecordedDataGet(int inputChannel, int samples) {
            int [] recordedData = new int[samples];
            AsioWrap_getRecordedData(inputChannel, recordedData, samples);
            return recordedData;
        }

        /** start input/output tasks
         * @return 0: success. other: ASIOError
         */
        public int Start() {
            System.Diagnostics.Debug.Assert(AsioStatus.Prepared == state);
            int rv = AsioWrap_start();
            if (0 == rv) {
                ChangeState(AsioStatus.Running);
            } else {
                Console.WriteLine("AsioWrap_start() rv={0} FAILED", rv);
            }
            return rv;
        }

        /** stop input/output tasks
         */
        public void Stop() {
            if (AsioStatus.Prepared == state) {
                return;
            }
            if (AsioStatus.Running == state) {
                AsioWrap_stop();
                ChangeState(AsioStatus.Prepared);
                return;
            }
            System.Diagnostics.Debug.Assert(false);
        }

        /** run looper (this is a blocking function. call from dedicated thread)
         */
        public bool Run() {
            bool rv = AsioWrap_run();
            if (rv) {
                // stopped
                ChangeState(AsioStatus.Prepared);
            }
            return rv;
        }

        public string AsioTrademarkStringGet() {
            return "ASIO version 2.1\nASIO is a trademark and software of Steinberg Media Technologies GmbH";
        }

        /** disp controlpanel
         */
        public int ControlPanel() {
            return AsioWrap_controlPanel();
        }

        /** returns num of clock sources
         */
        public int ClockSourceNumGet() {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            return AsioWrap_getClockSourceNum();
        }

        /** returns clock source name
         * @param idx clock source index (0 ... ClockSourceNumGet()-1)
         */
        public string ClockSourceNameGet(int idx) {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            StringBuilder buf = new StringBuilder(64);
            AsioWrap_getClockSourceName(idx, buf, buf.Capacity);
            return buf.ToString();
        }

        /** set clock source
         * @param idx clock source index (0 ... ClockSourceNumGet()-1)
         */
        public int ClockSourceSet(int idx) {
            System.Diagnostics.Debug.Assert(AsioStatus.SampleRateSet <= state);

            return AsioWrap_setClockSource(idx);
        }
    }
}
