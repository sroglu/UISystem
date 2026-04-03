using System;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Floating Action Button.
    ///
    /// Sizes:
    ///   Small   — 40×40dp, 12dp corners
    ///   Regular — 56×56dp, 16dp corners
    ///   Large   — 96×96dp, 28dp corners
    ///
    /// Extended mode: Row with icon + label, min-width 80dp, 16dp corners.
    ///
    /// M3 Spec:
    ///   Colors: primary-container bg, on-primary-container icon/text
    ///   Elevation: Level 3
    ///
    /// USS: fab.uss
    /// </summary>
    [UxmlElement]
    public partial class M3FAB : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass     = "m3-fab";
        private const string SmallClass    = "m3-fab--small";
        private const string RegularClass  = "m3-fab--regular";
        private const string LargeClass    = "m3-fab--large";
        private const string ExtendedClass = "m3-fab--extended";
        private const string IconClass     = "m3-fab__icon";
        private const string LabelClass    = "m3-fab__label";

        private const string DefaultIconText = "\ue145";

        // ------------------------------------------------------------------ //
        //  Size config                                                         //
        // ------------------------------------------------------------------ //
        private static readonly (float size, float radius)[] SizeConfig =
        {
            (40f, 12f),  // Small
            (56f, 16f),  // Regular
            (96f, 28f),  // Large
        };

        // Resolved theme colors
        private Color _themePrimaryContainer;
        private Color _themeOnPrimaryContainer;

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly SDFRectElement       _container;
        private readonly VisualElement       _iconEl;
        private readonly Label                _labelEl;
        private readonly RippleElement        _ripple;
        private readonly StateLayerController _stateLayer;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private FABSize _size     = FABSize.Regular;
        private bool    _extended = false;
        private string  _text     = string.Empty;
        private FABIcon _fabIcon  = FABIcon.Add;
        private bool    _disabled = false;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when the FAB is pressed.</summary>
        public event Action OnClick;

        /// <summary>FAB size (Small, Regular, Large).</summary>
        [UxmlAttribute("size")]
        public FABSize Size
        {
            get => _size;
            set { _size = value; ApplySize(); }
        }

        /// <summary>Extended mode — shows icon + label side by side.</summary>
        [UxmlAttribute("extended")]
        public bool Extended
        {
            get => _extended;
            set { _extended = value; ApplySize(); }
        }

        /// <summary>Label text (shown in extended mode).</summary>
        [UxmlAttribute("text")]
        public string Text
        {
            get => _text;
            set { _text = value; _labelEl.text = value ?? string.Empty; }
        }

        /// <summary>Painter2D icon type.</summary>
        [UxmlAttribute("icon")]
        public FABIcon Icon
        {
            get => _fabIcon;
            set { _fabIcon = value; _iconEl.MarkDirtyRepaint(); }
        }

        /// <summary>When true, dims and ignores input.</summary>
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

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3FAB()
        {
            AddToClassList(BaseClass);
            pickingMode = PickingMode.Position;

            // --- Container ---
            _container = new SDFRectElement { pickingMode = PickingMode.Position };
            _container.style.flexDirection = FlexDirection.Row;
            _container.style.alignItems    = Align.Center;
            _container.style.justifyContent = Justify.Center;
            _container.style.overflow      = Overflow.Hidden;

            // --- Ripple ---
            _ripple = new RippleElement();
            _container.Add(_ripple);

            // --- Icon (Painter2D) ---
            _iconEl = new VisualElement();
            _iconEl.AddToClassList(IconClass);
            _iconEl.pickingMode = PickingMode.Ignore;
            _iconEl.style.width  = 24f;
            _iconEl.style.height = 24f;
            _iconEl.generateVisualContent += OnDrawIcon;
            _container.Add(_iconEl);

            // --- Label (extended mode only) ---
            _labelEl = new Label(string.Empty);
            _labelEl.AddToClassList("m3-label");
            _labelEl.AddToClassList(LabelClass);
            _labelEl.pickingMode   = PickingMode.Ignore;
            _labelEl.style.display = DisplayStyle.None;
            _container.Add(_labelEl);

            // --- State layer ---
            _stateLayer = new StateLayerController(_container, _ripple);
            _stateLayer.Attach();

            // --- Events ---
            _container.RegisterCallback<ClickEvent>(OnContainerClicked);

            RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

            Add(_container);
            RefreshThemeColors();
            ApplySize();
        }

        // ------------------------------------------------------------------ //
        //  Visual State                                                        //
        // ------------------------------------------------------------------ //

        private void ApplySize()
        {
            // Clear size classes
            _container.RemoveFromClassList(SmallClass);
            _container.RemoveFromClassList(RegularClass);
            _container.RemoveFromClassList(LargeClass);
            _container.RemoveFromClassList(ExtendedClass);

            if (_extended)
            {
                _container.AddToClassList(ExtendedClass);
                float radius = 16f;
                _container.CornerRadius = radius;
                _container.style.borderTopLeftRadius     = radius;
                _container.style.borderTopRightRadius    = radius;
                _container.style.borderBottomLeftRadius  = radius;
                _container.style.borderBottomRightRadius = radius;
                _container.style.width  = StyleKeyword.Auto;
                _container.style.height = 56f;
                _container.style.minWidth = 80f;
                _container.style.paddingLeft  = 16f;
                _container.style.paddingRight = 20f;
                _iconEl.style.width  = 24f;
                _iconEl.style.height = 24f;
                _iconEl.style.marginRight = 8f;
                _labelEl.style.display = DisplayStyle.Flex;
            }
            else
            {
                var (size, radius) = SizeConfig[(int)_size];
                string sizeClass = _size switch
                {
                    FABSize.Small   => SmallClass,
                    FABSize.Large   => LargeClass,
                    _               => RegularClass,
                };
                _container.AddToClassList(sizeClass);
                _container.CornerRadius = radius;
                _container.style.borderTopLeftRadius     = radius;
                _container.style.borderTopRightRadius    = radius;
                _container.style.borderBottomLeftRadius  = radius;
                _container.style.borderBottomRightRadius = radius;
                _container.style.width  = size;
                _container.style.height = size;
                _container.style.minWidth = StyleKeyword.Auto;
                _container.style.paddingLeft  = 0f;
                _container.style.paddingRight = 0f;

                float iconSize = _size == FABSize.Large ? 36f : 24f;
                _iconEl.style.width  = iconSize;
                _iconEl.style.height = iconSize;
                _iconEl.style.marginRight = 0f;
                _labelEl.style.display = DisplayStyle.None;
            }

            _stateLayer.OverlayColor = _themeOnPrimaryContainer;
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

            _themePrimaryContainer   = theme.GetColor(ColorRole.PrimaryContainer);
            _themeOnPrimaryContainer = theme.GetColor(ColorRole.OnPrimaryContainer);

            _container.FillColorOverride = _themePrimaryContainer;
            _labelEl.style.color  = new StyleColor(_themeOnPrimaryContainer);
            _iconEl.MarkDirtyRepaint();

            ApplySize();
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                      //
        // ------------------------------------------------------------------ //

        private void OnContainerClicked(ClickEvent evt)
        {
            if (_disabled) return;
            OnClick?.Invoke();
        }

        // ------------------------------------------------------------------ //
        //  Painter2D Icon Drawing                                              //
        // ------------------------------------------------------------------ //

        private void OnDrawIcon(MeshGenerationContext ctx)
        {
            float w = _iconEl.layout.width;
            float h = _iconEl.layout.height;
            if (w < 1f || h < 1f) return;

            var p = ctx.painter2D;
            Color iconColor = _themeOnPrimaryContainer;
            p.strokeColor = iconColor;
            float strokeW = Mathf.Max(1.5f, Mathf.Min(w, h) * 0.10f);
            p.lineWidth   = strokeW;
            p.lineCap     = LineCap.Round;
            p.lineJoin    = LineJoin.Round;

            float s  = Mathf.Min(w, h);
            float cx = w / 2f, cy = h / 2f;

            switch (_fabIcon)
            {
                case FABIcon.Add:
                    // Plus sign
                    float half = s * 0.30f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx - half, cy));
                    p.LineTo(new Vector2(cx + half, cy));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx, cy - half));
                    p.LineTo(new Vector2(cx, cy + half));
                    p.Stroke();
                    break;

                case FABIcon.Edit:
                    // Pencil icon
                    float penLen = s * 0.55f;
                    float penW2  = s * 0.08f;
                    // Pen body (diagonal)
                    float dx = penLen * 0.707f;  // cos(45)
                    float tipX = cx - dx / 2f, tipY = cy + dx / 2f;
                    float endX = cx + dx / 2f, endY = cy - dx / 2f;
                    // Shaft
                    p.BeginPath();
                    p.MoveTo(new Vector2(tipX, tipY));
                    p.LineTo(new Vector2(endX, endY));
                    p.Stroke();
                    // Tip
                    p.BeginPath();
                    p.MoveTo(new Vector2(tipX - penW2 * 0.707f, tipY - penW2 * 0.707f));
                    p.LineTo(new Vector2(tipX, tipY + penW2 * 0.5f));
                    p.LineTo(new Vector2(tipX - penW2 * 1.2f, tipY));
                    p.ClosePath();
                    p.fillColor = iconColor;
                    p.Fill();
                    // Top cap
                    float capOff = penW2 * 0.707f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(endX - capOff, endY - capOff));
                    p.LineTo(new Vector2(endX + capOff, endY - capOff));
                    p.LineTo(new Vector2(endX + capOff, endY + capOff));
                    p.LineTo(new Vector2(endX - capOff, endY + capOff));
                    p.ClosePath();
                    p.Stroke();
                    break;

                case FABIcon.Mail:
                    // Envelope
                    float envW = s * 0.65f, envH = s * 0.45f;
                    float ex = cx - envW / 2f, ey = cy - envH / 2f;
                    float er = s * 0.04f;
                    // Rect body
                    p.BeginPath();
                    p.MoveTo(new Vector2(ex + er, ey));
                    p.LineTo(new Vector2(ex + envW - er, ey));
                    p.ArcTo(new Vector2(ex + envW, ey), new Vector2(ex + envW, ey + er), er);
                    p.LineTo(new Vector2(ex + envW, ey + envH - er));
                    p.ArcTo(new Vector2(ex + envW, ey + envH), new Vector2(ex + envW - er, ey + envH), er);
                    p.LineTo(new Vector2(ex + er, ey + envH));
                    p.ArcTo(new Vector2(ex, ey + envH), new Vector2(ex, ey + envH - er), er);
                    p.LineTo(new Vector2(ex, ey + er));
                    p.ArcTo(new Vector2(ex, ey), new Vector2(ex + er, ey), er);
                    p.ClosePath();
                    p.Stroke();
                    // V flap
                    p.BeginPath();
                    p.MoveTo(new Vector2(ex, ey));
                    p.LineTo(new Vector2(cx, cy + envH * 0.05f));
                    p.LineTo(new Vector2(ex + envW, ey));
                    p.Stroke();
                    break;

                case FABIcon.Search:
                    // Magnifying glass
                    float sr = s * 0.25f;
                    float scx = cx - s * 0.06f, scy = cy - s * 0.06f;
                    p.BeginPath();
                    p.Arc(new Vector2(scx, scy), sr, 0f, 360f);
                    p.Stroke();
                    p.BeginPath();
                    float sdx = sr * 0.7f;
                    p.MoveTo(new Vector2(scx + sdx, scy + sdx));
                    p.LineTo(new Vector2(scx + sdx + s * 0.2f, scy + sdx + s * 0.2f));
                    p.Stroke();
                    break;

                case FABIcon.Star:
                    // 5-point star
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
            }
        }
    }
}
