using System;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Chip component.
    ///
    /// Variants:
    ///   Assist     — icon + label, no toggle
    ///   Filter     — toggleable selected state, leading check icon when selected
    ///   Input      — label + trailing X (remove) button
    ///   Suggestion — label only, no toggle
    ///
    /// M3 Spec:
    ///   Height: 32dp, corners: 8dp (shape-small)
    ///   Border: 1dp outline
    ///   Horizontal padding: 12dp (16dp with leading/trailing icons)
    ///
    /// USS: chip.uss
    /// </summary>
    [UxmlElement]
    public partial class M3Chip : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass          = "m3-chip";
        private const string ContainerClass     = "m3-chip__container";
        private const string LeadingIconClass   = "m3-chip__leading-icon";
        private const string LabelClass         = "m3-chip__label";
        private const string TrailingIconClass  = "m3-chip__trailing-icon";
        private const string SelectedClass      = "m3-chip--selected";
        private const string UnselectedClass    = "m3-chip--unselected";

        // Resolved theme colors (read from ThemeData via ThemeManager)
        private Color _themeOutline;
        private Color _themeOnSurface;
        private Color _themeOnSurfaceVariant;
        private Color _themeOnSecondaryContainer;
        private Color _themeSurface;
        private Color _themeSecondaryContainer;

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement       _container;
        private readonly VisualElement        _leadingIcon;
        private readonly Label                _label;
        private readonly VisualElement        _trailingIcon;
        private readonly RippleElement        _ripple;
        private readonly StateLayerController _stateLayer;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private string      _text;
        private ChipVariant _variant;
        private bool        _selected;
        private bool        _disabled;
        private ChipIcon    _icon;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when the chip is clicked (all variants).</summary>
        public event Action OnClick;

        /// <summary>Fired when the trailing X is pressed (Input variant only).</summary>
        public event Action OnRemove;

        /// <summary>Chip label text.</summary>
        [UxmlAttribute("text")]
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _label.text = value ?? string.Empty;
            }
        }

        /// <summary>Chip variant (Assist, Filter, Input, Suggestion).</summary>
        [UxmlAttribute("variant")]
        public ChipVariant Variant
        {
            get => _variant;
            set
            {
                _variant = value;
                ApplyVariant();
            }
        }

        /// <summary>Selected state (Filter variant only).</summary>
        [UxmlAttribute("selected")]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_variant != ChipVariant.Filter) return;
                _selected = value;
                ApplySelectedState();
            }
        }

        /// <summary>When true, dims the chip and ignores input.</summary>
        [UxmlAttribute("disabled")]
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                _stateLayer.Disabled = value;
                EnableInClassList("m3-disabled", value);
            }
        }

        /// <summary>Leading icon for Assist variant. Ignored for other variants.</summary>
        [UxmlAttribute("icon")]
        public ChipIcon Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                ApplyVariant();
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3Chip()
        {
            AddToClassList(BaseClass);
            pickingMode = PickingMode.Position;

            // --- Container (the visible chip body) ---
            _container = new SDFRectElement
            {
                CornerRadius = 8f,
                pickingMode  = PickingMode.Position,
            };
            _container.AddToClassList(ContainerClass);
            _container.style.borderTopLeftRadius     = 8f;
            _container.style.borderTopRightRadius    = 8f;
            _container.style.borderBottomLeftRadius  = 8f;
            _container.style.borderBottomRightRadius = 8f;
            _container.style.flexDirection = FlexDirection.Row;
            _container.style.alignItems    = Align.Center;
            _container.style.height        = 32f;
            _container.style.paddingLeft   = 12f;
            _container.style.paddingRight  = 12f;

            // --- Ripple ---
            _ripple = new RippleElement();
            _container.Add(_ripple);

            // --- Leading icon (checkmark via Painter2D for selected filter chip) ---
            _leadingIcon = new VisualElement();
            _leadingIcon.AddToClassList(LeadingIconClass);
            _leadingIcon.style.width       = 18f;
            _leadingIcon.style.height      = 18f;
            _leadingIcon.style.display     = DisplayStyle.None;
            _leadingIcon.pickingMode       = PickingMode.Ignore;
            _leadingIcon.generateVisualContent += OnDrawLeadingIcon;
            _container.Add(_leadingIcon);

            // --- Label ---
            _label = new Label(string.Empty);
            _label.AddToClassList("m3-label");
            _label.AddToClassList(LabelClass);
            _label.pickingMode = PickingMode.Ignore;
            _container.Add(_label);

            // --- Trailing icon (close X via Painter2D for input chip) ---
            _trailingIcon = new VisualElement();
            _trailingIcon.AddToClassList(TrailingIconClass);
            _trailingIcon.style.width      = 18f;
            _trailingIcon.style.height     = 18f;
            _trailingIcon.style.display    = DisplayStyle.None;
            _trailingIcon.pickingMode      = PickingMode.Position;
            _trailingIcon.generateVisualContent += OnDrawTrailingIcon;
            _container.Add(_trailingIcon);

            // --- State layer ---
            _stateLayer = new StateLayerController(_container, _ripple);
            _stateLayer.Attach();

            // --- Events ---
            _container.RegisterCallback<ClickEvent>(OnContainerClicked);
            _trailingIcon.RegisterCallback<ClickEvent>(OnTrailingIconClicked);

            // Subscribe to theme + initial read after first layout (ThemeManager not ready in ctor)
            RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

            Add(_container);
            RefreshThemeColors();
            ApplyVariant();
        }

        // ------------------------------------------------------------------ //
        //  Visual State                                                        //
        // ------------------------------------------------------------------ //

        private void ApplyVariant()
        {
            // Show/hide trailing X
            _trailingIcon.style.display = _variant == ChipVariant.Input
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // Update padding based on whether we have trailing icon
            _container.style.paddingRight = _variant == ChipVariant.Input ? 8f : 12f;

            // Leading icon: show for Assist (if icon set) or selected Filter
            bool showLeading = (_variant == ChipVariant.Assist && _icon != ChipIcon.None)
                            || (_variant == ChipVariant.Filter && _selected);
            _leadingIcon.style.display = showLeading ? DisplayStyle.Flex : DisplayStyle.None;
            _container.style.paddingLeft = showLeading ? 8f : 12f;

            if (showLeading)
                _leadingIcon.MarkDirtyRepaint();

            // Reset selected state when not filter
            if (_variant != ChipVariant.Filter)
                _selected = false;

            ApplySelectedState();
            ApplyColors();
        }

        private void ApplySelectedState()
        {
            _container.RemoveFromClassList(SelectedClass);
            _container.RemoveFromClassList(UnselectedClass);

            if (_variant == ChipVariant.Filter && _selected)
            {
                _container.AddToClassList(SelectedClass);
                _leadingIcon.style.display = DisplayStyle.Flex;
                _leadingIcon.MarkDirtyRepaint();
                _container.style.paddingLeft = 8f;
                _container.OutlineThickness = 0f;
                _container.FillColorOverride = _themeSecondaryContainer;
                _label.style.color = new StyleColor(_themeOnSecondaryContainer);
            }
            else
            {
                _container.AddToClassList(UnselectedClass);
                bool hasAssistIcon = _variant == ChipVariant.Assist && _icon != ChipIcon.None;
                _leadingIcon.style.display = hasAssistIcon ? DisplayStyle.Flex : DisplayStyle.None;
                _container.style.paddingLeft = hasAssistIcon ? 8f : 12f;
                _container.OutlineThickness = 1f;
                _container.OutlineColor = _themeOutline;
                _container.FillColorOverride = _themeSurface;
                _label.style.color = new StyleColor(_themeOnSurfaceVariant);
            }
        }

        private void ApplyColors()
        {
            _stateLayer.OverlayColor = _themeOnSurface;
            if (!(_variant == ChipVariant.Filter && _selected))
                _container.OutlineColor = _themeOutline;
            // Background + label colors handled by USS via CSS variables
            _container.MarkDirtyRepaint();
            _leadingIcon.MarkDirtyRepaint();
            _trailingIcon.MarkDirtyRepaint();
        }

        // ------------------------------------------------------------------ //
        //  Theme-aware color resolution                                        //
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

            _themeOutline              = theme.GetColor(ColorRole.Outline);
            _themeOnSurface            = theme.GetColor(ColorRole.OnSurface);
            _themeOnSurfaceVariant     = theme.GetColor(ColorRole.OnSurfaceVariant);
            _themeOnSecondaryContainer = theme.GetColor(ColorRole.OnSecondaryContainer);
            _themeSurface              = theme.GetColor(ColorRole.Surface);
            _themeSecondaryContainer   = theme.GetColor(ColorRole.SecondaryContainer);

            ApplySelectedState();
            ApplyColors();
        }

        // ------------------------------------------------------------------ //
        //  Painter2D icon drawing                                              //
        // ------------------------------------------------------------------ //

        private void OnDrawLeadingIcon(MeshGenerationContext ctx)
        {
            float w = _leadingIcon.layout.width;
            float h = _leadingIcon.layout.height;
            if (w < 1f || h < 1f) return;

            var p = ctx.painter2D;
            // Selected filter → checkmark in on-secondary-container color
            // Assist → custom icon in on-surface-variant color
            Color iconColor = (_variant == ChipVariant.Filter && _selected)
                ? _themeOnSecondaryContainer
                : _themeOnSurfaceVariant;
            p.strokeColor = iconColor;
            float strokeW = Mathf.Max(1.5f, Mathf.Min(w, h) * 0.12f);
            p.lineWidth   = strokeW;
            p.lineCap     = LineCap.Round;
            p.lineJoin    = LineJoin.Round;

            if (_variant == ChipVariant.Filter && _selected)
                DrawCheckmark(p, w, h);
            else
                DrawIcon(p, w, h, iconColor);
        }

        private void DrawCheckmark(Painter2D p, float w, float h)
        {
            float s  = Mathf.Min(w, h) * 0.65f;
            float ox = (w - s) / 2f;
            float oy = (h - s) / 2f;
            p.BeginPath();
            p.MoveTo(new Vector2(ox + s * 0.10f, oy + s * 0.55f));
            p.LineTo(new Vector2(ox + s * 0.38f, oy + s * 0.82f));
            p.LineTo(new Vector2(ox + s * 0.90f, oy + s * 0.18f));
            p.Stroke();
        }

        private void DrawIcon(Painter2D p, float w, float h, Color fill)
        {
            float s = Mathf.Min(w, h);
            float cx = w / 2f, cy = h / 2f;

            switch (_icon)
            {
                case ChipIcon.Calendar:
                    // Calendar: rounded rect body + 2 top hooks + horizontal line
                    float bw = s * 0.7f, bh = s * 0.6f;
                    float bx = cx - bw / 2f, by = cy - bh / 2f + s * 0.08f;
                    float r = s * 0.08f;
                    p.BeginPath();
                    // Body with rounded corners
                    p.MoveTo(new Vector2(bx + r, by));
                    p.LineTo(new Vector2(bx + bw - r, by));
                    p.ArcTo(new Vector2(bx + bw, by), new Vector2(bx + bw, by + r), r);
                    p.LineTo(new Vector2(bx + bw, by + bh - r));
                    p.ArcTo(new Vector2(bx + bw, by + bh), new Vector2(bx + bw - r, by + bh), r);
                    p.LineTo(new Vector2(bx + r, by + bh));
                    p.ArcTo(new Vector2(bx, by + bh), new Vector2(bx, by + bh - r), r);
                    p.LineTo(new Vector2(bx, by + r));
                    p.ArcTo(new Vector2(bx, by), new Vector2(bx + r, by), r);
                    p.ClosePath();
                    p.Stroke();
                    // Horizontal divider (header line)
                    float ly = by + bh * 0.32f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(bx, ly));
                    p.LineTo(new Vector2(bx + bw, ly));
                    p.Stroke();
                    // Two hooks
                    float hookY = by - s * 0.1f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(bx + bw * 0.3f, hookY));
                    p.LineTo(new Vector2(bx + bw * 0.3f, by + bh * 0.1f));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(new Vector2(bx + bw * 0.7f, hookY));
                    p.LineTo(new Vector2(bx + bw * 0.7f, by + bh * 0.1f));
                    p.Stroke();
                    break;

                case ChipIcon.Search:
                    // Magnifying glass: circle + diagonal line
                    float sr = s * 0.25f;
                    float scx = cx - s * 0.06f, scy = cy - s * 0.06f;
                    p.BeginPath();
                    p.Arc(new Vector2(scx, scy), sr, 0f, 360f);
                    p.Stroke();
                    p.BeginPath();
                    float dx = sr * 0.7f;
                    p.MoveTo(new Vector2(scx + dx, scy + dx));
                    p.LineTo(new Vector2(scx + dx + s * 0.2f, scy + dx + s * 0.2f));
                    p.Stroke();
                    break;

                case ChipIcon.Add:
                    // Plus: horizontal + vertical lines
                    float half = s * 0.28f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx - half, cy));
                    p.LineTo(new Vector2(cx + half, cy));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx, cy - half));
                    p.LineTo(new Vector2(cx, cy + half));
                    p.Stroke();
                    break;

                case ChipIcon.Star:
                    // 5-point star outline
                    p.BeginPath();
                    float starR = s * 0.35f;
                    float innerR = starR * 0.4f;
                    for (int i = 0; i < 10; i++)
                    {
                        float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rad = (i % 2 == 0) ? starR : innerR;
                        var pt = new Vector2(cx + Mathf.Cos(angle) * rad, cy - Mathf.Sin(angle) * rad);
                        if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                    }
                    p.ClosePath();
                    p.Stroke();
                    break;

                case ChipIcon.Person:
                    // Head circle + body arc
                    float headR = s * 0.16f;
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy - s * 0.12f), headR, 0f, 360f);
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy + s * 0.55f), s * 0.3f, 200f, 340f);
                    p.Stroke();
                    break;

                case ChipIcon.Location:
                    // Pin: teardrop shape
                    float pinR = s * 0.22f;
                    float pinCy = cy - s * 0.08f;
                    p.BeginPath();
                    p.Arc(new Vector2(cx, pinCy), pinR, 150f, 390f);
                    p.LineTo(new Vector2(cx, cy + s * 0.35f));
                    p.ClosePath();
                    p.Stroke();
                    // Inner dot
                    p.BeginPath();
                    p.Arc(new Vector2(cx, pinCy), pinR * 0.35f, 0f, 360f);
                    p.fillColor = fill;
                    p.Fill();
                    break;
            }
        }

        private void OnDrawTrailingIcon(MeshGenerationContext ctx)
        {
            float w = _trailingIcon.layout.width;
            float h = _trailingIcon.layout.height;
            if (w < 1f || h < 1f) return;

            var p = ctx.painter2D;
            p.strokeColor = _themeOnSurfaceVariant;
            float strokeW = Mathf.Max(1.5f, Mathf.Min(w, h) * 0.10f);
            p.lineWidth   = strokeW;
            p.lineCap     = LineCap.Round;

            // X (close) path — two diagonal lines
            float pad = w * 0.22f;
            p.BeginPath();
            p.MoveTo(new Vector2(pad, pad));
            p.LineTo(new Vector2(w - pad, h - pad));
            p.Stroke();
            p.BeginPath();
            p.MoveTo(new Vector2(w - pad, pad));
            p.LineTo(new Vector2(pad, h - pad));
            p.Stroke();
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                      //
        // ------------------------------------------------------------------ //

        private void OnContainerClicked(ClickEvent evt)
        {
            if (_disabled) return;

            if (_variant == ChipVariant.Filter)
                Selected = !_selected;

            OnClick?.Invoke();
        }

        private void OnTrailingIconClicked(ClickEvent evt)
        {
            if (_disabled) return;
            evt.StopPropagation();
            OnRemove?.Invoke();
        }
    }
}
