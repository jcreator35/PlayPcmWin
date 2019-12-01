using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WWUserControls {
    /// <summary>
    /// Interaction logic for PlaylistUC.xaml
    /// </summary>
    public partial class PlaylistUC : UserControl {
        public PlaylistUC() {
            InitializeComponent();
        }

        private bool m_playListMouseDown = false;

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // プロパティー。

        public bool IsReadOnly {
            get { return mDGPlayList.IsReadOnly; }
            set { mDGPlayList.IsReadOnly = value; }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // イベントを発生します。

        public enum Events {
            StartPlay,
            SelectionChanged,
            DelistItem,
            DelistAll,
        }

        public delegate void PlaylistEventFunc(Events ev, int idx);

        private PlaylistEventFunc mPlaylistEventFunc = null;

        /// <summary>
        /// 再生リストイベントハンドラ関数をセット。
        /// 再生リストイベントはUIスレッドで発生。
        /// ・StatPlay: 再生開始(曲がマウスでダブルクリックされた)。idxは曲番号。
        /// ・SelectionChanged: 選択状態の曲が変わった。idxは曲番号。
        /// ・DelistItem: 曲削除。idxは曲番号。
        /// ・DelistAll: 全曲削除。
        /// </summary>
        /// <param name="f">イベントハンドラ関数。nullを指定するとイベントハンドラ関数は呼ばれなくなる。</param>
        public void SetEventHandler(PlaylistEventFunc f) {
            mPlaylistEventFunc = f;
        }

        private void RaiseEvent(Events ev, int idx) {
            if (mPlaylistEventFunc == null) {
                return;
            }

            mPlaylistEventFunc(ev, idx);
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // UI event handlers

        private void DGPlayList_LoadingRow(object sender, DataGridRowEventArgs e) {
            e.Row.MouseDoubleClick += new MouseButtonEventHandler(DGPlayList_RowMouseDoubleClick);
        }

        private void DGPlayList_RowMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            // 再生開始リクエストを送る。
            RaiseEvent(Events.StartPlay, mDGPlayList.SelectedIndex);
        }

        private void DGPlayList_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            m_playListMouseDown = true;

        }

        private void DGPlayList_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            m_playListMouseDown = false;
        }

        private void DGPlayList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
            /*
                if (m_state == State.プレイリストあり && 0 <= dataGridPlayList.SelectedIndex) {
                    buttonRemovePlayList.IsEnabled = true;
                } else {
                    buttonRemovePlayList.IsEnabled = false;
                }
            */
        }

        private void DGPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (0 <= mDGPlayList.SelectedIndex) {
                mDGButtonDelistSelected.IsEnabled = true;
            } else {
                mDGButtonDelistSelected.IsEnabled = false;
            }

            RaiseEvent(Events.SelectionChanged, mDGPlayList.SelectedIndex);
        }

        #region Drag and drop event

        private void DGPlayList_CheckDropTarget(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                // ファイルのドラッグアンドドロップ。
                // ここでハンドルせず、MainWindowのMainWindowDragDropに任せる。
                e.Handled = false;
                return;
            }

            e.Handled = true;
            var row = FindVisualParent<DataGridRow>(e.OriginalSource as UIElement);
            if (row == null || !(row.Item is PlayListItemInfo)) {
                // 行がドラッグされていない。
                e.Effects = DragDropEffects.None;
            } else {
                // 行がドラッグされている。
                // Id列を選択している場合のみドラッグアンドドロップ可能。
                //if (0 != "Id".CompareTo(dataGridPlayList.CurrentCell.Column.Header)) {
                //    e.Effects = DragDropEffects.None;
                //}
                // e.Effects = DragDropEffects.Move;
            }
        }

        private void DGPlayList_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                // ファイルのドラッグアンドドロップ。
                // ここでハンドルせず、MainWindowのMainWindowDragDropに任せる。
                e.Handled = false;
                return;
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
            DataGridRow row = FindVisualParent<DataGridRow>(e.OriginalSource as UIElement);
            if (row == null || !(row.Item is PlayListItemInfo)) {
                // 行がドラッグされていない。(セルがドラッグされている)
            } else {
                // 再生リスト項目のドロップ。
                m_dropTargetPlayListItem = row.Item as PlayListItemInfo;
                if (m_dropTargetPlayListItem != null) {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }

        private void DGPlayList_MouseMove(object sender, MouseEventArgs e) {
            /*
                if (m_state == State.再生中 ||
                    m_state == State.再生一時停止中) {
                    // 再生中は再生リスト項目入れ替え不可能。
                    return;
                }

                if (e.LeftButton != MouseButtonState.Pressed) {
                    // 左マウスボタンが押されていない。
                    return;
                }

                var row = FindVisualParent<DataGridRow>(e.OriginalSource as FrameworkElement);
                if ((row == null) || !row.IsSelected) {
                    Console.WriteLine("MouseMove row==null || !row.IsSelected");
                    return;
                }

                var pli = row.Item as PlayListItemInfo;

                // MainWindow.Drop()イベントを発生させる(ブロック)。
                var finalDropEffect = DragDrop.DoDragDrop(row, pli, DragDropEffects.Move);
                if (finalDropEffect == DragDropEffects.Move && m_dropTargetPlayListItem != null) {
                    // ドロップ操作実行。
                    // Console.WriteLine("MouseMove do move");

                    var oldIndex = m_playListItems.IndexOf(pli);
                    var newIndex = m_playListItems.IndexOf(m_dropTargetPlayListItem);
                    if (oldIndex != newIndex) {
                        // 項目が挿入された。PcmDataも挿入処理する。
                        m_playListItems.Move(oldIndex, newIndex);
                        PcmDataListItemsMove(oldIndex, newIndex);
                        // m_playListView.RefreshCollection();
                        dataGridPlayList.UpdateLayout();
                    }
                    m_dropTargetPlayListItem = null;
                }
            */
        }

        private static T FindVisualParent<T>(UIElement element) where T : UIElement {
            var parent = element;
            while (parent != null) {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null) {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private PlayListItemInfo m_dropTargetPlayListItem = null;

        private void DGButtonClearPlayList_Click(object sender, RoutedEventArgs e) {

        }

        private void DGButtonRemovePlayList_Click(object sender, RoutedEventArgs e) {

        }

        #endregion

#if false
        /// <summary>
        /// ap.PcmDataListForDispのIdとGroupIdをリナンバーする。
        /// </summary>
        private void PcmDataListForDispItemsRenumber() {
            m_groupIdNextAdd = 0;
            for (int i = 0; i < ap.PcmDataListForDisp.Count(); ++i) {
                var pcmData = ap.PcmDataListForDisp.At(i);
                var pli = m_playListItems[i];

                if (0 < i) {
                    var prevPcmData = ap.PcmDataListForDisp.At(i - 1);
                    var prevPli = m_playListItems[i - 1];

                    if (prevPli.ReadSeparaterAfter || !pcmData.IsSameFormat(prevPcmData)) {
                        /* 1つ前の項目にReadSeparatorAfterフラグが立っている、または
                         * 1つ前の項目とPCMフォーマットが異なる。
                         * ファイルグループ番号を更新する。
                         */
                        ++m_groupIdNextAdd;
                    }
                }

                pcmData.Id = i;
                pcmData.Ordinal = i;
                pcmData.GroupId = m_groupIdNextAdd;
            }
        }

        /// <summary>
        /// oldIdxの項目をnewIdxの項目の後に挿入する。
        /// </summary>
        private void PcmDataListItemsMove(int oldIdx, int newIdx) {
            System.Diagnostics.Debug.Assert(oldIdx != newIdx);

            /* oldIdx==0, newIdx==1, Count==2の場合
             * remove(0)
             * insert(1)
             * 
             * oldIdx==1, newIdx==0, Count==2の場合
             * remove(1)
             * insert(0)
             */

            var old = ap.PcmDataListForDisp.At(oldIdx);
            ap.PcmDataListForDisp.RemoveAt(oldIdx);
            ap.PcmDataListForDisp.Insert(newIdx, old);

            // Idをリナンバーする。
            PcmDataListForDispItemsRenumber();
        }
#endif
        /*
        void PlayListItemInfoPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ReadSeparaterAfter") {
                // グループ番号をリナンバーする。
                PcmDataListForDispItemsRenumber();
            }
        }
        */

    }
}
