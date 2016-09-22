using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWFlacRWCS {
    public enum FlacErrorCode {
        OK = 0,
        DataNotReady = -2,
        WriteOpenFailed = -3,
        StreamDecoderNewFailed = -4,
        StreamDecoderInitFailed = -5,
        DecoderProcessFailed = -6,
        LostSync = -7,
        BadHeader = -8,
        FrameCrcMismatch = -9,
        Unparseable = -10,
        NumFrameIsNotAligned = -11,
        RecvBufferSizeInsufficient = -12,
        Other = -13,
        FileReadOpen = -14,
        BufferSizeMismatch = -15,
        MemoryExhausted = -16,
        Encoder = -17,
        InvalidNumberOfChannels = -18,
        InvalidBitsPerSample = -19,
        InvalidSampleRate = -20,
        InvalidMetadata = -21,
        BadParams = -22,
        IdNotFound = -23,
        EncoderProcessFailed = -24,
        OutputFileTooLarge = -25,
        MD5SignatureDoesNotMatch = -26,
        SuccessButMd5WasNotCalculated = -27,
    };
}
