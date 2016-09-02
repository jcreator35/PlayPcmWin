using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PlayPcmWinAlbum {
    public class VerticalScrollTilePanel : Panel, IScrollInfo {
        private Canvas mContent;
        private TranslateTransform mTrans = new TranslateTransform();
        private Point mOffset;
        private ScrollViewer mOwner;
        private bool mCanHScroll = false;
        private bool mCanVScroll = false;
        private Size mExtent = new Size(0, 0);
        private Size mViewport = new Size(0, 0);
        private List<IVirtualTile> mVirtualChildList = new List<IVirtualTile>();

        public VerticalScrollTilePanel() {
            this.RenderTransform = mTrans;
            mContent = new Canvas();
            this.Children.Add(mContent);
        }

        public void UpdateTileSize(Size tileSize) {
            mTileSize = tileSize;
        }

        private Size mTileSize = new Size(1, 1);

        public Size TileSize {
            get { return mTileSize; }
        }

        public void AddVirtualChild(IVirtualTile child) {
            mVirtualChildList.Add(child);
        }

        private bool UpdateViewportAndExtent(Size newViewportSize, out Size totalChildrenSize, out int countW) {
            bool updated = false;

            countW = (int)(newViewportSize.Width / mTileSize.Width);
            if (countW <= 0) {
                countW = 1;
            }

            int countH = (mVirtualChildList.Count + countW - 1) / countW;

            totalChildrenSize = new Size(
                    countW * mTileSize.Width,
                    countH * mTileSize.Height);

            if (totalChildrenSize != mExtent) {
                mExtent = totalChildrenSize;
                updated = true;
            }

            if (newViewportSize != mViewport) {
                mViewport = newViewportSize;
                updated = true;
            }

            return updated;
        }

        protected override Size MeasureOverride(Size availableSize) {
            Size childSize;
            int countW;
            bool updated = UpdateViewportAndExtent(availableSize, out childSize, out countW);
            if (updated && mOwner != null) {
                mOwner.InvalidateScrollInfo();
            }

            foreach (UIElement child in this.InternalChildren) {
                child.Measure(childSize);
            }

            return availableSize;
        }

        public void UpdateChildPosition() {
            UpdateChildPosition(mViewport);
        }

        private void UpdateChildPosition(Size finalSize) {
            Size childSize;
            int countW;
            bool updated = UpdateViewportAndExtent(finalSize, out childSize, out countW);
            if (updated && mOwner != null) {
                mOwner.InvalidateScrollInfo();
            }

            mContent.Width = Math.Max(mContent.MinWidth, mExtent.Width);
            mContent.Height = Math.Max(mContent.MinHeight, mExtent.Height);
            mContent.Arrange(new Rect(0, 0, mContent.Width, mContent.Height));

            List<IVirtualTile>.Enumerator ite = mVirtualChildList.GetEnumerator();
            for (int i = 0; i < mVirtualChildList.Count; ++i) {
                int x = i % countW;
                int y = i / countW;

                ite.MoveNext();
                var c = ite.Current;

                // スクリーン座標系で比較。
                double viewPortTop = (this as IScrollInfo).VerticalOffset;
                double viewportBottom = (this as IScrollInfo).VerticalOffset + (this as IScrollInfo).ViewportHeight;
                double tileTop = y * mTileSize.Height;
                double tileBottom = (y + 1) * mTileSize.Height;

                if (viewPortTop <= tileBottom &&
                        tileTop <= viewportBottom) {
                    // タイルが一部分でも画面内に存在する。
                    var uie = c.Visual;
                    if (null == uie) {
                        uie = c.CreateVisual(this);
                        mContent.Children.Add(uie);
                    }
                    Canvas.SetLeft(uie, x * mTileSize.Width);
                    Canvas.SetTop(uie, y * mTileSize.Height);
                } else {
                    // タイルが完全に画面外。
                    var uie = c.Visual;
                    if (uie != null) {
                        mContent.Children.Remove(uie);
                        c.DisposeVisual();
                    }
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize) {
            UpdateChildPosition(finalSize);
            (this as IScrollInfo).SetVerticalOffset(mOffset.Y);

            return finalSize;
        }

        Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle) {
            return rectangle;
        }

        void IScrollInfo.SetVerticalOffset(double offset) {
            if (offset < 0 || mViewport.Height >= mExtent.Height) {
                offset = 0;
            } else {
                if (offset + mViewport.Height >= mExtent.Height) {
                    offset = mExtent.Height - mViewport.Height;
                }
            }

            mOffset.Y = offset;
            mTrans.Y = -offset;

            UpdateChildPosition();
            if (mOwner != null) {
                mOwner.InvalidateScrollInfo();
            }
        }

        ScrollViewer IScrollInfo.ScrollOwner {
            get { return mOwner; }
            set { mOwner = value; }
        }

        bool IScrollInfo.CanHorizontallyScroll {
            get { return mCanHScroll; }
            set { mCanHScroll = value; }
        }

        bool IScrollInfo.CanVerticallyScroll {
            get { return mCanVScroll; }
            set { mCanVScroll = value; }
        }

        double IScrollInfo.HorizontalOffset {
            get { return mOffset.X; }
        }

        double IScrollInfo.VerticalOffset {
            get { return mOffset.Y; }
        }

        double IScrollInfo.ExtentHeight {
            get { return mExtent.Height; }
        }

        double IScrollInfo.ExtentWidth {
            get { return mExtent.Width; }
        }

        double IScrollInfo.ViewportHeight {
            get { return mViewport.Height; }
        }

        double IScrollInfo.ViewportWidth {
            get { return mViewport.Width; }
        }

        void IScrollInfo.LineUp() {
            (this as IScrollInfo).SetVerticalOffset((this as IScrollInfo).VerticalOffset - 1);
        }

        void IScrollInfo.LineDown() {
            (this as IScrollInfo).SetVerticalOffset((this as IScrollInfo).VerticalOffset + 1);
        }

        void IScrollInfo.MouseWheelUp() {
            (this as IScrollInfo).SetVerticalOffset((this as IScrollInfo).VerticalOffset - mTileSize.Height/2);
        }

        void IScrollInfo.MouseWheelDown() {
            (this as IScrollInfo).SetVerticalOffset((this as IScrollInfo).VerticalOffset + mTileSize.Height/2);
        }

        void IScrollInfo.PageUp() {
            double childHeight = (mViewport.Height * 2) / this.InternalChildren.Count;
            (this as IScrollInfo).SetVerticalOffset((this as IScrollInfo).VerticalOffset - childHeight);
        }

        void IScrollInfo.PageDown() {
            double childHeight = (mViewport.Height * 2) / this.InternalChildren.Count;
            (this as IScrollInfo).SetVerticalOffset((this as IScrollInfo).VerticalOffset + childHeight);
        }

        void IScrollInfo.SetHorizontalOffset(double offset) {
        }

        void IScrollInfo.LineLeft() {
        }

        void IScrollInfo.LineRight() {
        }

        void IScrollInfo.MouseWheelLeft() {
        }

        void IScrollInfo.MouseWheelRight() {
        }

        void IScrollInfo.PageLeft() {
        }

        void IScrollInfo.PageRight() {
        }
    }
}
