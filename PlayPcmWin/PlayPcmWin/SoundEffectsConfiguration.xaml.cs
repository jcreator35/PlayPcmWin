using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace PlayPcmWin {
    public partial class SoundEffectsConfiguration : Window {
        private ObservableCollection<PreferenceAudioFilter> mAudioFilterList = new ObservableCollection<PreferenceAudioFilter>();

        public ObservableCollection<PreferenceAudioFilter> AudioFilterList {
            get {
                return mAudioFilterList;
            }
        }

        public SoundEffectsConfiguration() {
            InitializeComponent();

            groupBoxActivated.Header = Properties.Resources.AudioFilterActivated;
            groupBoxAvailable.Header = Properties.Resources.AudioFilterAvailable;
            buttonCancel.Content = Properties.Resources.SettingsButtonCancel;
            buttonClearAll.Content = Properties.Resources.AudioFilterClear;

            listBoxAvailableEffects.Items.Clear();
            // WWAudioFilterTypeと同じ順番にする
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterPolarityInvert);
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterMonauralMix);
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterChannelMapping);
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterMuteChannel);
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterSoloChannel);
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterZohNosdacCompensation);
            listBoxAvailableEffects.Items.Add(Properties.Resources.AudioFilterDelay);

            listBoxAvailableEffects.SelectedIndex = 0;
            buttonLeftArrow.IsEnabled = true;
            buttonRightArrow.IsEnabled = false;
            buttonClearAll.IsEnabled = false;
        }

        public void SetAudioFilterList(List<PreferenceAudioFilter> audioFilterList) {
            mAudioFilterList = new ObservableCollection<PreferenceAudioFilter>();
            foreach (var f in audioFilterList) {
                mAudioFilterList.Add(f.Copy());
            }

            AudioFilterListUpdated();
        }

        private void AudioFilterListUpdated() {
            int selectedIdx = listBoxActivatedEffects.SelectedIndex;

            listBoxActivatedEffects.ItemsSource = mAudioFilterList;

            // 選択位置を復旧する
            if (0 < listBoxActivatedEffects.Items.Count) {
                if (selectedIdx < 0) {
                    listBoxActivatedEffects.SelectedIndex = 0;
                } else if (selectedIdx < listBoxActivatedEffects.Items.Count) {
                    listBoxActivatedEffects.SelectedIndex = selectedIdx;
                } else {
                    listBoxActivatedEffects.SelectedIndex = listBoxActivatedEffects.Items.Count -1;
                }
            }

            if (mAudioFilterList.Count == 0) {
                buttonRightArrow.IsEnabled = false;
            } else {
                buttonRightArrow.IsEnabled = true;
                buttonClearAll.IsEnabled = true;
            }
        }

        private static string[] BuildChannelMappingArgArray(List<Tuple<int, int>> tupleList) {
            var rv = new string[tupleList.Count];

            for (int i=0; i<tupleList.Count; ++i) {
                rv[i] = string.Format("{0}>{1}", tupleList[i].Item1, tupleList[i].Item2);
            }
            return rv;
        }

        private static string SelectedChannelFlagsToString(bool[] channelFlags) {
            var sb = new StringBuilder();

            for (int i = 0; i < channelFlags.Length; ++i) {
                if (channelFlags[i]) {
                    if (sb.Length == 0) {
                        sb.AppendFormat("{0}", i);
                    } else {
                        sb.AppendFormat(",{0}", i);
                    }
                }
            }

            return sb.ToString();
        }

        private void buttonLeftArrow_Click(object sender, RoutedEventArgs e) {
            if (listBoxAvailableEffects.SelectedIndex < 0) {
                return;
            }

            PreferenceAudioFilter filter = null;
            var filterType = (PreferenceAudioFilterType)listBoxAvailableEffects.SelectedIndex;
            switch (filterType) {
            case PreferenceAudioFilterType.MonauralMix:
                filter = new PreferenceAudioFilter(PreferenceAudioFilterType.MonauralMix, null);
                break;
            case PreferenceAudioFilterType.ChannelRouting: {
                    var dlg = new ChannelMappingSettings();
                    dlg.UpdateChannelMapping(null);
                    var dlgResult = dlg.ShowDialog();
                    if (dlgResult != true) {
                        return;
                    }

                    filter = new PreferenceAudioFilter(PreferenceAudioFilterType.ChannelRouting, BuildChannelMappingArgArray(dlg.ChannelMapping));
                }
                break;
            case PreferenceAudioFilterType.PolarityInvert:
            case PreferenceAudioFilterType.MuteChannel:
            case PreferenceAudioFilterType.SoloChannel: {
                    var dlg = new ChannelSelect();

                    if (filterType == PreferenceAudioFilterType.PolarityInvert) {
                        // デフォルトで両チャンネル極性反転。
                        dlg.SetChannel(0, true);
                        dlg.SetChannel(1, true);
                    }

                    var dlgResult = dlg.ShowDialog();
                    if (dlgResult != true) {
                        return;
                    }

                    var selectedChannelsStr = SelectedChannelFlagsToString(dlg.SelectedChannels);
                    if (selectedChannelsStr.Length == 0) {
                        MessageBox.Show("Please select one or more channels.");
                        return;
                    } else {
                        filter = new PreferenceAudioFilter(filterType, new string[1] { selectedChannelsStr });
                    }
                }
                break;
            case PreferenceAudioFilterType.ZohNosdacCompensation:
                filter = new PreferenceAudioFilter(PreferenceAudioFilterType.ZohNosdacCompensation, null);
                break;
            case PreferenceAudioFilterType.Delay: {
                    var dlg = new DelaySettingsWindow();
                    var dlgResult = dlg.ShowDialog();
                    if (dlgResult != true) {
                        return;
                    }

                    filter = new PreferenceAudioFilter(PreferenceAudioFilterType.Delay, new string[1]{dlg.DelayString});
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            if (listBoxActivatedEffects.SelectedIndex < 0) {
                mAudioFilterList.Add(filter);
            } else {
                mAudioFilterList.Insert(listBoxActivatedEffects.SelectedIndex+1, filter);
            }

            AudioFilterListUpdated();
        }

        private void buttonRightArrow_Click(object sender, RoutedEventArgs e) {
            if (listBoxActivatedEffects.SelectedIndex < 0 || mAudioFilterList.Count <= listBoxActivatedEffects.SelectedIndex) {
                return;
            }

            mAudioFilterList.RemoveAt(listBoxActivatedEffects.SelectedIndex);

            AudioFilterListUpdated();
        }

        private void buttonClearAll_Click(object sender, RoutedEventArgs e) {
            mAudioFilterList = new ObservableCollection<PreferenceAudioFilter>();
            AudioFilterListUpdated();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void cmdListSettings_Click(object sender, RoutedEventArgs e) {
            Button cmd = (Button)sender;
            if (cmd.DataContext is PreferenceAudioFilter) {
                var before = cmd.DataContext as PreferenceAudioFilter;

                switch (before.FilterType) {
                case PreferenceAudioFilterType.ChannelRouting:
                    var dlg = new ChannelMappingSettings();
                    dlg.UpdateChannelMapping(before.ChannelMapping());
                    var dlgResult = dlg.ShowDialog();
                    if (dlgResult != true) {
                        return;
                    }
                    before.ArgArray = BuildChannelMappingArgArray(dlg.ChannelMapping);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
            }
        }
    }
}
