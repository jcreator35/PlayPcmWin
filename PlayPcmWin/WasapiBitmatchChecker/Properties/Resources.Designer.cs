﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WasapiBitmatchChecker.Properties {
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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WasapiBitmatchChecker.Properties.Resources", typeof(Resources).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start.
        /// </summary>
        internal static string buttonStart {
            get {
                return ResourceManager.GetString("buttonStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stop.
        /// </summary>
        internal static string buttonStop {
            get {
                return ResourceManager.GetString("buttonStop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Buffer size.
        /// </summary>
        internal static string groupBoxBufferSize {
            get {
                return ResourceManager.GetString("groupBoxBufferSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Data feed mode.
        /// </summary>
        internal static string groupBoxDataFeed {
            get {
                return ResourceManager.GetString("groupBoxDataFeed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test data pattern.
        /// </summary>
        internal static string groupBoxDataPattern {
            get {
                return ResourceManager.GetString("groupBoxDataPattern", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Log.
        /// </summary>
        internal static string groupBoxLog {
            get {
                return ResourceManager.GetString("groupBoxLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM data settings.
        /// </summary>
        internal static string groupBoxPcmDataSettings {
            get {
                return ResourceManager.GetString("groupBoxPcmDataSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM format.
        /// </summary>
        internal static string groupBoxPcmFormat {
            get {
                return ResourceManager.GetString("groupBoxPcmFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playback settings.
        /// </summary>
        internal static string groupBoxPlayback {
            get {
                return ResourceManager.GetString("groupBoxPlayback", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playback device.
        /// </summary>
        internal static string groupBoxPlaybackDevice {
            get {
                return ResourceManager.GetString("groupBoxPlaybackDevice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recording settings.
        /// </summary>
        internal static string groupBoxRecording {
            get {
                return ResourceManager.GetString("groupBoxRecording", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recording device.
        /// </summary>
        internal static string groupBoxRecordingDevice {
            get {
                return ResourceManager.GetString("groupBoxRecordingDevice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sample rate.
        /// </summary>
        internal static string groupBoxSampleRate {
            get {
                return ResourceManager.GetString("groupBoxSampleRate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM size:.
        /// </summary>
        internal static string labelPcmSize {
            get {
                return ResourceManager.GetString("labelPcmSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error. Captured data size was not sufficient to analyze.
        ///.
        /// </summary>
        internal static string msgCompareCaptureTooSmall {
            get {
                return ResourceManager.GetString("msgCompareCaptureTooSmall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Captured data was different from rendered data!
        ///  PCM size played = {0:F3} MiB ({1:F3} Mbit). Tested PCM Duration = {2:F1} seconds.
        ///  different bytes = {3}, capture glitch = {4}
        ///.
        /// </summary>
        internal static string msgCompareDifferent {
            get {
                return ResourceManager.GetString("msgCompareDifferent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test succeeded! Captured data was exactly the same as rendered data.
        ///  PCM size played = {0:F3} MiB ({1:F3} Mbit). Tested PCM Duration = {2:F1} seconds
        ///.
        /// </summary>
        internal static string msgCompareIdentical {
            get {
                return ResourceManager.GetString("msgCompareIdentical", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM data received. Now comparing recorded PCM with sent PCM...
        ///.
        /// </summary>
        internal static string msgCompareStarted {
            get {
                return ResourceManager.GetString("msgCompareStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error. Test start marker was not found in recorded PCM
        ///.
        /// </summary>
        internal static string msgCompareStartNotFound {
            get {
                return ResourceManager.GetString("msgCompareStartNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM size must be greater than or equals to 1.
        /// </summary>
        internal static string msgPcmSizeError {
            get {
                return ResourceManager.GetString("msgPcmSizeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PCM size must be smaller than or equal to {0}.
        /// </summary>
        internal static string msgPcmSizeTooLarge {
            get {
                return ResourceManager.GetString("msgPcmSizeTooLarge", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playback buffer size parse error.
        /// </summary>
        internal static string msgPlayBufferSizeError {
            get {
                return ResourceManager.GetString("msgPlayBufferSizeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playback buffer size must be smaller than 1000 ms.
        /// </summary>
        internal static string msgPlayBufferSizeTooLarge {
            get {
                return ResourceManager.GetString("msgPlayBufferSizeTooLarge", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error. Playback device select failed.
        /// </summary>
        internal static string msgPlayDeviceSelectError {
            get {
                return ResourceManager.GetString("msgPlayDeviceSelectError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playing device state is changed: {0} 
        ///Exiting program....
        /// </summary>
        internal static string msgPlayDeviceStateChanged {
            get {
                return ResourceManager.GetString("msgPlayDeviceStateChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to   Playback:  {0}, buffer size={1}ms, {2}, {3}
        ///.
        /// </summary>
        internal static string msgPlaySettings {
            get {
                return ResourceManager.GetString("msgPlaySettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playback Setup error. {0}Hz {1} {2}ch ProAudio Exclusive {3} {4}ms.
        /// </summary>
        internal static string msgPlaySetupError {
            get {
                return ResourceManager.GetString("msgPlaySetupError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recording buffer size parse error.
        /// </summary>
        internal static string msgRecBufferSizeError {
            get {
                return ResourceManager.GetString("msgRecBufferSizeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recording buffer size must be smaller than 1000 ms.
        /// </summary>
        internal static string msgRecBufferSizeTooLarge {
            get {
                return ResourceManager.GetString("msgRecBufferSizeTooLarge", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error. Recording device select failed.
        /// </summary>
        internal static string msgRecDeviceSelectError {
            get {
                return ResourceManager.GetString("msgRecDeviceSelectError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recording device state is changed: {0} 
        ///Exiting program....
        /// </summary>
        internal static string msgRecDeviceStateChanged {
            get {
                return ResourceManager.GetString("msgRecDeviceStateChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to   Recording: {0}, buffer size={1}ms, {2}, {3}
        ///.
        /// </summary>
        internal static string msgRecSettings {
            get {
                return ResourceManager.GetString("msgRecSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recording Setup error. {0}Hz {1} {2}ch ProAudio Exclusive {3} {4}ms.
        /// </summary>
        internal static string msgRecSetupError {
            get {
                return ResourceManager.GetString("msgRecSetupError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error. Could not receive Sync signal. Check your S/PDIF cabling.
        ///.
        /// </summary>
        internal static string msgSyncError {
            get {
                return ResourceManager.GetString("msgSyncError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test started. SampleRate={0}Hz, PCM data duration={1:F1} seconds, , {2:F3} M frames.
        ///.
        /// </summary>
        internal static string msgTestStarted {
            get {
                return ResourceManager.GetString("msgTestStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Event driven.
        /// </summary>
        internal static string radioButtonEventDriven {
            get {
                return ResourceManager.GetString("radioButtonEventDriven", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Random sequence:.
        /// </summary>
        internal static string radioButtonPcmRandom {
            get {
                return ResourceManager.GetString("radioButtonPcmRandom", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Integer 16-bit.
        /// </summary>
        internal static string radioButtonSint16 {
            get {
                return ResourceManager.GetString("radioButtonSint16", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Integer 24-bit.
        /// </summary>
        internal static string radioButtonSint24 {
            get {
                return ResourceManager.GetString("radioButtonSint24", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Integer 32-bit, valid bits=24.
        /// </summary>
        internal static string radioButtonSint32v24 {
            get {
                return ResourceManager.GetString("radioButtonSint32v24", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Timer driven.
        /// </summary>
        internal static string radioButtonTimerDriven {
            get {
                return ResourceManager.GetString("radioButtonTimerDriven", resourceCulture);
            }
        }
    }
}
