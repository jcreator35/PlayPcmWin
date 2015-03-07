using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;

namespace PlayPcmWin {
    class PreferenceAudioFilterStore {
        private const string m_fileName = "PlayPcmWinAudioFilterSettings.xml";

        public static void Save(List<PreferenceAudioFilter> audioFilterList) {
            using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(
                    m_fileName, System.IO.FileMode.Create,
                    IsolatedStorageFile.GetUserStoreForDomain())) {
                PreferenceAudioFilter.SaveFilteresToStream(audioFilterList, isfs);
            }
        }

        public static List<PreferenceAudioFilter> Load() {
            var result = new List<PreferenceAudioFilter>();
            try {
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(
                        m_fileName, System.IO.FileMode.Open,
                        IsolatedStorageFile.GetUserStoreForDomain())) {
                    result = PreferenceAudioFilter.LoadFiltersFromStream(isfs);
                }
            } catch (System.Exception ex) {
                Console.WriteLine(ex);
            }

            return result;
        }
    }
}
