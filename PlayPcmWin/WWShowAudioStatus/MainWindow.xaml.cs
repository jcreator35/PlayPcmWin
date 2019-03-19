using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WWShowAudioStatus {
    public partial class MainWindow : Window, IDisposable {
        private WWShowAudioStatusCs mSAS;
        private int mDefaultIdx;
        private WWShowAudioStatusCs.StateChangedCallback mStateChangedCb;

        public MainWindow() {
            InitializeComponent();

            mSAS = new WWShowAudioStatusCs();
            mDefaultIdx = -1;

            AudioDeviceListStart();
            UpdateAudioDeviceList();
            UpdateAudioClientData();
            UpdateSpatialAudioData();
            UpdateDeviceNodeGraph();
            UpdateAudioSessions();
        }

        private void StatusChanged(StringBuilder idStr, int dwNewState) {
            Dispatcher.BeginInvoke(new Action(delegate () {
                // 描画スレッドで実行。
                AudioDeviceListEnd();

                AudioDeviceListStart();
                UpdateAudioDeviceList();
                UpdateAudioClientData();
                UpdateSpatialAudioData();
                UpdateDeviceNodeGraph();
                UpdateAudioSessions();
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mStateChangedCb = new WWShowAudioStatusCs.StateChangedCallback(StatusChanged);
            mSAS.RegisterStateChangedCallback(mStateChangedCb);
        }

        private int AudioObjectTypeToSpeakerCount(int mask) {
            int c = 0;
            for (int i=0; i<32; ++i) {
                if ((mask & 1) == 1) {
                    ++c;
                }
                mask >>= 1;
            }

            return c;
        }
        private string AudioObjectTypeToStr(int mask) {
            var sb = new StringBuilder();
            if ((mask & 0x2) != 0) {
                sb.Append("FL ");
            }
            if ((mask & 0x4) != 0) {
                sb.Append("FR ");
            }
            if ((mask & 0x8) != 0) {
                sb.Append("FC ");
            }
            if ((mask & 0x10) != 0) {
                sb.Append("LFE ");
            }
            if ((mask & 0x20) != 0) {
                sb.Append("SL ");
            }
            if ((mask & 0x40) != 0) {
                sb.Append("SR ");
            }
            if ((mask & 0x80) != 0) {
                sb.Append("BL ");
            }
            if ((mask & 0x100) != 0) {
                sb.Append("BR ");
            }
            if ((mask & 0x200) != 0) {
                sb.Append("TFL ");
            }
            if ((mask & 0x400) != 0) {
                sb.Append("TFR ");
            }
            if ((mask & 0x800) != 0) {
                sb.Append("TBL ");
            }
            if ((mask & 0x1000) != 0) {
                sb.Append("TBR ");
            }
            if ((mask & 0x2000) != 0) {
                sb.Append("BFL ");
            }
            if ((mask & 0x4000) != 0) {
                sb.Append("BFR ");
            }
            if ((mask & 0x8000) != 0) {
                sb.Append("BBL ");
            }
            if ((mask & 0x10000) != 0) {
                sb.Append("BBR ");
            }
            if ((mask & 0x20000) != 0) {
                sb.Append("BC ");
            }

            return sb.ToString();
        }

        private string DwChannelMaskToStr(int mask) {
        var sb = new StringBuilder();
            if ((mask & 0x1) != 0) {
                sb.Append("FL ");
            }
            if ((mask & 0x2) != 0) {
                sb.Append("FR ");
            }
            if ((mask & 0x4) != 0) {
                sb.Append("FC ");
            }
            if ((mask & 0x8) != 0) {
                sb.Append("LFE ");
            }
            if ((mask & 0x10) != 0) {
                sb.Append("BL ");
            }
            if ((mask & 0x20) != 0) {
                sb.Append("BR ");
            }
            if ((mask & 0x40) != 0) {
                sb.Append("FLC ");
            }
            if ((mask & 0x80) != 0) {
                sb.Append("FRC ");
            }
            if ((mask & 0x100) != 0) {
                sb.Append("BC ");
            }
            if ((mask & 0x200) != 0) {
                sb.Append("SL ");
            }
            if ((mask & 0x400) != 0) {
                sb.Append("SR ");
            }
            if ((mask & 0x800) != 0) {
                sb.Append("TC ");
            }
            if ((mask & 0x1000) != 0) {
                sb.Append("TFL ");
            }
            if ((mask & 0x2000) != 0) {
                sb.Append("TFC ");
            }
            if ((mask & 0x4000) != 0) {
                sb.Append("TFR ");
            }
            if ((mask & 0x8000) != 0) {
                sb.Append("TBL ");
            }
            if ((mask & 0x10000) != 0) {
                sb.Append("TBC ");
            }
            if ((mask & 0x20000) != 0) {
                sb.Append("TBR ");
            }

            return sb.ToString();
        }
        private string DwChannelMaskToLongStr(int mask) {
            var sb = new StringBuilder();
            if ((mask & 0x1) != 0) {
                sb.Append("FrontLeft\n");
            }
            if ((mask & 0x2) != 0) {
                sb.Append("FrontRight\n");
            }
            if ((mask & 0x4) != 0) {
                sb.Append("FrontCenter\n");
            }
            if ((mask & 0x8) != 0) {
                sb.Append("Low-FrequencyEffects\n");
            }
            if ((mask & 0x10) != 0) {
                sb.Append("BackLeft\n");
            }
            if ((mask & 0x20) != 0) {
                sb.Append("BackRight\n");
            }
            if ((mask & 0x40) != 0) {
                sb.Append("FrintLeftOfCenter\n");
            }
            if ((mask & 0x80) != 0) {
                sb.Append("FrintRightOfCenter\n");
            }
            if ((mask & 0x100) != 0) {
                sb.Append("BackCenter\n");
            }
            if ((mask & 0x200) != 0) {
                sb.Append("SideLeft\n");
            }
            if ((mask & 0x400) != 0) {
                sb.Append("SideRight\n");
            }
            if ((mask & 0x800) != 0) {
                sb.Append("TopCenter\n");
            }
            if ((mask & 0x1000) != 0) {
                sb.Append("TopFrontLeft\n");
            }
            if ((mask & 0x2000) != 0) {
                sb.Append("TopFrontCenter\n");
            }
            if ((mask & 0x4000) != 0) {
                sb.Append("TopFrontRight\n");
            }
            if ((mask & 0x8000) != 0) {
                sb.Append("TopBackLeft\n");
            }
            if ((mask & 0x10000) != 0) {
                sb.Append("TopBackCenter\n");
            }
            if ((mask & 0x20000) != 0) {
                sb.Append("TopBackRight\n");
            }

            return sb.ToString().TrimEnd('\n');
        }

        private string NumChannelsListToStr(List<int> chs) {
            var sb = new StringBuilder(string.Format("{0}", chs[0]));
            for (int i=1; i<chs.Count; ++i) {
                int ch = chs[i];
                sb.AppendFormat(",{0}", ch);
            }
            sb.Append("ch");
            return sb.ToString();
        }

        private void UpdateAudioClientData() {
            mTextBoxAudioClient.Text = "";

            if (mListBoxAudioDevices.SelectedIndex < 0) {
                return;
            }

            var mfmt = mSAS.GetMixFormat(mListBoxAudioDevices.SelectedIndex);

            var sb = new StringBuilder();
            sb.Append(string.Format("Shared mode Sample rate = {0}kHz\n", 0.001 * mfmt.samplerate));
            sb.Append(string.Format("Shared mode numChannels = {0}ch\n", mfmt.numChannels));
            if (0 != mfmt.dwChannelMask) {
                sb.Append(string.Format("Shared mode speaker positions: {0}\n", DwChannelMaskToStr(mfmt.dwChannelMask)));
            }
            if (0 != mfmt.hnsDevicePeriod) {
                sb.Append(string.Format("Default data interval = {0:0.0}ms, Min data interval = {1:0.0}ms\n",
                    mfmt.hnsDevicePeriod * 0.0001, mfmt.hnsMinDevicePeriod * 0.0001));
            }
            //sb.Append(string.Format("IsOffloadCapable={0}\n", mfmt.offloadCapable));

            mTextBoxAudioClient.Text = sb.ToString();
        }

        private void UpdateSpatialAudioData() {
            mTextBoxSpatialAudio.Text = "";

            if (mListBoxAudioDevices.SelectedIndex < 0) {
                return;
            }

            var sap = mSAS.GetSpatialAudioParams(mListBoxAudioDevices.SelectedIndex);

            var sb = new StringBuilder();
            if (sap.maxDynamicObjectCount == 0) {
                sb.Append("Spatial Sound is not enabled on this device.");
            } else {
                sb.Append("Spatial Sound is enabled.\n");
                sb.Append(string.Format("Max number of dynamic object = {0}\n", sap.maxDynamicObjectCount));
                sb.Append(string.Format("Sample rate = {0}kHz, maxFrameCount={1} ({2:0.0}ms)\n",
                    0.001* sap.sampleRate, sap.maxFrameCount, 1000.0 * sap.maxFrameCount / sap.sampleRate));
                sb.Append(string.Format("Virtual Static Speakers : {0},\n", AudioObjectTypeToSpeakerCount(sap.virtualSpeakerMask)));
                sb.Append(string.Format("  {0}\n", AudioObjectTypeToStr(sap.virtualSpeakerMask)));
            }

            mTextBoxSpatialAudio.Text = sb.ToString();
        }

        private void AudioDeviceListStart() {
            mSAS.CreateDeviceList(WWShowAudioStatusCs.WWDataFlow.Render);
        }

        private void AudioDeviceListEnd() {
            mSAS.DestroyDeviceList();
        }

        private void UpdateAudioDeviceList() {
            mListBoxAudioDevices.Items.Clear();

            int nDevices = mSAS.GetDeviceCount();
            mDefaultIdx = -1;
            for (int i=0; i<nDevices;++i) {
                var item = mSAS.GetDeviceParams(i);
                mListBoxAudioDevices.Items.Add(string.Format("{0} : {1} {2}", i, item.name,
                    item.defaultDevice ? "■■ Default Device ■■" : ""));

                if (item.defaultDevice) {
                    mDefaultIdx = i;
                }
            }

            if (0 <= mDefaultIdx) {
                mListBoxAudioDevices.SelectedIndex = mDefaultIdx;
                mListBoxAudioDevices.ScrollIntoView(mDefaultIdx);
            }
        }

        private void MListBoxAudioDevices_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateAudioClientData();
            UpdateSpatialAudioData();
            UpdateDeviceNodeGraph();
        }

        private void UpdateDeviceNodeGraph() {
            if (mListBoxAudioDevices.SelectedIndex < 0) {
                mWFHost.Visibility = Visibility.Collapsed;
                return;
            }

            mWFHost.Visibility = Visibility.Visible;

            var viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            //viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            //viewer.BackColor = System.Drawing.Color.Gray;
            //viewer.ForeColor = System.Drawing.Color.Gray;
            var graph = new Microsoft.Msagl.Drawing.Graph("DeviceNodeGraph");


            // DeviceNodeのリストを作る。
            var dnList = new List<WWShowAudioStatusCs.DeviceNode>();

            mSAS.CreateDeviceNodeList(mListBoxAudioDevices.SelectedIndex);

            for (int i = 0; i < mSAS.GetDeviceNodeNum(); ++i) {
                var dn = mSAS.GetDeviceNodeNth(i);

                if (dn.type == WWShowAudioStatusCs.DeviceNodeType.IControlInterface
                    || dn.type  == WWShowAudioStatusCs.DeviceNodeType.T_Pointer) {
                    // 表示しない。
                    continue;
                }

                dnList.Add(dn);
                string name = string.Format("{0:x}", dn.self);
                string parentName = string.Format("{0:x}", dn.parent);

                if (dn.parent != 0) {
                    graph.AddEdge(parentName, name); //.LabelText = string.Format("{0}",i);
                }

                Microsoft.Msagl.Drawing.Node n = null;
                if (dn.type != WWShowAudioStatusCs.DeviceNodeType.T_Pointer) {
                    graph.AddNode(name);
                    n = graph.FindNode(name);
                } else {
                    n = graph.FindNode(name);
                }
                n.Attr.LabelMargin = 10;

                if (i == 0) {
                    n.Attr.LineWidth = 3;
                    n.Attr.Color = new Microsoft.Msagl.Drawing.Color(0, 0, 255);
                    //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                    {
                        // 説明文追加。
                        string desc = "description";
                        graph.AddNode(desc);
                        var nC = graph.FindNode(desc);
                        nC.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                        nC.LabelText =
                            "24_24bit : 24bit data in 24bit-size container\n"
                            + "24_32bit : 24bit data in 32bit-size container\n";
                        graph.AddEdge(name, desc).IsVisible = false ;
                    }
                }

                switch (dn.type) {
                case WWShowAudioStatusCs.DeviceNodeType.T_Pointer:
                    break;
                case WWShowAudioStatusCs.DeviceNodeType.IPart: {
                        var p = mSAS.GetPartParams(i);
                        n.LabelText = string.Format("{0}\n{1}\nLocalId={2}",
                            dn.type, p.name, p.localId);
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                    }
                    break;
                case WWShowAudioStatusCs.DeviceNodeType.IAudioMute: { 
                        var p = mSAS.GetAudioMuteParams(i);
                        n.LabelText = string.Format("{0}\n  {1}",
                            dn.type, p.bEnabled ? "Muted" : "Not Muted");
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                    }
                    break;
                case WWShowAudioStatusCs.DeviceNodeType.IAudioInputSelector: {
                        var p = mSAS.GetAudioInputSelectorParams(i);
                        n.LabelText = string.Format("{0}\n  InputNr={1}",
                            dn.type, p.id);
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                    }
                    break;
                case WWShowAudioStatusCs.DeviceNodeType.IAudioChannelConfig: {
                        int mask = mSAS.GetAudioChannelConfig(i);
                        n.LabelText = string.Format("{0}\n{1}",
                            dn.type, DwChannelMaskToLongStr(mask));
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                    }
                    break;
                case WWShowAudioStatusCs.DeviceNodeType.IAudioVolumeLevel: {
                        var p = mSAS.GetAudioVolumeLevelParams(i);
                        var sb = new StringBuilder();
                        sb.AppendFormat("{0}\n", dn.type);
                        for (int j=0; j<p.volumeLevels.Length; ++j) {
                            float v = p.volumeLevels[j];
                            sb.AppendFormat("  Ch{0} : {1:0.0}dB\n", j+1, v);
                        }
                        n.LabelText = sb.ToString();
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;
                    }
                    break;
                    /*
                case WWShowAudioStatusCs.DeviceNodeType.IControlInterface: {
                        var p = mSAS.GetControlInterfaceParams(i);
                        n.LabelText = string.Format("{0}\n{1}\n{2}", dn.type, p.name, p.iid);
                        n.Attr.LineWidth = 3;
                    }
                    break;
                    */
                case WWShowAudioStatusCs.DeviceNodeType.IKsJackDescription: {
                        var p = mSAS.GetKsJackDescriptionParams(i);
                        var sb = new StringBuilder();
                        sb.AppendFormat("{0}\n  {1} items", dn.type, p.descs.Length);
                        n.LabelText = sb.ToString();
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;

                        for (int j = 0; j < p.descs.Length; ++j) {
                            var d = p.descs[j];
                            string jackName = string.Format("{0:x}{1}", dn.self, j);
                            graph.AddNode(jackName);
                            var jn = graph.FindNode(jackName);
                            jn.LabelText = string.Format("{0}\n{1}\nLocation={2}\nGeometricLocation={3}\nConnectionType={4}\nJack Color=#{5:X6}",
                                d.ConnectionType,
                                DwChannelMaskToStr((int)d.ChannelMapping),
                                d.GenLocation,
                                d.GeoLocation,
                                d.PortConnection,
                                d.Color);
                            jn.Attr.Color = new Microsoft.Msagl.Drawing.Color(
                                (byte)((d.Color >> 16)&0xff), (byte)((d.Color >> 8)&0xff), (byte)((d.Color >> 0)&0xff));
                            jn.Attr.LineWidth = 3;
                            jn.Attr.LabelMargin = 10;
                            //jn.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;

                            graph.AddEdge(name, jackName);
                        }
                    }
                    break;
                case WWShowAudioStatusCs.DeviceNodeType.IKsFormatSupport: {
                        var p = mSAS.GetKsFormatpreferredFmt(i);
                        n.LabelText = string.Format("{0}\n preferred format = {1}kHz {2}_{3}bit {4}\n{5}",
                            dn.type,
                            p.sampleRate * 0.001,
                            p.validBitsPerSample, p.containerBitsPerSample,
                            p.bFloat ? "float" : "",
                            NumChannelsListToStr(p.numChannels));
                        n.Attr.LineWidth = 3;
                        //n.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Ellipse;

                        int nFmt = mSAS.GetKsFormatSupportedFmtNum(i);
                        for (int j=0; j<nFmt;++j) {
                            var sfmt = mSAS.GetKsFormatSupportedFmt(i, j);

                            string sfmtName = string.Format("{0:x}{1}", dn.self, j);
                            graph.AddNode(sfmtName);
                            var sn = graph.FindNode(sfmtName);
                            sn.LabelText = string.Format("{0}kHz\n{1}_{2}bit {3}\n{4}",
                                sfmt.sampleRate * 0.001,
                                sfmt.validBitsPerSample, sfmt.containerBitsPerSample,
                                sfmt.bFloat ? "float" : "",
                                NumChannelsListToStr(sfmt.numChannels));
                            sn.Attr.LabelMargin = 10;
                            graph.AddEdge(name, sfmtName);
                        }
                    }
                    break;
                default:
                    n.LabelText = string.Format("{0}", dn.type);
                    break;
                }

            }

            viewer.Graph = graph;
            mWFHost.Child = viewer;

            mSAS.ClearDeviceNodeList();
        }

        private void UpdateAudioSessions() {
            int hr = 0;
            if (mListBoxAudioDevices.SelectedIndex < 0) {
                return;
            }

            mTextBoxAudioSessions.Clear();

            hr = mSAS.CreateAudioSessionList(mListBoxAudioDevices.SelectedIndex);
            if (hr < 0) {
                return;
            }

            var sb = new StringBuilder();

            for (int i=0; i<mSAS.GetAudioSessionsNum(); ++i) {
                var asr = mSAS.GetAudioSessionNth(i);
                if (asr.state == WWShowAudioStatusCs.WWAudioSessionState.Active) { 
                    sb.AppendFormat("{0} : {1}, pid={2}, {3}, {4}\n", asr.nth,
                        asr.isSystemSoundsSession ? "SystemSession" : "NonSystemSession",
                        asr.pid, asr.state, asr.displayName);
                }
            }

            mSAS.ClearAudioSessions();

            mTextBoxAudioSessions.Text = sb.ToString();

        }


        private void Window_Closed(object sender, EventArgs e) {
            Dispose();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (mSAS != null) {
                        mSAS.Dispose();
                        mSAS = null;
                    }
                    if (mWFHost != null) {
                        mWFHost.Dispose();
                        mWFHost = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
