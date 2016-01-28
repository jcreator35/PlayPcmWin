using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;

namespace PlayPcmWin {
    class PreferenceAudioFilterStore {
        private const string m_fileName = "PlayPcmWinAudioFilterSettings.txt";

        // File extension is wrong. This file is actually not xml file.
        private const string m_oldFileName = "PlayPcmWinAudioFilterSettings.xml";

        public static void Save(List<PreferenceAudioFilter> audioFilterList) {
            using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(
                    m_fileName, System.IO.FileMode.Create,
                    IsolatedStorageFile.GetUserStoreForDomain())) {
                PreferenceAudioFilter.SaveFilteresToStream(audioFilterList, isfs);
            }
        }

        private static List<PreferenceAudioFilter> TryLoad(string path) {
            var result = new List<PreferenceAudioFilter>();
            try {
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(
                        path, System.IO.FileMode.Open,
                        IsolatedStorageFile.GetUserStoreForDomain())) {
                    result = PreferenceAudioFilter.LoadFiltersFromStream(isfs);
                }
            } catch (System.Exception ex) {
                // file not found
                Console.WriteLine(ex);
                result = null;
            }
            return result;
        }

        public static List<PreferenceAudioFilter> Load() {
            var result = TryLoad(m_fileName);
            if (result == null) {
                // new file not found. try load old file
                result = TryLoad(m_oldFileName);
                if (result == null) {
                    // both new file and old file not found.
                    result = new List<PreferenceAudioFilter>();
                }
            }

            return result;
        }
    }
}
