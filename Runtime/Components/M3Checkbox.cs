using System;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Checkbox component.
    ///
    /// Composition:
    ///   VisualElement (this)  — 48dp touch target wrapper
    ///   SDFRectElement (_box) — 18×18dp box, 2dp corners
    ///   Label (_icon)         — Material Symbols icon (check / dash)
    ///   RippleElement (_ripple)
    ///   StateLayerController (_stateLayer)
    ///
    /// M3 Checkbox spec:
    ///   Box: 18×18dp, 2dp border-radius
    ///   Unchecked: 2dp outline border, no fill
    ///   Checked: primary fill, on-primary checkmark icon
    ///   Indeterminate: primary fill, on-primary dash icon
    ///   Disabled: 0.38 opacity
    ///
    /// USS: checkbox.uss
    ///
    /// Usage (UXML):
    ///   &lt;components:M3Checkbox state="Checked" /&gt;
    /// </summary>
    [UxmlElement]
    public partial class M3Checkbox : M3ComponentBase
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass          = "m3-checkbox";
        private const string BoxClass           = "m3-checkbox__box";
        private const string IconClass          = "m3-checkbox__icon";
        private const string UncheckedClass     = "m3-checkbox--unchecked";
        private const string CheckedClass       = "m3-checkbox--checked";
        private const string IndeterminateClass = "m3-checkbox--indeterminate";

        // Resolved theme colors (read from ThemeData via ThemeManager)
        private Color _themeOutline;
        private Color _themeOnSurface;

        // ------------------------------------------------------------------ //
        //  Dimensions (M3 spec)                                                //
        // ------------------------------------------------------------------ //
        private const float BoxSize    = 18f;
        private const float BoxRadius  = 2f;  // shape-extra-small
        private const float BoxBorder  = 2f;

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement       _box;
        private readonly VisualElement        _icon;
        private readonly RippleElement        _ripple;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private CheckboxState _state;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when state changes.</summary>
        public event Action<CheckboxState> OnStateChanged;

        /// <summary>Current checkbox state.</summary>
        [UxmlAttribute("state")]
        public CheckboxState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                ApplyVisualState();
                OnStateChanged?.Invoke(_state);
            }
        }

        /// <summary>When true, dims the checkbox and ignores input.</summary>
        [UxmlAttribute("disabled")]
        public new bool Disabled
        {
            get => base.Disabled;
            set => base.Disabled = value;
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3Checkbox()
        {
            AddToClassList(BaseClass);
            style.alignSelf    = Align.FlexStart;
            style.justifyContent = Justify.Center;
            style.alignItems   = Align.Center;
            pickingMode        = PickingMode.Position;
            focusable          = true;

            // --- Box ---
            _box = new SDFRectElement
            {
                CornerRadius = BoxRadius,
                pickingMode  = PickingMode.Position,
            };
            _box.AddToClassList(BoxClass);
            _box.style.width  = BoxSize;
            _box.style.height = BoxSize;
            _box.style.borderTopLeftRadius     = BoxRadius;
            _box.style.borderTopRightRadius    = BoxRadius;
            _box.style.borderBottomLeftRadius  = BoxRadius;
            _box.style.borderBottomRightRadius = BoxRadius;
            _box.style.justifyContent = Justify.Center;
            _box.style.alignItems     = Align.Center;

            // --- Ripple ---
            _ripple = new RippleElement();
            _box.Add(_ripple);

            // --- Icon (check / dash) drawn via Painter2D ---
            _icon = new VisualElement();
            _icon.AddToClassList(IconClass);
            _icon.style.position = Position.Absolute;
            _icon.style.left     = 0;
            _icon.style.right    = 0;
            _icon.style.top      = 0;
            _icon.style.bottom   = 0;
            _icon.pickingMode    = PickingMode.Ignore;
            _icon.style.opacity  = 0f;
            _icon.generateVisualContent += OnDrawIcon;
            _box.Add(_icon);

            // --- State layer ---
            InitStateLayer(_box, _ripple);

            // --- Events ---
            _box.RegisterCallback<ClickEvent>(OnBoxClicked);

            Add(_box);
            ApplyVisualState();
        }

        // ------------------------------------------------------------------ //
        //  Visual State                                                        //
        // ------------------------------------------------------------------ //

        private void ApplyVisualState()
        {
            // Reset classes
            _box.RemoveFromClassList(UncheckedClass);
            _box.RemoveFromClassList(CheckedClass);
            _box.RemoveFromClassList(IndeterminateClass);

            switch (_state)
            {
                case CheckboxState.Unchecked:
                    _box.AddToClassList(UncheckedClass);
                    _box.OutlineThickness = BoxBorder;
                    _box.OutlineColor = _themeOutline;
                    _icon.style.opacity = 0f;
                    break;

                case CheckboxState.Checked:
                    _box.AddToClassList(CheckedClass);
                    _box.OutlineThickness = 0f;
                    _icon.style.opacity = 1f;
                    _icon.MarkDirtyRepaint();
                    break;

                case CheckboxState.Indeterminate:
                    _box.AddToClassList(IndeterminateClass);
                    _box.OutlineThickness = 0f;
                    _icon.style.opacity = 1f;
                    _icon.MarkDirtyRepaint();
                    break;
            }

            StateLayer.OverlayColor = _themeOnSurface;
        }

        protected override void RefreshThemeColors()
        {
            var theme = ThemeManager.ActiveTheme;
            if (theme == null) return;

            _themeOutline   = theme.GetColor(Enums.ColorRole.Outline);
            _themeOnSurface = theme.GetColor(Enums.ColorRole.OnSurface);

            ApplyVisualState();
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                      //
        // ------------------------------------------------------------------ //

        private void OnDrawIcon(MeshGenerationContext ctx)
        {
            float w = _icon.layout.width;
            float h = _icon.layout.height;
            if (w < 1f || h < 1f) return;

            var p = ctx.painter2D;
            Color iconColor = new Color(1f, 1f, 1f); // on-primary (white)
            float strokeW = Mathf.Max(2f, Mathf.Min(w, h) * 0.15f);
            p.strokeColor = iconColor;
            p.lineWidth   = strokeW;
            p.lineCap     = LineCap.Round;
            p.lineJoin    = LineJoin.Round;

            if (_state == CheckboxState.Checked)
            {
                // Checkmark path
                float s  = Mathf.Min(w, h) * 0.65f;
                float ox = (w - s) / 2f;
                float oy = (h - s) / 2f;
                p.BeginPath();
                p.MoveTo(new Vector2(ox + s * 0.10f, oy + s * 0.55f));
                p.LineTo(new Vector2(ox + s * 0.38f, oy + s * 0.82f));
                p.LineTo(new Vector2(ox + s * 0.90f, oy + s * 0.18f));
                p.Stroke();
            }
            else if (_state == CheckboxState.Indeterminate)
            {
                // Horizontal dash
                float pad = w * 0.2f;
                float cy  = h / 2f;
                p.BeginPath();
                p.MoveTo(new Vector2(pad, cy));
                p.LineTo(new Vector2(w - pad, cy));
                p.Stroke();
            }
        }

        private void OnBoxClicked(ClickEvent evt)
        {
            if (base.Disabled) return;
            // Unchecked <-> Checked toggle (Indeterminate set programmatically only)
            State = _state == CheckboxState.Checked ? CheckboxState.Unchecked : CheckboxState.Checked;
        }
    }
}
