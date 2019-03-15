#include "WWGuidToStr.h"
#include <mmdeviceapi.h>
#include <MMDeviceAPI.h>
#include <AudioClient.h>
#include <AudioPolicy.h>
#include <devicetopology.h>

const std::wstring
WWGuidToStr(GUID &t)
{
    if (GUID_NULL == t) {
        return L"GUID_NULL";
    }

    if (KSNODETYPE_INPUT_UNDEFINED == t) {
        return L"KSNODETYPE_INPUT_UNDEFINED";
    }
    if (KSNODETYPE_MICROPHONE == t) {
        return L"KSNODETYPE_MICROPHONE";
    }
    if (KSNODETYPE_DESKTOP_MICROPHONE == t) {
        return L"KSNODETYPE_DESKTOP_MICROPHONE";
    }
    if (KSNODETYPE_PERSONAL_MICROPHONE == t) {
        return L"KSNODETYPE_PERSONAL_MICROPHONE";
    }
    if (KSNODETYPE_OMNI_DIRECTIONAL_MICROPHONE == t) {
        return L"KSNODETYPE_OMNI_DIRECTIONAL_MICROPHONE";
    }
    if (KSNODETYPE_MICROPHONE_ARRAY == t) {
        return L"KSNODETYPE_MICROPHONE_ARRAY";
    }
    if (KSNODETYPE_PROCESSING_MICROPHONE_ARRAY == t) {
        return L"KSNODETYPE_PROCESSING_MICROPHONE_ARRAY";
    }
    if (KSNODETYPE_OUTPUT_UNDEFINED == t) {
        return L"KSNODETYPE_OUTPUT_UNDEFINED";
    }
    if (KSNODETYPE_SPEAKER == t) {
        return L"KSNODETYPE_SPEAKER";
    }
    if (KSNODETYPE_HEADPHONES == t) {
        return L"KSNODETYPE_HEADPHONES";
    }
    if (KSNODETYPE_HEAD_MOUNTED_DISPLAY_AUDIO == t) {
        return L"KSNODETYPE_HEAD_MOUNTED_DISPLAY_AUDIO";
    }
    if (KSNODETYPE_DESKTOP_SPEAKER == t) {
        return L"KSNODETYPE_DESKTOP_SPEAKER";
    }
    if (KSNODETYPE_ROOM_SPEAKER == t) {
        return L"KSNODETYPE_ROOM_SPEAKER";
    }
    if (KSNODETYPE_COMMUNICATION_SPEAKER == t) {
        return L"KSNODETYPE_COMMUNICATION_SPEAKER";
    }
    if (KSNODETYPE_LOW_FREQUENCY_EFFECTS_SPEAKER == t) {
        return L"KSNODETYPE_LOW_FREQUENCY_EFFECTS_SPEAKER";
    }
    if (KSNODETYPE_BIDIRECTIONAL_UNDEFINED == t) {
        return L"KSNODETYPE_BIDIRECTIONAL_UNDEFINED";
    }
    if (KSNODETYPE_HANDSET == t) {
        return L"KSNODETYPE_HANDSET";
    }
    if (KSNODETYPE_HEADSET_MICROPHONE == t) {
        return L"KSNODETYPE_HEADSET_MICROPHONE";
    }
    if (KSNODETYPE_HEADSET_SPEAKERS == t) {
        return L"KSNODETYPE_HEADSET_SPEAKERS";
    }
    if (KSNODETYPE_HEADSET == t) {
        return L"KSNODETYPE_HEADSET";
    }
    if (KSNODETYPE_SPEAKERPHONE_NO_ECHO_REDUCTION == t) {
        return L"KSNODETYPE_SPEAKERPHONE_NO_ECHO_REDUCTION";
    }
    if (KSNODETYPE_ECHO_SUPPRESSING_SPEAKERPHONE == t) {
        return L"KSNODETYPE_ECHO_SUPPRESSING_SPEAKERPHONE";
    }
    if (KSNODETYPE_ECHO_CANCELING_SPEAKERPHONE == t) {
        return L"KSNODETYPE_ECHO_CANCELING_SPEAKERPHONE";
    }
    if (KSNODETYPE_TELEPHONY_UNDEFINED == t) {
        return L"KSNODETYPE_TELEPHONY_UNDEFINED";
    }
    if (KSNODETYPE_PHONE_LINE == t) {
        return L"KSNODETYPE_PHONE_LINE";
    }
    if (KSNODETYPE_TELEPHONE == t) {
        return L"KSNODETYPE_TELEPHONE";
    }
    if (KSNODETYPE_DOWN_LINE_PHONE == t) {
        return L"KSNODETYPE_DOWN_LINE_PHONE";
    }
    if (KSNODETYPE_EXTERNAL_UNDEFINED == t) {
        return L"KSNODETYPE_EXTERNAL_UNDEFINED";
    }
    if (KSNODETYPE_ANALOG_CONNECTOR == t) {
        return L"KSNODETYPE_ANALOG_CONNECTOR";
    }
    if (KSNODETYPE_DIGITAL_AUDIO_INTERFACE == t) {
        return L"KSNODETYPE_DIGITAL_AUDIO_INTERFACE";
    }
    if (KSNODETYPE_LINE_CONNECTOR == t) {
        return L"KSNODETYPE_LINE_CONNECTOR";
    }
    if (KSNODETYPE_LEGACY_AUDIO_CONNECTOR == t) {
        return L"KSNODETYPE_LEGACY_AUDIO_CONNECTOR";
    }
    if (KSNODETYPE_SPDIF_INTERFACE == t) {
        return L"KSNODETYPE_SPDIF_INTERFACE";
    }
    if (KSNODETYPE_1394_DA_STREAM == t) {
        return L"KSNODETYPE_1394_DA_STREAM";
    }
    if (KSNODETYPE_1394_DV_STREAM_SOUNDTRACK == t) {
        return L"KSNODETYPE_1394_DV_STREAM_SOUNDTRACK";
    }
    if (KSNODETYPE_EMBEDDED_UNDEFINED == t) {
        return L"KSNODETYPE_EMBEDDED_UNDEFINED";
    }
    if (KSNODETYPE_LEVEL_CALIBRATION_NOISE_SOURCE == t) {
        return L"KSNODETYPE_LEVEL_CALIBRATION_NOISE_SOURCE";
    }
    if (KSNODETYPE_EQUALIZATION_NOISE == t) {
        return L"KSNODETYPE_EQUALIZATION_NOISE";
    }
    if (KSNODETYPE_CD_PLAYER == t) {
        return L"KSNODETYPE_CD_PLAYER";
    }
    if (KSNODETYPE_DAT_IO_DIGITAL_AUDIO_TAPE == t) {
        return L"KSNODETYPE_DAT_IO_DIGITAL_AUDIO_TAPE";
    }
    if (KSNODETYPE_DCC_IO_DIGITAL_COMPACT_CASSETTE == t) {
        return L"KSNODETYPE_DCC_IO_DIGITAL_COMPACT_CASSETTE";
    }
    if (KSNODETYPE_MINIDISK == t) {
        return L"KSNODETYPE_MINIDISK";
    }
    if (KSNODETYPE_ANALOG_TAPE == t) {
        return L"KSNODETYPE_ANALOG_TAPE";
    }
    if (KSNODETYPE_PHONOGRAPH == t) {
        return L"KSNODETYPE_PHONOGRAPH";
    }
    if (KSNODETYPE_VCR_AUDIO == t) {
        return L"KSNODETYPE_VCR_AUDIO";
    }
    if (KSNODETYPE_VIDEO_DISC_AUDIO == t) {
        return L"KSNODETYPE_VIDEO_DISC_AUDIO";
    }
    if (KSNODETYPE_DVD_AUDIO == t) {
        return L"KSNODETYPE_DVD_AUDIO";
    }
    if (KSNODETYPE_TV_TUNER_AUDIO == t) {
        return L"KSNODETYPE_TV_TUNER_AUDIO";
    }
    if (KSNODETYPE_SATELLITE_RECEIVER_AUDIO == t) {
        return L"KSNODETYPE_SATELLITE_RECEIVER_AUDIO";
    }
    if (KSNODETYPE_CABLE_TUNER_AUDIO == t) {
        return L"KSNODETYPE_CABLE_TUNER_AUDIO";
    }
    if (KSNODETYPE_DSS_AUDIO == t) {
        return L"KSNODETYPE_DSS_AUDIO";
    }
    if (KSNODETYPE_RADIO_RECEIVER == t) {
        return L"KSNODETYPE_RADIO_RECEIVER";
    }
    if (KSNODETYPE_RADIO_TRANSMITTER == t) {
        return L"KSNODETYPE_RADIO_TRANSMITTER";
    }
    if (KSNODETYPE_MULTITRACK_RECORDER == t) {
        return L"KSNODETYPE_MULTITRACK_RECORDER";
    }
    if (KSNODETYPE_SYNTHESIZER == t) {
        return L"KSNODETYPE_SYNTHESIZER";
    }
    if (KSNODETYPE_HDMI_INTERFACE == t) {
        return L"KSNODETYPE_HDMI_INTERFACE";
    }
    if (KSNODETYPE_DISPLAYPORT_INTERFACE == t) {
        return L"KSNODETYPE_DISPLAYPORT_INTERFACE";
    }
    if (KSNODETYPE_AUDIO_LOOPBACK == t) {
        return L"KSNODETYPE_AUDIO_LOOPBACK";
    }
    if (KSNODETYPE_AUDIO_KEYWORDDETECTOR == t) {
        return L"KSNODETYPE_AUDIO_KEYWORDDETECTOR";
    }
    if (KSNODETYPE_MIDI_JACK == t) {
        return L"KSNODETYPE_MIDI_JACK";
    }
    if (KSNODETYPE_MIDI_ELEMENT == t) {
        return L"KSNODETYPE_MIDI_ELEMENT";
    }
    if (KSNODETYPE_AUDIO_ENGINE == t) {
        return L"KSNODETYPE_AUDIO_ENGINE";
    }
    if (KSNODETYPE_SPEAKERS_STATIC_JACK == t) {
        return L"KSNODETYPE_SPEAKERS_STATIC_JACK";
    }
    if (KSNODETYPE_DRM_DESCRAMBLE == t) {
        return L"KSNODETYPE_DRM_DESCRAMBLE";
    }
    if (KSNODETYPE_TELEPHONY_BIDI == t) {
        return L"KSNODETYPE_TELEPHONY_BIDI";
    }
    if (KSNODETYPE_FM_RX == t) {
        return L"KSNODETYPE_FM_RX";
    }
    if (KSNODETYPE_DAC == t) {
        return L"KSNODETYPE_DAC";
    }
    if (KSNODETYPE_ADC == t) {
        return L"KSNODETYPE_ADC";
    }
    if (KSNODETYPE_SRC == t) {
        return L"KSNODETYPE_SRC";
    }
    if (KSNODETYPE_SUPERMIX == t) {
        return L"KSNODETYPE_SUPERMIX";
    }
    if (KSNODETYPE_MUX == t) {
        return L"KSNODETYPE_MUX";
    }
    if (KSNODETYPE_DEMUX == t) {
        return L"KSNODETYPE_DEMUX";
    }
    if (KSNODETYPE_SUM == t) {
        return L"KSNODETYPE_SUM";
    }
    if (KSNODETYPE_MUTE == t) {
        return L"KSNODETYPE_MUTE";
    }
    if (KSNODETYPE_VOLUME == t) {
        return L"KSNODETYPE_VOLUME";
    }
    if (KSNODETYPE_TONE == t) {
        return L"KSNODETYPE_TONE";
    }
    if (KSNODETYPE_EQUALIZER == t) {
        return L"KSNODETYPE_EQUALIZER";
    }
    if (KSNODETYPE_NOISE_SUPPRESS == t) {
        return L"KSNODETYPE_NOISE_SUPPRESS";
    }
    if (KSNODETYPE_DELAY == t) {
        return L"KSNODETYPE_DELAY";
    }
    if (KSNODETYPE_LOUDNESS == t) {
        return L"KSNODETYPE_LOUDNESS";
    }
    if (KSNODETYPE_PROLOGIC_DECODER == t) {
        return L"KSNODETYPE_PROLOGIC_DECODER";
    }
    if (KSNODETYPE_STEREO_WIDE == t) {
        return L"KSNODETYPE_STEREO_WIDE";
    }
    if (KSNODETYPE_REVERB == t) {
        return L"KSNODETYPE_REVERB";
    }
    if (KSNODETYPE_CHORUS == t) {
        return L"KSNODETYPE_CHORUS";
    }
    if (KSNODETYPE_3D_EFFECTS == t) {
        return L"KSNODETYPE_3D_EFFECTS";
    }
    if (KSNODETYPE_PARAMETRIC_EQUALIZER == t) {
        return L"KSNODETYPE_PARAMETRIC_EQUALIZER";
    }
    if (KSNODETYPE_UPDOWN_MIX == t) {
        return L"KSNODETYPE_UPDOWN_MIX";
    }
    if (KSNODETYPE_DYN_RANGE_COMPRESSOR == t) {
        return L"KSNODETYPE_DYN_RANGE_COMPRESSOR";
    }
    if (KSNODETYPE_ACOUSTIC_ECHO_CANCEL == t) {
        return L"KSNODETYPE_ACOUSTIC_ECHO_CANCEL";
    }
    if (KSNODETYPE_MICROPHONE_ARRAY_PROCESSOR == t) {
        return L"KSNODETYPE_MICROPHONE_ARRAY_PROCESSOR";
    }
    if (KSNODETYPE_DEV_SPECIFIC == t) {
        return L"KSNODETYPE_DEV_SPECIFIC";
    }
    if (KSNODETYPE_SURROUND_ENCODER == t) {
        return L"KSNODETYPE_SURROUND_ENCODER";
    }
    if (KSNODETYPE_PEAKMETER == t) {
        return L"KSNODETYPE_PEAKMETER";
    }
    if (KSNODETYPE_VIDEO_STREAMING == t) {
        return L"KSNODETYPE_VIDEO_STREAMING";
    }
    if (KSNODETYPE_VIDEO_INPUT_TERMINAL == t) {
        return L"KSNODETYPE_VIDEO_INPUT_TERMINAL";
    }
    if (KSNODETYPE_VIDEO_OUTPUT_TERMINAL == t) {
        return L"KSNODETYPE_VIDEO_OUTPUT_TERMINAL";
    }
    if (KSNODETYPE_VIDEO_SELECTOR == t) {
        return L"KSNODETYPE_VIDEO_SELECTOR";
    }
    if (KSNODETYPE_VIDEO_PROCESSING == t) {
        return L"KSNODETYPE_VIDEO_PROCESSING";
    }
    if (KSNODETYPE_VIDEO_CAMERA_TERMINAL == t) {
        return L"KSNODETYPE_VIDEO_CAMERA_TERMINAL";
    }
    if (KSNODETYPE_VIDEO_INPUT_MTT == t) {
        return L"KSNODETYPE_VIDEO_INPUT_MTT";
    }
    if (KSNODETYPE_VIDEO_OUTPUT_MTT == t) {
        return L"KSNODETYPE_VIDEO_OUTPUT_MTT";
    }

    if (KSCATEGORY_MICROPHONE_ARRAY_PROCESSOR == t) {
        return L"KSCATEGORY_MICROPHONE_ARRAY_PROCESSOR";
    }
    if (KSCATEGORY_AUDIO == t) {
        return L"KSCATEGORY_AUDIO";
    }
    if (KSCATEGORY_VIDEO == t) {
        return L"KSCATEGORY_VIDEO";
    }
    if (KSCATEGORY_REALTIME == t) {
        return L"KSCATEGORY_REALTIME";
    }
    if (KSCATEGORY_TEXT == t) {
        return L"KSCATEGORY_TEXT";
    }
    if (KSCATEGORY_NETWORK == t) {
        return L"KSCATEGORY_NETWORK";
    }
    if (KSCATEGORY_TOPOLOGY == t) {
        return L"KSCATEGORY_TOPOLOGY";
    }
    if (KSCATEGORY_VIRTUAL == t) {
        return L"KSCATEGORY_VIRTUAL";
    }
    if (KSCATEGORY_ACOUSTIC_ECHO_CANCEL == t) {
        return L"KSCATEGORY_ACOUSTIC_ECHO_CANCEL";
    }
    if (KSCATEGORY_SYNTHESIZER == t) {
        return L"KSCATEGORY_SYNTHESIZER";
    }
    if (KSCATEGORY_DRM_DESCRAMBLE == t) {
        return L"KSCATEGORY_DRM_DESCRAMBLE";
    }
    if (KSCATEGORY_WDMAUD_USE_PIN_NAME == t) {
        return L"KSCATEGORY_WDMAUD_USE_PIN_NAME";
    }
    if (KSCATEGORY_ESCALANTE_PLATFORM_DRIVER == t) {
        return L"KSCATEGORY_ESCALANTE_PLATFORM_DRIVER";
    }
    if (KSCATEGORY_TVTUNER == t) {
        return L"KSCATEGORY_TVTUNER";
    }
    if (KSCATEGORY_CROSSBAR == t) {
        return L"KSCATEGORY_CROSSBAR";
    }
    if (KSCATEGORY_TVAUDIO == t) {
        return L"KSCATEGORY_TVAUDIO";
    }
    if (KSCATEGORY_VPMUX == t) {
        return L"KSCATEGORY_VPMUX";
    }
    if (KSCATEGORY_VBICODEC == t) {
        return L"KSCATEGORY_VBICODEC";
    }
    if (KSCATEGORY_ENCODER == t) {
        return L"KSCATEGORY_ENCODER";
    }
    if (KSCATEGORY_MULTIPLEXER == t) {
        return L"KSCATEGORY_MULTIPLEXER";
    }
    if (KSCATEGORY_RENDER == t) {
        return L"KSCATEGORY_RENDER";
    }

    if (PINNAME_CAPTURE == t) {
        return L"PINNAME_CAPTURE";
    }
    if (PINNAME_VIDEO_CC_CAPTURE == t) {
        return L"PINNAME_VIDEO_CC_CAPTURE";
    }
    if (PINNAME_VIDEO_NABTS_CAPTURE == t) {
        return L"PINNAME_VIDEO_NABTS_CAPTURE";
    }
    if (PINNAME_PREVIEW == t) {
        return L"PINNAME_PREVIEW";
    }
    if (PINNAME_VIDEO_ANALOGVIDEOIN == t) {
        return L"PINNAME_VIDEO_ANALOGVIDEOIN";
    }
    if (PINNAME_VIDEO_VBI == t) {
        return L"PINNAME_VIDEO_VBI";
    }
    if (PINNAME_VIDEO_VIDEOPORT == t) {
        return L"PINNAME_VIDEO_VIDEOPORT";
    }
    if (PINNAME_VIDEO_NABTS == t) {
        return L"PINNAME_VIDEO_NABTS";
    }
    if (PINNAME_VIDEO_EDS == t) {
        return L"PINNAME_VIDEO_EDS";
    }
    if (PINNAME_VIDEO_TELETEXT == t) {
        return L"PINNAME_VIDEO_TELETEXT";
    }
    if (PINNAME_VIDEO_CC == t) {
        return L"PINNAME_VIDEO_CC";
    }
    if (PINNAME_VIDEO_STILL == t) {
        return L"PINNAME_VIDEO_STILL";
    }
    if (PINNAME_IMAGE == t) {
        return L"PINNAME_IMAGE";
    }
    if (PINNAME_VIDEO_TIMECODE == t) {
        return L"PINNAME_VIDEO_TIMECODE";
    }
    if (PINNAME_VIDEO_VIDEOPORT_VBI == t) {
        return L"PINNAME_VIDEO_VIDEOPORT_VBI";
    }

    if (__uuidof(IAudioMute) == t) {
        return L"IAudioMute";
    }
    if (__uuidof(IAudioVolumeLevel) == t) {
        return L"IAudioVolumeLevel";
    }
    if (__uuidof(IAudioPeakMeter) == t) {
        return L"IAudioPeakMeter";
    }
    if (__uuidof(IAudioAutoGainControl) == t) {
        return L"IAudioAutoGainControl";
    }
    if (__uuidof(IAudioBass) == t) {
        return L"IAudioBass";
    }
    if (__uuidof(IAudioChannelConfig) == t) {
        return L"IAudioChannelConfig";
    }
    if (__uuidof(IAudioInputSelector) == t) {
        return L"IAudioInputSelector";
    }
    if (__uuidof(IAudioLoudness) == t) {
        return L"IAudioLoudness";
    }
    if (__uuidof(IAudioMidrange) == t) {
        return L"IAudioMidrange";
    }
    if (__uuidof(IAudioOutputSelector) == t) {
        return L"IAudioOutputSelector";
    }
    if (__uuidof(IAudioTreble) == t) {
        return L"IAudioTreble";
    }
    if (__uuidof(IKsJackDescription) == t) {
        return L"IKsJackDescription";
    }
    if (__uuidof(IKsFormatSupport) == t) {
        return L"IKsFormatSupport";
    }

    if (KSDATAFORMAT_TYPE_STREAM == t) {
        return L"KSDATAFORMAT_TYPE_STREAM";
    }
    if (KSDATAFORMAT_TYPE_VIDEO == t) {
        return L"KSDATAFORMAT_TYPE_VIDEO";
    }
    if (KSDATAFORMAT_TYPE_AUDIO == t) {
        return L"KSDATAFORMAT_TYPE_AUDIO";
    }
    if (KSDATAFORMAT_TYPE_TEXT == t) {
        return L"KSDATAFORMAT_TYPE_TEXT";
    }
    if (KSDATAFORMAT_TYPE_MUSIC == t) {
        return L"KSDATAFORMAT_TYPE_MUSIC";
    }
    if (KSDATAFORMAT_TYPE_MIDI == t) {
        return L"KSDATAFORMAT_TYPE_MIDI";
    }
    if (KSDATAFORMAT_TYPE_STANDARD_ELEMENTARY_STREAM == t) {
        return L"KSDATAFORMAT_TYPE_STANDARD_ELEMENTARY_STREAM";
    }
    if (KSDATAFORMAT_TYPE_STANDARD_PES_PACKET == t) {
        return L"KSDATAFORMAT_TYPE_STANDARD_PES_PACKET";
    }
    if (KSDATAFORMAT_TYPE_STANDARD_PACK_HEADER == t) {
        return L"KSDATAFORMAT_TYPE_STANDARD_PACK_HEADER";
    }
    if (KSDATAFORMAT_TYPE_MPEG2_PES == t) {
        return L"KSDATAFORMAT_TYPE_MPEG2_PES";
    }
    if (KSDATAFORMAT_TYPE_MPEG2_PROGRAM == t) {
        return L"KSDATAFORMAT_TYPE_MPEG2_PROGRAM";
    }
    if (KSDATAFORMAT_TYPE_MPEG2_TRANSPORT == t) {
        return L"KSDATAFORMAT_TYPE_MPEG2_TRANSPORT";
    }
    if (KSDATAFORMAT_TYPE_IMAGE == t) {
        return L"KSDATAFORMAT_TYPE_IMAGE";
    }
    if (KSDATAFORMAT_TYPE_ANALOGVIDEO == t) {
        return L"KSDATAFORMAT_TYPE_ANALOGVIDEO";
    }
    if (KSDATAFORMAT_TYPE_ANALOGAUDIO == t) {
        return L"KSDATAFORMAT_TYPE_ANALOGAUDIO";
    }
    if (KSDATAFORMAT_TYPE_VBI == t) {
        return L"KSDATAFORMAT_TYPE_VBI";
    }
    if (KSDATAFORMAT_TYPE_NABTS == t) {
        return L"KSDATAFORMAT_TYPE_NABTS";
    }
    if (KSDATAFORMAT_TYPE_AUXLine21Data == t) {
        return L"KSDATAFORMAT_TYPE_AUXLine21Data";
    }
    if (KSDATAFORMAT_TYPE_DVD_ENCRYPTED_PACK == t) {
        return L"KSDATAFORMAT_TYPE_DVD_ENCRYPTED_PACK";
    }

    if (KSDATAFORMAT_SUBTYPE_NONE == t) {
        return L"KSDATAFORMAT_SUBTYPE_NONE";
    }
    if (KSDATAFORMAT_SUBTYPE_WAVEFORMATEX == t) {
        return L"KSDATAFORMAT_SUBTYPE_WAVEFORMATEX";
    }
    if (KSDATAFORMAT_SUBTYPE_ANALOG == t) {
        return L"KSDATAFORMAT_SUBTYPE_ANALOG";
    }
    if (KSDATAFORMAT_SUBTYPE_PCM == t) {
        return L"KSDATAFORMAT_SUBTYPE_PCM";
    }
    if (KSDATAFORMAT_SUBTYPE_IEEE_FLOAT == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEEE_FLOAT";
    }
    if (KSDATAFORMAT_SUBTYPE_DRM == t) {
        return L"KSDATAFORMAT_SUBTYPE_DRM";
    }
    if (KSDATAFORMAT_SUBTYPE_ALAW == t) {
        return L"KSDATAFORMAT_SUBTYPE_ALAW";
    }
    if (KSDATAFORMAT_SUBTYPE_MULAW == t) {
        return L"KSDATAFORMAT_SUBTYPE_MULAW";
    }
    if (KSDATAFORMAT_SUBTYPE_ADPCM == t) {
        return L"KSDATAFORMAT_SUBTYPE_ADPCM";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG";
    }
    if (KSDATAFORMAT_SUBTYPE_RIFF == t) {
        return L"KSDATAFORMAT_SUBTYPE_RIFF";
    }
    if (KSDATAFORMAT_SUBTYPE_RIFFWAVE == t) {
        return L"KSDATAFORMAT_SUBTYPE_RIFFWAVE";
    }
    if (KSDATAFORMAT_SUBTYPE_MIDI == t) {
        return L"KSDATAFORMAT_SUBTYPE_MIDI";
    }
    if (KSDATAFORMAT_SUBTYPE_MIDI_BUS == t) {
        return L"KSDATAFORMAT_SUBTYPE_MIDI_BUS";
    }
    if (KSDATAFORMAT_SUBTYPE_RIFFMIDI == t) {
        return L"KSDATAFORMAT_SUBTYPE_RIFFMIDI";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_VIDEO == t) {
        return L"KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_VIDEO == t) {
        return L"KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_AC3_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_STANDARD_AC3_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_VIDEO == t) {
        return L"KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_DSS_VIDEO == t) {
        return L"KSDATAFORMAT_SUBTYPE_DSS_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_DSS_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_DSS_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG1Packet == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG1Packet";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG1Payload == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG1Payload";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG1Video == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG1Video";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG2_VIDEO == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG2_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_LPCM_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_LPCM_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_AC3_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_AC3_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_WMA_PRO == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_WMA_PRO";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DTS == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DTS";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_MPEG1 == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_MPEG1";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_MPEG2 == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_MPEG2";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_MPEG3 == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_MPEG3";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_AAC == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_AAC";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_ATRAC == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_ATRAC";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_ONE_BIT_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_ONE_BIT_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS_ATMOS == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS_ATMOS";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DTS_HD == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DTS_HD";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MLP == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MLP";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT20 == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT20";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT21 == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT21";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DST == t) {
        return L"KSDATAFORMAT_SUBTYPE_IEC61937_DST";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEGLAYER3 == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEGLAYER3";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG_HEAAC == t) {
        return L"KSDATAFORMAT_SUBTYPE_MPEG_HEAAC";
    }
    if (KSDATAFORMAT_SUBTYPE_WMAUDIO2 == t) {
        return L"KSDATAFORMAT_SUBTYPE_WMAUDIO2";
    }
    if (KSDATAFORMAT_SUBTYPE_WMAUDIO3 == t) {
        return L"KSDATAFORMAT_SUBTYPE_WMAUDIO3";
    }
    if (KSDATAFORMAT_SUBTYPE_WMAUDIO_LOSSLESS == t) {
        return L"KSDATAFORMAT_SUBTYPE_WMAUDIO_LOSSLESS";
    }
    if (KSDATAFORMAT_SUBTYPE_DTS_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_DTS_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_SDDS_AUDIO == t) {
        return L"KSDATAFORMAT_SUBTYPE_SDDS_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_SUBPICTURE == t) {
        return L"KSDATAFORMAT_SUBTYPE_SUBPICTURE";
    }
    if (KSDATAFORMAT_SUBTYPE_VPVideo == t) {
        return L"KSDATAFORMAT_SUBTYPE_VPVideo";
    }
    if (KSDATAFORMAT_SUBTYPE_VPVBI == t) {
        return L"KSDATAFORMAT_SUBTYPE_VPVBI";
    }
    if (KSDATAFORMAT_SUBTYPE_JPEG == t) {
        return L"KSDATAFORMAT_SUBTYPE_JPEG";
    }
    if (KSDATAFORMAT_SUBTYPE_IMAGE_RGB32 == t) {
        return L"KSDATAFORMAT_SUBTYPE_IMAGE_RGB32";
    }
    if (KSDATAFORMAT_SUBTYPE_L8 == t) {
        return L"KSDATAFORMAT_SUBTYPE_L8";
    }
    if (KSDATAFORMAT_SUBTYPE_L8_IR == t) {
        return L"KSDATAFORMAT_SUBTYPE_L8_IR";
    }
    if (KSDATAFORMAT_SUBTYPE_L8_CUSTOM == t) {
        return L"KSDATAFORMAT_SUBTYPE_L8_CUSTOM";
    }
    if (KSDATAFORMAT_SUBTYPE_L16 == t) {
        return L"KSDATAFORMAT_SUBTYPE_L16";
    }
    if (KSDATAFORMAT_SUBTYPE_L16_IR == t) {
        return L"KSDATAFORMAT_SUBTYPE_L16_IR";
    }
    if (KSDATAFORMAT_SUBTYPE_D16 == t) {
        return L"KSDATAFORMAT_SUBTYPE_D16";
    }
    if (KSDATAFORMAT_SUBTYPE_L16_CUSTOM == t) {
        return L"KSDATAFORMAT_SUBTYPE_L16_CUSTOM";
    }
    if (KSDATAFORMAT_SUBTYPE_MJPG_IR == t) {
        return L"KSDATAFORMAT_SUBTYPE_MJPG_IR";
    }
    if (KSDATAFORMAT_SUBTYPE_MJPG_DEPTH == t) {
        return L"KSDATAFORMAT_SUBTYPE_MJPG_DEPTH";
    }
    if (KSDATAFORMAT_SUBTYPE_MJPG_CUSTOM == t) {
        return L"KSDATAFORMAT_SUBTYPE_MJPG_CUSTOM";
    }
    if (KSDATAFORMAT_SUBTYPE_RAW8 == t) {
        return L"KSDATAFORMAT_SUBTYPE_RAW8";
    }
    if (KSDATAFORMAT_SUBTYPE_CC == t) {
        return L"KSDATAFORMAT_SUBTYPE_CC";
    }
    if (KSDATAFORMAT_SUBTYPE_NABTS == t) {
        return L"KSDATAFORMAT_SUBTYPE_NABTS";
    }
    if (KSDATAFORMAT_SUBTYPE_TELETEXT == t) {
        return L"KSDATAFORMAT_SUBTYPE_TELETEXT";
    }
    if (KSDATAFORMAT_SUBTYPE_NABTS_FEC == t) {
        return L"KSDATAFORMAT_SUBTYPE_NABTS_FEC";
    }
    if (KSDATAFORMAT_SUBTYPE_OVERLAY == t) {
        return L"KSDATAFORMAT_SUBTYPE_OVERLAY";
    }
    if (KSDATAFORMAT_SUBTYPE_Line21_BytePair == t) {
        return L"KSDATAFORMAT_SUBTYPE_Line21_BytePair";
    }
    if (KSDATAFORMAT_SUBTYPE_Line21_GOPPacket == t) {
        return L"KSDATAFORMAT_SUBTYPE_Line21_GOPPacket";
    }

    if (KSDATAFORMAT_SPECIFIER_VC_ID == t) {
        return L"KSDATAFORMAT_SPECIFIER_VC_ID";
    }
    if (KSDATAFORMAT_SPECIFIER_WAVEFORMATEX == t) {
        return L"KSDATAFORMAT_SPECIFIER_WAVEFORMATEX";
    }
    if (KSDATAFORMAT_SPECIFIER_DSOUND == t) {
        return L"KSDATAFORMAT_SPECIFIER_DSOUND";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_AUDIO == t) {
        return L"KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_VIDEO == t) {
        return L"KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_AUDIO == t) {
        return L"KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_AC3_AUDIO == t) {
        return L"KSDATAFORMAT_SPECIFIER_DIALECT_AC3_AUDIO";
    }

    if (KSDATAFORMAT_SPECIFIER_MPEG1_VIDEO == t) {
        return L"KSDATAFORMAT_SPECIFIER_MPEG1_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_MPEG2_VIDEO == t) {
        return L"KSDATAFORMAT_SPECIFIER_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_MPEG2_AUDIO == t) {
        return L"KSDATAFORMAT_SPECIFIER_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_LPCM_AUDIO == t) {
        return L"KSDATAFORMAT_SPECIFIER_LPCM_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_AC3_AUDIO == t) {
        return L"KSDATAFORMAT_SPECIFIER_AC3_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_VIDEOINFO == t) {
        return L"KSDATAFORMAT_SPECIFIER_VIDEOINFO";
    }
    if (KSDATAFORMAT_SPECIFIER_VIDEOINFO2 == t) {
        return L"KSDATAFORMAT_SPECIFIER_VIDEOINFO2";
    }
    if (KSDATAFORMAT_SPECIFIER_H264_VIDEO == t) {
        return L"KSDATAFORMAT_SPECIFIER_H264_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_JPEG_IMAGE == t) {
        return L"KSDATAFORMAT_SPECIFIER_JPEG_IMAGE";
    }
    if (KSDATAFORMAT_SPECIFIER_IMAGE == t) {
        return L"KSDATAFORMAT_SPECIFIER_IMAGE";
    }
    if (KSDATAFORMAT_SPECIFIER_ANALOGVIDEO == t) {
        return L"KSDATAFORMAT_SPECIFIER_ANALOGVIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_VBI == t) {
        return L"KSDATAFORMAT_SPECIFIER_VBI";
    }
    if (__uuidof(IKsJackDescription2) == t) {
        return L"IKsJackDescription2";
    }

    OLECHAR* guidString = nullptr;
    StringFromCLSID(t, &guidString);

    wchar_t s[256];
    swprintf_s(s, L"Unknown%s", guidString);

    std::wstring r(s);

    CoTaskMemFree(guidString);

    return r;
}

