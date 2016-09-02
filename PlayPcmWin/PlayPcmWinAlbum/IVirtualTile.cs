using System.Windows;

namespace PlayPcmWinAlbum {
    public interface IVirtualTile {
        /// <summary>
        /// Return the current Visual or null if it has not been created yet.
        /// </summary>
        UIElement Visual { get; }

        /// <summary>
        /// Create the WPF visual for this object.
        /// </summary>
        /// <param name="parent">The canvas that is calling this method</param>
        /// <param name="ordinal">item number start from 0</param>
        /// <returns>The visual that can be displayed</returns>
        UIElement CreateVisual(VerticalScrollTilePanel parent);

        /// <summary>
        /// Dispose the WPF visual for this object.
        /// </summary>
        void DisposeVisual();
    }
}
