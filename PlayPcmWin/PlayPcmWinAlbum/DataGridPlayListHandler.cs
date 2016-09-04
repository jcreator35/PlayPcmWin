using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace PlayPcmWinAlbum {
    class DataGridPlayListHandler {
        private DataGrid mDg;
        private ObservableCollection<PlayListItemInfo> mPlayListItems = new ObservableCollection<PlayListItemInfo>();


        public DataGridPlayListHandler(DataGrid dg) {
            mDg = dg;
        }

        public void ShowAlbum(ContentList.Album album) {
            mPlayListItems.Clear();
            PlayListItemInfo.SetNextRowId(1);

            for (int i = 0; i < album.AudioFileCount; ++i) {
                var af = album.AudioFileNth(i);
                mPlayListItems.Add(new PlayListItemInfo(af.Pcm));
            }

            mDg.ItemsSource = mPlayListItems;
        }
    }
}
