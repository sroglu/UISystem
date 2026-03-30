using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Data;
using mehmetsrl.UISystem.Enums;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace mehmetsrl.UISystem
{
    /// <summary>
    /// uGUI Graphic that renders a rounded rectangle using the UISystem SDF shader.
    /// Visual parameters are packed into UV1/UV2 channels so all instances sharing
    /// the same material batch into a single draw call.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class SDFRectGraphic : MaskableGraphic, IThemeSubscriber
    {
        // ------------------------------------------------------------------ //
        //  Shape                                                               //
        // ------------------------------------------------------------------ //
        [TitleGroup("Shape")]
        [Tooltip("Corner radii in reference-resolution pixels (1080x1920). x=TL, y=TR, z=BR, w=BL.")]
        [SerializeField] private Vector4 _cornerRadius = new Vector4(16, 16, 16, 16);

        // ------------------------------------------------------------------ //
        //  Shadow / Elevation                                                  //
        // ------------------------------------------------------------------ //
        [TitleGroup("Shadow")]
        [SerializeField] private bool _shadowEnabled;

        [TitleGroup("Shadow"), ShowIf(nameof(_shadowEnabled))]
        [Range(0, 5)]
        [SerializeField] private int _elevationLevel = 1;

        // ------------------------------------------------------------------ //
        //  Outline                                                             //
        // ------------------------------------------------------------------ //
        [TitleGroup("Outline")]
        [SerializeField] private bool _outlineEnabled;

        [TitleGroup("Outline"), ShowIf(nameof(_outlineEnabled))]
        [SerializeField] private float _outlineThickness = 2f;

        [TitleGroup("Outline"), ShowIf(nameof(_outlineEnabled))]
        [SerializeField] private Color _outlineColor = Color.black;

        // ------------------------------------------------------------------ //
        //  Theme Color                                                         //
        // ------------------------------------------------------------------ //
        [TitleGroup("Theme")]
        [Tooltip("If true, the fill color is fetched from the active ThemeData via BaseColorRole.")]
        [SerializeField] private bool _useThemeColor = true;

        [TitleGroup("Theme"), ShowIf(nameof(_useThemeColor))]
        [SerializeField] private ColorRole _baseColorRole = ColorRole.Surface;

        // ------------------------------------------------------------------ //
        //  Shader property IDs (cached)                                        //
        // ------------------------------------------------------------------ //
        private static readonly int ShadowOffsetId  = Shader.PropertyToID("_ShadowOffset");
        private static readonly int ShadowBlurId    = Shader.PropertyToID("_ShadowBlur");
        private static readonly int ShadowColorId   = Shader.PropertyToID("_ShadowColor");
        private static readonly int ShadowEnabledId = Shader.PropertyToID("_ShadowEnabled");
        private static readonly int OutlineThickId  = Shader.PropertyToID("_OutlineThickness");
        private static readonly int OutlineColorId  = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineEnabledId= Shader.PropertyToID("_OutlineEnabled");

        // ------------------------------------------------------------------ //
        //  Properties                                                          //
        // ------------------------------------------------------------------ //
        public Vector4 CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; SetVerticesDirty(); }
        }

        public bool ShadowEnabled
        {
            get => _shadowEnabled;
            set { _shadowEnabled = value; SetVerticesDirty(); UpdateMaterialProperties(); }
        }

        public int ElevationLevel
        {
            get => _elevationLevel;
            set { _elevationLevel = Mathf.Clamp(value, 0, 5); SetVerticesDirty(); UpdateMaterialProperties(); }
        }

        public bool OutlineEnabled
        {
            get => _outlineEnabled;
            set { _outlineEnabled = value; SetVerticesDirty(); UpdateMaterialProperties(); }
        }

        public float OutlineThickness
        {
            get => _outlineThickness;
            set { _outlineThickness = value; UpdateMaterialProperties(); }
        }

        public Color OutlineColor
        {
            get => _outlineColor;
            set { _outlineColor = value; UpdateMaterialProperties(); }
        }

        public bool UseThemeColor
        {
            get => _useThemeColor;
            set { _useThemeColor = value; SetVerticesDirty(); }
        }

        public ColorRole BaseColorRole
        {
            get => _baseColorRole;
            set { _baseColorRole = value; SetVerticesDirty(); }
        }

        // ------------------------------------------------------------------ //
        //  Material                                                            //
        // ------------------------------------------------------------------ //
        private Material _materialInstance;

        public override Material defaultMaterial
        {
            get
            {
                if (_materialInstance != null) return _materialInstance;
                var shader = Shader.Find("UISystem/SDFRect");
                if (shader == null)
                {
                    Debug.LogError("[UISystem] SDFRect shader not found. Make sure it is in a Resources folder or included in the build.");
                    return base.defaultMaterial;
                }
                _materialInstance = new Material(shader) { name = "SDFRectMat_Instance" };
                return _materialInstance;
            }
        }

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                           //
        // ------------------------------------------------------------------ //
        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToThemeManager();
            UpdateMaterialProperties();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromThemeManager();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_materialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(_materialInstance);
                else
                    DestroyImmediate(_materialInstance);
            }
        }

        private void SubscribeToThemeManager()
        {
            if (Core.ThemeManager.Instance != null)
                Core.ThemeManager.Instance.OnThemeChanged += OnThemeChanged;
        }

        private void UnsubscribeFromThemeManager()
        {
            if (Core.ThemeManager.Instance != null)
                Core.ThemeManager.Instance.OnThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(ThemeData theme) => OnThemeApplied(theme);

        /// <inheritdoc/>
        public void OnThemeApplied(ThemeData theme)
        {
            SetVerticesDirty();
            UpdateMaterialProperties();
        }

        // ------------------------------------------------------------------ //
        //  Mesh Generation                                                     //
        // ------------------------------------------------------------------ //
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect    = rectTransform.rect;
            float halfW  = rect.width  * 0.5f;
            float halfH  = rect.height * 0.5f;
            float left   = rect.xMin;
            float right  = rect.xMax;
            float bottom = rect.yMin;
            float top    = rect.yMax;

            // Resolve fill color
            Color fillColor = _useThemeColor ? ResolveThemeColor() : color;

            // Pack corner radii into UV1
            float packedX = PackRadii(_cornerRadius.x, _cornerRadius.y); // TL, TR
            float packedY = PackRadii(_cornerRadius.z, _cornerRadius.w); // BR, BL

            // 4 vertices: bottom-left, bottom-right, top-right, top-left
            UIVertex[] verts = new UIVertex[4];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = UIVertex.simpleVert;
                verts[i].color = fillColor;
                verts[i].uv1   = new Vector4(packedX, packedY, 0f, 0f); // corner radii
                verts[i].uv2   = new Vector4(0f, 0f, 0f, 0f);           // reserved for WP-4
            }

            // Positions and UV0 (xy=texture UV, zw=halfSize)
            verts[0].position = new Vector3(left,  bottom, 0f);
            verts[0].uv0      = new Vector4(0f, 0f, halfW, halfH);

            verts[1].position = new Vector3(right, bottom, 0f);
            verts[1].uv0      = new Vector4(1f, 0f, halfW, halfH);

            verts[2].position = new Vector3(right, top,    0f);
            verts[2].uv0      = new Vector4(1f, 1f, halfW, halfH);

            verts[3].position = new Vector3(left,  top,    0f);
            verts[3].uv0      = new Vector4(0f, 1f, halfW, halfH);

            vh.AddUIVertexQuad(verts);
        }

        // ------------------------------------------------------------------ //
        //  Material Properties (shadow/outline — shared at material level)    //
        // ------------------------------------------------------------------ //
        private void UpdateMaterialProperties()
        {
            var mat = materialForRendering;
            if (mat == null) return;

            // Shadow
            mat.SetFloat(ShadowEnabledId, _shadowEnabled ? 1f : 0f);
            if (_shadowEnabled)
            {
                var elevPreset = GetElevationPreset();
                mat.SetVector(ShadowOffsetId, new Vector4(elevPreset.ShadowOffset.x, elevPreset.ShadowOffset.y, 0f, 0f));
                mat.SetFloat(ShadowBlurId,   elevPreset.ShadowBlur);
                mat.SetColor(ShadowColorId,  elevPreset.ShadowColor);
            }

            // Outline
            mat.SetFloat(OutlineEnabledId, _outlineEnabled ? 1f : 0f);
            mat.SetFloat(OutlineThickId,   _outlineThickness);
            mat.SetColor(OutlineColorId,   _outlineColor);
        }

        private ElevationPreset GetElevationPreset()
        {
            var theme = Core.ThemeManager.Instance?.ActiveTheme;
            if (theme != null) return theme.GetElevation(_elevationLevel);

            // Fallback defaults
            return new ElevationPreset
            {
                ShadowOffset      = new Vector2(0f, -4f * _elevationLevel),
                ShadowBlur        = 4f * _elevationLevel,
                ShadowColor       = new Color(0f, 0f, 0f, 0.15f + 0.03f * _elevationLevel),
                TonalOverlayAlpha = 0f
            };
        }

        private Color ResolveThemeColor()
        {
            var theme = Core.ThemeManager.Instance?.ActiveTheme;
            if (theme != null) return theme.GetColor(_baseColorRole);
            Debug.LogWarning("[UISystem] SDFRectGraphic: ThemeManager not found. Using Graphic.color as fallback.", this);
            return color;
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //
        /// <summary>Pack two 12-bit corner radius values into a single float.</summary>
        private static float PackRadii(float a, float b)
            => Mathf.Floor(Mathf.Clamp(a, 0f, 4095f)) * 4096f
             + Mathf.Floor(Mathf.Clamp(b, 0f, 4095f));

        /// <summary>Force mesh and material rebuild (useful after batch property changes).</summary>
        public void RefreshVisuals()
        {
            SetVerticesDirty();
            SetMaterialDirty();
            UpdateMaterialProperties();
        }

        // ------------------------------------------------------------------ //
        //  Editor Validation                                                   //
        // ------------------------------------------------------------------ //
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _elevationLevel = Mathf.Clamp(_elevationLevel, 0, 5);
            _cornerRadius   = new Vector4(
                Mathf.Max(0f, _cornerRadius.x),
                Mathf.Max(0f, _cornerRadius.y),
                Mathf.Max(0f, _cornerRadius.z),
                Mathf.Max(0f, _cornerRadius.w));

            // Warn if Canvas lacks required shader channels
            ValidateCanvasShaderChannels();
        }

        private void ValidateCanvasShaderChannels()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var required = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            if ((canvas.additionalShaderChannels & required) != required)
            {
                Debug.LogWarning(
                    $"[UISystem] Canvas '{canvas.name}' is missing TexCoord1 and/or TexCoord2 in Additional Shader Channels. " +
                    $"SDFRectGraphic will not render corner radii correctly. Enable them in the Canvas component.",
                    canvas);
            }
        }
#endif
    }
}
