using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayPcmWin {
    class AudioPlayer {
        enum State {
            EmptyPlayList,
            ReadingPlayList,
            再生リストあり,

            // これ以降の状態にいる場合、再生リストに新しいファイルを追加できない。
            デバイスSetup完了,
            ファイル読み込み完了,
            再生中,
            再生一時停止中,
            再生停止開始,
            再生グループ読み込み中,
        }

        /// <summary>
        /// UIの状態。
        /// </summary>
        private State m_state = State.EmptyPlayList;

        private void ChangeState(State nowState) {
            m_state = nowState;
        }
    
    
    }
}
