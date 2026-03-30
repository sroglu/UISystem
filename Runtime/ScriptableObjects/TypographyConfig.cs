using mehmetsrl.UISystem.Data;
using mehmetsrl.UISystem.Enums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace mehmetsrl.UISystem
{
    /// <summary>
    /// ScriptableObject that maps each TextRole to a concrete TextStyle (font, size,
    /// weight, spacing). Assign as the TypographyConfig on ThemeManager, or override
    /// per-component via TypographyResolver._configOverride.
    /// </summary>
    [CreateAssetMenu(menuName = "UISystem/Typography Config", fileName = "TypographyConfig")]
    public class TypographyConfig : ScriptableObject
    {
        [BoxGroup("Scale")]
        [Tooltip("36+ sp — splash screen, large numbers (Regular weight).")]
        [SerializeField] private TextStyle _display;

        [BoxGroup("Scale")]
        [Tooltip("28 sp — section headings (Regular weight).")]
        [SerializeField] private TextStyle _headline;

        [BoxGroup("Scale")]
        [Tooltip("22 sp — card and dialog titles (Medium weight).")]
        [SerializeField] private TextStyle _title;

        [BoxGroup("Scale")]
        [Tooltip("16 sp — general body text (Regular weight).")]
        [SerializeField] private TextStyle _body;

        [BoxGroup("Scale")]
        [Tooltip("14 sp — button labels and captions (Medium weight).")]
        [SerializeField] private TextStyle _label;

        [BoxGroup("Scale")]
        [Tooltip("12 sp — helper text and timestamps (Regular weight).")]
        [SerializeField] private TextStyle _caption;

        // ------------------------------------------------------------------ //
        //  API                                                                 //
        // ------------------------------------------------------------------ //
        /// <summary>Returns the TextStyle for the given role.</summary>
        public TextStyle GetStyle(TextRole role) => role switch
        {
            TextRole.Display  => _display,
            TextRole.Headline => _headline,
            TextRole.Title    => _title,
            TextRole.Body     => _body,
            TextRole.Label    => _label,
            TextRole.Caption  => _caption,
            _                 => _body
        };
    }
}
