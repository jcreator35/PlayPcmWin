// 日本語
#pragma once
#include <SpatialAudioClient.h>
#include "WWUtil.h"
#include <assert.h>

class WWDynamicAudioStreamChannel {
public:
    void ReleaseAll(void) {
        SafeRelease(&sao);

        delete[] buffer;
        buffer = nullptr;
    }

    /// @retval true : 最後のPCMデータを送出した。
    bool CopyNextPcmTo(BYTE *buffTo, int buffToBytes) {
        assert(buffToBytes);
        int copyBytes = bufferBytes - posInBytes;
        if (copyBytes < buffToBytes) {
            // 残りのPCMデータが少ない場合。
            // データがない部分は0で埋める。
            memset(buffTo, 0, buffToBytes);
            memcpy(buffTo, &buffer[posInBytes], copyBytes);
        } else {
            copyBytes = buffToBytes;
            memcpy(buffTo, &buffer[posInBytes], copyBytes);
        }
        posInBytes += copyBytes;

        return copyBytes != buffToBytes;
    }

    void SetPos3D(float x, float y, float z) {
        posX = x;
        posY = y;
        posZ = z;
    }

    ISpatialAudioObject *sao = nullptr;
    BYTE *buffer = nullptr; ///< new BYTE[] で確保すること。
    int   bufferBytes = 0;
    int   posInBytes = 0;

    int idx; //< set on WWSpatialAudioUser::AddStream(). unique index starts from 0
    float posX   = +0.0f;   ///< in meters, positive value : right
    float posY   = +0.0f;     ///< in meters, positive value : above
    float posZ   = -1.0f;   ///< in meters, negative value : in front
    float volume = +1.0f; ///< 0 to 1
};
