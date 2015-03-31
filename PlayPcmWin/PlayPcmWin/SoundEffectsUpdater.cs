using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayPcmWin {
    class SoundEffectsUpdater {
        public SoundEffectsUpdater() {
        }

        public void Update(Wasapi.WasapiCS wasapi, List<PreferenceAudioFilter> audioFilterList) {
            wasapi.ClearAudioFilter();

            foreach (var f in audioFilterList) {
                wasapi.AppendAudioFilter((Wasapi.WasapiCS.WWAudioFilterType)f.FilterType, f.ToSaveText());
            }
        }
    }
}
