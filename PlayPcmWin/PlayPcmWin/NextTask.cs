using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayPcmWin {
    // 再生停止完了後に行うタスク。
    enum NextTaskType {
        /// <summary>
        /// 停止する。
        /// </summary>
        None,

        /// <summary>
        /// 指定されたグループをメモリに読み込み、グループの先頭の項目を再生開始する。
        /// </summary>
        PlaySpecifiedGroup,

        /// <summary>
        /// 指定されたグループをメモリに読み込み、グループの先頭の項目を再生一時停止状態にする。
        /// </summary>
        PlayPauseSpecifiedGroup,
    }

    class NextTask {
        public NextTask() {
            Type = NextTaskType.None;
            GroupId = -1;
            PcmDataId = -1;
        }

        public NextTask(NextTaskType type) {
            Set(type);
        }

        public NextTask(NextTaskType type, int groupId, int wavDataId) {
            Set(type, groupId, wavDataId);
        }

        public void Set(NextTaskType type) {
            // 現時点で、このSet()のtypeはNoneしかありえない。
            System.Diagnostics.Debug.Assert(type == NextTaskType.None);
            Type = type;
        }

        public void Set(NextTaskType type, int groupId, int wavDataId) {
            Type = type;
            GroupId = groupId;
            PcmDataId = wavDataId;
        }

        public NextTaskType Type { get; set; }
        public int GroupId { get; set; }
        public int PcmDataId { get; set; }
    };
}
