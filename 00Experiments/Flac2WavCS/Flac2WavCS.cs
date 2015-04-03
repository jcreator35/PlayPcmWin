using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Flac2WavCS {
    public class Flac2WavCS {

        [DllImport("Flac2Wav.dll", CharSet = CharSet.Ansi)]
        private extern static int
        Flac2Wav(string fromFlacPath, string toFlacPath);

        public enum ResultType {
            Success = 0,
            WriteOpenFailed,
            FlacStreamDecoderNewFailed,
            FlacStreamDecoderInitFailed,
            LostSync,
            BadHeader,
            FrameCrcMismatch,
            Unparseable,
            OtherError
        };

        public static ResultType Flac2WavBlocking(string fromFlacPath, string toWavPath) {
            return (ResultType)Flac2Wav(fromFlacPath, toWavPath);
        }

    }
}
