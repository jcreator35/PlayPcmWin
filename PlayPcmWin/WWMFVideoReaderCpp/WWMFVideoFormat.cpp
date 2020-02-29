// 日本語。

#include "WWMFVideoFormat.h"

#include <stdio.h>

void
WWMFVideoFormatPrint(WWMFVideoFormat &vf)
{
    printf("  WH=(%d,%d)\n", vf.pixelWH.w, vf.pixelWH.h);
    printf("  aspectStretchedWH=(%d,%d)\n", vf.aspectStretchedWH.w, vf.aspectStretchedWH.h);
    printf("  apertureXY=(%d,%d) WH=(%d,%d)\n", vf.aperture.x, vf.aperture.y, vf.aperture.w, vf.aperture.h);
    printf("  aspectRatio=%d:%d\n", vf.aspectRatio.numer, vf.aspectRatio.denom);
    printf("  frameRate=%.2f fps\n", (double)vf.frameRate.numer / vf.frameRate.denom);
    printf("  duration=%.2f sec\n", 0.001 * 0.001 *0.1 * vf.duration);
    printf("  timeStamp=%.2f sec\n", 0.001 * 0.001 *0.1 * vf.timeStamp);
    if (vf.flags & WW_MF_VIDEO_IMAGE_FMT_TopDown) {
        printf("  image is top-down\n");
    } else {
        printf("  image is bottom-up\n");
    }
    printf("  canSeek=%d\n", 0 != (WW_MF_VIDEO_IMAGE_FMT_CAN_SEEK & vf.flags));
    printf("  slowSeek=%d\n", 0 != (WW_MF_VIDEO_IMAGE_FMT_SLOW_SEEK & vf.flags));
    if (vf.flags & WW_MF_VIDEO_IMAGE_FMT_LIMITED_RANGE_16_to_235) {
        printf("  limited range(16 to 235)\n");
    } else {
        printf("  not limited range\n");
    }
}
