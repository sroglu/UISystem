using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Custom VisualElement that renders a rounded rectangle with optional soft shadow
    /// and outline via Painter2D (CPU vector graphics). Corner radius, shadow, and outline
    /// are exposed as [UxmlAttribute] properties. Background color is set via USS
    /// (e.g. background-color: var(--m3-surface)) or falls back to resolvedStyle.backgroundColor.
    /// </summary>
    [UxmlElement]
    public partial class SDFRectElement : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  Shape                                                               //
        // ------------------------------------------------------------------ //
        private float _cornerRadius = 12f;
        private float _cornerRadiusTL = -1f;
        private float _cornerRadiusTR = -1f;
        private float _cornerRadiusBR = -1f;
        private float _cornerRadiusBL = -1f;

        [UxmlAttribute("corner-radius")]
        public float CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("corner-radius-tl")]
        public float CornerRadiusTL
        {
            get => _cornerRadiusTL;
            set { _cornerRadiusTL = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("corner-radius-tr")]
        public float CornerRadiusTR
        {
            get => _cornerRadiusTR;
            set { _cornerRadiusTR = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("corner-radius-br")]
        public float CornerRadiusBR
        {
            get => _cornerRadiusBR;
            set { _cornerRadiusBR = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("corner-radius-bl")]
        public float CornerRadiusBL
        {
            get => _cornerRadiusBL;
            set { _cornerRadiusBL = value; MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Shadow                                                              //
        // TODO(006): GPU SDF Shader deferred — Painter2D accepted per research R2
        //   Current: CPU Painter2D concentric fill loop for soft shadow approximation.
        //   Future: per-element GPU SDF pass once UI Toolkit supports batching-friendly
        //   custom mesh injection without breaking draw-call batching on mobile.
        // ------------------------------------------------------------------ //
        private float _shadowBlur;
        private float _shadowOffsetX;
        private float _shadowOffsetY;
        private Color _shadowColor   = new Color(0f, 0f, 0f, 0.20f);

        [UxmlAttribute("shadow-blur")]
        public float ShadowBlur
        {
            get => _shadowBlur;
            set { _shadowBlur = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("shadow-offset-x")]
        public float ShadowOffsetX
        {
            get => _shadowOffsetX;
            set { _shadowOffsetX = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("shadow-offset-y")]
        public float ShadowOffsetY
        {
            get => _shadowOffsetY;
            set { _shadowOffsetY = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("shadow-color-r")]
        public float ShadowColorR
        {
            get => _shadowColor.r;
            set { _shadowColor.r = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("shadow-color-g")]
        public float ShadowColorG
        {
            get => _shadowColor.g;
            set { _shadowColor.g = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("shadow-color-b")]
        public float ShadowColorB
        {
            get => _shadowColor.b;
            set { _shadowColor.b = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("shadow-color-a")]
        public float ShadowColorA
        {
            get => _shadowColor.a;
            set { _shadowColor.a = value; MarkDirtyRepaint(); }
        }

        /// <summary>
        /// Shadow color as a whole Color struct. Not a UXML attribute — set from C# or
        /// use the individual ShadowColorR/G/B/A attributes in UXML.
        /// </summary>
        public Color ShadowColor
        {
            get => _shadowColor;
            set { _shadowColor = value; MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Shadow Padding — insets the visual rect so shadow fits in element  //
        // ------------------------------------------------------------------ //
        private float _shadowPadding;

        /// <summary>
        /// Insets the fill/outline/overlay drawing rect by this amount on all sides.
        /// The shadow is drawn around the inset rect, giving it room to expand
        /// outward without being clipped by the element bounds.
        /// Pair with matching CSS padding on the element so children are also inset.
        /// Default 0 (no inset — legacy behavior).
        /// </summary>
        public float ShadowPadding
        {
            get => _shadowPadding;
            set { _shadowPadding = Mathf.Max(0f, value); MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Outline                                                             //
        // ------------------------------------------------------------------ //
        private float _outlineThickness;
        private Color _outlineColor = new Color(0.47f, 0.46f, 0.49f, 1f); // M3 Outline

        [UxmlAttribute("outline-thickness")]
        public float OutlineThickness
        {
            get => _outlineThickness;
            set { _outlineThickness = value; MarkDirtyRepaint(); }
        }

        /// <summary>Outline color. Set from C# after construction.</summary>
        public Color OutlineColor
        {
            get => _outlineColor;
            set { _outlineColor = value; MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Direct Fill Color (bypasses resolvedStyle for immediate updates)  //
        // ------------------------------------------------------------------ //
        private Color? _fillColorOverride;

        /// <summary>
        /// When set, Painter2D uses this color directly instead of resolvedStyle.backgroundColor.
        /// Set to null to fall back to USS/inline style resolution.
        /// </summary>
        public Color? FillColorOverride
        {
            get => _fillColorOverride;
            set { _fillColorOverride = value; MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Tonal Elevation Overlay (persistent primary-tint for dark mode)    //
        // ------------------------------------------------------------------ //
        private float _tonalOverlayOpacity;
        private Color _tonalOverlayColor = Color.clear;

        /// <summary>
        /// Persistent primary-color tint opacity (0–1) for M3 tonal elevation.
        /// Independent of the interaction state overlay — not reset on idle.
        /// Typically 0.05 for elevation level 1 (Elevated card/button in dark mode).
        /// </summary>
        public float TonalOverlayOpacity
        {
            get => _tonalOverlayOpacity;
            set { _tonalOverlayOpacity = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }

        /// <summary>Tonal overlay tint color. Use M3 primary for each theme.</summary>
        public Color TonalOverlayColor
        {
            get => _tonalOverlayColor;
            set { _tonalOverlayColor = value; MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  State Overlay (driven by WP-4 StateLayerController)                //
        // ------------------------------------------------------------------ //
        private float _stateOverlayOpacity;
        private Color _stateOverlayColor = Color.white;

        /// <summary>State overlay opacity (0–1). Set from StateLayerController in WP-4.</summary>
        public float StateOverlayOpacity
        {
            get => _stateOverlayOpacity;
            set { _stateOverlayOpacity = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }

        /// <summary>State overlay tint color.</summary>
        public Color StateOverlayColor
        {
            get => _stateOverlayColor;
            set { _stateOverlayColor = value; MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Ripple (driven from C# — not UXML attributes)                      //
        // ------------------------------------------------------------------ //
        private Vector2 _rippleCenter;
        private float   _rippleRadius;
        private float   _rippleAlpha;

        /// <summary>Ripple origin in element local pixels.</summary>
        public Vector2 RippleCenter
        {
            get => _rippleCenter;
            set { _rippleCenter = value; MarkDirtyRepaint(); }
        }

        /// <summary>Ripple radius as fraction of element diagonal (0–1).</summary>
        public float RippleRadius
        {
            get => _rippleRadius;
            set { _rippleRadius = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }

        /// <summary>Ripple opacity (0–1).</summary>
        public float RippleAlpha
        {
            get => _rippleAlpha;
            set { _rippleAlpha = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //
        public SDFRectElement()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        // ------------------------------------------------------------------ //
        //  Rendering                                                           //
        // ------------------------------------------------------------------ //
        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            // Use full element bounds (padding box) so Painter2D fill matches the
            // CSS background-color area. contentRect excludes padding, which causes
            // heavily-padded elements (e.g. buttons) to render as ovals instead of pills.
            Rect fullRect = new Rect(0f, 0f, layout.width, layout.height);
            if (fullRect.width < 1f || fullRect.height < 1f) return;

            // When ShadowPadding > 0, inset the visual rect so the shadow has room
            // to expand outward without being clipped by the element bounds.
            float sp = _shadowPadding;
            Rect rect = sp > 0f
                ? new Rect(sp, sp, fullRect.width - sp * 2f, fullRect.height - sp * 2f)
                : fullRect;
            if (rect.width < 1f || rect.height < 1f) return;

            var painter = ctx.painter2D;

            // Resolve corner radii
            float tl = _cornerRadiusTL >= 0f ? _cornerRadiusTL : _cornerRadius;
            float tr = _cornerRadiusTR >= 0f ? _cornerRadiusTR : _cornerRadius;
            float br = _cornerRadiusBR >= 0f ? _cornerRadiusBR : _cornerRadius;
            float bl = _cornerRadiusBL >= 0f ? _cornerRadiusBL : _cornerRadius;

            // Clamp radii so they don't exceed half the dimension
            float maxR = Mathf.Min(rect.width, rect.height) * 0.5f;
            tl = Mathf.Min(tl, maxR);
            tr = Mathf.Min(tr, maxR);
            br = Mathf.Min(br, maxR);
            bl = Mathf.Min(bl, maxR);

            // --- Shadow pass ---
            if (_shadowBlur > 0f || Mathf.Abs(_shadowOffsetX) > 0f || Mathf.Abs(_shadowOffsetY) > 0f)
            {
                DrawShadow(painter, rect, tl, tr, br, bl);
            }

            // --- Main fill ---
            Color fillColor = _fillColorOverride ?? resolvedStyle.backgroundColor;
            if (fillColor.a > 0f)
            {
                painter.fillColor = fillColor;
                DrawRoundedRect(painter, rect, tl, tr, br, bl);
                painter.Fill(FillRule.OddEven);
            }

            // --- Outline pass ---
            if (_outlineThickness > 0f)
            {
                painter.strokeColor = _outlineColor;
                painter.lineWidth   = _outlineThickness;
                DrawRoundedRect(painter, ShrinkRect(rect, _outlineThickness * 0.5f), tl, tr, br, bl);
                painter.Stroke();
            }

            // --- Tonal elevation overlay ---
            if (_tonalOverlayOpacity > 0f)
            {
                var tonalColor = _tonalOverlayColor;
                tonalColor.a   = _tonalOverlayOpacity;
                painter.fillColor = tonalColor;
                DrawRoundedRect(painter, rect, tl, tr, br, bl);
                painter.Fill(FillRule.OddEven);
            }

            // --- State overlay ---
            if (_stateOverlayOpacity > 0f)
            {
                var overlayColor = _stateOverlayColor;
                overlayColor.a   = _stateOverlayOpacity;
                painter.fillColor = overlayColor;
                DrawRoundedRect(painter, rect, tl, tr, br, bl);
                painter.Fill(FillRule.OddEven);
            }

            // --- Ripple ---
            if (_rippleRadius > 0f && _rippleAlpha > 0f)
            {
                float diag   = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height);
                float radius = _rippleRadius * diag * 0.5f;
                var   rColor = _stateOverlayColor;
                rColor.a     = _rippleAlpha * 0.20f; // M3 pressed opacity
                painter.fillColor = rColor;
                painter.BeginPath();
                painter.Arc(_rippleCenter + rect.min, radius, 0f, 360f);
                painter.ClosePath();
                painter.Fill(FillRule.OddEven);
            }
        }

        // ------------------------------------------------------------------ //
        //  Shadow — layered semi-transparent copies, shifted & expanded       //
        // ------------------------------------------------------------------ //
        private void DrawShadow(Painter2D p, Rect rect, float tl, float tr, float br, float bl)
        {
            if (_shadowColor.a <= 0f) return;

            // Approximate Gaussian blur as N concentric transparent layers.
            // This is a rasterization-level approximation only — the Shader Graph
            // (WP-1, T014) provides true GPU SDF shadow when assigned.
            int   layers     = Mathf.Max(1, Mathf.RoundToInt(_shadowBlur * 0.4f));
            float alphaStep  = _shadowColor.a / layers;
            float expandStep = _shadowBlur / layers;

            for (int i = layers; i >= 1; i--)
            {
                float t       = (float)i / layers;
                float expand  = expandStep * i;
                float alpha   = alphaStep * (1f - t * 0.5f);
                Rect  sRect   = new Rect(
                    rect.x  + _shadowOffsetX - expand * 0.5f,
                    rect.y  + _shadowOffsetY - expand * 0.5f,
                    rect.width  + expand,
                    rect.height + expand);

                var c = _shadowColor;
                c.a   = alpha;
                p.fillColor = c;
                DrawRoundedRect(p, sRect, tl + expand * 0.5f, tr + expand * 0.5f,
                                          br + expand * 0.5f, bl + expand * 0.5f);
                p.Fill(FillRule.OddEven);
            }
        }

        // ------------------------------------------------------------------ //
        //  Rounded Rectangle Path                                              //
        // ------------------------------------------------------------------ //
        private static void DrawRoundedRect(Painter2D p, Rect r, float tl, float tr, float br, float bl)
        {
            // Clamp to avoid degenerate arcs
            float maxR = Mathf.Min(r.width, r.height) * 0.5f;
            tl = Mathf.Clamp(tl, 0f, maxR);
            tr = Mathf.Clamp(tr, 0f, maxR);
            br = Mathf.Clamp(br, 0f, maxR);
            bl = Mathf.Clamp(bl, 0f, maxR);

            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin + tl, r.yMin));
            p.LineTo(new Vector2(r.xMax - tr, r.yMin));
            if (tr > 0f) p.Arc(new Vector2(r.xMax - tr, r.yMin + tr), tr, 270f, 360f);
            p.LineTo(new Vector2(r.xMax, r.yMax - br));
            if (br > 0f) p.Arc(new Vector2(r.xMax - br, r.yMax - br), br, 0f, 90f);
            p.LineTo(new Vector2(r.xMin + bl, r.yMax));
            if (bl > 0f) p.Arc(new Vector2(r.xMin + bl, r.yMax - bl), bl, 90f, 180f);
            p.LineTo(new Vector2(r.xMin, r.yMin + tl));
            if (tl > 0f) p.Arc(new Vector2(r.xMin + tl, r.yMin + tl), tl, 180f, 270f);
            p.ClosePath();
        }

        private static Rect ShrinkRect(Rect r, float amount)
            => new Rect(r.x + amount, r.y + amount, r.width - amount * 2f, r.height - amount * 2f);
    }
}
