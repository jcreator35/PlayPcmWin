// 日本語

#include <stdint.h>
#include <assert.h>

struct WWAudioSampleBuffer {
    /// @param aBuff new[]で割り当てたメモリを渡して下さい。
    WWAudioSampleBuffer(uint8_t *aBuff, int aBytesPerFrame, int64_t aTotalBytes) {
        buff = aBuff;
        bytesPerFrame = aBytesPerFrame;
        totalBytes = aTotalBytes;
        posBytes = 0;
    }

    int64_t RemainBytes(void) const {
        return totalBytes - posBytes;
    }

    int64_t RemainFrames(void) const {
        return RemainBytes() / bytesPerFrame;
    }

    void CopyTo(uint8_t *to, int64_t bytes) {
        assert(bytes <= RemainBytes());
        memcpy(to, &buff[posBytes], bytes);
        posBytes += bytes;
    }

    /// buffを delete[]します。
    void Release(void) {
        delete[] buff;
        Forget();
    }

    /// buffを忘れます。
    void Forget(void) {
        buff = nullptr;
        totalBytes = 0;
        posBytes = 0;
        bytesPerFrame = 0;
    }

    /// サンプル配列。
    uint8_t *buff = nullptr;

    /// バイト総数
    int64_t totalBytes = 0;

    /// バイト位置 (0 ≦ posBytes <= totalBytes)
    /// posBytes == totalBytesのとき、このバッファーは読み終わり。
    int64_t posBytes = 0;

    /// 1フレームあたりのバイト数。nCh * bytesPerSample
    int bytesPerFrame = 0;
};
