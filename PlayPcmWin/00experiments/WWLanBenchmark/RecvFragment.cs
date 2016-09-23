using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWLanBenchmark {
    class RecvFragment {
        public long mStartPos;
        public long mSizeBytes;
        public byte[] mContent;

        public long StartPos { get { return mStartPos; } }
        public long SizeBytes { get { return mSizeBytes; } }
        public byte[] Content { get { return mContent; } }

        public RecvFragment(long startPos, long sizeBytes, byte [] content) {
            mStartPos = startPos;
            mSizeBytes = sizeBytes;
            mContent = content;
        }
    }
}
