// 日本語
#include "WWGuidToStr.h"
#include <mmdeviceapi.h>
#include <MMDeviceAPI.h>
#include <AudioClient.h>
#include <AudioPolicy.h>
#include <devicetopology.h>

const std::string
WWGuidToStr(GUID &t)
{
    if (GUID_NULL == t) {
        return "GUID_NULL";
    }

    if (KSNODETYPE_INPUT_UNDEFINED == t) {
        return "KSNODETYPE_INPUT_UNDEFINED";
    }
    if (KSNODETYPE_MICROPHONE == t) {
        return "KSNODETYPE_MICROPHONE";
    }
    if (KSNODETYPE_DESKTOP_MICROPHONE == t) {
        return "KSNODETYPE_DESKTOP_MICROPHONE";
    }
    if (KSNODETYPE_PERSONAL_MICROPHONE == t) {
        return "KSNODETYPE_PERSONAL_MICROPHONE";
    }
    if (KSNODETYPE_OMNI_DIRECTIONAL_MICROPHONE == t) {
        return "KSNODETYPE_OMNI_DIRECTIONAL_MICROPHONE";
    }
    if (KSNODETYPE_MICROPHONE_ARRAY == t) {
        return "KSNODETYPE_MICROPHONE_ARRAY";
    }
    if (KSNODETYPE_PROCESSING_MICROPHONE_ARRAY == t) {
        return "KSNODETYPE_PROCESSING_MICROPHONE_ARRAY";
    }
    if (KSNODETYPE_OUTPUT_UNDEFINED == t) {
        return "KSNODETYPE_OUTPUT_UNDEFINED";
    }
    if (KSNODETYPE_SPEAKER == t) {
        return "KSNODETYPE_SPEAKER";
    }
    if (KSNODETYPE_HEADPHONES == t) {
        return "KSNODETYPE_HEADPHONES";
    }
    if (KSNODETYPE_HEAD_MOUNTED_DISPLAY_AUDIO == t) {
        return "KSNODETYPE_HEAD_MOUNTED_DISPLAY_AUDIO";
    }
    if (KSNODETYPE_DESKTOP_SPEAKER == t) {
        return "KSNODETYPE_DESKTOP_SPEAKER";
    }
    if (KSNODETYPE_ROOM_SPEAKER == t) {
        return "KSNODETYPE_ROOM_SPEAKER";
    }
    if (KSNODETYPE_COMMUNICATION_SPEAKER == t) {
        return "KSNODETYPE_COMMUNICATION_SPEAKER";
    }
    if (KSNODETYPE_LOW_FREQUENCY_EFFECTS_SPEAKER == t) {
        return "KSNODETYPE_LOW_FREQUENCY_EFFECTS_SPEAKER";
    }
    if (KSNODETYPE_BIDIRECTIONAL_UNDEFINED == t) {
        return "KSNODETYPE_BIDIRECTIONAL_UNDEFINED";
    }
    if (KSNODETYPE_HANDSET == t) {
        return "KSNODETYPE_HANDSET";
    }
    if (KSNODETYPE_HEADSET_MICROPHONE == t) {
        return "KSNODETYPE_HEADSET_MICROPHONE";
    }
    if (KSNODETYPE_HEADSET_SPEAKERS == t) {
        return "KSNODETYPE_HEADSET_SPEAKERS";
    }
    if (KSNODETYPE_HEADSET == t) {
        return "KSNODETYPE_HEADSET";
    }
    if (KSNODETYPE_SPEAKERPHONE_NO_ECHO_REDUCTION == t) {
        return "KSNODETYPE_SPEAKERPHONE_NO_ECHO_REDUCTION";
    }
    if (KSNODETYPE_ECHO_SUPPRESSING_SPEAKERPHONE == t) {
        return "KSNODETYPE_ECHO_SUPPRESSING_SPEAKERPHONE";
    }
    if (KSNODETYPE_ECHO_CANCELING_SPEAKERPHONE == t) {
        return "KSNODETYPE_ECHO_CANCELING_SPEAKERPHONE";
    }
    if (KSNODETYPE_TELEPHONY_UNDEFINED == t) {
        return "KSNODETYPE_TELEPHONY_UNDEFINED";
    }
    if (KSNODETYPE_PHONE_LINE == t) {
        return "KSNODETYPE_PHONE_LINE";
    }
    if (KSNODETYPE_TELEPHONE == t) {
        return "KSNODETYPE_TELEPHONE";
    }
    if (KSNODETYPE_DOWN_LINE_PHONE == t) {
        return "KSNODETYPE_DOWN_LINE_PHONE";
    }
    if (KSNODETYPE_EXTERNAL_UNDEFINED == t) {
        return "KSNODETYPE_EXTERNAL_UNDEFINED";
    }
    if (KSNODETYPE_ANALOG_CONNECTOR == t) {
        return "KSNODETYPE_ANALOG_CONNECTOR";
    }
    if (KSNODETYPE_DIGITAL_AUDIO_INTERFACE == t) {
        return "KSNODETYPE_DIGITAL_AUDIO_INTERFACE";
    }
    if (KSNODETYPE_LINE_CONNECTOR == t) {
        return "KSNODETYPE_LINE_CONNECTOR";
    }
    if (KSNODETYPE_LEGACY_AUDIO_CONNECTOR == t) {
        return "KSNODETYPE_LEGACY_AUDIO_CONNECTOR";
    }
    if (KSNODETYPE_SPDIF_INTERFACE == t) {
        return "KSNODETYPE_SPDIF_INTERFACE";
    }
    if (KSNODETYPE_1394_DA_STREAM == t) {
        return "KSNODETYPE_1394_DA_STREAM";
    }
    if (KSNODETYPE_1394_DV_STREAM_SOUNDTRACK == t) {
        return "KSNODETYPE_1394_DV_STREAM_SOUNDTRACK";
    }
    if (KSNODETYPE_EMBEDDED_UNDEFINED == t) {
        return "KSNODETYPE_EMBEDDED_UNDEFINED";
    }
    if (KSNODETYPE_LEVEL_CALIBRATION_NOISE_SOURCE == t) {
        return "KSNODETYPE_LEVEL_CALIBRATION_NOISE_SOURCE";
    }
    if (KSNODETYPE_EQUALIZATION_NOISE == t) {
        return "KSNODETYPE_EQUALIZATION_NOISE";
    }
    if (KSNODETYPE_CD_PLAYER == t) {
        return "KSNODETYPE_CD_PLAYER";
    }
    if (KSNODETYPE_DAT_IO_DIGITAL_AUDIO_TAPE == t) {
        return "KSNODETYPE_DAT_IO_DIGITAL_AUDIO_TAPE";
    }
    if (KSNODETYPE_DCC_IO_DIGITAL_COMPACT_CASSETTE == t) {
        return "KSNODETYPE_DCC_IO_DIGITAL_COMPACT_CASSETTE";
    }
    if (KSNODETYPE_MINIDISK == t) {
        return "KSNODETYPE_MINIDISK";
    }
    if (KSNODETYPE_ANALOG_TAPE == t) {
        return "KSNODETYPE_ANALOG_TAPE";
    }
    if (KSNODETYPE_PHONOGRAPH == t) {
        return "KSNODETYPE_PHONOGRAPH";
    }
    if (KSNODETYPE_VCR_AUDIO == t) {
        return "KSNODETYPE_VCR_AUDIO";
    }
    if (KSNODETYPE_VIDEO_DISC_AUDIO == t) {
        return "KSNODETYPE_VIDEO_DISC_AUDIO";
    }
    if (KSNODETYPE_DVD_AUDIO == t) {
        return "KSNODETYPE_DVD_AUDIO";
    }
    if (KSNODETYPE_TV_TUNER_AUDIO == t) {
        return "KSNODETYPE_TV_TUNER_AUDIO";
    }
    if (KSNODETYPE_SATELLITE_RECEIVER_AUDIO == t) {
        return "KSNODETYPE_SATELLITE_RECEIVER_AUDIO";
    }
    if (KSNODETYPE_CABLE_TUNER_AUDIO == t) {
        return "KSNODETYPE_CABLE_TUNER_AUDIO";
    }
    if (KSNODETYPE_DSS_AUDIO == t) {
        return "KSNODETYPE_DSS_AUDIO";
    }
    if (KSNODETYPE_RADIO_RECEIVER == t) {
        return "KSNODETYPE_RADIO_RECEIVER";
    }
    if (KSNODETYPE_RADIO_TRANSMITTER == t) {
        return "KSNODETYPE_RADIO_TRANSMITTER";
    }
    if (KSNODETYPE_MULTITRACK_RECORDER == t) {
        return "KSNODETYPE_MULTITRACK_RECORDER";
    }
    if (KSNODETYPE_SYNTHESIZER == t) {
        return "KSNODETYPE_SYNTHESIZER";
    }
    if (KSNODETYPE_HDMI_INTERFACE == t) {
        return "KSNODETYPE_HDMI_INTERFACE";
    }
    if (KSNODETYPE_DISPLAYPORT_INTERFACE == t) {
        return "KSNODETYPE_DISPLAYPORT_INTERFACE";
    }
    if (KSNODETYPE_AUDIO_LOOPBACK == t) {
        return "KSNODETYPE_AUDIO_LOOPBACK";
    }
    if (KSNODETYPE_AUDIO_KEYWORDDETECTOR == t) {
        return "KSNODETYPE_AUDIO_KEYWORDDETECTOR";
    }
    if (KSNODETYPE_MIDI_JACK == t) {
        return "KSNODETYPE_MIDI_JACK";
    }
    if (KSNODETYPE_MIDI_ELEMENT == t) {
        return "KSNODETYPE_MIDI_ELEMENT";
    }
    if (KSNODETYPE_AUDIO_ENGINE == t) {
        return "KSNODETYPE_AUDIO_ENGINE";
    }
    if (KSNODETYPE_SPEAKERS_STATIC_JACK == t) {
        return "KSNODETYPE_SPEAKERS_STATIC_JACK";
    }
    if (KSNODETYPE_DRM_DESCRAMBLE == t) {
        return "KSNODETYPE_DRM_DESCRAMBLE";
    }
    if (KSNODETYPE_TELEPHONY_BIDI == t) {
        return "KSNODETYPE_TELEPHONY_BIDI";
    }
    if (KSNODETYPE_FM_RX == t) {
        return "KSNODETYPE_FM_RX";
    }
    if (KSNODETYPE_DAC == t) {
        return "KSNODETYPE_DAC";
    }
    if (KSNODETYPE_ADC == t) {
        return "KSNODETYPE_ADC";
    }
    if (KSNODETYPE_SRC == t) {
        return "KSNODETYPE_SRC";
    }
    if (KSNODETYPE_SUPERMIX == t) {
        return "KSNODETYPE_SUPERMIX";
    }
    if (KSNODETYPE_MUX == t) {
        return "KSNODETYPE_MUX";
    }
    if (KSNODETYPE_DEMUX == t) {
        return "KSNODETYPE_DEMUX";
    }
    if (KSNODETYPE_SUM == t) {
        return "KSNODETYPE_SUM";
    }
    if (KSNODETYPE_MUTE == t) {
        return "KSNODETYPE_MUTE";
    }
    if (KSNODETYPE_VOLUME == t) {
        return "KSNODETYPE_VOLUME";
    }
    if (KSNODETYPE_TONE == t) {
        return "KSNODETYPE_TONE";
    }
    if (KSNODETYPE_EQUALIZER == t) {
        return "KSNODETYPE_EQUALIZER";
    }
    if (KSNODETYPE_NOISE_SUPPRESS == t) {
        return "KSNODETYPE_NOISE_SUPPRESS";
    }
    if (KSNODETYPE_DELAY == t) {
        return "KSNODETYPE_DELAY";
    }
    if (KSNODETYPE_LOUDNESS == t) {
        return "KSNODETYPE_LOUDNESS";
    }
    if (KSNODETYPE_PROLOGIC_DECODER == t) {
        return "KSNODETYPE_PROLOGIC_DECODER";
    }
    if (KSNODETYPE_STEREO_WIDE == t) {
        return "KSNODETYPE_STEREO_WIDE";
    }
    if (KSNODETYPE_REVERB == t) {
        return "KSNODETYPE_REVERB";
    }
    if (KSNODETYPE_CHORUS == t) {
        return "KSNODETYPE_CHORUS";
    }
    if (KSNODETYPE_3D_EFFECTS == t) {
        return "KSNODETYPE_3D_EFFECTS";
    }
    if (KSNODETYPE_PARAMETRIC_EQUALIZER == t) {
        return "KSNODETYPE_PARAMETRIC_EQUALIZER";
    }
    if (KSNODETYPE_UPDOWN_MIX == t) {
        return "KSNODETYPE_UPDOWN_MIX";
    }
    if (KSNODETYPE_DYN_RANGE_COMPRESSOR == t) {
        return "KSNODETYPE_DYN_RANGE_COMPRESSOR";
    }
    if (KSNODETYPE_ACOUSTIC_ECHO_CANCEL == t) {
        return "KSNODETYPE_ACOUSTIC_ECHO_CANCEL";
    }
    if (KSNODETYPE_MICROPHONE_ARRAY_PROCESSOR == t) {
        return "KSNODETYPE_MICROPHONE_ARRAY_PROCESSOR";
    }
    if (KSNODETYPE_DEV_SPECIFIC == t) {
        return "KSNODETYPE_DEV_SPECIFIC";
    }
    if (KSNODETYPE_SURROUND_ENCODER == t) {
        return "KSNODETYPE_SURROUND_ENCODER";
    }
    if (KSNODETYPE_PEAKMETER == t) {
        return "KSNODETYPE_PEAKMETER";
    }
    if (KSNODETYPE_VIDEO_STREAMING == t) {
        return "KSNODETYPE_VIDEO_STREAMING";
    }
    if (KSNODETYPE_VIDEO_INPUT_TERMINAL == t) {
        return "KSNODETYPE_VIDEO_INPUT_TERMINAL";
    }
    if (KSNODETYPE_VIDEO_OUTPUT_TERMINAL == t) {
        return "KSNODETYPE_VIDEO_OUTPUT_TERMINAL";
    }
    if (KSNODETYPE_VIDEO_SELECTOR == t) {
        return "KSNODETYPE_VIDEO_SELECTOR";
    }
    if (KSNODETYPE_VIDEO_PROCESSING == t) {
        return "KSNODETYPE_VIDEO_PROCESSING";
    }
    if (KSNODETYPE_VIDEO_CAMERA_TERMINAL == t) {
        return "KSNODETYPE_VIDEO_CAMERA_TERMINAL";
    }
    if (KSNODETYPE_VIDEO_INPUT_MTT == t) {
        return "KSNODETYPE_VIDEO_INPUT_MTT";
    }
    if (KSNODETYPE_VIDEO_OUTPUT_MTT == t) {
        return "KSNODETYPE_VIDEO_OUTPUT_MTT";
    }

    if (KSCATEGORY_MICROPHONE_ARRAY_PROCESSOR == t) {
        return "KSCATEGORY_MICROPHONE_ARRAY_PROCESSOR";
    }
    if (KSCATEGORY_AUDIO == t) {
        return "KSCATEGORY_AUDIO";
    }
    if (KSCATEGORY_VIDEO == t) {
        return "KSCATEGORY_VIDEO";
    }
    if (KSCATEGORY_REALTIME == t) {
        return "KSCATEGORY_REALTIME";
    }
    if (KSCATEGORY_TEXT == t) {
        return "KSCATEGORY_TEXT";
    }
    if (KSCATEGORY_NETWORK == t) {
        return "KSCATEGORY_NETWORK";
    }
    if (KSCATEGORY_TOPOLOGY == t) {
        return "KSCATEGORY_TOPOLOGY";
    }
    if (KSCATEGORY_VIRTUAL == t) {
        return "KSCATEGORY_VIRTUAL";
    }
    if (KSCATEGORY_ACOUSTIC_ECHO_CANCEL == t) {
        return "KSCATEGORY_ACOUSTIC_ECHO_CANCEL";
    }
    if (KSCATEGORY_SYNTHESIZER == t) {
        return "KSCATEGORY_SYNTHESIZER";
    }
    if (KSCATEGORY_DRM_DESCRAMBLE == t) {
        return "KSCATEGORY_DRM_DESCRAMBLE";
    }
    if (KSCATEGORY_WDMAUD_USE_PIN_NAME == t) {
        return "KSCATEGORY_WDMAUD_USE_PIN_NAME";
    }
    if (KSCATEGORY_ESCALANTE_PLATFORM_DRIVER == t) {
        return "KSCATEGORY_ESCALANTE_PLATFORM_DRIVER";
    }
    if (KSCATEGORY_TVTUNER == t) {
        return "KSCATEGORY_TVTUNER";
    }
    if (KSCATEGORY_CROSSBAR == t) {
        return "KSCATEGORY_CROSSBAR";
    }
    if (KSCATEGORY_TVAUDIO == t) {
        return "KSCATEGORY_TVAUDIO";
    }
    if (KSCATEGORY_VPMUX == t) {
        return "KSCATEGORY_VPMUX";
    }
    if (KSCATEGORY_VBICODEC == t) {
        return "KSCATEGORY_VBICODEC";
    }
    if (KSCATEGORY_ENCODER == t) {
        return "KSCATEGORY_ENCODER";
    }
    if (KSCATEGORY_MULTIPLEXER == t) {
        return "KSCATEGORY_MULTIPLEXER";
    }
    if (KSCATEGORY_RENDER == t) {
        return "KSCATEGORY_RENDER";
    }

    if (PINNAME_CAPTURE == t) {
        return "PINNAME_CAPTURE";
    }
    if (PINNAME_VIDEO_CC_CAPTURE == t) {
        return "PINNAME_VIDEO_CC_CAPTURE";
    }
    if (PINNAME_VIDEO_NABTS_CAPTURE == t) {
        return "PINNAME_VIDEO_NABTS_CAPTURE";
    }
    if (PINNAME_PREVIEW == t) {
        return "PINNAME_PREVIEW";
    }
    if (PINNAME_VIDEO_ANALOGVIDEOIN == t) {
        return "PINNAME_VIDEO_ANALOGVIDEOIN";
    }
    if (PINNAME_VIDEO_VBI == t) {
        return "PINNAME_VIDEO_VBI";
    }
    if (PINNAME_VIDEO_VIDEOPORT == t) {
        return "PINNAME_VIDEO_VIDEOPORT";
    }
    if (PINNAME_VIDEO_NABTS == t) {
        return "PINNAME_VIDEO_NABTS";
    }
    if (PINNAME_VIDEO_EDS == t) {
        return "PINNAME_VIDEO_EDS";
    }
    if (PINNAME_VIDEO_TELETEXT == t) {
        return "PINNAME_VIDEO_TELETEXT";
    }
    if (PINNAME_VIDEO_CC == t) {
        return "PINNAME_VIDEO_CC";
    }
    if (PINNAME_VIDEO_STILL == t) {
        return "PINNAME_VIDEO_STILL";
    }
    if (PINNAME_IMAGE == t) {
        return "PINNAME_IMAGE";
    }
    if (PINNAME_VIDEO_TIMECODE == t) {
        return "PINNAME_VIDEO_TIMECODE";
    }
    if (PINNAME_VIDEO_VIDEOPORT_VBI == t) {
        return "PINNAME_VIDEO_VIDEOPORT_VBI";
    }

    if (__uuidof(IAudioMute) == t) {
        return "IAudioMute";
    }
    if (__uuidof(IAudioVolumeLevel) == t) {
        return "IAudioVolumeLevel";
    }
    if (__uuidof(IAudioPeakMeter) == t) {
        return "IAudioPeakMeter";
    }
    if (__uuidof(IAudioAutoGainControl) == t) {
        return "IAudioAutoGainControl";
    }
    if (__uuidof(IAudioBass) == t) {
        return "IAudioBass";
    }
    if (__uuidof(IAudioChannelConfig) == t) {
        return "IAudioChannelConfig";
    }
    if (__uuidof(IAudioInputSelector) == t) {
        return "IAudioInputSelector";
    }
    if (__uuidof(IAudioLoudness) == t) {
        return "IAudioLoudness";
    }
    if (__uuidof(IAudioMidrange) == t) {
        return "IAudioMidrange";
    }
    if (__uuidof(IAudioOutputSelector) == t) {
        return "IAudioOutputSelector";
    }
    if (__uuidof(IAudioTreble) == t) {
        return "IAudioTreble";
    }
    if (__uuidof(IKsJackDescription) == t) {
        return "IKsJackDescription";
    }
    if (__uuidof(IKsFormatSupport) == t) {
        return "IKsFormatSupport";
    }

    if (KSDATAFORMAT_TYPE_STREAM == t) {
        return "KSDATAFORMAT_TYPE_STREAM";
    }
    if (KSDATAFORMAT_TYPE_VIDEO == t) {
        return "KSDATAFORMAT_TYPE_VIDEO";
    }
    if (KSDATAFORMAT_TYPE_AUDIO == t) {
        return "KSDATAFORMAT_TYPE_AUDIO";
    }
    if (KSDATAFORMAT_TYPE_TEXT == t) {
        return "KSDATAFORMAT_TYPE_TEXT";
    }
    if (KSDATAFORMAT_TYPE_MUSIC == t) {
        return "KSDATAFORMAT_TYPE_MUSIC";
    }
    if (KSDATAFORMAT_TYPE_MIDI == t) {
        return "KSDATAFORMAT_TYPE_MIDI";
    }
    if (KSDATAFORMAT_TYPE_STANDARD_ELEMENTARY_STREAM == t) {
        return "KSDATAFORMAT_TYPE_STANDARD_ELEMENTARY_STREAM";
    }
    if (KSDATAFORMAT_TYPE_STANDARD_PES_PACKET == t) {
        return "KSDATAFORMAT_TYPE_STANDARD_PES_PACKET";
    }
    if (KSDATAFORMAT_TYPE_STANDARD_PACK_HEADER == t) {
        return "KSDATAFORMAT_TYPE_STANDARD_PACK_HEADER";
    }
    if (KSDATAFORMAT_TYPE_MPEG2_PES == t) {
        return "KSDATAFORMAT_TYPE_MPEG2_PES";
    }
    if (KSDATAFORMAT_TYPE_MPEG2_PROGRAM == t) {
        return "KSDATAFORMAT_TYPE_MPEG2_PROGRAM";
    }
    if (KSDATAFORMAT_TYPE_MPEG2_TRANSPORT == t) {
        return "KSDATAFORMAT_TYPE_MPEG2_TRANSPORT";
    }
    if (KSDATAFORMAT_TYPE_IMAGE == t) {
        return "KSDATAFORMAT_TYPE_IMAGE";
    }
    if (KSDATAFORMAT_TYPE_ANALOGVIDEO == t) {
        return "KSDATAFORMAT_TYPE_ANALOGVIDEO";
    }
    if (KSDATAFORMAT_TYPE_ANALOGAUDIO == t) {
        return "KSDATAFORMAT_TYPE_ANALOGAUDIO";
    }
    if (KSDATAFORMAT_TYPE_VBI == t) {
        return "KSDATAFORMAT_TYPE_VBI";
    }
    if (KSDATAFORMAT_TYPE_NABTS == t) {
        return "KSDATAFORMAT_TYPE_NABTS";
    }
    if (KSDATAFORMAT_TYPE_AUXLine21Data == t) {
        return "KSDATAFORMAT_TYPE_AUXLine21Data";
    }
    if (KSDATAFORMAT_TYPE_DVD_ENCRYPTED_PACK == t) {
        return "KSDATAFORMAT_TYPE_DVD_ENCRYPTED_PACK";
    }

    if (KSDATAFORMAT_SUBTYPE_NONE == t) {
        return "KSDATAFORMAT_SUBTYPE_NONE";
    }
    if (KSDATAFORMAT_SUBTYPE_WAVEFORMATEX == t) {
        return "KSDATAFORMAT_SUBTYPE_WAVEFORMATEX";
    }
    if (KSDATAFORMAT_SUBTYPE_ANALOG == t) {
        return "KSDATAFORMAT_SUBTYPE_ANALOG";
    }
    if (KSDATAFORMAT_SUBTYPE_PCM == t) {
        return "KSDATAFORMAT_SUBTYPE_PCM";
    }
    if (KSDATAFORMAT_SUBTYPE_IEEE_FLOAT == t) {
        return "KSDATAFORMAT_SUBTYPE_IEEE_FLOAT";
    }
    if (KSDATAFORMAT_SUBTYPE_DRM == t) {
        return "KSDATAFORMAT_SUBTYPE_DRM";
    }
    if (KSDATAFORMAT_SUBTYPE_ALAW == t) {
        return "KSDATAFORMAT_SUBTYPE_ALAW";
    }
    if (KSDATAFORMAT_SUBTYPE_MULAW == t) {
        return "KSDATAFORMAT_SUBTYPE_MULAW";
    }
    if (KSDATAFORMAT_SUBTYPE_ADPCM == t) {
        return "KSDATAFORMAT_SUBTYPE_ADPCM";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG";
    }
    if (KSDATAFORMAT_SUBTYPE_RIFF == t) {
        return "KSDATAFORMAT_SUBTYPE_RIFF";
    }
    if (KSDATAFORMAT_SUBTYPE_RIFFWAVE == t) {
        return "KSDATAFORMAT_SUBTYPE_RIFFWAVE";
    }
    if (KSDATAFORMAT_SUBTYPE_MIDI == t) {
        return "KSDATAFORMAT_SUBTYPE_MIDI";
    }
    if (KSDATAFORMAT_SUBTYPE_MIDI_BUS == t) {
        return "KSDATAFORMAT_SUBTYPE_MIDI_BUS";
    }
    if (KSDATAFORMAT_SUBTYPE_RIFFMIDI == t) {
        return "KSDATAFORMAT_SUBTYPE_RIFFMIDI";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_VIDEO == t) {
        return "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG1_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_VIDEO == t) {
        return "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_STANDARD_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_STANDARD_AC3_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_STANDARD_AC3_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_VIDEO == t) {
        return "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_DSS_VIDEO == t) {
        return "KSDATAFORMAT_SUBTYPE_DSS_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_DSS_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_DSS_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG1Packet == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG1Packet";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG1Payload == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG1Payload";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG1Video == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG1Video";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG2_VIDEO == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG2_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_LPCM_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_LPCM_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_AC3_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_AC3_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_WMA_PRO == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_WMA_PRO";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DTS == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DTS";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_MPEG1 == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_MPEG1";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_MPEG2 == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_MPEG2";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_MPEG3 == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_MPEG3";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_AAC == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_AAC";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_ATRAC == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_ATRAC";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_ONE_BIT_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_ONE_BIT_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS_ATMOS == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS_ATMOS";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DTS_HD == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DTS_HD";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MLP == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MLP";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT20 == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT20";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT21 == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_MAT21";
    }
    if (KSDATAFORMAT_SUBTYPE_IEC61937_DST == t) {
        return "KSDATAFORMAT_SUBTYPE_IEC61937_DST";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEGLAYER3 == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEGLAYER3";
    }
    if (KSDATAFORMAT_SUBTYPE_MPEG_HEAAC == t) {
        return "KSDATAFORMAT_SUBTYPE_MPEG_HEAAC";
    }
    if (KSDATAFORMAT_SUBTYPE_WMAUDIO2 == t) {
        return "KSDATAFORMAT_SUBTYPE_WMAUDIO2";
    }
    if (KSDATAFORMAT_SUBTYPE_WMAUDIO3 == t) {
        return "KSDATAFORMAT_SUBTYPE_WMAUDIO3";
    }
    if (KSDATAFORMAT_SUBTYPE_WMAUDIO_LOSSLESS == t) {
        return "KSDATAFORMAT_SUBTYPE_WMAUDIO_LOSSLESS";
    }
    if (KSDATAFORMAT_SUBTYPE_DTS_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_DTS_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_SDDS_AUDIO == t) {
        return "KSDATAFORMAT_SUBTYPE_SDDS_AUDIO";
    }
    if (KSDATAFORMAT_SUBTYPE_SUBPICTURE == t) {
        return "KSDATAFORMAT_SUBTYPE_SUBPICTURE";
    }
    if (KSDATAFORMAT_SUBTYPE_VPVideo == t) {
        return "KSDATAFORMAT_SUBTYPE_VPVideo";
    }
    if (KSDATAFORMAT_SUBTYPE_VPVBI == t) {
        return "KSDATAFORMAT_SUBTYPE_VPVBI";
    }
    if (KSDATAFORMAT_SUBTYPE_JPEG == t) {
        return "KSDATAFORMAT_SUBTYPE_JPEG";
    }
    if (KSDATAFORMAT_SUBTYPE_IMAGE_RGB32 == t) {
        return "KSDATAFORMAT_SUBTYPE_IMAGE_RGB32";
    }
    if (KSDATAFORMAT_SUBTYPE_L8 == t) {
        return "KSDATAFORMAT_SUBTYPE_L8";
    }
    if (KSDATAFORMAT_SUBTYPE_L8_IR == t) {
        return "KSDATAFORMAT_SUBTYPE_L8_IR";
    }
    if (KSDATAFORMAT_SUBTYPE_L8_CUSTOM == t) {
        return "KSDATAFORMAT_SUBTYPE_L8_CUSTOM";
    }
    if (KSDATAFORMAT_SUBTYPE_L16 == t) {
        return "KSDATAFORMAT_SUBTYPE_L16";
    }
    if (KSDATAFORMAT_SUBTYPE_L16_IR == t) {
        return "KSDATAFORMAT_SUBTYPE_L16_IR";
    }
    if (KSDATAFORMAT_SUBTYPE_D16 == t) {
        return "KSDATAFORMAT_SUBTYPE_D16";
    }
    if (KSDATAFORMAT_SUBTYPE_L16_CUSTOM == t) {
        return "KSDATAFORMAT_SUBTYPE_L16_CUSTOM";
    }
    if (KSDATAFORMAT_SUBTYPE_MJPG_IR == t) {
        return "KSDATAFORMAT_SUBTYPE_MJPG_IR";
    }
    if (KSDATAFORMAT_SUBTYPE_MJPG_DEPTH == t) {
        return "KSDATAFORMAT_SUBTYPE_MJPG_DEPTH";
    }
    if (KSDATAFORMAT_SUBTYPE_MJPG_CUSTOM == t) {
        return "KSDATAFORMAT_SUBTYPE_MJPG_CUSTOM";
    }
    if (KSDATAFORMAT_SUBTYPE_RAW8 == t) {
        return "KSDATAFORMAT_SUBTYPE_RAW8";
    }
    if (KSDATAFORMAT_SUBTYPE_CC == t) {
        return "KSDATAFORMAT_SUBTYPE_CC";
    }
    if (KSDATAFORMAT_SUBTYPE_NABTS == t) {
        return "KSDATAFORMAT_SUBTYPE_NABTS";
    }
    if (KSDATAFORMAT_SUBTYPE_TELETEXT == t) {
        return "KSDATAFORMAT_SUBTYPE_TELETEXT";
    }
    if (KSDATAFORMAT_SUBTYPE_NABTS_FEC == t) {
        return "KSDATAFORMAT_SUBTYPE_NABTS_FEC";
    }
    if (KSDATAFORMAT_SUBTYPE_OVERLAY == t) {
        return "KSDATAFORMAT_SUBTYPE_OVERLAY";
    }
    if (KSDATAFORMAT_SUBTYPE_Line21_BytePair == t) {
        return "KSDATAFORMAT_SUBTYPE_Line21_BytePair";
    }
    if (KSDATAFORMAT_SUBTYPE_Line21_GOPPacket == t) {
        return "KSDATAFORMAT_SUBTYPE_Line21_GOPPacket";
    }

    if (KSDATAFORMAT_SPECIFIER_VC_ID == t) {
        return "KSDATAFORMAT_SPECIFIER_VC_ID";
    }
    if (KSDATAFORMAT_SPECIFIER_WAVEFORMATEX == t) {
        return "KSDATAFORMAT_SPECIFIER_WAVEFORMATEX";
    }
    if (KSDATAFORMAT_SPECIFIER_DSOUND == t) {
        return "KSDATAFORMAT_SPECIFIER_DSOUND";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_AUDIO == t) {
        return "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG1_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_VIDEO == t) {
        return "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_AUDIO == t) {
        return "KSDATAFORMAT_SPECIFIER_DIALECT_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_DIALECT_AC3_AUDIO == t) {
        return "KSDATAFORMAT_SPECIFIER_DIALECT_AC3_AUDIO";
    }

    if (KSDATAFORMAT_SPECIFIER_MPEG1_VIDEO == t) {
        return "KSDATAFORMAT_SPECIFIER_MPEG1_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_MPEG2_VIDEO == t) {
        return "KSDATAFORMAT_SPECIFIER_MPEG2_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_MPEG2_AUDIO == t) {
        return "KSDATAFORMAT_SPECIFIER_MPEG2_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_LPCM_AUDIO == t) {
        return "KSDATAFORMAT_SPECIFIER_LPCM_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_AC3_AUDIO == t) {
        return "KSDATAFORMAT_SPECIFIER_AC3_AUDIO";
    }
    if (KSDATAFORMAT_SPECIFIER_VIDEOINFO == t) {
        return "KSDATAFORMAT_SPECIFIER_VIDEOINFO";
    }
    if (KSDATAFORMAT_SPECIFIER_VIDEOINFO2 == t) {
        return "KSDATAFORMAT_SPECIFIER_VIDEOINFO2";
    }
    if (KSDATAFORMAT_SPECIFIER_H264_VIDEO == t) {
        return "KSDATAFORMAT_SPECIFIER_H264_VIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_JPEG_IMAGE == t) {
        return "KSDATAFORMAT_SPECIFIER_JPEG_IMAGE";
    }
    if (KSDATAFORMAT_SPECIFIER_IMAGE == t) {
        return "KSDATAFORMAT_SPECIFIER_IMAGE";
    }
    if (KSDATAFORMAT_SPECIFIER_ANALOGVIDEO == t) {
        return "KSDATAFORMAT_SPECIFIER_ANALOGVIDEO";
    }
    if (KSDATAFORMAT_SPECIFIER_VBI == t) {
        return "KSDATAFORMAT_SPECIFIER_VBI";
    }
    if (__uuidof(IKsJackDescription2) == t) {
        return "IKsJackDescription2";
    }

    OLECHAR* guidString = nullptr;
    HRESULT hr = StringFromCLSID(t, &guidString);
    if (FAILED(hr)) {
        return "Unknown";
    }

    char s[256];
    sprintf_s(s, "Unknown%S", guidString);

    std::string r(s);

    CoTaskMemFree(guidString);

    return r;
}

