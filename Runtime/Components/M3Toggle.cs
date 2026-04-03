using System;
using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Toggle (Switch) component.
    ///
    /// Composition:
    ///   VisualElement (this)  — touch target wrapper (48dp min)
    ///   SDFRectElement (_track) — pill-shaped track (52×32dp, 16dp corners)
    ///   SDFRectElement (_thumb) — circular handle, animates position and size
    ///   RippleElement (_ripple) — press ripple on thumb area
    ///   StateLayerController (_stateLayer) — hover/press/focus feedback on thumb
    ///
    /// M3 Switch spec:
    ///   Track: 52×32dp, pill (border-radius: 16dp)
    ///   Thumb: 16dp (off) → 24dp (on), 28dp (pressed)
    ///   Track unselected: surface-container-highest + 2dp outline
    ///   Track selected: primary, no outline
    ///   Thumb unselected: outline color
    ///   Thumb selected: on-primary
    ///
    /// USS: toggle.uss. All colors via var(--m3-*) tokens.
    ///
    /// Usage (C#):
    ///   var toggle = new M3Toggle { Value = true };
    ///   toggle.OnValueChanged += val => Debug.Log(val);
    ///
    /// Usage (UXML):
    ///   &lt;components:M3Toggle value="true" /&gt;
    /// </summary>
    [UxmlElement]
    public partial class M3Toggle : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass      = "m3-toggle";
        private const string TrackClass     = "m3-toggle__track";
        private const string ThumbClass     = "m3-toggle__thumb";
        private const string CheckedClass   = "m3-toggle--checked";
        private const string UncheckedClass = "m3-toggle--unchecked";

        // Resolved theme colors (read from ThemeData via ThemeManager)
        private Color _themeOutline;
        private Color _themeOnSurface;
        private Color _themePrimary;

        // ------------------------------------------------------------------ //
        //  Dimensions (M3 spec)                                                //
        // ------------------------------------------------------------------ //
        private const float TrackWidth      = 52f;
        private const float TrackHeight     = 32f;
        private const float TrackRadius     = 16f; // pill shape
        private const float ThumbSizeOff    = 16f;
        private const float ThumbSizeOn     = 24f;
        private const float ThumbSizePress  = 28f;
        private const float TrackOutline    = 2f;  // unselected track border

        // Thumb center positions (from left edge of track)
        // Unselected: track padding (TrackHeight/2) from left
        // Selected:   TrackWidth - TrackHeight/2 from left
        private const float ThumbOffX = TrackHeight / 2f;           // 16dp
        private const float ThumbOnX  = TrackWidth - TrackHeight / 2f; // 36dp

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement  _track;
        private readonly SDFRectElement  _thumb;
        private readonly VisualElement   _checkIcon;
        private readonly RippleElement   _ripple;
        private readonly StateLayerController _stateLayer;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private bool _value;
        private bool _disabled;
        private bool _pressed;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when value changes. Arg is the new value.</summary>
        public event Action<bool> OnValueChanged;

        /// <summary>Current on/off state.</summary>
        [UxmlAttribute("value")]
        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                ApplyVisualState();
                OnValueChanged?.Invoke(_value);
            }
        }

        /// <summary>When true, dims the toggle and ignores input.</summary>
        [UxmlAttribute("disabled")]
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                _stateLayer.Disabled = value;
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3Toggle()
        {
            // Touch target wrapper
            AddToClassList(BaseClass);
            style.alignSelf = Align.FlexStart;
            pickingMode = PickingMode.Position;
            focusable = true;

            // --- Track ---
            _track = new SDFRectElement
            {
                CornerRadius = TrackRadius,
                pickingMode  = PickingMode.Position,
            };
            _track.AddToClassList(TrackClass);
            _track.style.width  = TrackWidth;
            _track.style.height = TrackHeight;
            _track.style.borderTopLeftRadius     = TrackRadius;
            _track.style.borderTopRightRadius    = TrackRadius;
            _track.style.borderBottomLeftRadius  = TrackRadius;
            _track.style.borderBottomRightRadius = TrackRadius;

            // --- Ripple (on thumb area) ---
            _ripple = new RippleElement();
            _track.Add(_ripple);

            // --- Thumb ---
            _thumb = new SDFRectElement
            {
                CornerRadius = ThumbSizeOn / 2f, // circular: radius = size/2
                pickingMode  = PickingMode.Ignore,
            };
            _thumb.AddToClassList(ThumbClass);
            _thumb.style.position       = Position.Absolute;
            _thumb.style.justifyContent = Justify.Center;
            _thumb.style.alignItems     = Align.Center;

            // Checkmark icon drawn via Painter2D (visible when on, hidden when off)
            _checkIcon = new VisualElement();
            _checkIcon.style.position = Position.Absolute;
            _checkIcon.style.left     = 0;
            _checkIcon.style.right    = 0;
            _checkIcon.style.top      = 0;
            _checkIcon.style.bottom   = 0;
            _checkIcon.pickingMode    = PickingMode.Ignore;
            _checkIcon.style.opacity  = 0f;
            _checkIcon.generateVisualContent += ctx =>
            {
                float w = _checkIcon.layout.width;
                float h = _checkIcon.layout.height;
                if (w < 1f || h < 1f) return;

                var p = ctx.painter2D;
                // M3 checkmark: 3 points forming an L-shape tick
                // Normalized to element bounds, centered, ~60% of size
                float s  = Mathf.Min(w, h) * 0.55f;
                float ox = (w - s) / 2f;
                float oy = (h - s) / 2f;

                p.strokeColor = _value ? _themePrimary : Color.clear;
                p.lineWidth = Mathf.Max(2f, s * 0.15f);
                p.lineCap   = LineCap.Round;
                p.lineJoin  = LineJoin.Round;
                p.BeginPath();
                p.MoveTo(new Vector2(ox + s * 0.15f, oy + s * 0.55f));
                p.LineTo(new Vector2(ox + s * 0.40f, oy + s * 0.80f));
                p.LineTo(new Vector2(ox + s * 0.85f, oy + s * 0.20f));
                p.Stroke();
            };
            _thumb.Add(_checkIcon);

            _track.Add(_thumb);

            // --- State layer on track (covers thumb area) ---
            _stateLayer = new StateLayerController(_track, _ripple);
            _stateLayer.Attach();

            // --- Events ---
            _track.RegisterCallback<ClickEvent>(OnTrackClicked);
            _track.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _track.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _track.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

            Add(_track);

            // Apply initial state
            RefreshThemeColors();
            ApplyVisualState();
        }

        // ------------------------------------------------------------------ //
        //  Visual State                                                        //
        // ------------------------------------------------------------------ //

        private void ApplyVisualState()
        {
            if (_value)
            {
                RemoveFromClassList(UncheckedClass);
                AddToClassList(CheckedClass);
                _track.RemoveFromClassList(UncheckedClass);
                _track.AddToClassList(CheckedClass);
            }
            else
            {
                RemoveFromClassList(CheckedClass);
                AddToClassList(UncheckedClass);
                _track.RemoveFromClassList(CheckedClass);
                _track.AddToClassList(UncheckedClass);
            }

            // Track outline: 2dp when off, 0 when on
            _track.OutlineThickness = _value ? 0f : TrackOutline;
            _track.OutlineColor = _themeOutline;

            // Thumb size
            float thumbSize = _pressed ? ThumbSizePress : (_value ? ThumbSizeOn : ThumbSizeOff);
            float thumbRadius = thumbSize / 2f;
            _thumb.style.width  = thumbSize;
            _thumb.style.height = thumbSize;
            _thumb.CornerRadius = thumbRadius; // circular: radius = size/2
            // USS border-radius so background-color is also rounded
            _thumb.style.borderTopLeftRadius     = thumbRadius;
            _thumb.style.borderTopRightRadius    = thumbRadius;
            _thumb.style.borderBottomLeftRadius  = thumbRadius;
            _thumb.style.borderBottomRightRadius = thumbRadius;

            // Thumb position (centered vertically, animated horizontally via USS transition)
            float thumbX = _value ? ThumbOnX : ThumbOffX;
            _thumb.style.left = thumbX - thumbSize / 2f;
            _thumb.style.top  = (TrackHeight - thumbSize) / 2f;

            // Checkmark: visible only when on (M3 Switch spec)
            _checkIcon.style.opacity = _value ? 1f : 0f;
            _checkIcon.MarkDirtyRepaint();

            // State layer overlay color: on-surface for both states (per M3 spec)
            _stateLayer.OverlayColor = _themeOnSurface;
        }

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

            _themeOutline   = theme.GetColor(Enums.ColorRole.Outline);
            _themeOnSurface = theme.GetColor(Enums.ColorRole.OnSurface);
            _themePrimary   = theme.GetColor(Enums.ColorRole.Primary);

            ApplyVisualState();
            _checkIcon.MarkDirtyRepaint();
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                      //
        // ------------------------------------------------------------------ //

        private void OnTrackClicked(ClickEvent evt)
        {
            if (_disabled) return;
            Value = !_value;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_disabled) return;
            _pressed = true;
            ApplyThumbSize();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_disabled) return;
            _pressed = false;
            ApplyThumbSize();
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (_pressed)
            {
                _pressed = false;
                ApplyThumbSize();
            }
        }

        private void ApplyThumbSize()
        {
            float thumbSize = _pressed ? ThumbSizePress : (_value ? ThumbSizeOn : ThumbSizeOff);
            float thumbRadius = thumbSize / 2f;
            _thumb.style.width  = thumbSize;
            _thumb.style.height = thumbSize;
            _thumb.CornerRadius = thumbRadius;
            _thumb.style.borderTopLeftRadius     = thumbRadius;
            _thumb.style.borderTopRightRadius    = thumbRadius;
            _thumb.style.borderBottomLeftRadius  = thumbRadius;
            _thumb.style.borderBottomRightRadius = thumbRadius;

            float thumbX = _value ? ThumbOnX : ThumbOffX;
            _thumb.style.left = thumbX - thumbSize / 2f;
            _thumb.style.top  = (TrackHeight - thumbSize) / 2f;
        }
    }
}
