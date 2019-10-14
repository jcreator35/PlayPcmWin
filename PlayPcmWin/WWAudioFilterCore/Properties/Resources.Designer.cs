﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WWAudioFilterCore.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WWAudioFilterCore.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error .
        /// </summary>
        public static string Error {
            get {
                return ResourceManager.GetString("Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input Cic delay value in larger than 1 integer.
        /// </summary>
        public static string ErrorCicDelay {
            get {
                return ResourceManager.GetString("ErrorCicDelay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please specify crossfeed Filter file.
        /// </summary>
        public static string ErrorCrossfeedFile {
            get {
                return ResourceManager.GetString("ErrorCrossfeedFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error Dynamic range scaling lsb scaling value parse error.
        /// </summary>
        public static string ErrorDynamicRangeCompressionLsbScaling {
            get {
                return ResourceManager.GetString("ErrorDynamicRangeCompressionLsbScaling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please add one or more filters.
        /// </summary>
        public static string ErrorFilterEmpty {
            get {
                return ResourceManager.GetString("ErrorFilterEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Filter file version mismatch. expected version={0}, file version={1}.
        /// </summary>
        public static string ErrorFilterFileVersionMismatch {
            get {
                return ResourceManager.GetString("ErrorFilterFileVersionMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error First order all-pass IIR a parameter.
        /// </summary>
        public static string ErrorFirstOrderAllPassIIR {
            get {
                return ResourceManager.GetString("ErrorFirstOrderAllPassIIR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input fundamentals gain in number.
        /// </summary>
        public static string ErrorFundamentalsGainValue {
            get {
                return ResourceManager.GetString("ErrorFundamentalsGainValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input gain value in number.
        /// </summary>
        public static string ErrorGainValueIsNan {
            get {
                return ResourceManager.GetString("ErrorGainValueIsNan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input gain value larger than 0.0.
        /// </summary>
        public static string ErrorGainValueIsTooSmall {
            get {
                return ResourceManager.GetString("ErrorGainValueIsTooSmall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Gaussian noise level parse error..
        /// </summary>
        public static string ErrorGaussianNoiseLevel {
            get {
                return ResourceManager.GetString("ErrorGaussianNoiseLevel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input Filter taps value N where N+1 equals power of 4.
        /// </summary>
        public static string ErrorHalfbandTaps {
            get {
                return ResourceManager.GetString("ErrorHalfbandTaps", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input Low pass filter cutoff frequency in number.
        /// </summary>
        public static string ErrorLpfCutoffFreqIsNan {
            get {
                return ResourceManager.GetString("ErrorLpfCutoffFreqIsNan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input Low pass filter cutoff frequency larger than 0.0.
        /// </summary>
        public static string ErrorLpfCutoffFreqIsNegative {
            get {
                return ResourceManager.GetString("ErrorLpfCutoffFreqIsNegative", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input Low pass filter slope in number.
        /// </summary>
        public static string ErrorLpfSlopeIsNan {
            get {
                return ResourceManager.GetString("ErrorLpfSlopeIsNan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please input Low pass filter slope larger than 1.
        /// </summary>
        public static string ErrorLpfSlopeIsTooSmall {
            get {
                return ResourceManager.GetString("ErrorLpfSlopeIsTooSmall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Prease input Target Quantization Bit Rate in number.
        /// </summary>
        public static string ErrorNoiseShapingBitIsNan {
            get {
                return ResourceManager.GetString("ErrorNoiseShapingBitIsNan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Prease input Target Quantization Bit Rate in integer in the range of 1 to 23.
        /// </summary>
        public static string ErrorNoiseShapingBitIsOutOfRange {
            get {
                return ResourceManager.GetString("ErrorNoiseShapingBitIsOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error normalize amplutude value must be number equals to or less than 0.0.
        /// </summary>
        public static string ErrorNormalizeValue {
            get {
                return ResourceManager.GetString("ErrorNormalizeValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Following upsamplers do not support non-power-of-2 upsample: FFT. Please select CubicHermiteSpline, LineDraw, InsertZeroes, WindowedSinc or ZOH. .
        /// </summary>
        public static string ErrorNotImplementedUpsampler {
            get {
                return ResourceManager.GetString("ErrorNotImplementedUpsampler", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Output data becomes too large! {0} Gbytes.
        /// </summary>
        public static string ErrorOutputDataTooLarge {
            get {
                return ResourceManager.GetString("ErrorOutputDataTooLarge", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to RPDF Jitter Amount must be 0 or larger value.
        /// </summary>
        public static string ErrorRpdfJitterAmount {
            get {
                return ResourceManager.GetString("ErrorRpdfJitterAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: sample rate is too high({0}Hz). FLAC supports up to 655,350Hz sample rate. WAV supports up to 2,147,483,647Hz.
        /// </summary>
        public static string ErrorSampleRateTooHigh {
            get {
                return ResourceManager.GetString("ErrorSampleRateTooHigh", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Too large magnitude sample detected! channel={0}, magnitude={1:0.000}
        ///.
        /// </summary>
        public static string ErrorSampleValueClipped {
            get {
                return ResourceManager.GetString("ErrorSampleValueClipped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error Second order all-pass IIR r parameter.
        /// </summary>
        public static string ErrorSecondOrderAllPassIirR {
            get {
                return ResourceManager.GetString("ErrorSecondOrderAllPassIirR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error Second order all-pass IIR θ parameter.
        /// </summary>
        public static string ErrorSecondOrderAllPassIirT {
            get {
                return ResourceManager.GetString("ErrorSecondOrderAllPassIirT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sinusoidal Jitter Amount must be 0 or larger value.
        /// </summary>
        public static string ErrorSinusolidalJitterAmount {
            get {
                return ResourceManager.GetString("ErrorSinusolidalJitterAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sinusoidal Jitter Freq must be 0 or larger value.
        /// </summary>
        public static string ErrorSinusolidalJitterFreq {
            get {
                return ResourceManager.GetString("ErrorSinusolidalJitterFreq", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Subsonic filter cutoff frequency must greater than or equal to 1.0.
        /// </summary>
        public static string ErrorSubsonicFilterCutoffFrequency {
            get {
                return ResourceManager.GetString("ErrorSubsonicFilterCutoffFrequency", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: target bit depth b must number in the range:  1 ≤ b ≤ 23.
        /// </summary>
        public static string ErrorTargetBitDepth {
            get {
                return ResourceManager.GetString("ErrorTargetBitDepth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Timing Error audio data should have the same or more sample count than Input PCM data.
        /// </summary>
        public static string ErrorTimingErrorFile {
            get {
                return ResourceManager.GetString("ErrorTimingErrorFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Jitter Timing Error scaling value should be a number.
        /// </summary>
        public static string ErrorTimingErrorNanosec {
            get {
                return ResourceManager.GetString("ErrorTimingErrorNanosec", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TPDF Jitter Amount must be 0 or larger value.
        /// </summary>
        public static string ErrorTpdfJitterAmount {
            get {
                return ResourceManager.GetString("ErrorTpdfJitterAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error uneven bit dac unevenness value parse error.
        /// </summary>
        public static string ErrorUnevenBitDacLsbScaling {
            get {
                return ResourceManager.GetString("ErrorUnevenBitDacLsbScaling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please specify different file to write. WWAudioFilter cannot write to input file..
        /// </summary>
        public static string ErrorWriteToReadFile {
            get {
                return ResourceManager.GetString("ErrorWriteToReadFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add octave bass unison of the sound below 80Hz: Gain={0:0.00}dB.
        /// </summary>
        public static string FilterAddFundamentalsDesc {
            get {
                return ResourceManager.GetString("FilterAddFundamentalsDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A-Weighting fs={0}Hz.
        /// </summary>
        public static string FilterAWeightingDesc {
            get {
                return ResourceManager.GetString("FilterAWeightingDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CIC Filter: Order={0}, Comb delay={1}.
        /// </summary>
        public static string FilterCicFilterDesc {
            get {
                return ResourceManager.GetString("FilterCicFilterDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Crossfeed: ConfigFile={0}.
        /// </summary>
        public static string FilterCrossfeedDesc {
            get {
                return ResourceManager.GetString("FilterCrossfeedDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cubic Hermite Spline upsample: {0}x.
        /// </summary>
        public static string FilterCubicHermiteSplineDesc {
            get {
                return ResourceManager.GetString("FilterCubicHermiteSplineDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Downsample: {0}x, pick {1} th sample.
        /// </summary>
        public static string FilterDownsamplerDesc {
            get {
                return ResourceManager.GetString("FilterDownsamplerDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dynamic range compression: 24bit LSB gain= {0} dB.
        /// </summary>
        public static string FilterDynamicRangeCompressionDesc {
            get {
                return ResourceManager.GetString("FilterDynamicRangeCompressionDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FFT downsample: {0}x, FFT length={1}.
        /// </summary>
        public static string FilterFftDownsampleDesc {
            get {
                return ResourceManager.GetString("FilterFftDownsampleDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FFT upsample: {0}x, FFT length={1}.
        /// </summary>
        public static string FilterFftUpsampleDesc {
            get {
                return ResourceManager.GetString("FilterFftUpsampleDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to First Order All-pass IIR Filter: a={0}.
        /// </summary>
        public static string FilterFirstOrderAllPassIIRDesc {
            get {
                return ResourceManager.GetString("FilterFirstOrderAllPassIIRDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FLAC files|*.flac.
        /// </summary>
        public static string FilterFlacFiles {
            get {
                return ResourceManager.GetString("FilterFlacFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Gain : {0}x ({1:0.00}dB).
        /// </summary>
        public static string FilterGainDesc {
            get {
                return ResourceManager.GetString("FilterGainDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Half-band Filter: taps={0}.
        /// </summary>
        public static string FilterHalfbandDesc {
            get {
                return ResourceManager.GetString("FilterHalfbandDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Insert Zeroes Upsampler: {0}x.
        /// </summary>
        public static string FilterInsertZeroesDesc {
            get {
                return ResourceManager.GetString("FilterInsertZeroesDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ITU-R 468-4 Weighting fs={0}Hz.
        /// </summary>
        public static string FilterITUR4684WeightingDesc {
            get {
                return ResourceManager.GetString("FilterITUR4684WeightingDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add Jitter : FilterLen={4}, Sine={0}Hz {1}ns, TPDF={2}ns,  RPDF={3}ns,  TEF={5}ns {6}.
        /// </summary>
        public static string FilterJitterAddDesc {
            get {
                return ResourceManager.GetString("FilterJitterAddDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Linear inerpolation upsample: {0}x.
        /// </summary>
        public static string FilterLineDrawDesc {
            get {
                return ResourceManager.GetString("FilterLineDrawDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to LPF : Cutoff={0}Hz, slope={1}db/oct, FIR length={2}.
        /// </summary>
        public static string FilterLpfDesc {
            get {
                return ResourceManager.GetString("FilterLpfDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 2nd order MASH noise shaping: targetBitsPerSample={0}.
        /// </summary>
        public static string FilterMashDesc {
            get {
                return ResourceManager.GetString("FilterMashDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 4th order noise shaping: targetBitsPerSample={0}.
        /// </summary>
        public static string FilterNoiseShaping4thDesc {
            get {
                return ResourceManager.GetString("FilterNoiseShaping4thDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Noise shaping: order={0}, targetBitsPerSample={1}.
        /// </summary>
        public static string FilterNoiseShapingDesc {
            get {
                return ResourceManager.GetString("FilterNoiseShapingDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Normalize signal level max={0}dBFS.
        /// </summary>
        public static string FilterNormalizeDesc {
            get {
                return ResourceManager.GetString("FilterNormalizeDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One-bit conversion (Work in progress).
        /// </summary>
        public static string FilterOnebitConversionDesc {
            get {
                return ResourceManager.GetString("FilterOnebitConversionDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Audio files(*.wav, *.flac, *.dsf)|*.wav;*.flac;*.dsf.
        /// </summary>
        public static string FilterReadAudioFiles {
            get {
                return ResourceManager.GetString("FilterReadAudioFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reduce bit depth to {0} bit.
        /// </summary>
        public static string FilterReduceBitDepthDesc {
            get {
                return ResourceManager.GetString("FilterReduceBitDepthDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Second Order All-pass IIR Filter: r={0}, θ={1}°.
        /// </summary>
        public static string FilterSecondOrderAllPassIIRDesc {
            get {
                return ResourceManager.GetString("FilterSecondOrderAllPassIIRDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Edit Tag : {0} = &quot;{1}&quot;.
        /// </summary>
        public static string FilterTagEdit {
            get {
                return ResourceManager.GetString("FilterTagEdit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Time Reversal Filter.
        /// </summary>
        public static string FilterTimeReversalDesc {
            get {
                return ResourceManager.GetString("FilterTimeReversalDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Uneven bit DAC simulation. 16bit LSB unevenness +{0} dB.
        /// </summary>
        public static string FilterUnevenBitDacDesc {
            get {
                return ResourceManager.GetString("FilterUnevenBitDacDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windowed Sinc downsample: {0}x, window length={1}.
        /// </summary>
        public static string FilterWindowedSincDownsampleDesc {
            get {
                return ResourceManager.GetString("FilterWindowedSincDownsampleDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windowed Sinc upsample: {0}x, window length={1}.
        /// </summary>
        public static string FilterWindowedSincUpsampleDesc {
            get {
                return ResourceManager.GetString("FilterWindowedSincUpsampleDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Audio files(*.wav,*.flac,*.dsf)|*.wav;*.flac;*.dsf|FLAC files|*.flac|DSF files|*.dsf|WAV files|*.wav.
        /// </summary>
        public static string FilterWriteAudioFiles {
            get {
                return ResourceManager.GetString("FilterWriteAudioFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WWAudioFilter files|*.wwaf.
        /// </summary>
        public static string FilterWWAFilterFiles {
            get {
                return ResourceManager.GetString("FilterWWAFilterFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Zero order hold upsample: {0}x.
        /// </summary>
        public static string FilterZOHDesc {
            get {
                return ResourceManager.GetString("FilterZOHDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ZOH NOSDAC Compensation FIR Filter: taps={0}.
        /// </summary>
        public static string FilterZohNosdacCompensationDesc {
            get {
                return ResourceManager.GetString("FilterZohNosdacCompensationDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add Gaussian noise: {0} dB.
        /// </summary>
        public static string GaussianNoiseDesc {
            get {
                return ResourceManager.GetString("GaussianNoiseDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Completed. Elapsed time: {0}
        ///.
        /// </summary>
        public static string LogCompleted {
            get {
                return ResourceManager.GetString("LogCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Read completed. 
        ///Processing...
        ///.
        /// </summary>
        public static string LogFileReadCompleted {
            get {
                return ResourceManager.GetString("LogFileReadCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reading file {0} ...
        ///.
        /// </summary>
        public static string LogFileReadStarted {
            get {
                return ResourceManager.GetString("LogFileReadStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process completed. Writing to {0} ...
        ///.
        /// </summary>
        public static string LogfileWriteStarted {
            get {
                return ResourceManager.GetString("LogfileWriteStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Nothing to store..
        /// </summary>
        public static string NothingToStore {
            get {
                return ResourceManager.GetString("NothingToStore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add Random noise: {0} {1} dB.
        /// </summary>
        public static string RandomNoiseDesc {
            get {
                return ResourceManager.GetString("RandomNoiseDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Subsonic filter: Cutoff freq={0}Hz.
        /// </summary>
        public static string SubsonicFilterDesc {
            get {
                return ResourceManager.GetString("SubsonicFilterDesc", resourceCulture);
            }
        }
    }
}
