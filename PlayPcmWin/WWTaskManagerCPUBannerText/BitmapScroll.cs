using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace WWTaskManagerText {
    class BitmapScroll {
        const int IMAGE_W = 8;
        const int IMAGE_H = 8;

        /// <summary>
        /// 8ビットのグレー画像をスクロールする。
        /// </summary>
        /// <param name="bm"></param>
        public BitmapScroll(byte [] grayImage, int grayImageWidth) {
            mGrayImage = grayImage;
            mGrayImageWidth = grayImageWidth;
            mPos = 0;
        }

        public byte [] Update() {
            var rawImage = new byte[IMAGE_W * IMAGE_H];
            for (int y = 0; y < IMAGE_H; ++y) {
                for (int x = 0; x < IMAGE_W; ++x) {
                    if (mGrayImageWidth <= x + mPos) {
                        break;
                    }
                    rawImage[x + y * IMAGE_W] = mGrayImage[x + mPos + y * mGrayImageWidth];
                }
            }

            {
                // mPosの更新。
                ++mPos;
                if (PosNum <= mPos) {
                    mPos = 0;
                }
            }

            return rawImage;
        }

        public int PosNum {
            get {
                // mPos==posNumのとき最後の字が完全に消える。
                return mGrayImageWidth;
            }
        }

        private byte[] mGrayImage;
        private int mGrayImageWidth;
        private int mPos;
    }
}
