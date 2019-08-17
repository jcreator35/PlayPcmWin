using System;
using System.Globalization;

namespace WWAudioFilterCore {
    public class OnebitConversionFilter : FilterBase {
        public OnebitConversionFilter()
                : base(FilterType.OnebitConversion) {

            Prepare();
        }

        public override FilterBase CreateCopy() {
            return new OnebitConversionFilter();
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterOnebitConversionDesc);
        }

        public override string ToSaveText() {
            return String.Empty;
        }

        public static FilterBase Restore(string[] tokens) {
            return new OnebitConversionFilter();
        }

        private void Prepare() {

        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length];



            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}
