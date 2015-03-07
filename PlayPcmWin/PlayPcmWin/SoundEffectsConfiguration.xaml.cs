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

namespace PlayPcmWin {


    public partial class SoundEffectsConfiguration : Window {
        List<PreferenceAudioFilter> mAudioFilterList = new List<PreferenceAudioFilter>();

        public void SetAudioFilterList(List<PreferenceAudioFilter> audioFilterList) {

            mAudioFilterList = new List<PreferenceAudioFilter>();
            foreach (var f in audioFilterList) {
                mAudioFilterList.Add(f.Copy());
            }

            AudioFilterListUpdated();
        }

        public List<PreferenceAudioFilter> AudioFilterList {
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

            listBoxAvailableEffects.SelectedIndex = 0;
            buttonLeftArrow.IsEnabled = true;
            buttonRightArrow.IsEnabled = false;
            buttonClearAll.IsEnabled = false;
        }

        private void AudioFilterListUpdated() {
            int selectedIdx = listBoxActivatedEffects.SelectedIndex;

            listBoxActivatedEffects.Items.Clear();
            foreach (var item in mAudioFilterList) {
                listBoxActivatedEffects.Items.Add(item.ToDescriptionText());
            }

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

        private void buttonLeftArrow_Click(object sender, RoutedEventArgs e) {
            if (listBoxAvailableEffects.SelectedIndex < 0) {
                return;
            }

            PreferenceAudioFilter filter = null;
            switch ((PreferenceAudioFilterType)listBoxAvailableEffects.SelectedIndex) {
            case PreferenceAudioFilterType.PolarityInvert:
                filter = new PreferenceAudioFilter(PreferenceAudioFilterType.PolarityInvert);
                break;
            case PreferenceAudioFilterType.MonauralMix:
                filter = new PreferenceAudioFilter(PreferenceAudioFilterType.MonauralMix);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            if (listBoxActivatedEffects.SelectedIndex < 0) {
                mAudioFilterList.Add(filter);
            } else {
                mAudioFilterList.Insert(listBoxActivatedEffects.SelectedIndex, filter);
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
            mAudioFilterList = new List<PreferenceAudioFilter>();
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
    }
}
