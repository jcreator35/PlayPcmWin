using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PcmDataLib;

namespace PlayPcmWin {
    class PcmDataList {
        List<PcmData> mPcmDataList = new List<PcmData>();

        public PcmData FindById(int wavDataId) {
            for (int i=0; i < mPcmDataList.Count; ++i) {
                var pcmData = mPcmDataList[i];
                if (pcmData.Id == wavDataId) {
                    return pcmData;
                }
            }
            return null;
        }

        /// <summary>
        /// 指定された再生グループIdに属するWavDataの数を数える。O(n)
        /// </summary>
        /// <param name="groupId">指定された再生グループId</param>
        /// <returns>WavDataの数。1つもないときは0</returns>
        public int CountPcmDataOnPlayGroup(int groupId) {
            int count = 0;
            for (int i = 0; i < mPcmDataList.Count(); ++i) {
                if (mPcmDataList[i].GroupId == groupId) {
                    ++count;
                }
            }

            return count;
        }

        /// <summary>
        /// 再生グループId==groupIdの先頭のファイルのPcmIdを取得。O(n)
        /// </summary>
        /// <param name="groupId">再生グループId</param>
        /// <returns>再生グループId==groupIdの先頭のファイルのWavDataId。見つからないときは-1</returns>
        public int GetFirstPcmDataIdOnGroup(int groupId) {
            for (int i = 0; i < mPcmDataList.Count(); ++i) {
                if (mPcmDataList[i].GroupId == groupId) {
                    return mPcmDataList[i].Id;
                }
            }

            return -1;
        }

        public void Add(PcmData pd) {
            mPcmDataList.Add(pd);
        }

        public void Clear() {
            mPcmDataList.Clear();
        }

        public int Count() {
            return mPcmDataList.Count;
        }

        public PcmData Last() {
            return mPcmDataList.Last();
        }

        public void RemoveAt(int idx) {
            mPcmDataList.RemoveAt(idx);
        }

        public void Insert(int idx, PcmData pd) {
            mPcmDataList.Insert(idx, pd);
        }

        public PcmData At(int idx) {
            return mPcmDataList.ElementAt(idx);
        }
    }
}
