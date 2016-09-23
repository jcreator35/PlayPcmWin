using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WavDiff;

namespace WavCompositionC
{
    class Program
    {
        static void PrintUsageAndExit()
        {
            Console.WriteLine("Usage: WavCompositionC -ch0 Ch0WavFileName {ch0,ch1} [-ch1 Ch1WavFIleName {ch0,ch1} ] outputWavFileName");
            System.Environment.Exit(1);
        }

        struct OptionChInfo
        {
            public string chFileName;
            public int    chFileUseCh;
        };

        struct OptionInfo {
            public OptionChInfo[] channels;
            public string outputWavFileName;

            public OptionInfo(int nCh) {
                outputWavFileName = string.Empty;
                
                channels = new OptionChInfo[nCh];
                for (int i = 0; i < nCh; ++i) {
                    channels[0] = new OptionChInfo();
                    channels[0].chFileName = string.Empty;
                    channels[0].chFileUseCh = 0;
                }
            }
        };

        static void ParseInputFileOption(ref OptionChInfo optionChInfo, string path, string useCh)
        {
            optionChInfo.chFileName = path;
            switch (useCh) {
            case "ch0":
                optionChInfo.chFileUseCh = 0;
                break;
            case "ch1":
                optionChInfo.chFileUseCh = 0;
                break;
            default:
                PrintUsageAndExit();
                break;
            }
        }

        static void ParseOptions(ref OptionInfo optionInfo, string[] args)
        {
            if (args.Length < 2) {
                PrintUsageAndExit();
            }

            for (int argPos = 0; argPos < args.Length; ) {
                switch (args[argPos]) {
                case "-ch0":
                    if (args.Length <= argPos + 2) {
                        PrintUsageAndExit();
                    }
                    ParseInputFileOption(ref optionInfo.channels[0], args[argPos + 1], args[argPos + 2]);
                    argPos += 3;
                    break;
                case "-ch1":
                    if (args.Length <= argPos + 2) {
                        PrintUsageAndExit();
                    }
                    ParseInputFileOption(ref optionInfo.channels[1], args[argPos + 1], args[argPos + 2]);
                    argPos += 3;
                    break;
                default:
                    optionInfo.outputWavFileName = args[argPos];
                    ++argPos;
                    break;
                }
            }

            if (optionInfo.channels[0].chFileName == string.Empty ||
                optionInfo.channels[1].chFileName == string.Empty ||
                optionInfo.outputWavFileName == string.Empty) {
                PrintUsageAndExit();
            }
        }

        static WavData ReadWavFromFile(string path)
        {
            WavData wavData = new WavData();

            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open))) {
                if (!wavData.Read(br)) {
                    Console.WriteLine("E: Read failed. unknown format. {0}", path);
                    PrintUsageAndExit();
                }
            }
            return wavData;
        }

        static void Main(string[] args)
        {
            OptionInfo optionInfo = new OptionInfo(2);

            ParseOptions(ref optionInfo, args);

            WavData ch0Wav = ReadWavFromFile(optionInfo.channels[0].chFileName);
            WavData ch1Wav = ReadWavFromFile(optionInfo.channels[1].chFileName);

            if (ch0Wav.NumSamples != ch1Wav.NumSamples) {
                Console.WriteLine("E: NumSamples mismatch. ch0.numSamples={0}, ch1.numSamples={1}",
                    ch0Wav.NumSamples, ch1Wav.NumSamples);
                PrintUsageAndExit();
            }

            WavData writeWav = new WavData();
            List<PcmSamples1Channel> channels = new List<PcmSamples1Channel>();
            for (int i=0; i<2; ++i) {
                channels.Add(new PcmSamples1Channel(ch0Wav.NumSamples, ch0Wav.BitsPerSample));
            }

            int ch0UseCh = optionInfo.channels[0].chFileUseCh;
            int ch1UseCh = optionInfo.channels[1].chFileUseCh;
            for (int i=0; i<ch0Wav.NumSamples; ++i) {
                channels[0].Set16(i, ch0Wav.Sample16Get(ch0UseCh, i));
                channels[1].Set16(i, ch1Wav.Sample16Get(ch1UseCh, i));
            }

            writeWav.Create(ch0Wav.SampleRate, ch0Wav.BitsPerSample, channels);

            try {
                using (BinaryWriter bw = new BinaryWriter(File.Open(optionInfo.outputWavFileName, FileMode.CreateNew))) {
                    writeWav.Write(bw);
                }
            } catch (Exception ex) {
                Console.WriteLine("E: {0}", ex);
                PrintUsageAndExit();
            }
        }
    }
}
