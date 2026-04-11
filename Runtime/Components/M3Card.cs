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
    public partial class M3Card : M3ComponentBase
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
        // M3 uses two combined shadows (key + ambient) for elevation 1.
        // Painter2D approximates this with a single layered shadow.
        // blur=12 / offset-y=4 / alpha=0.40 gives visible shadow matching M3 reference.
        // ShadowPadding insets the visual rect so the shadow fits within element bounds
        // (Painter2D content is clipped to element layout rect).
        private const float ElevatedShadowBlur    = 12f;
        private const float ElevatedShadowOffsetY = 4f;
        private const float ElevatedShadowPadding = 10f;

        // Resolved theme colors
        private Color _themeOnSurface;
        private Color _themeOutlineVariant;
        private Color _themePrimary;

        // ------------------------------------------------------------------ //
        //  Children                                                             //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement  _root;
        private readonly SDFRectElement  _clipArea;  // clips ripple + content to visual card area
        private readonly RippleElement   _ripple;
        private readonly VisualElement   _content;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                       //
        // ------------------------------------------------------------------ //
        private CardVariant _variant   = CardVariant.Elevated;
        private bool        _clickable;

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
        public new bool Disabled
        {
            get => base.Disabled;
            set => base.Disabled = value;
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

            // --- clip area: constrains ripple + content to visual card bounds ---
            // Elevated variant uses ShadowPadding which expands _root beyond the
            // visible card. This inner container clips children to the actual card area.
            _clipArea = new SDFRectElement
            {
                CornerRadius = CornerRadius,
                pickingMode  = PickingMode.Position,
            };
            _clipArea.style.flexGrow  = 1;
            _clipArea.style.flexDirection = FlexDirection.Column;
            _clipArea.style.overflow  = Overflow.Hidden;
            _clipArea.style.borderTopLeftRadius     = CornerRadius;
            _clipArea.style.borderTopRightRadius    = CornerRadius;
            _clipArea.style.borderBottomLeftRadius  = CornerRadius;
            _clipArea.style.borderBottomRightRadius = CornerRadius;
            _root.Add(_clipArea);

            // --- ripple (only activated when Clickable = true) ---
            _ripple = new RippleElement();
            _ripple.style.display = DisplayStyle.None;
            _clipArea.Add(_ripple);

            // --- content area (contentContainer target) ---
            _content = new VisualElement();
            _content.AddToClassList(ContentClass);
            _clipArea.Add(_content);

            // CRITICAL: use hierarchy.Add to bypass contentContainer override.
            // this.Add(_root) would route _root into _content → infinite recursion.
            hierarchy.Add(_root);

            // Apply default variant
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
                    _root.ShadowColor   = new Color(0f, 0f, 0f, 0.40f);
                    _root.ShadowPadding = ElevatedShadowPadding;
                    _root.OutlineThickness = 0f;
                    // CSS padding gives the element extra space for the shadow.
                    // Negative margin pulls _root back so the visual card aligns
                    // with Filled/Outlined variants (net layout impact = 0).
                    _root.style.paddingLeft   = ElevatedShadowPadding;
                    _root.style.paddingRight  = ElevatedShadowPadding;
                    _root.style.paddingTop    = ElevatedShadowPadding;
                    _root.style.paddingBottom = ElevatedShadowPadding;
                    _root.style.marginLeft    = -ElevatedShadowPadding;
                    _root.style.marginRight   = -ElevatedShadowPadding;
                    _root.style.marginTop     = -ElevatedShadowPadding;
                    _root.style.marginBottom  = -ElevatedShadowPadding;
                    // M3 tonal elevation — primary tint at level-1 opacity (5%).
                    _root.TonalOverlayColor   = _themePrimary;
                    _root.TonalOverlayOpacity = 0.05f;
                    // Transfer USS background-color to FillColorOverride so it only fills
                    // the inset rect (not the shadow padding area). Reset inline style first
                    // so USS class value resolves, then defer the transfer.
                    _root.style.backgroundColor = StyleKeyword.Null;
                    _root.schedule.Execute(TransferBackgroundToFill);
                    break;

                case CardVariant.Filled:
                    _root.ShadowBlur    = 0f;
                    _root.ShadowOffsetY = 0f;
                    _root.ShadowPadding = 0f;
                    _root.OutlineThickness = 0f;
                    _root.TonalOverlayOpacity = 0f;
                    _root.FillColorOverride = null;
                    _root.style.backgroundColor = StyleKeyword.Null;
                    _root.style.paddingLeft   = 0f;
                    _root.style.paddingRight  = 0f;
                    _root.style.paddingTop    = 0f;
                    _root.style.paddingBottom = 0f;
                    _root.style.marginLeft    = 0f;
                    _root.style.marginRight   = 0f;
                    _root.style.marginTop     = 0f;
                    _root.style.marginBottom  = 0f;
                    break;

                case CardVariant.Outlined:
                    _root.ShadowBlur    = 0f;
                    _root.ShadowOffsetY = 0f;
                    _root.ShadowPadding = 0f;
                    _root.OutlineThickness = 1f;
                    _root.OutlineColor = _themeOutlineVariant;
                    _root.TonalOverlayOpacity = 0f;
                    _root.FillColorOverride = null;
                    _root.style.backgroundColor = StyleKeyword.Null;
                    _root.style.paddingLeft   = 0f;
                    _root.style.paddingRight  = 0f;
                    _root.style.paddingTop    = 0f;
                    _root.style.paddingBottom = 0f;
                    _root.style.marginLeft    = 0f;
                    _root.style.marginRight   = 0f;
                    _root.style.marginTop     = 0f;
                    _root.style.marginBottom  = 0f;
                    break;
            }

            // State overlay tint: OnSurface for all card variants
            if (StateLayer != null)
                StateLayer.OverlayColor = _themeOnSurface;
        }

        /// <summary>
        /// Deferred callback: reads the USS-resolved background-color, stores it as
        /// FillColorOverride (drawn on inset rect by Painter2D), then clears the USS
        /// background so it doesn't fill the shadow padding area.
        /// </summary>
        private void TransferBackgroundToFill()
        {
            if (_variant != CardVariant.Elevated) return;
            var bg = _root.resolvedStyle.backgroundColor;
            if (bg.a > 0f)
            {
                _root.FillColorOverride = bg;
                _root.style.backgroundColor = Color.clear;
            }
        }

        // ------------------------------------------------------------------ //
        //  Clickable                                                            //
        // ------------------------------------------------------------------ //

        private void ApplyClickable(bool clickable)
        {
            if (clickable && StateLayer == null)
            {
                _ripple.style.display = DisplayStyle.Flex;
                // Attach to _clipArea (not _root) so hover/press area matches visible card
                InitStateLayer(_clipArea, _ripple);
                StateLayer.OverlayColor = _themeOnSurface;
                _clipArea.RegisterCallback<ClickEvent>(OnRootClicked);
            }
            else if (!clickable && StateLayer != null)
            {
                StateLayer.Detach();
                _clipArea.UnregisterCallback<ClickEvent>(OnRootClicked);
                _ripple.style.display = DisplayStyle.None;
            }
        }

        // ------------------------------------------------------------------ //
        //  Theme-aware color resolution                                         //
        // ------------------------------------------------------------------ //

        protected override void RefreshThemeColors()
        {
            var theme = ThemeManager.ActiveTheme;
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
            if (base.Disabled) return;
            OnClick?.Invoke();
        }
    }
}
