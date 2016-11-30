using System;
using System.Globalization;
using System.Text;

namespace WWAudioFilter {
    public class TagEditFilter : FilterBase {
        public enum Type {
            Title,
            Album,
            AlbumArtist,
            Artist,
            Genre,
        };

        public Type TagType { get; set; }

        public string Text { get; set; }

        public TagEditFilter(Type type, string text)
                : base(FilterType.TagEdit) {
            TagType = type;
            Text = text;
        }

        public override FilterBase CreateCopy() {
            return new TagEditFilter(TagType, Text);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture,
                    Properties.Resources.FilterTagEdit, TagType, Text);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", TagType, WWAFUtil.EscapeString(Text));
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            Type tagType;
            string text;
            switch (tokens[1]) {
            case "Title":
                tagType = Type.Title;
                text = tokens[2];
                break;
            case "Artist":
                tagType = Type.Artist;
                text = tokens[2];
                break;
            case "Album":
                tagType = Type.Album;
                text = tokens[2];
                break;
            case "AlbumArtist":
                tagType = Type.AlbumArtist;
                text = tokens[2];
                break;
            case "Genre":
                tagType = Type.Genre;
                text = tokens[2];
                break;
            default:
                return null;
            }

            return new TagEditFilter(tagType, text);
        }

        public override TagData TagEdit(TagData tagData) {
            switch (TagType) {
            case Type.Title:
                tagData.Meta.titleStr = Text;
                break;
            case Type.Artist:
                tagData.Meta.artistStr = Text;
                break;
            case Type.Album:
                tagData.Meta.albumStr = Text;
                break;
            case Type.AlbumArtist:
                tagData.Meta.albumArtistStr = Text;
                break;
            case Type.Genre:
                tagData.Meta.genreStr = Text;
                break;
            }
            return tagData;
        }
    }
}
