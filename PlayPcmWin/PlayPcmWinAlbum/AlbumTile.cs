using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PlayPcmWinAlbum {
    public class AlbumTile : IVirtualTile {
        private static Size mTileSize;
        private Button mButton;
        private DockPanel mDockPanel;
        private TextBlock mTextBlockAlbumName;
        private Image mImage;
        private TiledItemContent mContent;
        private CancellationTokenSource mCts;
        private Object mLock = new Object();
        private AlbumTileClicked mAlbumTileClicked;
        private CancellationToken mAppExitToken;

        private const int ALBUM_TEXT_HEIGHT = 20;

        public static void UpdateTileSize(Size size) {
            mTileSize = new Size(size.Width, size.Height);
        }

        public AlbumTile(TiledItemContent content, AlbumTileClicked clickedListener, CancellationToken appExitToken) {
            mContent = content;
            mAlbumTileClicked = clickedListener;
            mAppExitToken = appExitToken;
        }

        public TiledItemContent TiledItemContent {
            get { return mContent; }
        }

        UIElement IVirtualTile.Visual {
            get {
                return mButton;
            }
        }

        UIElement IVirtualTile.CreateVisual(VerticalScrollTilePanel parent) {
            lock (mLock) {
                if (null != mButton) {
                    return mButton;
                }

                mCts = new CancellationTokenSource();

                mImage = new Image();
                mImage.Stretch = Stretch.Uniform;
                mImage.Width = mTileSize.Width;
                mImage.Height = mTileSize.Height - ALBUM_TEXT_HEIGHT;

                mTextBlockAlbumName = new TextBlock();
                mTextBlockAlbumName.HorizontalAlignment = HorizontalAlignment.Center;
                mTextBlockAlbumName.TextTrimming = TextTrimming.CharacterEllipsis;
                mTextBlockAlbumName.Text = mContent.DisplayName;

                mDockPanel = new DockPanel();
                mDockPanel.LastChildFill = true;
                mDockPanel.Children.Add(mTextBlockAlbumName);
                DockPanel.SetDock(mTextBlockAlbumName, Dock.Bottom);
                mDockPanel.Children.Add(mImage);

                mButton = new Button();
                mButton.BorderBrush = null;
                mButton.Width = mTileSize.Width;
                mButton.Height = mTileSize.Height;
                mButton.Content = mDockPanel;
                mButton.Click += mButton_Click;
            }

            var uiThreadDispatcher = Dispatcher.CurrentDispatcher;

            Console.WriteLine("StartNew {0}",
                mContent.DisplayName);

            Task.Factory.StartNew(new Action(() => {
                var token = mCts.Token;
                if (token.IsCancellationRequested) {
                    return;
                }

                if (token.IsCancellationRequested) {
                    Console.WriteLine("Cancelled 1 {0}", mContent.DisplayName);
                    return;
                }

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = new MemoryStream(mContent.ImageBytes);
                bi.EndInit();
                bi.Freeze();

                if (token.IsCancellationRequested) {
                    Console.WriteLine("Cancelled 2 {0}", mContent.DisplayName);
                    return;
                }

                uiThreadDispatcher.Invoke(new Action(() => {
                    lock (mLock) {
                        if (mImage != null) {
                            mImage.Source = bi;
                        }
                    }
                }));
            }), mAppExitToken, TaskCreationOptions.LongRunning, PriorityScheduler.BelowNormal);

            return mButton;
        }

        void mButton_Click(object sender, RoutedEventArgs e) {
            mAlbumTileClicked(this, mContent);
        }

        void IVirtualTile.DisposeVisual() {
            lock (mLock) {
                if (mCts != null) {
                    mCts.Cancel();
                }

                mButton = null;
                mImage = null;
                mTextBlockAlbumName = null;
                mDockPanel = null;
            }
        }
    }
}
