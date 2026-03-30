using mehmetsrl.UISystem;
using mehmetsrl.UISystem.Data;
using mehmetsrl.UISystem.Enums;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Companion component for TMP_Text. Assigns a TextRole and automatically fetches
    /// the matching TextStyle from the active TypographyConfig, applying it to the
    /// sibling TMP_Text component. Re-applies when ThemeManager fires OnThemeChanged.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypographyResolver : MonoBehaviour, IThemeSubscriber
    {
        [Tooltip("Semantic typography role to resolve.")]
        [SerializeField] private TextRole _role = TextRole.Body;

        [Tooltip("Optional per-component config override. When null, TypographyConfig from ThemeManager is used.")]
        [SerializeField] private TypographyConfig _configOverride;

        private TMP_Text _tmp;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //
        /// <summary>Assigns a new role and immediately re-applies the style.</summary>
        public TextRole Role
        {
            get => _role;
            set { _role = value; ApplyStyle(); }
        }

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                           //
        // ------------------------------------------------------------------ //
        private void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            if (ThemeManager.Instance != null)
                ThemeManager.Instance.OnThemeChanged += OnThemeChanged;

            ApplyStyle();
        }

        private void OnDestroy()
        {
            if (ThemeManager.Instance != null)
                ThemeManager.Instance.OnThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(ThemeData theme) => OnThemeApplied(theme);

        /// <inheritdoc/>
        public void OnThemeApplied(ThemeData theme) => ApplyStyle();

        // ------------------------------------------------------------------ //
        //  Core                                                                //
        // ------------------------------------------------------------------ //
        private void ApplyStyle()
        {
            if (_tmp == null)
                _tmp = GetComponent<TMP_Text>();

            var config = ResolveConfig();
            if (config == null)
            {
                Debug.LogWarning("[UISystem] TypographyResolver: no TypographyConfig found. " +
                                 "Assign one to ThemeManager or use the local configOverride field.", this);
                return;
            }

            TextStyle s = config.GetStyle(_role);

            if (s.FontAsset != null)
                _tmp.font = s.FontAsset;

            _tmp.fontSize        = s.FontSize;
            _tmp.fontStyle       = s.FontStyle;
            _tmp.lineSpacing     = s.LineSpacing;
            _tmp.characterSpacing = s.CharSpacing;
        }

        private TypographyConfig ResolveConfig()
        {
            if (_configOverride != null) return _configOverride;
            return ThemeManager.Instance != null ? ThemeManager.Instance.TypographyConfig : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tmp == null)
                _tmp = GetComponent<TMP_Text>();
            if (_tmp != null && Application.isPlaying)
                ApplyStyle();
        }
#endif
    }
}
