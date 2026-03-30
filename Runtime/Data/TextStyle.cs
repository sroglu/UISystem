using System;
using TMPro;

namespace mehmetsrl.UISystem.Data
{
    /// <summary>
    /// Defines the visual style for one typography role (font, size, weight, spacing).
    /// </summary>
    [Serializable]
    public struct TextStyle
    {
        /// <summary>TMP font asset. Use the weight-appropriate asset (Regular or Medium).</summary>
        public TMP_FontAsset FontAsset;

        /// <summary>Font size in reference-resolution pixels (1080x1920).</summary>
        public float FontSize;

        /// <summary>TMP font style (Normal, Bold, Italic, etc.).</summary>
        public FontStyles FontStyle;

        /// <summary>TMP line spacing in TMP units.</summary>
        public float LineSpacing;

        /// <summary>TMP character spacing in TMP units.</summary>
        public float CharSpacing;
    }
}
