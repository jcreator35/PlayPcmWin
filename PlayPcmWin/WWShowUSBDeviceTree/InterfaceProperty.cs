using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWShowUSBDeviceTree {
    public class InterfaceProperty {
        // 同じifNr altSet endpointAddr のEndpointを１つにまとめるのに使用。
        public Module lastEndpointModule;

        // Interface Descriptor
        public int ifNr = 0;
        public int altSet = 0;
        public int numEP = 0;
        public int ifClass = 0;
        public int ifSubClass = 0;
        public string name;

        // ReadEndpointDescでセット。
        public enum Direction {
            Unknown = -1,
            OutputDirection,
            InputDirection,
        }
        public Direction direction;
        public int endpointType;
        public int syncType;
        public int usageType;
        public int endpointAddr;
        public string endpointAttributes;
        //public int interval;
        public int maxPacketBytes;


        // ReadAudioStreamingInterfaceDescでセット。
        public int inTermNr = 0;
        public int outTermNr = 0;
        public int delayFrames = 0;
        public string formatStr;
        public int nCh = 0;
        public string channelNames;

        // ReadAudioCSEndpointでセット。
        public string endpointStr;

        // ReadAudioStreamingFormatTypeDescでセット。
        public int bitResolution;
        public int subSlotSize;
        public List<int> samFreqs = new List<int>();
        // nChを更新。
        public int lowerSamFreq = 0;
        public int upperSamFreq = 0;

        public void Clear() {
            lastEndpointModule = null;
            ifNr = 0;
            altSet = 0;
            numEP = 0;
            ifClass = 0;
            ifSubClass = 0;
            name = "";

            direction = Direction.Unknown;
            endpointAddr = 0;
            endpointAttributes = "";
            maxPacketBytes = 0;

            inTermNr = 0;
            outTermNr = 0;

            delayFrames = 0;
            formatStr = "";
            nCh = 0;
            channelNames = "";

            endpointStr = "";

            bitResolution = 0;
            subSlotSize = 0;
            samFreqs.Clear();
            lowerSamFreq = 0;
            upperSamFreq = 0;

        }
    }
}
