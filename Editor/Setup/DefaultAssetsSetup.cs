using mehmetsrl.UISystem;
using mehmetsrl.UISystem.Data;
using mehmetsrl.UISystem.Enums;
using UnityEditor;
using UnityEngine;

namespace mehmetsrl.UISystem.Editor
{
    /// <summary>
    /// Runs once after domain reload. Creates DefaultLight.asset, DefaultDark.asset,
    /// and DefaultTypography.asset if they are missing from the project.
    /// </summary>
    [InitializeOnLoad]
    internal static class DefaultAssetsSetup
    {
        private const string ThemeFolder      = "Assets/UISystem/Assets/Themes";
        private const string TypographyFolder = "Assets/UISystem/Assets/Typography";
        private const string LightPath        = ThemeFolder + "/DefaultLight.asset";
        private const string DarkPath         = ThemeFolder + "/DefaultDark.asset";
        private const string TypoPath         = TypographyFolder + "/DefaultTypography.asset";

        static DefaultAssetsSetup()
        {
            EditorApplication.delayCall += CreateMissingAssets;
        }

        [MenuItem("Assets/UISystem/Create Default Assets")]
        public static void CreateMissingAssets()
        {
            bool changed = false;
            changed |= EnsureThemeAssets();
            changed |= EnsureTypographyAsset();

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[UISystem] Default assets created. Assign them to ThemeManager in your scene.");
            }
        }

        // ------------------------------------------------------------------ //
        //  Theme assets                                                        //
        // ------------------------------------------------------------------ //
        private static bool EnsureThemeAssets()
        {
            bool changed = false;

            EnsureFolder(ThemeFolder);

            if (AssetDatabase.LoadAssetAtPath<ThemeData>(LightPath) == null)
            {
                var asset = CreateThemeData(isLight: true);
                AssetDatabase.CreateAsset(asset, LightPath);
                Debug.Log("[UISystem] Created DefaultLight.asset");
                changed = true;
            }

            if (AssetDatabase.LoadAssetAtPath<ThemeData>(DarkPath) == null)
            {
                var asset = CreateThemeData(isLight: false);
                AssetDatabase.CreateAsset(asset, DarkPath);
                Debug.Log("[UISystem] Created DefaultDark.asset");
                changed = true;
            }

            return changed;
        }

        private static ThemeData CreateThemeData(bool isLight)
        {
            var asset = ScriptableObject.CreateInstance<ThemeData>();

            // Inject serialized fields via reflection (fields are private, SO)
            SetField(asset, "_colors",          isLight ? LightPalette() : DarkPalette());
            SetField(asset, "_elevationPresets", BuildElevationPresets());
            SetField(asset, "_shapes",           ShapePresets.Default);
            SetField(asset, "_motionPresets",    BuildMotionPresets());

            return asset;
        }

        // ------------------------------------------------------------------ //
        //  Typography asset                                                    //
        // ------------------------------------------------------------------ //
        private static bool EnsureTypographyAsset()
        {
            EnsureFolder(TypographyFolder);

            if (AssetDatabase.LoadAssetAtPath<TypographyConfig>(TypoPath) != null) return false;

            var asset = ScriptableObject.CreateInstance<TypographyConfig>();

            // Font sizes in reference-resolution pixels (1080x1920 at 420 dpi ≈ 2.625× scale)
            // M3 sp * 2.625 ≈ ref-px
            SetField(asset, "_display",  BuildTextStyle(size: 96f));
            SetField(asset, "_headline", BuildTextStyle(size: 73f));
            SetField(asset, "_title",    BuildTextStyle(size: 58f));
            SetField(asset, "_body",     BuildTextStyle(size: 42f));
            SetField(asset, "_label",    BuildTextStyle(size: 37f));
            SetField(asset, "_caption",  BuildTextStyle(size: 32f));

            AssetDatabase.CreateAsset(asset, TypoPath);
            Debug.Log("[UISystem] Created DefaultTypography.asset. Assign TMP font assets from Assets/UISystem/Assets/Typography/Fonts/");
            return true;
        }

        // ------------------------------------------------------------------ //
        //  Data builders                                                       //
        // ------------------------------------------------------------------ //
        private static ColorPalette LightPalette() => new ColorPalette
        {
            Primary              = Hex("#6750A4"),
            OnPrimary            = Hex("#FFFFFF"),
            PrimaryContainer     = Hex("#EADDFF"),
            OnPrimaryContainer   = Hex("#21005D"),
            Secondary            = Hex("#625B71"),
            OnSecondary          = Hex("#FFFFFF"),
            SecondaryContainer   = Hex("#E8DEF8"),
            OnSecondaryContainer = Hex("#1D192B"),
            Surface              = Hex("#FFFBFE"),
            OnSurface            = Hex("#1C1B1F"),
            SurfaceVariant       = Hex("#E7E0EC"),
            OnSurfaceVariant     = Hex("#49454F"),
            Error                = Hex("#B3261E"),
            OnError              = Hex("#FFFFFF"),
            Outline              = Hex("#79747E"),
            OutlineVariant       = Hex("#CAC4D0"),
            Background           = Hex("#FFFBFE"),
        };

        private static ColorPalette DarkPalette() => new ColorPalette
        {
            Primary              = Hex("#D0BCFF"),
            OnPrimary            = Hex("#381E72"),
            PrimaryContainer     = Hex("#4F378B"),
            OnPrimaryContainer   = Hex("#EADDFF"),
            Secondary            = Hex("#CCC2DC"),
            OnSecondary          = Hex("#332D41"),
            SecondaryContainer   = Hex("#4A4458"),
            OnSecondaryContainer = Hex("#E8DEF8"),
            Surface              = Hex("#1C1B1F"),
            OnSurface            = Hex("#E6E1E5"),
            SurfaceVariant       = Hex("#49454F"),
            OnSurfaceVariant     = Hex("#CAC4D0"),
            Error                = Hex("#F2B8B5"),
            OnError              = Hex("#601410"),
            Outline              = Hex("#938F99"),
            OutlineVariant       = Hex("#49454F"),
            Background           = Hex("#1C1B1F"),
        };

        private static ElevationPreset[] BuildElevationPresets() => new[]
        {
            new ElevationPreset { ShadowOffset = Vector2.zero,        ShadowBlur = 0f,  ShadowColor = new Color(0,0,0,0f),    TonalOverlayAlpha = 0f },
            new ElevationPreset { ShadowOffset = new Vector2(0,-2f),  ShadowBlur = 4f,  ShadowColor = new Color(0,0,0,0.12f), TonalOverlayAlpha = 0.05f },
            new ElevationPreset { ShadowOffset = new Vector2(0,-4f),  ShadowBlur = 8f,  ShadowColor = new Color(0,0,0,0.16f), TonalOverlayAlpha = 0.08f },
            new ElevationPreset { ShadowOffset = new Vector2(0,-6f),  ShadowBlur = 12f, ShadowColor = new Color(0,0,0,0.20f), TonalOverlayAlpha = 0.11f },
            new ElevationPreset { ShadowOffset = new Vector2(0,-8f),  ShadowBlur = 16f, ShadowColor = new Color(0,0,0,0.24f), TonalOverlayAlpha = 0.12f },
            new ElevationPreset { ShadowOffset = new Vector2(0,-12f), ShadowBlur = 24f, ShadowColor = new Color(0,0,0,0.30f), TonalOverlayAlpha = 0.14f },
        };

        private static MotionPreset[] BuildMotionPresets()
        {
            var linear  = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var easeOut = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));
            return new[]
            {
                new MotionPreset { Curve = easeOut, DurationMs = 500f }, // Emphasized
                new MotionPreset { Curve = linear,  DurationMs = 300f }, // Standard
                new MotionPreset { Curve = easeOut, DurationMs = 400f }, // EmphasizedDecelerate
                new MotionPreset { Curve = linear,  DurationMs = 200f }, // StandardDecelerate
            };
        }

        private static TextStyle BuildTextStyle(float size) => new TextStyle
        {
            FontSize    = size,
            LineSpacing = 0f,
            CharSpacing = 0f,
        };

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //
        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string child  = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
