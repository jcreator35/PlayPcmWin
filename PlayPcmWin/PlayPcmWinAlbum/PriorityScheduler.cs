using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlayPcmWinAlbum
{
    public class PriorityScheduler : TaskScheduler
    {
        public static PriorityScheduler BelowNormal = new PriorityScheduler(ThreadPriority.BelowNormal);

        private BlockingCollection<Task> mTasks = new BlockingCollection<Task>();
        private Thread[] mThreads;
        private ThreadPriority mPriority;
        private readonly int mMaximumConcurrencyLevel = Math.Max(1, Environment.ProcessorCount);

        public PriorityScheduler(ThreadPriority priority) {
            mPriority = priority;
        }

        public override int MaximumConcurrencyLevel {
            get { return mMaximumConcurrencyLevel; }
        }

        protected override IEnumerable<Task> GetScheduledTasks() {
            return mTasks;
        }

        protected override void QueueTask(Task task) {
            mTasks.Add(task);

            if (mThreads == null) {
                mThreads = new Thread[mMaximumConcurrencyLevel];
                for (int i = 0; i < mThreads.Length; i++) {
                    int local = i;
                    mThreads[i] = new Thread(() => {
                        foreach (Task t in mTasks.GetConsumingEnumerable())
                            base.TryExecuteTask(t);
                    });
                    mThreads[i].Name = string.Format("PriorityScheduler: ", i);
                    mThreads[i].Priority = mPriority;
                    mThreads[i].IsBackground = true;
                    mThreads[i].Start();
                }
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            // we might not want to execute task that should schedule as high or low priority inline
            return false;
        }
    }
}
