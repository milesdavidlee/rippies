using UnityEngine;

namespace Rippies.Reveal
{
    /// <summary>
    /// Unity mirror of shared/rippies-design-tokens.json. Keep this deliberately
    /// small: Unity owns the reveal surface, while React Native remains the
    /// product-shell source of truth.
    /// </summary>
    public static class ProductDesignLanguage
    {
        public static readonly Color Canvas = Html("#07090F");
        public static readonly Color Surface = Html("#11141E");
        public static readonly Color SurfaceRaised = Html("#191D29");
        public static readonly Color Line = Html("#2A3040");
        public static readonly Color Text = Html("#F7F8FC");
        public static readonly Color TextMuted = Html("#9DA4B5");
        public static readonly Color Cyan = Html("#70E6FF");
        public static readonly Color Success = Html("#77F5B0");

        public const float QuickSeconds = 0.18f;
        public const float StandardSeconds = 0.36f;
        public const float HeroSeconds = 0.62f;

        private static Color Html(string value)
        {
            // The project renders in linear color space. Convert authored sRGB
            // product tokens so the final display values match React Native.
            return ColorUtility.TryParseHtmlString(value, out Color color)
                ? color.linear
                : Color.white;
        }
    }
}
