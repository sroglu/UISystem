using System;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Card with 3 variants: Elevated, Filled, Outlined.
    ///
    /// Composition:
    ///   SDFRectElement (_root) — rounded container (12dp corners), receives pointer events
    ///   RippleElement  (_ripple) — M3 press ripple, child of _root (only when Clickable)
    ///   VisualElement  (_content) — flex-column content area, child of _root
    ///   StateLayerController (_stateLayer) — hover/press/focused/disabled feedback (only when Clickable)
    ///
    /// contentContainer is overridden to route card.Add(child) → _content.
    /// Constructor uses hierarchy.Add(_root) to avoid infinite recursion.
    ///
    /// USS: card.uss for variant styles. All colors via var(--m3-*) tokens.
    ///
    /// Usage (C#):
    ///   var card = new M3Card { Variant = CardVariant.Elevated, Clickable = true };
    ///   card.Add(new Label("Title"));
    ///   root.Add(card);
    ///
    /// Usage (UXML):
    ///   &lt;mehmetsrl.UISystem.Components.M3Card variant="Elevated" clickable="true"&gt;
    ///     &lt;Label text="Title" /&gt;
    ///   &lt;/mehmetsrl.UISystem.Components.M3Card&gt;
    /// </summary>
    [UxmlElement]
    public partial class M3Card : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                  //
        // ------------------------------------------------------------------ //
        private const string BaseClass    = "m3-card";
        private const string ContentClass = "m3-card__body";

        private static readonly string[] VariantClasses =
        {
            "m3-card--elevated",
            "m3-card--filled",
            "m3-card--outlined",
        };

        // Corner radius — M3 shape-medium (12dp), set in C# because
        // Unity renders border-radius: 9999px as ellipse. 12dp is a fixed
        // value so inline style is sufficient.
        private const float CornerRadius = 12f;

        // Elevation level 1 shadow values (Elevated variant)
        // M3 token: blur 2dp, offset-y 1dp, but at card scale this is invisible.
        // Using blur=8, offset=3, alpha=0.22 — visually matches M3 card elevation 1
        // appearance on mobile (shadow is more prominent than on button).
        private const float ElevatedShadowBlur    = 8f;
        private const float ElevatedShadowOffsetY = 3f;

        // Resolved theme colors
        private Color _themeOnSurface;
        private Color _themeOutlineVariant;
        private Color _themePrimary;

        // ------------------------------------------------------------------ //
        //  Children                                                             //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement  _root;
        private readonly RippleElement   _ripple;
        private readonly VisualElement   _content;
        private          StateLayerController _stateLayer;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                       //
        // ------------------------------------------------------------------ //
        private CardVariant _variant   = CardVariant.Elevated;
        private bool        _clickable;
        private bool        _disabled;

        // ------------------------------------------------------------------ //
        //  contentContainer override                                            //
        // ------------------------------------------------------------------ //
        /// <summary>
        /// Routes card.Add(child) into the inner _content area.
        /// IMPORTANT: constructor must use hierarchy.Add(_root), not this.Add(_root),
        /// to avoid routing _root through this override.
        /// </summary>
        public override VisualElement contentContainer => _content;

        // ------------------------------------------------------------------ //
        //  Public API                                                           //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when the card is clicked and Clickable + not Disabled.</summary>
        public event Action OnClick;

        /// <summary>Visual variant — Elevated, Filled, or Outlined.</summary>
        [UxmlAttribute("variant")]
        public CardVariant Variant
        {
            get => _variant;
            set { _variant = value; ApplyVariant(_variant); }
        }

        /// <summary>
        /// When true, attaches StateLayerController for hover/press/focus feedback
        /// and fires OnClick on pointer up.
        /// </summary>
        [UxmlAttribute("clickable")]
        public bool Clickable
        {
            get => _clickable;
            set { _clickable = value; ApplyClickable(_clickable); }
        }

        /// <summary>
        /// When true, dims the card and ignores all input.
        /// Only effective when Clickable is also true.
        /// </summary>
        [UxmlAttribute("disabled")]
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                if (_stateLayer != null)
                    _stateLayer.Disabled = value;
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                          //
        // ------------------------------------------------------------------ //

        public M3Card()
        {
            // This wrapper sizes itself to its content
            style.alignSelf = Align.FlexStart;

            // --- root container ---
            _root = new SDFRectElement
            {
                CornerRadius = CornerRadius,
                pickingMode  = PickingMode.Position,
            };
            _root.AddToClassList(BaseClass);

            // Set CSS border-radius explicitly (12dp, same as CornerRadius)
            // Unity renders border-radius: 9999px as ellipse; explicit px value is safe for 12dp.
            _root.style.borderTopLeftRadius     = CornerRadius;
            _root.style.borderTopRightRadius    = CornerRadius;
            _root.style.borderBottomLeftRadius  = CornerRadius;
            _root.style.borderBottomRightRadius = CornerRadius;

            // --- ripple (only activated when Clickable = true) ---
            _ripple = new RippleElement();
            _ripple.style.display = DisplayStyle.None;
            _root.Add(_ripple);

            // --- content area (contentContainer target) ---
            _content = new VisualElement();
            _content.AddToClassList(ContentClass);
            _root.Add(_content);

            // CRITICAL: use hierarchy.Add to bypass contentContainer override.
            // this.Add(_root) would route _root into _content → infinite recursion.
            hierarchy.Add(_root);

            RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

            // Apply default variant
            RefreshThemeColors();
            ApplyVariant(CardVariant.Elevated);
        }

        // ------------------------------------------------------------------ //
        //  Variant                                                              //
        // ------------------------------------------------------------------ //

        private void ApplyVariant(CardVariant v)
        {
            foreach (var cls in VariantClasses)
                _root.RemoveFromClassList(cls);

            int idx = (int)v;
            if (idx >= 0 && idx < VariantClasses.Length)
                _root.AddToClassList(VariantClasses[idx]);

            switch (v)
            {
                case CardVariant.Elevated:
                    _root.ShadowBlur    = ElevatedShadowBlur;
                    _root.ShadowOffsetY = ElevatedShadowOffsetY;
                    _root.ShadowColor   = new Color(0f, 0f, 0f, 0.22f);
                    _root.OutlineThickness = 0f;
                    // M3 tonal elevation — primary tint at level-1 opacity (5%).
                    // Makes Elevated cards visible in dark mode where surface-container-low
                    // is only 2 units above background.
                    // Primary color is hardcoded to M3 baseline dark primary (#D0BCFF).
                    // A future improvement could read from ThemeManager for per-theme accuracy.
                    _root.TonalOverlayColor   = _themePrimary;
                    _root.TonalOverlayOpacity = 0.05f;
                    break;

                case CardVariant.Filled:
                    _root.ShadowBlur    = 0f;
                    _root.ShadowOffsetY = 0f;
                    _root.OutlineThickness = 0f;
                    _root.TonalOverlayOpacity = 0f;
                    break;

                case CardVariant.Outlined:
                    _root.ShadowBlur    = 0f;
                    _root.ShadowOffsetY = 0f;
                    _root.OutlineThickness = 1f;
                    _root.OutlineColor = _themeOutlineVariant;
                    _root.TonalOverlayOpacity = 0f;
                    break;
            }

            // State overlay tint: OnSurface for all card variants
            if (_stateLayer != null)
                _stateLayer.OverlayColor = _themeOnSurface;
        }

        // ------------------------------------------------------------------ //
        //  Clickable                                                            //
        // ------------------------------------------------------------------ //

        private void ApplyClickable(bool clickable)
        {
            if (clickable && _stateLayer == null)
            {
                _ripple.style.display = DisplayStyle.Flex;
                _stateLayer = new StateLayerController(_root, _ripple);
                _stateLayer.OverlayColor = _themeOnSurface;
                _stateLayer.Attach();
                _root.RegisterCallback<ClickEvent>(OnRootClicked);
            }
            else if (!clickable && _stateLayer != null)
            {
                _stateLayer.Detach();
                _stateLayer = null;
                _ripple.style.display = DisplayStyle.None;
                _root.UnregisterCallback<ClickEvent>(OnRootClicked);
            }
        }

        // ------------------------------------------------------------------ //
        //  Theme-aware color resolution                                         //
        // ------------------------------------------------------------------ //

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

            var tm = ThemeManager.Instance;
            if (tm != null)
                tm.OnThemeChanged += _ => RefreshThemeColors();
            RefreshThemeColors();
        }

        private void RefreshThemeColors()
        {
            var theme = ThemeManager.Instance?.ActiveTheme;
            if (theme == null) return;

            _themeOnSurface      = theme.GetColor(ColorRole.OnSurface);
            _themeOutlineVariant = theme.GetColor(ColorRole.OutlineVariant);
            _themePrimary        = theme.GetColor(ColorRole.Primary);

            ApplyVariant(_variant);
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                       //
        // ------------------------------------------------------------------ //

        private void OnRootClicked(ClickEvent evt)
        {
            if (_disabled) return;
            OnClick?.Invoke();
        }
    }
}
