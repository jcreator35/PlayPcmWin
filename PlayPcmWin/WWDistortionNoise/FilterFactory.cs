
namespace WWDistortionNoise {
    class FilterFactory {
        private FilterFactory() {
        }

        public static FilterBase Create(string s) {
            var tokens = s.Split(null);
            if (tokens == null || tokens.Length < 1) {
                return null;
            }

            // Refer FilterType enum in FilterBase.cs

            switch (tokens[0]) {
            case "Gain":
                return GainFilter.Restore(tokens);
            case "JitterAdd":
                return JitterAddFilter.Restore(tokens);
            default:
                return null;
            }
        }
    }
}
