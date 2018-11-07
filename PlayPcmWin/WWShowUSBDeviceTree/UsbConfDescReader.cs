using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WWShowUSBDeviceTree.UsbDeviceTreeCs;

namespace WWShowUSBDeviceTree {
    public class UsbConfDescReader {
        WWUsbHubPortCs mHp;
        StringBuilder mSB = new StringBuilder();
        List<WWUsbStringDescCs> mSds;
        UsbDeviceTreeCs.BusSpeed mSpeed;

        enum USBAudioClass {
            Other,
            AC1,
            AC2,
        };

        USBAudioClass mAudioClass = USBAudioClass.Other;

        int interfaceClass = 0;
        int interfaceSubClass = 0;
        int interfaceProtocol = 0;


        private string FindString(int idx) {
            foreach (var v in mSds) {
                if (v.descIdx == idx) {
                    return v.name;
                }
            }

            return "";
        }

        private const int CONFIG_BUS_POWERED = 0x80;
        private const int CONFIG_SELF_POWERED = 0x40;
        private const int CONFIG_REMOTE_WAKEUP = 0x20;

        private const int DEVICE_CLASS_RESERVED = 0x00;
        private const int DEVICE_CLASS_AUDIO = 0x01;
        private const int DEVICE_CLASS_COMMUNICATIONS = 0x02;
        private const int DEVICE_CLASS_HUMAN_INTERFACE = 0x03;
        private const int DEVICE_CLASS_MONITOR = 0x04;

        private const int DEVICE_CLASS_PHYSICAL_INTERFACE = 0x05;
        private const int DEVICE_CLASS_POWER = 0x06;
        private const int DEVICE_CLASS_IMAGE = 0x06; //< ?
        private const int DEVICE_CLASS_PRINTER = 0x07;
        private const int DEVICE_CLASS_STORAGE = 0x08;

        private const int DEVICE_CLASS_HUB = 0x09;
        private const int DEVICE_CLASS_CDC_DATA = 0x0A;
        private const int DEVICE_CLASS_SMART_CARD = 0x0B;
        private const int DEVICE_CLASS_CONTENT_SECURITY = 0x0D;
        private const int DEVICE_CLASS_VIDEO = 0x0E;

        private const int DEVICE_CLASS_PERSONAL_HEALTHCARE = 0x0F;
        private const int DEVICE_CLASS_AUDIO_VIDEO = 0x10;
        private const int DEVICE_CLASS_BILLBOARD = 0x11;
        private const int DEVICE_CLASS_DIAGNOSTIC_DEVICE = 0xDC;
        private const int DEVICE_CLASS_WIRELESS_CONTROLLER = 0xE0;

        private const int DEVICE_CLASS_MISCELLANEOUS = 0xEF;
        private const int DEVICE_CLASS_APPLICATION_SPECIFIC = 0xFE;
        private const int DEVICE_CLASS_VENDOR_SPECIFIC = 0xFF;

        private const int AUDIO_SUBCLASS_UNDEFINED = 0x00;
        private const int AUDIO_SUBCLASS_AUDIOCONTROL = 0x01;
        private const int AUDIO_SUBCLASS_AUDIOSTREAMING = 0x02;
        private const int AUDIO_SUBCLASS_MIDISTREAMING = 0x03;

        private string InterfaceClassToStr(int intClass, int intSubClass, int intProto) {
            switch (intClass) {
            case DEVICE_CLASS_RESERVED: return "Reserved device";
            case DEVICE_CLASS_AUDIO:
                switch (intSubClass) {
                case AUDIO_SUBCLASS_AUDIOCONTROL: return "Audio Control";
                case AUDIO_SUBCLASS_AUDIOSTREAMING: return "Audio Streaming";
                case AUDIO_SUBCLASS_MIDISTREAMING: return "MIDI Streaming";
                default: return string.Format("Audio Interface with unknown subclass {0:X2}", intSubClass);
                }
            case DEVICE_CLASS_COMMUNICATIONS: return "Communications";
            case DEVICE_CLASS_HUMAN_INTERFACE: return "Human Interface";
            case DEVICE_CLASS_MONITOR: return "Monitor";

            case DEVICE_CLASS_PHYSICAL_INTERFACE: return "Physical Interface";
            case DEVICE_CLASS_POWER:
                if (intSubClass == 1 && intProto == 1) {
                    return "Image Device";
                } else {
                    return "Power Device";
                }
            case DEVICE_CLASS_PRINTER: return "Printer";
            case DEVICE_CLASS_STORAGE: return "Storage";

            case DEVICE_CLASS_HUB: return "Hub";
            case DEVICE_CLASS_CDC_DATA: return "CDC_DATA";
            case DEVICE_CLASS_SMART_CARD: return "Smart Card";
            case DEVICE_CLASS_CONTENT_SECURITY: return "Content Security";
            case DEVICE_CLASS_VIDEO: return "Video";

            case DEVICE_CLASS_PERSONAL_HEALTHCARE: return "Personal Healthcare";
            case DEVICE_CLASS_AUDIO_VIDEO: return "Audio Video";
            case DEVICE_CLASS_BILLBOARD: return "Billboard";
            case DEVICE_CLASS_DIAGNOSTIC_DEVICE: return "Diagnostic Device";
            case DEVICE_CLASS_WIRELESS_CONTROLLER: return "Wireless Controller";

            case DEVICE_CLASS_MISCELLANEOUS: return "Miscellaneous";
            case DEVICE_CLASS_APPLICATION_SPECIFIC:
                switch (intSubClass) {
                case 1: return "Device Firmware Application Specific Device";
                case 2: return "IrDA Bridge";
                case 3: return "Test & Measurement Class Device";
                default: return string.Format("Application Specific Device with subClass{0:X2}", intSubClass);
                }
            case DEVICE_CLASS_VENDOR_SPECIFIC: return "Vendor Specific";
            default: return "Unknown";
            }
        }

        private const int ENDPOINT_DIRECTION_MASK = 0x80;
        private const int ENDPOINT_ADDRESS_MASK = 0x0f;

        private const int ENDPOINT_TYPE_MASK = 0x03;
        private const int ENDPOINT_TYPE_CONTROL = 0x00;
        private const int ENDPOINT_TYPE_ISOCHRONOUS = 0x01;
        private const int ENDPOINT_TYPE_BULK = 0x02;
        private const int ENDPOINT_TYPE_INTERRUPT = 0x03;

        private const int ENDPOINT_TYPE_BULK_RESERVED_MASK = 0xFC;
        private const int U20_ENDPOINT_TYPE_INTERRUPT_RESERVED_MASK = 0xFC;
        private const int U30_ENDPOINT_TYPE_INTERRUPT_RESERVED_MASK = 0xCC;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_RESERVED_MASK = 0xC0;

        private const int U30_ENDPOINT_TYPE_INTERRUPT_USAGE_MASK = 0x30;
        private const int U30_ENDPOINT_TYPE_INTERRUPT_USAGE_PERIODIC = 0x00;
        private const int U30_ENDPOINT_TYPE_INTERRUPT_USAGE_NOTIFICATION = 0x10;
        private const int U30_ENDPOINT_TYPE_INTERRUPT_USAGE_RESERVED10 = 0x20;
        private const int U30_ENDPOINT_TYPE_INTERRUPT_USAGE_RESERVED11 = 0x30;

        private const int ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_MASK = 0x0C;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_NO_SYNCHRONIZATION = 0x00;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_ASYNCHRONOUS = 0x04;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_ADAPTIVE = 0x08;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_SYNCHRONOUS = 0x0C;

        private const int ENDPOINT_TYPE_ISOCHRONOUS_USAGE_MASK = 0x30;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_USAGE_DATA_ENDOINT = 0x00;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_USAGE_FEEDBACK_ENDPOINT = 0x10;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_USAGE_IMPLICIT_FEEDBACK_DATA_ENDPOINT = 0x20;
        private const int ENDPOINT_TYPE_ISOCHRONOUS_USAGE_RESERVED = 0x30;


        private const int AUDIO_CS_UNDEFINED = 0x20;
        private const int AUDIO_CS_DEVICE = 0x21;
        private const int AUDIO_CS_CONFIGURATION = 0x22;
        private const int AUDIO_CS_STRING = 0x23;
        private const int AUDIO_CS_INTERFACE = 0x24;
        private const int AUDIO_CS_ENDPOINT = 0x25;

        // USB Device Class Definition for Audio Devices A.9 Audio Class-Specific AC Interface Descriptor Subtypes

        private const int AUDIO_AC_UNDEFINED = 0x00;
        private const int AUDIO_AC_HEADER = 0x01;
        private const int AUDIO_AC_INPUT_TERMINAL = 0x02;
        private const int AUDIO_AC_OUTPUT_TERMINAL = 0x03;
        private const int AUDIO_AC_MIXER_UNIT = 0x04;
        private const int AUDIO_AC_SELECTOR_UNIT = 0x05;
        private const int AUDIO_AC_FEATURE_UNIT = 0x06;

        /// <summary>
        /// This may also be USBAC1 Processing Unit
        /// </summary>
        private const int AUDIO_AC2_EFFECT_UNIT = 0x07;

        /// <summary>
        /// This may also be USBAC1 Extension Unit
        /// </summary>
        private const int AUDIO_AC2_PROCESSING_UNIT = 0x08;

        private const int AUDIO_AC2_EXTENSION_UNIT = 0x09;
        private const int AUDIO_AC2_CLOCK_SOURCE = 0x0A;
        private const int AUDIO_AC2_CLOCK_SELECTOR = 0x0B;
        private const int AUDIO_AC2_CLOCK_MULTIPLIER = 0x0C;
        private const int AUDIO_AC2_SAMPLE_RATE_CONVERTER = 0x0D;

        // USB AC1 A.5 Audio Class-Specific AC Interface Descriptor Subtypes
        private const int AUDIO_AC1_PROCESSING_UNIT = 0x07;
        private const int AUDIO_AC1_EXTENSION_UNIT = 0x08;

        private string AudioControlTypeToStr(int t) {
            switch (t) {
            case AUDIO_AC_HEADER: return "Audio Control Header";
            case AUDIO_AC_INPUT_TERMINAL: return "Audio Input Terminal";
            case AUDIO_AC_OUTPUT_TERMINAL: return "Audio Output Terminal";
            case AUDIO_AC_MIXER_UNIT: return "Audio Mixer Unit";
            case AUDIO_AC_SELECTOR_UNIT: return "Audio Selector Unit";
            case AUDIO_AC_FEATURE_UNIT: return "Audio Feature Unit";
            case AUDIO_AC2_EFFECT_UNIT: return "Audio Effect Unit(AC2) or Processing Unit(AC1)";
            case AUDIO_AC2_PROCESSING_UNIT: return "Audio Processing Unit(AC2) or Extension Unit(AC1)";
            case AUDIO_AC2_EXTENSION_UNIT: return "Audio Extension Unit(AC2)";
            case AUDIO_AC2_CLOCK_SOURCE: return "Audio Clock Source";
            case AUDIO_AC2_CLOCK_SELECTOR: return "Audio Clock Selector";
            case AUDIO_AC2_CLOCK_MULTIPLIER: return "Audio Clock Multiplier";
            case AUDIO_AC2_SAMPLE_RATE_CONVERTER: return "Audio Sample Rate Converter";
            default: return string.Format("Unknown Audio Control type {0}", t);
            }
        }

        private const int AUDIO_AS_UNDEFINED = 0x00;
        private const int AUDIO_AS_INTERFACE = 0x01;      //< USBAC2 Table A-10ではGeneral
        private const int AUDIO_AS_FORMAT_TYPE = 0x02;    
        private const int AUDIO_AS_FORMAT_SPECIFIC = 0x03;//< USBAC2 Table A-10ではENCODER

        private string AudioStreamingTypeToStr(int t) {
            switch (t) {
            case AUDIO_AS_UNDEFINED: return "Audio Streaming Undefined descriptor";
            case AUDIO_AS_INTERFACE: return "Audio Streaming Inteface/General Descriptor";
            case AUDIO_AS_FORMAT_TYPE: return "Audio Streaming Format Type Descriptor";
            case AUDIO_AS_FORMAT_SPECIFIC: return "Audio Streaming Format Specific/Encoder Descriptor";

            default: return string.Format("Audio Streaming Unknown Descriptor {0}", t);
            }
        }

        private const int USB_TERMINAL_TYPE_Undefined = 0x100;
        private const int USB_TERMINAL_TYPE_Streaming = 0x101;
        private const int USB_TERMINAL_TYPE_VendorSpecific = 0x1ff;

        private const int INPUT_TERMINAL_TYPE_Undefined = 0x200;
        private const int INPUT_TERMINAL_TYPE_Microphone = 0x201;
        private const int INPUT_TERMINAL_TYPE_DesktopMicrophone = 0x202;
        private const int INPUT_TERMINAL_TYPE_PersonalMicrophone = 0x203;
        private const int INPUT_TERMINAL_TYPE_OmniDirectionalMic = 0x204;
        private const int INPUT_TERMINAL_TYPE_MicrophoneArray = 0x205;
        private const int INPUT_TERMINAL_TYPE_ProcessingMicrophoneArray = 0x206;

        private const int OUTPUT_TERMINAL_TYPE_Undefined = 0x300;
        private const int OUTPUT_TERMINAL_TYPE_Speaker = 0x301;
        private const int OUTPUT_TERMINAL_TYPE_Headphones = 0x302;
        private const int OUTPUT_TERMINAL_TYPE_HeadMountDisplay = 0x303;
        private const int OUTPUT_TERMINAL_TYPE_DesktopSpeaker = 0x304;
        private const int OUTPUT_TERMINAL_TYPE_RoomSpeaker = 0x305;
        private const int OUTPUT_TERMINAL_TYPE_CommunicationSpeaker = 0x306;
        private const int OUTPUT_TERMINAL_TYPE_LowFrequencyEffectSpeaker = 0x307;

        private const int BIDI_TERMINAL_TYPE_Undefined = 0x400;
        private const int BIDI_TERMINAL_TYPE_Handset = 0x401;
        private const int BIDI_TERMINAL_TYPE_Headset = 0x402;
        private const int BIDI_TERMINAL_TYPE_Speakerphone = 0x403;
        private const int BIDI_TERMINAL_TYPE_EchoSuppressingSpeakerphone = 0x404;
        private const int BIDI_TERMINAL_TYPE_EchoCancellingSpeakerphone = 0x405;

        private const int TELEPHONY_TERMINAL_TYPE_Undefined = 0x500;
        private const int TELEPHONY_TERMINAL_TYPE_PhoneLine = 0x501;
        private const int TELEPHONY_TERMINAL_TYPE_Telephone = 0x502;
        private const int TELEPHONY_TERMINAL_TYPE_DownLinePhone = 0x503;

        private const int EXTERNAL_TERMINAL_TYPE_Undefined = 0x600;
        private const int EXTERNAL_TERMINAL_TYPE_AnalogConnector = 0x601;
        private const int EXTERNAL_TERMINAL_TYPE_DigitalAudioInterface = 0x602;
        private const int EXTERNAL_TERMINAL_TYPE_LineConnector = 0x603;
        private const int EXTERNAL_TERMINAL_TYPE_LegacyAudioConnector = 0x604;
        private const int EXTERNAL_TERMINAL_TYPE_SPDIF = 0x605;
        private const int EXTERNAL_TERMINAL_TYPE_1394DA = 0x606;
        private const int EXTERNAL_TERMINAL_TYPE_1394DV = 0x607;
        private const int EXTERNAL_TERMINAL_TYPE_ADAT = 0x608;
        private const int EXTERNAL_TERMINAL_TYPE_TDIF = 0x609;
        private const int EXTERNAL_TERMINAL_TYPE_MADI = 0x60a;

        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_Undefined = 0x700;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_LevelCalibrationNoiseSource = 0x701;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_EqualizationNoise = 0x702;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_CDPlayer = 0x703;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_DAT = 0x704;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_DCC = 0x705;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_MiniDisk = 0x706;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_AnalogTape = 0x707;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_Phonograph = 0x708;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_VCRAudio = 0x709;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_VideoDiscAudio = 0x70a;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_DVDAudio = 0x70b;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_TVTunerAudio = 0x70c;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_SatelliteReceiverAudio = 0x70d;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_CableTunerAudio = 0x70e;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_DSSAudio = 0x70f;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_RadioReceiver = 0x710;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_RadioTransmitter = 0x711;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_MultiTrackRecorder = 0x712;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_Synthesizer = 0x713;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_Piano = 0x714;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_Guitar = 0x715;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_Drums = 0x716;
        private const int EMBEDDED_FUNCTION_TERMINAL_TYPE_OtherMusicalInstrument = 0x717;

        private string TerminalTypeToStr(int itt) {
            switch (itt) {
            case USB_TERMINAL_TYPE_Undefined: return "Undefined terminal";
            case USB_TERMINAL_TYPE_Streaming: return "Streaming";
            case USB_TERMINAL_TYPE_VendorSpecific: return "Vendor specific type";
            case INPUT_TERMINAL_TYPE_Undefined: return "Undefined in terminal";
            case INPUT_TERMINAL_TYPE_Microphone: return "Microphone";
            case INPUT_TERMINAL_TYPE_DesktopMicrophone: return "Desktop microphone";
            case INPUT_TERMINAL_TYPE_PersonalMicrophone: return "Personal microphone";
            case INPUT_TERMINAL_TYPE_OmniDirectionalMic: return "Omni-directional mic";
            case INPUT_TERMINAL_TYPE_MicrophoneArray: return "Microphone array";
            case INPUT_TERMINAL_TYPE_ProcessingMicrophoneArray: return "Processing microphone array";
            case OUTPUT_TERMINAL_TYPE_Undefined: return "Undefined out terminal";
            case OUTPUT_TERMINAL_TYPE_Speaker: return "Speaker";
            case OUTPUT_TERMINAL_TYPE_Headphones: return "Headphones";
            case OUTPUT_TERMINAL_TYPE_HeadMountDisplay: return "Head mount display";
            case OUTPUT_TERMINAL_TYPE_DesktopSpeaker: return "Desktop spekaer";
            case OUTPUT_TERMINAL_TYPE_RoomSpeaker: return "Room speaker";
            case OUTPUT_TERMINAL_TYPE_CommunicationSpeaker: return "Communication speaker";
            case OUTPUT_TERMINAL_TYPE_LowFrequencyEffectSpeaker: return "Low frequency effect speaker";
            case BIDI_TERMINAL_TYPE_Undefined: return "Undefined bi-directional terminal";
            case BIDI_TERMINAL_TYPE_Handset: return "Handset";
            case BIDI_TERMINAL_TYPE_Headset: return "Headset";
            case BIDI_TERMINAL_TYPE_Speakerphone: return "Speakerphone";
            case BIDI_TERMINAL_TYPE_EchoSuppressingSpeakerphone: return "Echo-suppressing speakerphone";
            case BIDI_TERMINAL_TYPE_EchoCancellingSpeakerphone: return "Echo-cancelling speakerphone";

            case TELEPHONY_TERMINAL_TYPE_Undefined: return "Undefined Telephony terminal";
            case TELEPHONY_TERMINAL_TYPE_PhoneLine: return "Phone line";
            case TELEPHONY_TERMINAL_TYPE_Telephone: return "Telephone";
            case TELEPHONY_TERMINAL_TYPE_DownLinePhone: return "Down line phone";

            case EXTERNAL_TERMINAL_TYPE_Undefined: return "Undefined external terminal";
            case EXTERNAL_TERMINAL_TYPE_AnalogConnector: return "Analog connector";
            case EXTERNAL_TERMINAL_TYPE_DigitalAudioInterface: return "Digital connector";
            case EXTERNAL_TERMINAL_TYPE_LineConnector: return "Line connector";
            case EXTERNAL_TERMINAL_TYPE_LegacyAudioConnector: return "Legacy audio connector";
            case EXTERNAL_TERMINAL_TYPE_SPDIF: return "S/P DIF";
            case EXTERNAL_TERMINAL_TYPE_1394DA: return "1394 DA";
            case EXTERNAL_TERMINAL_TYPE_1394DV: return "1394 DV";
            case EXTERNAL_TERMINAL_TYPE_ADAT: return "ADAT";
            case EXTERNAL_TERMINAL_TYPE_TDIF: return "TDIF";
            case EXTERNAL_TERMINAL_TYPE_MADI: return "MADI";

            case EMBEDDED_FUNCTION_TERMINAL_TYPE_Undefined: return "Undefined Embedded function";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_LevelCalibrationNoiseSource: return "Level calibration noise source";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_EqualizationNoise: return "Equalization noise";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_CDPlayer: return "CD player";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_DAT: return "DAT";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_DCC: return "DCC";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_MiniDisk: return "MiniDisc";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_AnalogTape: return "Analog tape";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_Phonograph: return "Phonograph";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_VCRAudio: return "VCR audio";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_VideoDiscAudio: return "VideoDisc audio";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_DVDAudio: return "DVD audio";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_TVTunerAudio: return "TV tuner audio";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_SatelliteReceiverAudio: return "Satellite receiver audio";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_CableTunerAudio: return "Cable tuner audio";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_DSSAudio: return "DSS receiver audio track";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_RadioReceiver: return "Radio receiver";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_RadioTransmitter: return "Radio transmitter";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_MultiTrackRecorder: return "Multi track recorder";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_Synthesizer: return "Synthesizer";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_Piano: return "Piano";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_Guitar: return "Guitar";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_Drums: return "Drums/Rhythm";
            case EMBEDDED_FUNCTION_TERMINAL_TYPE_OtherMusicalInstrument: return "Other Musical instrument";
            default: return "Unknown";
            }
        }

        private string WChannelConfigToStr(int wChannelConfig) {
            var sb = new StringBuilder();
            if (0 != (wChannelConfig & 1)) {
                sb.Append("L ");
            }
            if (0 != (wChannelConfig & 2)) {
                sb.Append("R ");
            }
            if (0 != (wChannelConfig & 4)) {
                sb.Append("C ");
            }
            if (0 != (wChannelConfig & 8)) {
                sb.Append("LFE ");
            }
            if (0 != (wChannelConfig & 16)) {
                sb.Append("LS ");
            }
            if (0 != (wChannelConfig & 32)) {
                sb.Append("RS ");
            }
            if (0 != (wChannelConfig & 64)) {
                sb.Append("LC ");
            }
            if (0 != (wChannelConfig & 128)) {
                sb.Append("RC ");
            }
            if (0 != (wChannelConfig & 256)) {
                sb.Append("S ");
            }
            if (0 != (wChannelConfig & 512)) {
                sb.Append("SL ");
            }
            if (0 != (wChannelConfig & 1024)) {
                sb.Append("SR ");
            }
            if (0 != (wChannelConfig & 2048)) {
                sb.Append("T ");
            }
            return sb.ToString();
        }

        private string BmChannelConfigToStr(uint b) {
            var sb = new StringBuilder();
            if (0 != (b & (1<<0))) {
                sb.Append("FL ");
            }
            if (0 != (b & (1 << 1))) {
                sb.Append("FR ");
            }
            if (0 != (b & (1 << 2))) {
                sb.Append("FC ");
            }
            if (0 != (b & (1 << 3))) {
                sb.Append("LFE ");
            }
            if (0 != (b & (1 << 4))) {
                sb.Append("BL ");
            }
            if (0 != (b & (1 << 5))) {
                sb.Append("BR ");
            }
            if (0 != (b & (1 << 6))) {
                sb.Append("FLC ");
            }
            if (0 != (b & (1 << 7))) {
                sb.Append("FRC ");
            }
            if (0 != (b & (1 << 8))) {
                sb.Append("BC ");
            }
            if (0 != (b & (1 << 9))) {
                sb.Append("SL ");
            }
            if (0 != (b & (1 << 10))) {
                sb.Append("SR ");
            }
            if (0 != (b & (1 << 11))) {
                sb.Append("TC ");
            }
            if (0 != (b & (1 << 12))) {
                sb.Append("TFL ");
            }
            if (0 != (b & (1 << 13))) {
                sb.Append("TFC ");
            }
            if (0 != (b & (1 << 14))) {
                sb.Append("TFR ");
            }
            if (0 != (b & (1 << 15))) {
                sb.Append("TBL ");
            }
            if (0 != (b & (1 << 16))) {
                sb.Append("TBC ");
            }
            if (0 != (b & (1 << 17))) {
                sb.Append("TBR ");
            }
            if (0 != (b & (1 << 18))) {
                sb.Append("TFLC ");
            }
            if (0 != (b & (1 << 19))) {
                sb.Append("TFRC ");
            }
            if (0 != (b & (1 << 20))) {
                sb.Append("LLFE ");
            }
            if (0 != (b & (1 << 21))) {
                sb.Append("RLFE ");
            }
            if (0 != (b & (1 << 22))) {
                sb.Append("TSL ");
            }
            if (0 != (b & (1 << 23))) {
                sb.Append("TSR ");
            }
            if (0 != (b & (1 << 24))) {
                sb.Append("BC ");
            }
            if (0 != (b & (1 << 25))) {
                sb.Append("BLC ");
            }
            if (0 != (b & (1 << 26))) {
                sb.Append("BRC ");
            }
            if (0 != (b & (1 << 31))) {
                sb.Append("Raw_Data ");
            }
            return sb.ToString();
        }

        private const int AUDIO_FORMAT_TYPE_1_UNDEFINED = 0x0000;
        private const int AUDIO_FORMAT_PCM = 1;
        private const int AUDIO_FORMAT_PCM8 = 2;
        private const int AUDIO_FORMAT_IEEE_FLOAT = 3;
        private const int AUDIO_FORMAT_ALAW = 4;
        private const int AUDIO_FORMAT_MULAW = 5;
        private const int AUDIO_FORMAT_TYPE_2_UNDEFINED = 0x1000;
        private const int AUDIO_FORMAT_MPEG = 0x1001;
        private const int AUDIO_FORMAT_AC3 = 0x1002;
        private const int AUDIO_FORMAT_TYPE_3_UNDEFINED = 0x2000;
        private const int AUDIO_FORMAT_IEC1937AC3 = 0x2001;
        private const int AUDIO_FORMAT_IEC1937MPEG1Layer1 = 0x2002;
        private const int AUDIO_FORMAT_IEC1937MPEG1Layer2 = 0x2003;
        private const int AUDIO_FORMAT_IEC1937MPEG2Ext = 0x2004;
        private const int AUDIO_FORMAT_IEC1937MPEG2Layer1LS = 0x2005;
        private const int AUDIO_FORMAT_IEC1937MPEG2Layer2LS = 0x2006;


        private string WFormatTagToStr(int f) {
            switch (f) {
            case AUDIO_FORMAT_TYPE_1_UNDEFINED: return "FormatType1 Undefined";
            case AUDIO_FORMAT_TYPE_2_UNDEFINED: return "FormatType2 Undefined";
            case AUDIO_FORMAT_TYPE_3_UNDEFINED: return "FormatType3 Undefined";
            case AUDIO_FORMAT_PCM: return "PCM";
            case AUDIO_FORMAT_PCM8: return "PCM8";
            case AUDIO_FORMAT_IEEE_FLOAT: return "IEEE-FLOAT";
            case AUDIO_FORMAT_ALAW: return "ALAW";
            case AUDIO_FORMAT_MULAW: return "MULAW";
            case AUDIO_FORMAT_MPEG: return "MPEG";
            case AUDIO_FORMAT_AC3: return "AC-3";
            case AUDIO_FORMAT_IEC1937AC3: return "IEC1937 AC-3";
            case AUDIO_FORMAT_IEC1937MPEG1Layer1: return "IEC1937 MPEG-1 Layer1";
            case AUDIO_FORMAT_IEC1937MPEG1Layer2: return "IEC1937 MPEG-1 Layer2/3 or MPEG-2 NOEXT";
            case AUDIO_FORMAT_IEC1937MPEG2Ext: return "IEC1937 MPEG-2 EXT";
            case AUDIO_FORMAT_IEC1937MPEG2Layer1LS: return "IEC1937 MPEG-2 Layer1 LS";
            case AUDIO_FORMAT_IEC1937MPEG2Layer2LS: return "IEC1937 MPEG-2 Layer2/3 LS";
            default: return string.Format("Unknown({0:X4})", f);
            }
        }

        private string BmFormatsToStr(ulong b, int formatType) {
            var sb = new StringBuilder();

            if (0 != (b & (1<<0))) {
                sb.Append(" PCM");
            }
            if (0 != (b & (1 << 1))) {
                sb.Append(" PCM8");
            }
            if (0 != (b & (1 << 2))) {
                sb.Append(" IEEE_FLOAT");
            }
            if (0 != (b & (1 << 3))) {
                sb.Append(" ALAW");
            }
            if (0 != (b & (1 << 4))) {
                sb.Append(" MULAW");
            }
            if (0 != (b & (1 << 5))) {
                sb.Append(" DSD");
            }
            if (0 != (b & (1 << 6))) {
                sb.Append(" RAW_DATA");
            }

            if (FORMAT_TYPE_1 == formatType) {
                // Universal Serial Bus Device Class Definition for Audio Data Formats Appendix A.2.1
                if (0 != (b & (1U <<31))) {
                    sb.Append(" TYPE_I_RAW_DATA");
                }
            }

            if (FORMAT_TYPE_3 <= formatType) {
                if (0 != (b & (1 << 7))) {
                    sb.Append(" PCM_IEC60958");
                }
                if (0 != (b & (1 << 8))) {
                    sb.Append(" AC-3");
                }
                if (0 != (b & (1 << 9))) {
                    sb.Append(" MPEG-1_Layer1");
                }
                if (0 != (b & (1 << 10))) {
                    sb.Append(" MPEG-1_Layer2/3_or_MPEG-2_NOEXT");
                }
                if (0 != (b & (1 << 11))) {
                    sb.Append(" MPEG-2_EXT");
                }
                if (0 != (b & (1 << 12))) {
                    sb.Append(" MPEG-2_AAC_ADTS");
                }
                if (0 != (b & (1 << 13))) {
                    sb.Append(" MPEG-2_Layer1_LS");
                }
                if (0 != (b & (1 << 14))) {
                    sb.Append(" MPEG-2_Layer2/3_LS");
                }
                if (0 != (b & (1 << 15))) {
                    sb.Append(" DTS-I");
                }
                if (0 != (b & (1 << 16))) {
                    sb.Append(" DTS-II");
                }
                if (0 != (b & (1 << 17))) {
                    sb.Append(" DTS-III");
                }
                if (0 != (b & (1 << 18))) {
                    sb.Append(" ATRAC");
                }
                if (0 != (b & (1 << 19))) {
                    sb.Append(" ATRAC2/3");
                }
                if (0 != (b & (1 << 20))) {
                    sb.Append(" WMA");
                }
                if (0 != (b & (1 << 21))) {
                    sb.Append(" E-AC-3");
                }
                if (0 != (b & (1 << 22))) {
                    sb.Append(" MAT");
                }
                if (0 != (b & (1 << 23))) {
                    sb.Append(" DTS-IV");
                }
                if (0 != (b & (1 << 24))) {
                    sb.Append(" MPEG-4_HE_AAC");
                }
                if (0 != (b & (1 << 25))) {
                    sb.Append(" MPEG-4_HE_AAC_V2");
                }
                if (0 != (b & (1 << 26))) {
                    sb.Append(" MPEG-4_AAC_LC");
                }
                if (0 != (b & (1 << 27))) {
                    sb.Append(" DRA");
                }
                if (0 != (b & (1 << 28))) {
                    sb.Append(" MPEG-4_HE_AAC_SURROUND");
                }
                if (0 != (b & (1 << 29))) {
                    sb.Append(" MPEG-4_AAC_LC_SURROUND");
                }
                if (0 != (b & (1 << 30))) {
                    sb.Append(" MPEG-H_3D_AUDIO");
                }
                if (0 != (b & (1U << 31))) {
                    sb.Append(" AC4");
                }
                if (0 != (b & (1UL << 32))) {
                    sb.Append(" MPEG-4_AAC_ELD");
                }
            }
            return sb.ToString();
        }

        private const int FORMAT_TYPE_1 = 0x1;
        private const int FORMAT_TYPE_2 = 0x2;
        private const int FORMAT_TYPE_3 = 0x3;

        private const int AUDIO_FUNCTION_CATEGORY_FUNCTION_SUBCLASS_UNDEFINED = 0x00;
        private const int AUDIO_FUNCTION_CATEGORY_DESKTOP_SPEAKER = 0x01;
        private const int AUDIO_FUNCTION_CATEGORY_HOME_THEATER = 0x02;
        private const int AUDIO_FUNCTION_CATEGORY_MICROPHONE = 0x03;
        private const int AUDIO_FUNCTION_CATEGORY_HEADSET = 0x04;
        private const int AUDIO_FUNCTION_CATEGORY_TELEPHONE = 0x05;
        private const int AUDIO_FUNCTION_CATEGORY_CONVERTER = 0x06;
        private const int AUDIO_FUNCTION_CATEGORY_VOICE_SOUND_RECORDER = 0x07;
        private const int AUDIO_FUNCTION_CATEGORY_IO_BOX = 0x08;
        private const int AUDIO_FUNCTION_CATEGORY_MUSICAL_INSTRUMENT = 0x09;
        private const int AUDIO_FUNCTION_CATEGORY_PRO_AUDIO = 0x0A;
        private const int AUDIO_FUNCTION_CATEGORY_AUDIO_VIDEO = 0x0B;
        private const int AUDIO_FUNCTION_CATEGORY_CONTROL_PANEL = 0x0C;
        private const int AUDIO_FUNCTION_CATEGORY_OTHER = 0xFF;

        private string AudioFunctionCategoryToStr(int a) {
            switch (a) {
            case AUDIO_FUNCTION_CATEGORY_FUNCTION_SUBCLASS_UNDEFINED: return "FUNCTION_SUBCLASS_UNDEFINED";
            case AUDIO_FUNCTION_CATEGORY_DESKTOP_SPEAKER: return "Desktop Speaker";
            case AUDIO_FUNCTION_CATEGORY_HOME_THEATER: return "Home Theater";
            case AUDIO_FUNCTION_CATEGORY_MICROPHONE: return "Microphone";
            case AUDIO_FUNCTION_CATEGORY_HEADSET: return "Headset";
            case AUDIO_FUNCTION_CATEGORY_TELEPHONE: return "Telephone";
            case AUDIO_FUNCTION_CATEGORY_CONVERTER: return "Converter";
            case AUDIO_FUNCTION_CATEGORY_VOICE_SOUND_RECORDER: return "Voice/Sound recorder";
            case AUDIO_FUNCTION_CATEGORY_IO_BOX: return "I/O box";
            case AUDIO_FUNCTION_CATEGORY_MUSICAL_INSTRUMENT: return "Musical instrument";
            case AUDIO_FUNCTION_CATEGORY_PRO_AUDIO: return "Pro audio";
            case AUDIO_FUNCTION_CATEGORY_AUDIO_VIDEO: return "Audio video";
            case AUDIO_FUNCTION_CATEGORY_CONTROL_PANEL: return "Control panel";
            case AUDIO_FUNCTION_CATEGORY_OTHER: return "Other audio function category";
            default: return string.Format("Unknown Audio function category 0x{0:X2}", a);
            }
        }

        private const int AUDIO_FUNCTION_CLASS_AUDIO = 1;
        private const int AUDIO_FUNCTION_SUBCLASS_UNDEFINED = 0;
        private const int AUDIO_FUNCTION_PROTOCOL_UNDEFINED = 0;
        private const int AUDIO_FUNCTION_PROTOCOL_20 = 0x20;

        private string AudioFunctionClassToStr(int f) {
            switch (f) {
            case AUDIO_FUNCTION_CLASS_AUDIO: return "Audio";
            default: return string.Format("Unknown(0x{0:X})", f);
            }
        }

        private const int AUDIO_CLOCK_SOURCE_EXTERNAL = 0;
        private const int AUDIO_CLOCK_SOURCE_FIXED_FREQ = 1;
        private const int AUDIO_CLOCK_SOURCE_VARIABLE_FREQ = 2;
        private const int AUDIO_CLOCK_SOURCE_PROGRAMMABLE_FREQ = 3;

        private string AudioClockSourceAttrToClockTypeStr(int bmAttributes, int bmControls) {
            var sb = new StringBuilder();

            switch (bmAttributes & 0x3) {
            case AUDIO_CLOCK_SOURCE_EXTERNAL: sb.Append("External clock"); break;
            case AUDIO_CLOCK_SOURCE_FIXED_FREQ:
            default:
                sb.Append("Fixed clock"); break;
            case AUDIO_CLOCK_SOURCE_VARIABLE_FREQ: sb.Append("Variable frequency clock"); break;
            case AUDIO_CLOCK_SOURCE_PROGRAMMABLE_FREQ: sb.Append("Programmable frequency clock"); break;
            }

            if (0 !=(bmAttributes & 0x4)) {
                sb.Append(", Sync to SOF");
            } else {
                sb.Append(", Free running");
            }

            switch (bmControls&0x3) {
            case 0: sb.Append(", Frequency not programmable"); break;
            case 1: sb.Append(", Frequency host readable"); break;
            case 3: sb.Append(", Frequency host programmable"); break;
            default: break;
            }
            switch ((bmControls>>2) & 0x3) {
            case 0: sb.Append(", Clock validity not programmable"); break;
            case 1: sb.Append(", Clock validity host readable"); break;
            case 3: sb.Append(", Clock validity host programmable"); break;
            default: break;
            }

            return sb.ToString();
        }

        private string AudioClockSelectorCotrolsToStr(int bmControls) {
            var sb = new StringBuilder();
            switch (bmControls & 0x3) {
            case 0: sb.Append("Not programmable"); break;
            case 1: sb.Append("Host readable"); break;
            case 3: sb.Append("Host programmable"); break;
            default: break;
            }
            return sb.ToString();
        }

        private string AudioClockMultiplierCotrolsToStr(int bmControls) {
            var sb = new StringBuilder();
            switch (bmControls & 0x3) {
            case 0: sb.Append("Numerator Not programmable"); break;
            case 1: sb.Append("Numerator Host readable"); break;
            case 3: sb.Append("Numerator Host programmable"); break;
            default: break;
            }
            switch ((bmControls>>2) & 0x3) {
            case 0: sb.Append("Denominator Not programmable"); break;
            case 1: sb.Append("Denominator Host readable"); break;
            case 3: sb.Append("Denominator Host programmable"); break;
            default: break;
            }
            return sb.ToString();
        }

        private string AudioControlFeatureControlsToStr(uint b) {
            var sb = new StringBuilder();
            if ((b & 0x3) == 3) {
                sb.Append(", Mute");
            }
            if (((b >> 2) & 0x3) == 3) {
                sb.Append(", VolumeControl");
            }
            if (((b >> 4) & 0x3) == 3) {
                sb.Append(", BassControl");
            }
            if (((b >> 6) & 0x3) == 3) {
                sb.Append(", MidControl");
            }
            if (((b >> 8) & 0x3) == 3) {
                sb.Append(", TrebleControl");
            }
            if (((b >> 10) & 0x3) == 3) {
                sb.Append(", GraphicsEqualizerControl");
            }
            if (((b >> 12) & 0x3) == 3) {
                sb.Append(", AutoGainControl");
            }
            if (((b >> 14) & 0x3) == 3) {
                sb.Append(", DelayControl");
            }
            if (((b >> 16) & 0x3) == 3) {
                sb.Append(", BassBoostControl");
            }
            if (((b >> 18) & 0x3) == 3) {
                sb.Append(", LoudnessControl");
            }
            if (((b >> 20) & 0x3) == 3) {
                sb.Append(", InputGainControl");
            }
            if (((b >> 22) & 0x3) == 3) {
                sb.Append(", InputGainPadControl");
            }
            if (((b >> 24) & 0x3) == 3) {
                sb.Append(", PhaseInvertControl");
            }
            if (((b >> 26) & 0x3) == 3) {
                sb.Append(", UnderflowControl");
            }
            if (((b >> 28) & 0x3) == 3) {
                sb.Append(", OverflowControl");
            }

            string s = sb.ToString();
            if (s.Length == 0) {
                return ", Uncontrollable from host";
            } else {
                return s;
            }

        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        public string Read(WWUsbHubPortCs hp) {
            mHp = hp;
            var buff = hp.confDesc;
            mSds = hp.stringDescList;
            mSpeed = (UsbDeviceTreeCs.BusSpeed)hp.speed;

            mSB.Clear();

            int offs = 0;

            for (; ; ) {
                if (buff == null || buff.Length - offs < 2) {
                    return mSB.ToString();
                }

                var commD = new UsbCommonDescriptor(buff, offs);
                switch (commD.descType) {
                case UsbConfDescType.Configuration:
                    ReadConfDesc(buff, offs);
                    break;
                case UsbConfDescType.Interface:
                    ReadInterfaceDesc(buff, offs);
                    interfaceClass = buff[offs + 5];
                    interfaceSubClass = buff[offs + 6];
                    interfaceProtocol = buff[offs + 7];
                    break;
                case UsbConfDescType.Endpoint:
                    ReadEndpointDesc(buff, offs);
                    break;
                case UsbConfDescType.InterfaceAssociation:
                    ReadInterfaceAssocDesc(buff, offs);
                    break;
                default:
                    switch (interfaceClass) {
                    case DEVICE_CLASS_AUDIO:
                        ReadAudioDesc(interfaceSubClass, buff, offs);
                        break;
                    }
                    break;
                }

                offs += commD.bytes;
            }
        }
        private void ReadConfDesc(byte[] buff, int offs) {
            /*
            0 UCHAR bLength;
            1 UCHAR bDescriptorType;
            2 USHORT wTotalLength;
            4 UCHAR bNumInterfaces;
            5 UCHAR bConfigurationValue;
            6 UCHAR iConfiguration;
            7 UCHAR bmAttributes;
            8 UCHAR MaxPower;
            */
            int length = buff[offs];
            if (length < 9) {
                return;
            }

            int bConfigurationValue = buff[offs + 5];
            int iConfiguration = buff[offs + 6];
            int bmAttributes = buff[offs + 7];
            int maxPower = buff[offs + 8];

            var s = FindString(iConfiguration);

            // maxPowerAを計算。
            int maxPowerA;
            if (BusSpeed.SuperSpeed <= mSpeed) {
                maxPowerA = maxPower * 8;
            } else {
                maxPowerA = maxPower * 2;
            }

            // バスパワーかどうか。
            string power = "";
            if (mSpeed <= BusSpeed.FullSpeed) {
                if (0 != (USB_CONFIG_BUS_POWERED & bmAttributes)) {
                    power = "Bus powered,";
                }
                if (0 != (USB_CONFIG_SELF_POWERED & bmAttributes)) {
                    power = "Self powered,";
                }
            } else {
                if (0 != (USB_CONFIG_SELF_POWERED & bmAttributes)) {
                    power = "Self powered,";
                } else {
                    power = "Bus powered,";
                }
            }
            if (0 != (USB_CONFIG_REMOTE_WAKEUP & bmAttributes)) {
                power += "Remote wakeup,";
            }

            mSB.AppendFormat("    {0} {1} Max power={2}mA", s, power, maxPowerA);
        }
        private void ReadInterfaceDesc(byte[] buff, int offs) {
            /* USB Device Class Definition for Audio Devices 4.7.1 Standard AC Interface Descriptor
            0 UCHAR   bLength;
            1 UCHAR   bDescriptorType;
            2 UCHAR   bInterfaceNumber;
            3 UCHAR   bAlternateSetting;
            4 UCHAR   bNumEndpoints;
            5 UCHAR   bInterfaceClass;
            6 UCHAR   bInterfaceSubClass;
            7 UCHAR   bInterfaceProtocol;
            8 UCHAR   iInterface;
            */
            int length = buff[offs];
            if (length < 9) {
                return;
            }

            int interfaceNr = buff[offs + 2];
            int altSet = buff[offs + 3];
            int numEP = buff[offs + 4];
            int intClass = buff[offs + 5];
            int intSubClass = buff[offs + 6];
            int intProto = buff[offs + 7];
            int iInterface = buff[offs + 8];

            string name = FindString(iInterface);
            if (0 < name.Length) {
                name = ": " + name;
            }

            string sIntClass = InterfaceClassToStr(intClass, intSubClass, intProto);

            mSB.AppendFormat("\n      Interface {0}:{1} {2} {3}", interfaceNr, altSet, sIntClass, name);
        }

        private void ReadInterfaceAssocDesc(byte [] buff, int offs) {
            /* USB 2.0 ECN Interface Association Descriptors Table 9-Z
            0 UCHAR bLength
            1 UCHAR bDescriptorType 
            2 UCHAR bFirstInterface 1 
            3 UCHAR bInterfaceCount 1 
            4 UCHAR bFunctionClass    USB Audio Class 2.0 Table A-1
            5 UCHAR bFunctionSubClass USB Audio Class 2.0 Table A-2
            6 UCHAR bFunctionProtocol USB Audio Class 2.0 Table A-3
            7 UCHAR iFunction
            */
            int length = buff[offs];
            if (length != 8) {
                mSB.AppendFormat("\n      Interface Association Descriptor of unknown size 0x{0:X2}", length);
                return;
            }

            int bFirstInterface = buff[offs + 2];
            int bInterfaceCount = buff[offs + 3];
            int bFunctionClass = buff[offs + 4];
            int bFunctionSubClass = buff[offs + 5];
            int bFunctionProtocol = buff[offs + 6];
            int iFunction = buff[offs + 7];
            string sFuncClass = AudioFunctionClassToStr(bFunctionClass);
            string name = FindString(iFunction);
            mSB.AppendFormat("\n      Interface association, interface #{0} to {1} is {2}, ProtocolVersion={3:X}",
                bFirstInterface, bFirstInterface+bInterfaceCount-1, sFuncClass, bFunctionProtocol);
            if (0 < name.Length) {
                mSB.AppendFormat(" ({0})", name);
            }

        }

        private void ReadEndpointDesc(byte[] buff, int offs) {
            /*
            0 UCHAR   bLength;
            1 UCHAR   bDescriptorType;
            2 UCHAR   bEndpointAddress;
            3 UCHAR   bmAttributes;
            4 USHORT  wMaxPacketSize;
            6 UCHAR   bInterval;
            */
            int length = buff[offs];
            if (length < 7) {
                return;
            }


            int bEpAddr = buff[offs + 2];
            int bmAttributes = buff[offs + 3];
            int maxPacket = 0x7ff & BitConverter.ToUInt16(buff, offs + 4);
            int interval = 0;
            int syncAddr = -1;
            if (7 == length) {
                interval = buff[offs + 6];
            } else if (9 == length) {
                // ENDPOINT DESCRIPTOR 2
                /*
                0 UCHAR   bLength;
                1 UCHAR   bDescriptorType;
                2 UCHAR   bEndpointAddress;
                3 UCHAR   bmAttributes;
                4 USHORT  wMaxPacketSize;
                6 USHORT  wInterval;
                8 UCHAR   bSyncAddress;
                */
                interval = BitConverter.ToUInt16(buff, offs + 6);
                syncAddr = buff[offs + 8];
            } else {
                mSB.AppendFormat("\n        Unknown size of EndpointDesc {0:X4}", length);
                return;
            }

            int epAddr = bEpAddr & ENDPOINT_ADDRESS_MASK;
            bool epIsInput = 0 != (bEpAddr & ENDPOINT_DIRECTION_MASK);

            mSB.AppendFormat("\n        {0} {1}: ", epIsInput ? "Input Endpoint" : "Output Endpoint", epAddr);

            double unit = 0.125;
            if (mSpeed <= BusSpeed.FullSpeed) {
                unit = 1;
            }

            int epType = bmAttributes & ENDPOINT_TYPE_MASK;
            switch (epType) {
            case ENDPOINT_TYPE_CONTROL:
                mSB.Append("Control");
                break;
            case ENDPOINT_TYPE_ISOCHRONOUS:
                mSB.Append("Isochronous ");
                switch (ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_MASK & bmAttributes) {
                case ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_NO_SYNCHRONIZATION: mSB.Append("No sync "); break;
                case ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_ASYNCHRONOUS: mSB.Append("Asynchronous "); break;
                case ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_ADAPTIVE: mSB.Append("Adaptive "); break;
                case ENDPOINT_TYPE_ISOCHRONOUS_SYNCHRONIZATION_SYNCHRONOUS: mSB.Append("Synchronous "); break;
                default: break;
                }
                switch (ENDPOINT_TYPE_ISOCHRONOUS_USAGE_MASK & bmAttributes) {
                case ENDPOINT_TYPE_ISOCHRONOUS_USAGE_DATA_ENDOINT: mSB.Append("Data"); break;
                case ENDPOINT_TYPE_ISOCHRONOUS_USAGE_FEEDBACK_ENDPOINT: mSB.Append("Feedback"); break;
                case ENDPOINT_TYPE_ISOCHRONOUS_USAGE_IMPLICIT_FEEDBACK_DATA_ENDPOINT: mSB.Append("Implicit Feedback"); break;
                case ENDPOINT_TYPE_ISOCHRONOUS_USAGE_RESERVED: mSB.Append("Reserved"); break;
                default: break;
                }
                mSB.AppendFormat(" Interval={0}ms", Math.Pow(2, (0x1f & interval) - 1) * unit);
                break;
            case ENDPOINT_TYPE_BULK:
                mSB.Append("Bulk");
                break;
            case ENDPOINT_TYPE_INTERRUPT:
                mSB.Append("Interrupt, ");
                switch (U30_ENDPOINT_TYPE_INTERRUPT_USAGE_MASK & bmAttributes) {
                case U30_ENDPOINT_TYPE_INTERRUPT_USAGE_PERIODIC: mSB.Append("Usage: Periodic"); break;
                case U30_ENDPOINT_TYPE_INTERRUPT_USAGE_NOTIFICATION: mSB.Append("Usage: Notification"); break;
                case U30_ENDPOINT_TYPE_INTERRUPT_USAGE_RESERVED10:
                case U30_ENDPOINT_TYPE_INTERRUPT_USAGE_RESERVED11:
                default:
                    mSB.Append("Usage: Reserved"); break;
                }
                if (mSpeed <= BusSpeed.FullSpeed) {
                    mSB.AppendFormat(" Interval={0}ms", interval);
                } else {
                    mSB.AppendFormat(" Interval={0}ms", Math.Pow(2, (0x1f & interval) - 1) * unit);
                }
                break;
            default:
                break;
            }
            mSB.AppendFormat(" Max {0} bytes", maxPacket);
        }

        private void ReadAudioDesc(int interfaceSubClass, byte[] buff, int offs) {
            int length = buff[offs + 0];
            int descType = buff[offs + 1];
            int descSubType = buff[offs + 2];

            switch (descType) {
            case AUDIO_CS_INTERFACE:
                switch (interfaceSubClass) {
                case AUDIO_SUBCLASS_AUDIOCONTROL:
                    switch (descSubType) {
                    case AUDIO_AC_HEADER:
                        ReadAudioControlInterfaceHeaderDesc(buff, offs);
                        break;
                    case AUDIO_AC_INPUT_TERMINAL:
                        ReadAudioControlInputTerminal(buff, offs);
                        break;
                    case AUDIO_AC_OUTPUT_TERMINAL:
                        ReadAudioControlOutputTerminal(buff, offs);
                        break;
                    case AUDIO_AC_MIXER_UNIT:
                        ReadAudioControlMixerUnit(buff, offs);
                        break;
                    case AUDIO_AC_SELECTOR_UNIT:
                        ReadAudioControlSelectorUnit(buff, offs);
                        break;
                    case AUDIO_AC_FEATURE_UNIT:
                        ReadAudioControlFeatureUnit(buff, offs);
                        break;
                    case AUDIO_AC2_EFFECT_UNIT:
                        switch (mAudioClass) {
                        case USBAudioClass.AC1:
                            ReadAudioControl1ProcessingUnit(buff, offs);
                            break;
                        case USBAudioClass.AC2: 
                            ReadAudioControl2EffectUnit(buff, offs);
                            break;
                        default:
                            break;
                        }
                        break;
                    case AUDIO_AC2_PROCESSING_UNIT:
                        switch (mAudioClass) {
                        case USBAudioClass.AC1:
                            ReadAudioControl1ExtensionUnit(buff, offs);
                            break;
                        case USBAudioClass.AC2:
                            ReadAudioControl2ProcessingUnit(buff, offs);
                            break;
                        default:
                            break;
                        }
                        break;
                    case AUDIO_AC2_EXTENSION_UNIT:
                        ReadAudioControl2ExtensionUnit(buff, offs);
                        break;
                    case AUDIO_AC2_CLOCK_SOURCE:
                        ReadAudioControlClockSource(buff, offs);
                        break;
                    case AUDIO_AC2_CLOCK_SELECTOR:
                        ReadAudioControlClockSelector(buff, offs);
                        break;
                    case AUDIO_AC2_CLOCK_MULTIPLIER:
                        ReadAudioControlClockMultiplier(buff, offs);
                        break;
                    case AUDIO_AC2_SAMPLE_RATE_CONVERTER:
                        ReadAudioControl2SampleRateConverter(buff, offs);
                        break;
                    default:
                        ReadAudioControlOther(buff, offs);
                        break;
                    }
                    break;
                case AUDIO_SUBCLASS_AUDIOSTREAMING:
                    switch (descSubType) {
                    case AUDIO_AS_INTERFACE:
                        ReadAudioStreamingInterfaceDesc(buff, offs);
                        break;
                    case AUDIO_AS_FORMAT_TYPE:
                        ReadAudioStreamingFormatTypeDesc(buff, offs);
                        break;
                    default:
                        ReadAudioStreamingOtherDesc(buff, offs);
                        break;
                    }
                    break;
                case AUDIO_SUBCLASS_MIDISTREAMING:
                    break;
                }
                break;
            case AUDIO_CS_ENDPOINT:
                ReadAudioCSEndpoint(buff, offs);
                break;
            default:
                ReadAudioOther(buff, offs);
                break;
            }
        }

        private void ReadAudioControlInterfaceHeaderDesc(byte [] buff, int offs) {
            int length = buff[offs + 0];
            if (length < 9) {
                return;
            }

            int bcdADC = BitConverter.ToUInt16(buff, offs + 3);

            int majorVersion = (0xf & (bcdADC >> 12)) * 10 + (0xf & (bcdADC >> 8));
            int minorVersion = (0xf & (bcdADC >> 4)) * 10 + (0xf & (bcdADC >> 0));

            if (majorVersion == 2) {
                /* Audio Class 2 Table 4-5
                0 UCHAR bLength
                1 UCHAR bDescriptorType
                2 UCHAR bDescriptorSubType
                3 USHORT bcdADC
                5 UCHAR bCategory
                6 USHORT wTotalLength
                8 UCHAR bmControls
                */
                int bCategory = buff[offs + 5];
                string sCategory = AudioFunctionCategoryToStr(bCategory);

                mSB.AppendFormat("\n          USB Audio Class {0}.{1}, {2}", majorVersion, minorVersion, sCategory);
                mAudioClass = USBAudioClass.AC2;
            } else if (majorVersion == 1) {
                /* Audio Class 1 C.3.3.2 Class-specific Interface Descriptor
                0 UCHAR bLength = 10
                1 UCHAR bDescriptorType = 0x24
                2 UCHAR bDescriptorSubType = 0x01
                3 USHORT bcdADC = 0x0100
                5 USHORT wTotalLength
                7 UCHAR bInCollection  (2のときbaInterfaceNr2まで)
                8 baInerfaceNr1 AudioStreaming interface 1 belongs to this AC interface
                9 baInerfaceNr2 AudioStreaming interface 1 belongs to this AC interface
                */

                int bInCollection = buff[offs + 7];
                if (length-8 < bInCollection) {
                    bInCollection = length - 8;
                }

                mSB.AppendFormat("\n          USB Audio Class {0}.{1} ", majorVersion, minorVersion);
                if (1 <= bInCollection) {
                    mSB.AppendFormat("Audio Streaming Nr. ");
                }
                for (int i = 0; i < bInCollection; ++i) {
                    mSB.AppendFormat("{0} ", buff[offs + 8 + i]);
                }
                mAudioClass = USBAudioClass.AC1;
            } else {
                mSB.AppendFormat("\n          USB Audio Class {0:x4}", bcdADC);
            }
        }

        private void ReadAudioControlInputTerminal(byte[] buff, int offs) {
            int length = buff[offs + 0];
            if (length < 12) {
                return;
            }

            if (length == 12) {
                // Audio Class 1
                /*
                0 UCHAR  bLength;
                1 UCHAR  bDescriptorType;
                2 UCHAR  bDescriptorSubtype;
                3 UCHAR  bTerminalID;
                4 USHORT wTerminalType;
                6 UCHAR  bAssocTerminal;
                7 UCHAR  bNrChannels;
                8 USHORT wChannelConfig;
                10 UCHAR  iChannelNames;
                11 UCHAR  iTerminal;
                */

                int terminalID = buff[offs + 3];
                int terminalType = BitConverter.ToUInt16(buff, offs + 4);
                string terminalTypeStr = TerminalTypeToStr(terminalType);
                int ch = buff[offs + 7];
                int assocT = buff[offs + 6];
                int iTerminal = buff[offs + 11];
                string sTerm = FindString(iTerminal);
                if (sTerm.Length == 0 && terminalType == USB_TERMINAL_TYPE_Streaming) {
                    sTerm = Properties.Resources.StreamForPlayback;
                }

                int wChannelConfig = BitConverter.ToUInt16(buff, offs + 8);
                string wChannelConfigStr = WChannelConfigToStr(wChannelConfig);

                mSB.AppendFormat("\n          Input Terminal {0} : {1} {2}ch ({3}) {4}", terminalID, terminalTypeStr, ch, wChannelConfigStr, sTerm);
            } else if (length == 17) {
                // Audio Class 2
                /* Table 4-9: Input Terminal Descriptor
                0 UCHAR bLength
                1 UCHAR bDescriptorType
                2 UCHAR bDescriptorSubtype
                3 UCHAR bTerminalID
                4 USHORT wTerminalType
                6 UCHAR bAssocTerminal
                7 UCHAR bCSourceID
                8 UCHAR bNrChannels
                9 UINT bmChannelConfig
                13 UCHAR iChannelNames
                14 USHORT bmControls
                16 UCHAR iTerminal
                */
                int terminalID = buff[offs + 3];
                int terminalType = BitConverter.ToUInt16(buff, offs + 4);
                string terminalTypeStr = TerminalTypeToStr(terminalType);
                int assocOutTerminal = buff[offs + 6];
                int bCSourceId = buff[offs + 7];
                int ch = buff[offs + 8];
                uint bmChConf = BitConverter.ToUInt32(buff, offs + 9);
                string chConfStr = BmChannelConfigToStr(bmChConf);
                int iChannelNames = buff[offs + 13];
                string sChNames = FindString(iChannelNames);
                int iTerminal = buff[offs + 16];
                string sTerm = FindString(iTerminal);
                if (sTerm.Length == 0 && terminalType == USB_TERMINAL_TYPE_Streaming) {
                    sTerm = Properties.Resources.StreamForPlayback;
                }

                mSB.AppendFormat("\n          Input Terminal {0} {1} : {2}ch ({3}) {4} assocOutTerminal={5} clockSource={6} {7}",
                    terminalID, terminalTypeStr, ch, chConfStr, sChNames, assocOutTerminal, bCSourceId, sTerm);

            } else {
                mSB.AppendFormat("\n          Unknown AudioControl input terminal of size 0x{0:X4}", length);
            }
        }

        private void ReadAudioControlOutputTerminal(byte[] buff, int offs) {

            int length = buff[offs + 0];
            if (length < 9) {
                return;
            }

            if (length == 9) {
                // USB Audio Class 1
                /*
                0 UCHAR  bLength;
                1 UCHAR  bDescriptorType;
                2 UCHAR  bDescriptorSubtype;
                3 UCHAR  bTerminalID;
                4 USHORT wTerminalType;
                6 UCHAR  bAssocTerminal;
                7 UCHAR  bSourceID;
                8 UCHAR  iTerminal;
                */
                int terminalID = buff[offs + 3];
                int assocT = buff[offs + 6];
                int sourceID = buff[offs + 7];
                int terminalType = BitConverter.ToUInt16(buff, offs + 4);
                string terminalTypeStr = TerminalTypeToStr(terminalType);
                int iTerminal = buff[offs + 8];
                string sTerm = FindString(iTerminal);
                if (sTerm.Length == 0 && terminalType == USB_TERMINAL_TYPE_Streaming) {
                    sTerm = Properties.Resources.StreamForRecording;
                }

                mSB.AppendFormat("\n          Output Terminal {0} : ClockSourceID={1}, {2} {3}",
                    terminalID, sourceID, terminalTypeStr, sTerm);
            } else if (length == 12) {
                // USB Audio Class 2
                /*
                0 UCHAR bLength
                1 UCHAR bDescriptorType
                2 UCHAR bDescriptorSubtype
                3 UCHAR bTerminalID
                4 USHORT wTerminalType
                6 UCHAR bAssocTerminal
                7 UCHAR bSourceID
                8 UCHAR bCSourceID
                9 USHORT bmControls
                11 UCHAR iTerminal
                */
                int terminalID = buff[offs + 3];
                int terminalType = BitConverter.ToUInt16(buff, offs + 4);
                string terminalTypeStr = TerminalTypeToStr(terminalType);
                int assocT = buff[offs + 6];
                int sourceID = buff[offs + 7];
                int bCSourceID = buff[offs + 8];
                int iTerminal = buff[offs + 11];
                string sTerm = FindString(iTerminal);
                if (sTerm.Length == 0 && terminalType == USB_TERMINAL_TYPE_Streaming) {
                    sTerm = Properties.Resources.StreamForRecording;
                }

                mSB.AppendFormat("\n          Output Terminal {0} : assocT={1} Source={2} ClockSource={3} {4} {5}",
                    terminalID, assocT, sourceID, bCSourceID, terminalTypeStr, sTerm);
            } else {
                mSB.AppendFormat("\n          Output Terminal descriptor of unknown size 0x{0:X4}", length);
            }
        }

        private void ReadAudioControl2EffectUnit(byte [] buff, int offs) {
            // USB AC2 Effect Unit size : 16+ch*4
        }

        private void ReadAudioControl2ProcessingUnit(byte[] buff, int offs) {
            // USB AC2 Processing Unit size : 17+p+x
        }
        private void ReadAudioControl1ProcessingUnit(byte[] buff, int offs) {
        }

        private void ReadAudioControl1ExtensionUnit(byte[] buff, int offs) {
        }

        private void ReadAudioControl2ExtensionUnit(byte[] buff, int offs) {
        }

        private void ReadAudioControlClockSource(byte[] buff, int offs) {
            /* USB Audio Class 2 Table 4-6: Clock Source Descriptor
            0 UCHAR bLength = 8
            1 UCHAR bDescriptorType = CS_INTERFACE
            2 UCHAR bDescriptorSubtype = CLOCK_SOURCE
            3 UCHAR bClockID 
            4 UCHAR bmAttributes
            5 UCHAR bmControls 
            6 UCHAR bAssocTerminal 
            7 UCHAR iClockSource 
            */
            int length = buff[offs + 0];
            if (length != 8) {
                mSB.AppendFormat("\n          Clock Source Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int bClockID = buff[offs + 3];
            int bmAttributes = buff[offs + 4];
            int bmControls = buff[offs + 5];
            int bAssocTerminal = buff[offs + 6];
            int iClockSource = buff[offs + 7];
            string sCS = FindString(iClockSource);
            string clockTypeStr = AudioClockSourceAttrToClockTypeStr(bmAttributes, bmControls);

            mSB.AppendFormat("\n          Clock Source {0} {1}, assocTerminal={2}:\n            {3}",
                   bClockID, sCS, bAssocTerminal, clockTypeStr);
        }

        private void ReadAudioControlClockSelector(byte[] buff, int offs) {
            /* USB Audio Class 2  Table 4-7: Clock Selector Descriptor
            p==1のとき
            0 UCHAR bLength = 7+p
            1 UCHAR bDescriptorType CS_INTERFACE
            2 UCHAR bDescriptorSubType CLOCK_SELECTOR
            3 UCHAR bClockID
            4 UCHAR bNrInPins (nr of input pin, p)
            5 UCHAR baCSourceID1
            6 UCHAR bmControls
            7 UCHAR iClockSelector

            p==2のとき
            5 UCHAR baCSourceID1
            6 UCHAR baCSourceID2
            7 UCHAR bmControls
            8 UCHAR iClockSelector
            */
            int length = buff[offs + 0];
            if (length < 7) {
                mSB.AppendFormat("\n          Clock Selector Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int bClockSelectorId = buff[offs + 3];
            int nrInPins = buff[offs + 4];
            if (length-7 < nrInPins) {
                Console.WriteLine("nrInPins invalid {0}, bLength={1}\n", nrInPins, length);
                nrInPins = length - 7;
            }

            int bmControls = buff[offs + nrInPins + 5];
            string sControls = AudioClockSelectorCotrolsToStr(bmControls);

            int iClockSelector = buff[offs + nrInPins + 6];
            string sCS = FindString(iClockSelector);
            mSB.AppendFormat(    "\n          Clock Selector {0} {1}, {2}", bClockSelectorId, sCS, sControls);
            for (int i=0; i<nrInPins; ++i) {
                mSB.AppendFormat("\n            InputPin {0} : Clock entity id={1}", i+1, buff[offs+5+i]);
            }
        }

        private void ReadAudioControlClockMultiplier(byte[] buff, int offs) {
            /* USB Audio Class 2 Table 4-8: Clock Multiplier Descriptor
            0 UCHAR bLength =7
            1 UCHAR bDescriptorType CS_INTERFACE
            2 UCHAR bDescriptorSubType CLOCK_MULTIPLIER
            3 UCHAR bClockMultiplierId
            4 UCHAR bCSourceId
            5 UCHAR bmControls
            6 UCHAR iClockMultiplier
            */
            int length = buff[offs + 0];
            if (length != 7) {
                mSB.AppendFormat("\n          Clock Multiplier Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int bClockMultiplierId = buff[offs + 3];
            int bCSourceId = buff[offs + 4];
            int bmControls = buff[offs + 5];
            string sControls = AudioClockMultiplierCotrolsToStr(bmControls);

            int iClockMultiplier = buff[offs + 6];
            string sCS = FindString(iClockMultiplier);

            mSB.AppendFormat("\n          Clock Multiplier {0} {1}, InputPin={2}", bClockMultiplierId, sCS, bCSourceId);
            mSB.AppendFormat("\n            {3}", sControls);
        }

        private void ReadAudioControl2SampleRateConverter(byte[] buff, int offs) {
            /*USB Audio Class 2 Table 4-14: Sampling Rate Converter Unit Descriptor
            0 UCHAR bLength =8
            1 UCHAR bDescriptorType CS_INTERFACE
            2 UCHAR bDescriptorSubType SAMPLE_RATE_CONVERTER
            3 UCHAR bUnitID
            4 UCHAR bSourceID
            5 UCHAR bClockIn
            6 UCHAR bClockOut
            7 UCHAR iString
            */
            int length = buff[offs + 0];
            if (length != 8) {
                mSB.AppendFormat("\n          Sampling Rate Converter Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int bId = buff[offs + 3];
            int bSourceId = buff[offs + 4];
            int bClockIn = buff[offs + 5];
            int bClockOut = buff[offs + 6];
            int iString = buff[offs + 7];
            string s = FindString(iString);
            mSB.AppendFormat("\n          Sampling Rate Converter {0} {1}, source={1} clockIn={2} clockOut={3}",
                bId, s, bSourceId, bClockIn, bClockOut);
        }

        private void ReadAudioControlMixerUnit(byte[] buff, int offs) {
            /*
            0 UCHAR bLength  13+p+N
            1 UCHAR bDescriptorType
            2 UCHAR bDescriptorSubtype
            3 UCHAR bUnitID
            4 UCHAR bNrInPins p
            5 UCHAR baSourceID(1)
            5+p-1 UCHAR bNrChannels(p)
            5+p UCHAR bNrChannels
            6+p UINT bmChannelConfig
            10+p UCHAR iChannelNames
            11+p bmMixerControls N
            11+p+N UCHAR bmControls 
            12+p+N UCHAR iMixer
            */
            int length = buff[offs + 0];
            if (length != 15) {
                mSB.AppendFormat("\n          Mixer Unit Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int bUnitID = buff[offs + 3];
            int p = buff[offs + 4];
            int iMixer = buff[offs + length - 1];
            string sMixer = FindString(iMixer);
            mSB.AppendFormat("\n          Mixer Unit {0} {1}", bUnitID, sMixer);
        }

        private void ReadAudioControlSelectorUnit(byte [] buff, int offs) {
            /* Table 4-12: Selector Unit Descriptor
            0 UCHAR bLength
            1 UCHAR bDescriptorType
            2 UCHAR bDescriptorSubtype
            3 UCHAR bUnitID
            4 UCHAR bNrInPins
            5 UCHAR baSourceID1
            5+p bmControls
            6+p iSelector
            */

            int length = buff[offs + 0];
            if (length <= 7) {
                mSB.AppendFormat("\n          Selector Unit Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int bUnitId = buff[offs + 3];
            int bNrInPins = buff[offs + 4];
            if (length -6 < bNrInPins) {
                Console.WriteLine("Error Selector Unit size bNrInPins={0} bLength={1}", bNrInPins, length);
                bNrInPins = length - 6;
            }

            int iSelector = buff[offs + length - 1];
            string sSelector = FindString(iSelector);
            mSB.AppendFormat("\n          Selector Unit {0} {1}", bUnitId, sSelector);
            for (int i = 0; i < bNrInPins; ++i) {
                mSB.AppendFormat("\n            Input Pin {0}", buff[offs+5+i]);
            }
        }

        private void ReadAudioControlFeatureUnit(byte[] buff, int offs) {
            /* Table 4-13: Feature Unit Descriptor
            0 bLength
            1 bDescriptorType
            2 bDescriptorSubType
            3 bUnitID
            4 bSourceID unit or terminal
            5 UINT bmaControls0 master channel
            9 UINT bmaControlsCh1
            13 UINT bmaControlsCh2
            ...
            5+(ch+1)*4 iFeature
            */
            int length = buff[offs + 0];
            if (((length-6) & 0x3) !=0) {
                mSB.AppendFormat("\n          Feature Unit Descriptor of unknown size 0x{0:X4}", length);
                return;
            }

            int nCh = (length - 6) / 4 - 1;

            int bUnitID = buff[offs + 3];
            int bSourceID = buff[offs + 4];
            int iFeature = buff[offs + length - 1];
            string sFeature = FindString(iFeature);

            mSB.AppendFormat("\n          Feature Unit {0} {1}, SourceID={2}", bUnitID, sFeature, bSourceID);
            uint bmMC = BitConverter.ToUInt32(buff, offs + 5);
            mSB.AppendFormat("\n            Master channel 0 {0}", AudioControlFeatureControlsToStr(bmMC));
            for (int i=0; i<nCh; ++i) {
                uint bm = BitConverter.ToUInt32(buff, offs + 9+i*4);
                mSB.AppendFormat("\n            Channel {0} {1}", i+1, AudioControlFeatureControlsToStr(bm));
            }
        }

        private void ReadAudioControlOther(byte[] buff, int offs) {
            int length = buff[offs + 0];
            int descSub = buff[offs + 2];
            string s = AudioControlTypeToStr(descSub);

            mSB.AppendFormat("\n          {0} length={1}", s, length);
        }

        // Class-Specific AS Interface Descriptor
        private void ReadAudioStreamingInterfaceDesc(byte[] buff, int offs) {

            int length = buff[offs];
            if (length < 6) {
                return;
            }

            if (length == 7) {
                /* USB Audio Class 1
                0 UCHAR  bLength;
                1 UCHAR  bDescriptorType;
                2 UCHAR  bDescriptorSubtype;
                3 UCHAR  bTerminalLink;
                4 UCHAR  bDelay;
                5 USHORT wFormatTag;
                */

            int terminalLink = buff[offs + 3];
                int delay = buff[4];
                int formatTag = BitConverter.ToUInt16(buff, offs + 5);
                string formatStr = WFormatTagToStr(formatTag);

                mSB.AppendFormat("\n          Uses Terminal {0}, {1}, delay={2}f", terminalLink, formatStr, delay);
            } else if (length == 16) {
                // USB Audio Class 2.0 Table 4-27
                /*
                0 UCHAR  bLength;
                1 UCHAR  bDescriptorType;
                2 UCHAR  bDescriptorSubtype; AS_GENERAL
                3 UCHAR  bTerminalLink;
                4 UCHAR  bmControls;
                5 UCHAR  bFormatType;
                6 UINT   bmFormats;
                10 UCHAR bNrChannels;
                11 UINT  bmChannelConfig
                15 UCHAR iChannelNames
                */
                int terminalLink = buff[offs + 3];
                uint bmFormats = BitConverter.ToUInt32(buff, offs + 6);
                int formatType = buff[offs + 5];
                string formatStr = BmFormatsToStr(bmFormats, formatType);
                int nrChannels = buff[offs + 10];

                int iChannelNames = buff[offs + 15];
                string chStr = FindString(iChannelNames);

                mSB.AppendFormat("\n          Uses Terminal {0}, {1}ch{2} {3}", terminalLink, nrChannels, formatStr, chStr);

            } else {
                mSB.AppendFormat("\n          Unknwon Class-specific AS Interface desc! {0:X4}", length);
            }
        }

        private void ReadAudioStreamingOtherDesc(byte[] buff, int offs) {
            int length = buff[offs];
            int descSub = buff[offs + 2];
            string s = AudioStreamingTypeToStr(descSub);
            mSB.AppendFormat("\n          {0}, length={1}", s,length);
        }

        private void ReadAudioOther(byte[] buff, int offs) {
            int length = buff[offs];
            int descType = buff[offs + 1];
            if (3 <= length) {
                int descSubType = buff[offs + 2];
                mSB.AppendFormat("\n          Unknown Audio Descriptor length={0}, descType=0x{1:X2} subType={2:X2}", length, descType, descSubType);
            } else {
                mSB.AppendFormat("\n          Unknown Audio Descriptor length={0}, descType=0x{1:X2}", length, descType);
            }
        }

        private string TransTypeToStr(int t) {
            switch (t) {
            case 0: return "Control";
            case 1: return "Isochronous";
            case 2: return "Bulk";
            case 3: return "Interrupt";
            default: return "Unknown";
            }
        }
        private string SyncTypeToStr(int t) {
            switch (t) {
            case 0: return "NoSync";
            case 1: return "Asynchronous";
            case 2: return "Adaptive";
            case 3: return "Synchronous";
            default: return "Unknown";
            }
        }

        private string UsageTypeToStr(int t) {
           switch (t) {
            case 0: return "Data";
            case 2: return "Implicit feedback data";
            default: return "Unknown";
            }
        }

        private string AudioCSEndpointAttrToStr(int attr) {
            var sb = new StringBuilder();
            if (0 == (attr & 0x3)) {
                sb.Append("Control: None");
            } else {
                sb.Append("Control:");
                if (0 != (attr & 1)) {
                    sb.Append(" SamplingFreq");
                }
                if (0 != (attr & 2)) {
                    sb.Append(" Pitch");
                }
            }

            if (0 != (attr & 0x80)) {
                sb.Append(", MaxPacketsOnly");
            }
            return sb.ToString();
        }

        private string Audio2CSEndpointControlsToStr(int attr, int cntl) {
            var sb = new StringBuilder();
            if (0 != (attr & 0x80)) {
                sb.Append(" MaxPacketsOnly");
            }

            if (0 == (cntl & 0x3f)) {
                sb.Append(" Controls: None");
            } else {
                sb.Append(" Controls:");
                if (1 == (cntl & 0x3)) {
                    sb.Append(" ReadPitch");
                }
                if (1 == ((cntl >> 2) & 0x3)) {
                    sb.Append(" ReadDataOverrun");
                }
                if (1 == ((cntl >> 2) & 0x3)) {
                    sb.Append(" ReadDataUnderrun");
                }
                if (3 == (cntl & 0x3)) {
                    sb.Append(" PitchControl");
                }
                if (3 == ((cntl >>2)&0x3)) {
                    sb.Append(" DataOverrunControl");
                }
                if (3 == ((cntl >> 2) & 0x3)) {
                    sb.Append(" DataUnderrunControl");
                }
            }
            return sb.ToString();

        }

        private const int LOCK_DELAY_UNIT_UNDEFINED = 0;
        private const int LOCK_DELAY_UNIT_Millisec = 1;
        private const int LOCK_DELAY_UNIT_DecodedPCMsamples = 2;


        private void ReadAudioCSEndpoint(byte[] buff, int offs) {
            int length = buff[offs];
            if (7 == length) {
                /* 4.6.1.2 Table 4-21 Class-Specific Audio Stream Isochronous Audio Data Endpoint Descriptor
                0 UCHAR  bLength = 7
                1 UCHAR  bDescriptorType; 0x25
                2 UCHAR  bDescriptorSubtype; = 1 EP_GENERAL
                3 UCHAR  bmAttributes;
                4 UCHAR  bLockDelayUnits;
                5 USHORT wLockDelay;
                */
                int bmAttr = buff[offs + 3];
                int bLockDelayUnits = buff[offs + 4];
                int wLockDelay = BitConverter.ToUInt16(buff, offs + 5);
                string sControls = AudioCSEndpointAttrToStr(bmAttr);
                mSB.AppendFormat("\n          Endpoint {0}", sControls);
                switch (bLockDelayUnits) {
                case LOCK_DELAY_UNIT_Millisec: mSB.AppendFormat(" ,LockDelay={0} ms", wLockDelay); break;
                case LOCK_DELAY_UNIT_DecodedPCMsamples: mSB.AppendFormat(" ,LockDelay={0} samples", wLockDelay);break;
                case LOCK_DELAY_UNIT_UNDEFINED:
                default:
                    break; //< 表示しない。
                }
            } else if (8 == length) {
                /* USBAC2 Table 4-34: Class-Specific AS Isochronous Audio Data Endpoint Descriptor
                0 bLength = 8
                1 bDescriptorType = 0x25 ENDPOINT
                2 bDescSubType = 1 EP_GENERAL
                3 bmAttr
                4 bmControls
                5 bLockDelayUnits
                6 wLockDelay
                */
                int bmAttr = buff[offs + 3];
                int bmControls = buff[offs + 4];
                string sControls = Audio2CSEndpointControlsToStr(bmAttr, bmControls);

                int bLockDelayUnits = buff[offs + 5];
                int wLockDelay = BitConverter.ToUInt16(buff, offs + 6);
                mSB.AppendFormat("\n          Endpoint {0}", sControls);

                switch (bLockDelayUnits) {
                case LOCK_DELAY_UNIT_Millisec: mSB.AppendFormat(" ,LockDelay={0} ms", wLockDelay); break;
                case LOCK_DELAY_UNIT_DecodedPCMsamples: mSB.AppendFormat(" ,LockDelay={0} samples", wLockDelay); break;
                case LOCK_DELAY_UNIT_UNDEFINED:
                default:
                    break; //< 表示しない。
                }
            } else {
                mSB.AppendFormat("\n          Unknown Audio Stream Endpoint length={0}", length);
            }
        }

        private void ReadAudioStreamingFormatTypeDesc(byte[] buff, int offs) {

            int length = buff[offs];
            if (length < 4) {
                return;
            }

            int formatType = buff[offs + 3];
            if (formatType == FORMAT_TYPE_1) {
                if (length < 6) {
                    return;
                }
                if (length == 6) {
                    /* USB Audio Class 2 Audio Data Formats Table 2-2: Type I Format Type Descriptor
                    0 UCHAR  bLength;
                    1 UCHAR  bDescriptorType;
                    2 UCHAR  bDescriptorSubtype;
                    3 UCHAR  bFormatType;
                    4 UCHAR  bSubSlotSize
                    5 UCHAR  bBitResolution
                    */
            int subSlotSize = buff[offs + 4];
                    int bitResolution = buff[offs + 5];

                    mSB.AppendFormat(" {0}bit", bitResolution);
                } else if (8 <= length) {
                    /* USB Audio Class 1 Audio Data Formats Table 2-1: Type I Format Type Descriptor
                    0 UCHAR  bLength;
                    1 UCHAR  bDescriptorType;
                    2 UCHAR  bDescriptorSubtype;
                    3 UCHAR  bFormatType;
                    4 UCHAR  bNrChannels
                    5 UCHAR  bSubframeSize
                    6 UCHAR  bBitResolution 
                    7 UCHAR  bSamFreqType
                    8 U24bit tSamFreq
                    */

                    int nrChannels = buff[offs + 4];
                    int bitResolution = buff[offs + 6];
                    int samFreqType = buff[offs + 7];
                    mSB.AppendFormat(" {0}ch {1}bit", nrChannels, bitResolution);
                    if (1 <= samFreqType) {
                        if (length < samFreqType * 3 + 8) {
                            samFreqType = (length - 8) / 3;
                        }
                        for (int i = 0; i < samFreqType; ++i) {
                            uint freq = 0xffffff & BitConverter.ToUInt32(buff, offs + 8 + i * 3);
                            mSB.AppendFormat(" {0}kHz", freq*0.001);
                        }
                    }
                } else {
                    mSB.AppendFormat(" ??Unknown AS format {0:X4}", length);
                }
            }
            if (formatType == FORMAT_TYPE_2) {
                mSB.AppendFormat(" FormatType2Desc");
            }
            if (formatType == FORMAT_TYPE_3) {
                mSB.AppendFormat(" FormatType3Desc");
            }
        }
    }
}
