using System;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Radio Button component.
    ///
    /// Composition:
    ///   VisualElement (this)    — 48dp touch target wrapper
    ///   SDFRectElement (_outer) — 20×20dp circle, 2dp border
    ///   SDFRectElement (_inner) — 10×10dp filled circle (hidden when unselected)
    ///   RippleElement (_ripple)
    ///   StateLayerController (_stateLayer)
    ///
    /// M3 Spec:
    ///   Outer: 20×20dp circle (radius=10), 2dp border
    ///   Inner: 10×10dp circle (radius=5) when selected
    ///   Unselected: outline border = --m3-outline
    ///   Selected: border + inner fill = --m3-primary
    ///   Touch target: 48dp wrapper
    ///   Disabled: 0.38 opacity
    ///
    /// USS: radio.uss
    ///
    /// Usage (UXML):
    ///   &lt;components:M3RadioButton selected="true" /&gt;
    /// </summary>
    [UxmlElement]
    public partial class M3RadioButton : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass         = "m3-radio";
        private const string OuterClass        = "m3-radio__outer";
        private const string InnerClass        = "m3-radio__inner";
        private const string SelectedClass     = "m3-radio--selected";
        private const string UnselectedClass   = "m3-radio--unselected";

        // ------------------------------------------------------------------ //
        //  Dimensions (M3 spec)                                                //
        // ------------------------------------------------------------------ //
        private const float OuterSize   = 20f;
        private const float OuterRadius = 10f;
        private const float InnerSize   = 10f;
        private const float InnerRadius = 5f;
        private const float BorderWidth = 2f;

        // Resolved theme colors
        private Color _themePrimary;
        private Color _themeOutline;
        private Color _themeOnSurface;

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement       _outer;
        private readonly SDFRectElement       _inner;
        private readonly RippleElement        _ripple;
        private readonly StateLayerController _stateLayer;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private bool   _selected;
        private bool   _disabled;
        private string _groupName;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when selection changes.</summary>
        public event Action<bool> OnSelectionChanged;

        /// <summary>Internal group changed event (used by M3RadioGroup).</summary>
        internal event Action<M3RadioButton> GroupSelectionRequested;

        /// <summary>Whether this radio button is selected.</summary>
        [UxmlAttribute("selected")]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                ApplyVisualState();
                OnSelectionChanged?.Invoke(_selected);
            }
        }

        /// <summary>When true, dims the radio button and ignores input.</summary>
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

        /// <summary>Group name for mutual exclusion (optional; use M3RadioGroup for code-based grouping).</summary>
        [UxmlAttribute("group-name")]
        public string GroupName
        {
            get => _groupName;
            set => _groupName = value;
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3RadioButton()
        {
            AddToClassList(BaseClass);
            style.alignSelf      = Align.FlexStart;
            style.justifyContent = Justify.Center;
            style.alignItems     = Align.Center;
            pickingMode          = PickingMode.Position;
            focusable            = true;

            // --- Outer circle ---
            _outer = new SDFRectElement
            {
                CornerRadius = OuterRadius,
                pickingMode  = PickingMode.Position,
            };
            _outer.AddToClassList(OuterClass);
            _outer.style.width  = OuterSize;
            _outer.style.height = OuterSize;
            _outer.style.borderTopLeftRadius     = OuterRadius;
            _outer.style.borderTopRightRadius    = OuterRadius;
            _outer.style.borderBottomLeftRadius  = OuterRadius;
            _outer.style.borderBottomRightRadius = OuterRadius;
            _outer.style.justifyContent = Justify.Center;
            _outer.style.alignItems     = Align.Center;

            // --- Ripple ---
            _ripple = new RippleElement();
            _outer.Add(_ripple);

            // --- Inner circle (shown when selected) ---
            _inner = new SDFRectElement
            {
                CornerRadius = InnerRadius,
                pickingMode  = PickingMode.Ignore,
            };
            _inner.AddToClassList(InnerClass);
            _inner.style.width  = InnerSize;
            _inner.style.height = InnerSize;
            _inner.style.borderTopLeftRadius     = InnerRadius;
            _inner.style.borderTopRightRadius    = InnerRadius;
            _inner.style.borderBottomLeftRadius  = InnerRadius;
            _inner.style.borderBottomRightRadius = InnerRadius;
            _inner.style.position = Position.Absolute;
            _inner.style.display  = DisplayStyle.None;
            _outer.Add(_inner);

            // --- State layer ---
            _stateLayer = new StateLayerController(_outer, _ripple);
            _stateLayer.Attach();

            // --- Events ---
            _outer.RegisterCallback<ClickEvent>(OnOuterClicked);

            RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

            Add(_outer);
            RefreshThemeColors();
            ApplyVisualState();
        }

        // ------------------------------------------------------------------ //
        //  Visual State                                                        //
        // ------------------------------------------------------------------ //

        private void ApplyVisualState()
        {
            _outer.RemoveFromClassList(SelectedClass);
            _outer.RemoveFromClassList(UnselectedClass);

            if (_selected)
            {
                _outer.AddToClassList(SelectedClass);
                _outer.OutlineThickness = BorderWidth;
                _outer.OutlineColor     = _themePrimary;
                _inner.style.display    = DisplayStyle.Flex;
            }
            else
            {
                _outer.AddToClassList(UnselectedClass);
                _outer.OutlineThickness = BorderWidth;
                _outer.OutlineColor     = _themeOutline;
                _inner.style.display    = DisplayStyle.None;
            }

            _stateLayer.OverlayColor = _themeOnSurface;
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

            _themePrimary   = theme.GetColor(ColorRole.Primary);
            _themeOutline   = theme.GetColor(ColorRole.Outline);
            _themeOnSurface = theme.GetColor(ColorRole.OnSurface);

            ApplyVisualState();
        }

        // ------------------------------------------------------------------ //
        //  Internal: select without firing GroupSelectionRequested             //
        //  (used by M3RadioGroup to avoid re-entrant loops)                   //
        // ------------------------------------------------------------------ //
        internal void SelectSilently()
        {
            if (_selected) return;
            _selected = true;
            ApplyVisualState();
            OnSelectionChanged?.Invoke(_selected);
        }

        internal void DeselectSilently()
        {
            if (!_selected) return;
            _selected = false;
            ApplyVisualState();
            OnSelectionChanged?.Invoke(_selected);
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                      //
        // ------------------------------------------------------------------ //

        private void OnOuterClicked(ClickEvent evt)
        {
            if (_disabled || _selected) return;
            GroupSelectionRequested?.Invoke(this);
            if (GroupSelectionRequested == null)
            {
                // Standalone (no group): just select
                Selected = true;
            }
        }
    }
}
