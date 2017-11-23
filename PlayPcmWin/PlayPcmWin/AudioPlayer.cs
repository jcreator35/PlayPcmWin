using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wasapi;
using System.Security.Cryptography;
using PcmDataLib;
using System.Globalization;
using WWUtil;
using WasapiPcmUtil;
using System.ComponentModel;

namespace PlayPcmWin {
    class AudioPlayer {
        /// <summary>
        /// 再生の進捗状況を取りに行き表示を更新する時間間隔。単位はミリ秒
        /// </summary>
        const int PROGRESS_REPORT_INTERVAL_MS = 100;

        public WasapiCS wasapi;

        private BackgroundWorker m_playWorker;

        /// <summary>
        /// true: 再生停止 無音を送出してから停止する
        /// </summary>
        private bool m_bStopGently;

        public AudioPlayer() {
            m_playWorker = new BackgroundWorker();
            m_playWorker.WorkerReportsProgress = true;
            m_playWorker.DoWork += new DoWorkEventHandler(PlayDoWork);
            m_playWorker.ProgressChanged += new ProgressChangedEventHandler(PlayProgressChanged);
            m_playWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlayRunWorkerCompleted);
            m_playWorker.WorkerSupportsCancellation = true;
        }

        public int WasapiInit() {
            System.Diagnostics.Debug.Assert(wasapi == null);

            wasapi = new WasapiCS();
            int hr = wasapi.Init();
            return hr;
        }

        public void WasapiTerm() {
            wasapi.Term();
            wasapi = null;
        }

        /// <summary>
        /// PcmDataの表示用リスト。
        /// </summary>
        private PcmDataList m_pcmDataListForDisp = new PcmDataList();

        public PcmDataList PcmDataListForDisp {
            get {
                return m_pcmDataListForDisp;
            }
        }

        /// <summary>
        /// PcmDataの再生用リスト。(通常は表示用リストと同じ。シャッフルの時は順番が入れ替わる)
        /// </summary>
        private PcmDataList m_pcmDataListForPlay = new PcmDataList();

        public PcmDataList PcmDataListForPlay {
            get {
                return m_pcmDataListForPlay;
            }
        }

        /// <summary>
        /// 0 <= r < nMaxPlus1の範囲の整数値rをランダムに戻す。
        /// </summary>
        private static int GetRandomNumber(RNGCryptoServiceProvider gen, int nMaxPlus1) {
            var v = new byte[4];
            gen.GetBytes(v);
            return ( BitConverter.ToInt32(v, 0) & 0x7fffffff ) % nMaxPlus1;
        }

        /// <summary>
        /// シャッフルした再生リストap.PcmDataListForPlayを作成する
        /// </summary>
        public void CreateShuffledPlayList() {
            // 適当にシャッフルされた番号が入っている配列pcmDataIdxArrayを作成。
            var pcmDataIdxArray = new int[PcmDataListForDisp.Count()];
            for (int i=0; i < pcmDataIdxArray.Length; ++i) {
                pcmDataIdxArray[i] = i;
            }

            var gen = new RNGCryptoServiceProvider();
            int N = pcmDataIdxArray.Length;
            for (int i=0; i < N * 100; ++i) {
                var a = GetRandomNumber(gen, N);
                var b = GetRandomNumber(gen, N);
                if (a == b) {
                    // 入れ替え元と入れ替え先が同じ。あんまり意味ないのでスキップする。
                    continue;
                }

                // a番目とb番目を入れ替える
                var tmp = pcmDataIdxArray[a];
                pcmDataIdxArray[a] = pcmDataIdxArray[b];
                pcmDataIdxArray[b] = tmp;
            }

            // ap.PcmDataListForPlayを作成。
            m_pcmDataListForPlay = new PcmDataList();
            for (int i=0; i < pcmDataIdxArray.Length; ++i) {
                var idx = pcmDataIdxArray[i];

                // 再生順番号Ordinalを付け直す
                // GroupIdをバラバラの番号にする(1曲ずつ読み込む)
                var pcmData = new PcmData();
                pcmData.CopyFrom(m_pcmDataListForDisp.At(idx));
                pcmData.Ordinal = i;
                pcmData.GroupId = i;

                m_pcmDataListForPlay.Add(pcmData);
            }
        }

        /// <summary>
        /// 1曲再生のプレイリストをap.PcmDataListForPlayに作成。
        /// </summary>
        public void CreateOneTrackPlayList(int wavDataId) {
            var pcmData = new PcmData();
            pcmData.CopyFrom(m_pcmDataListForDisp.FindById(wavDataId));
            pcmData.GroupId = 0;

            m_pcmDataListForPlay = new PcmDataList();
            m_pcmDataListForPlay.Add(pcmData);
        }

        /// <summary>
        /// 全曲が表示順に並んでいる再生リストap.PcmDataListForPlayを作成。
        /// </summary>
        public void CreateAllTracksPlayList() {
            m_pcmDataListForPlay = new PcmDataList();
            for (int i=0; i < m_pcmDataListForDisp.Count(); ++i) {
                var pcmData = new PcmData();
                pcmData.CopyFrom(m_pcmDataListForDisp.At(i));
                m_pcmDataListForPlay.Add(pcmData);
            }
        }

        public void ClearPlayList() {
            PcmDataListForDisp.Clear();
            PcmDataListForPlay.Clear();
            wasapi.ClearPlayList();
        }

        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// PlayWorkerが戻すイベント。
        /// </summary>
        public enum PlayEventType {
            ProgressChanged,
            Finished,
            Canceled
        };

        public class PlayEvent {
            public PlayEventType eventType;
            public int ercd;
            public BackgroundWorker bw;

            public PlayEvent(PlayEventType t, int aErcd, BackgroundWorker aBw) {
                eventType = t;
                ercd = aErcd;
                bw = aBw;
            }
        };

        public delegate void PlayEventCallback(PlayEvent ev);

        private PlayEventCallback m_playEventCb = null;

        class PlayDoWorkResult {
            public int hr;
        };

        /// <summary>
        /// 再生中。バックグラウンドスレッド。
        /// </summary>
        private void PlayDoWork(object o, DoWorkEventArgs args) {
            //Console.WriteLine("PlayDoWork started");
            var r = new PlayDoWorkResult();
            r.hr = 0;
            args.Result = r;
            var bw = o as BackgroundWorker;
            bool cancelProcessed = false;

            while (!wasapi.Run(PROGRESS_REPORT_INTERVAL_MS)) {
                m_playWorker.ReportProgress(0);

                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending && !cancelProcessed) {
                    Console.WriteLine("PlayDoWork() CANCELED StopGently=" + m_bStopGently);
                    if (m_bStopGently) {
                        // 最後に再生する無音の再生にジャンプする。その後再生するものが無くなって停止する
                        wasapi.UpdatePlayPcmDataById(-1);
                        wasapi.Unpause();
                        cancelProcessed = true;
                    } else {
                        r.hr = wasapi.Stop();
                    }
                    args.Cancel = true;
                }
            }

            // 正常に最後まで再生が終わった場合、ここでStopを呼んで、後始末する。
            // キャンセルの場合は、2回Stopが呼ばれることになるが、問題ない!!!
            int hr = wasapi.Stop();
            if (0 <= r.hr) {
                r.hr = hr;
            }

            // 停止完了後タスクの処理は、ここではなく、PlayRunWorkerCompletedで行う。
            Console.WriteLine("PlayDoWork() end");
        }

        private void PlayProgressChanged(object sender, ProgressChangedEventArgs e) {
            var ev = new PlayEvent(PlayEventType.ProgressChanged ,0, m_playWorker);
            CallEventCallback(ev);
        }

        private void PlayRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            PlayDoWorkResult r = new PlayDoWorkResult();
            r.hr = 0;

            if (!e.Cancelled) {
                r = e.Result as PlayDoWorkResult;
            }

            PlayEventType t = (e.Cancelled) ? PlayEventType.Canceled : PlayEventType.Finished;
            var ev = new PlayEvent(t, r.hr, m_playWorker);
            CallEventCallback(ev);
        }

        private void CallEventCallback(PlayEvent ev) {
            if (m_playEventCb != null) {
                m_playEventCb(ev);
            }
        }

        private void StartPlayWorker(PlayEventCallback cb) {
            m_playEventCb = cb;

            m_playWorker.RunWorkerAsync();
        }

        public void SetPlayEventCallback(PlayEventCallback cb) {
            m_playEventCb = cb;
        }

        public bool IsPlayWorkerBusy() {
            return m_playWorker.IsBusy;
        }

        public int StartPlayback(int wavDataId, PlayEventCallback cb) {
            int hr = wasapi.StartPlayback(wavDataId);
            StartPlayWorker(cb);
            return hr;
        }

        public bool PlayStop(bool stopGently) {
            if (m_playWorker.IsBusy) {
                m_bStopGently = stopGently;
                m_playWorker.CancelAsync();
                return true;
            }

            // StartPlayback()が呼ばれたが、例外が出てスレッドが開始していない場合。
            wasapi.Stop();
            return true;
        }

    }
}
