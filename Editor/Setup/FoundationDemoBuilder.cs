using mehmetsrl.UISystem;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace mehmetsrl.UISystem.Editor
{
    /// <summary>
    /// Builds the FoundationDemo sample scene.
    /// Run via: Assets > UISystem > Build Foundation Demo Scene
    /// Output:  Assets/UISystem/Samples~/Foundation/FoundationDemo.unity
    /// </summary>
    public static class FoundationDemoBuilder
    {
        private const string ScenePath = "Assets/UISystem/Samples~/Foundation/FoundationDemo.unity";

        [MenuItem("Assets/UISystem/Build Foundation Demo Scene")]
        public static void Build()
        {
            // ── Ensure folder exists ──────────────────────────────────────
            const string folder = "Assets/UISystem/Samples~/Foundation";
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            // ── New empty scene ───────────────────────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── [UISystem] root + ThemeManager ────────────────────────────
            var root        = CreateGO("[UISystem]", null);
            var themeManager = root.AddComponent<ThemeManager>();

            var lightTheme = AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/UISystem/Assets/Themes/DefaultLight.asset");
            var darkTheme  = AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/UISystem/Assets/Themes/DefaultDark.asset");
            var typoConfig = AssetDatabase.LoadAssetAtPath<TypographyConfig>("Assets/UISystem/Assets/Typography/DefaultTypography.asset");

            const System.Reflection.BindingFlags Bf =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var tmType = typeof(ThemeManager);
            tmType.GetField("_lightTheme",       Bf)?.SetValue(themeManager, lightTheme);
            tmType.GetField("_darkTheme",        Bf)?.SetValue(themeManager, darkTheme);
            tmType.GetField("_activeTheme",      Bf)?.SetValue(themeManager, lightTheme);
            tmType.GetField("_typographyConfig", Bf)?.SetValue(themeManager, typoConfig);

            // ── Canvas (1080×1920, TexCoord1+TexCoord2) ───────────────────
            var canvasGO = CreateGO("Canvas", null);
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── EventSystem ───────────────────────────────────────────────
            var esGO = CreateGO("EventSystem", null);
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();

            // ── Background (full-screen, no rounding) ─────────────────────
            var bg   = MakeSDF(canvasGO.transform, "Background", ColorRole.Background, Vector4.zero, 0, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // ── DemoPanel (Surface, r=48, elev=2) ─────────────────────────
            var panel = MakeSDF(canvasGO.transform, "DemoPanel", ColorRole.Surface,
                                new Vector4(48, 48, 48, 48), 2, true);
            SetCenter(panel.GetComponent<RectTransform>(), new Vector2(800, 1200), Vector2.zero);

            // ── Card 1 (SurfaceVariant, r=32, elev=1) + Title ─────────────
            var card1 = MakeSDF(canvasGO.transform, "Card1", ColorRole.SurfaceVariant,
                                new Vector4(32, 32, 32, 32), 1, true);
            SetCenter(card1.GetComponent<RectTransform>(), new Vector2(680, 200), new Vector2(0f, 200f));
            var titleGO = MakeTMP(card1.transform, "TitleText", TextRole.Title, "UISystem Foundation");
            FillRT(titleGO.GetComponent<RectTransform>(), 24f, 12f);

            // ── Card 2 (PrimaryContainer, r=32, elev=1) + Body ────────────
            var card2 = MakeSDF(canvasGO.transform, "Card2", ColorRole.PrimaryContainer,
                                new Vector4(32, 32, 32, 32), 1, true);
            SetCenter(card2.GetComponent<RectTransform>(), new Vector2(680, 200), new Vector2(0f, -30f));
            var bodyGO = MakeTMP(card2.transform, "BodyText", TextRole.Body,
                                 "SDF Shader · Theme System · Typography");
            FillRT(bodyGO.GetComponent<RectTransform>(), 24f, 12f);

            // ── Typography showcase ───────────────────────────────────────
            var typoParent = CreateGO("TypographyShowcase", canvasGO.transform);
            var typoRT     = typoParent.AddComponent<RectTransform>();
            SetCenter(typoRT, new Vector2(680f, 700f), new Vector2(0f, -480f));
            var vl = typoParent.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6f;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childAlignment         = TextAnchor.UpperLeft;
            vl.padding                = new RectOffset(16, 16, 16, 16);

            var roles  = new[] { TextRole.Display, TextRole.Headline, TextRole.Title,
                                  TextRole.Body,    TextRole.Label,    TextRole.Caption };
            var labels = new[] { "Display — UISystem Foundation",
                                  "Headline — Theme + Typography",
                                  "Title — SDF Rounded Rect",
                                  "Body — per-corner radius, shadow, outline. Material batching per elevation level.",
                                  "Label — Button · Caption",
                                  "Caption — 12 sp · helper text · timestamps" };

            for (int i = 0; i < roles.Length; i++)
            {
                var t = MakeTMP(typoParent.transform, roles[i] + "Text", roles[i], labels[i]);
                t.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // ── Switch Theme button (SDFRectGraphic pill shape) ───────────
            var btnGO = MakeSDF(canvasGO.transform, "SwitchThemeButton", ColorRole.Primary,
                                new Vector4(9999f, 9999f, 9999f, 9999f), 1, true);
            SetCenter(btnGO.GetComponent<RectTransform>(), new Vector2(400f, 96f), new Vector2(0f, -900f));

            var btn        = btnGO.AddComponent<Button>();
            var switcher   = btnGO.AddComponent<ThemeSwitchButton>();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                btn.onClick, switcher.SwitchTheme);

            var btnLabel = MakeTMP(btnGO.transform, "ButtonLabel", TextRole.Label, "Switch Theme");
            btnLabel.GetComponent<TextMeshProUGUI>().color = Color.white;
            FillRT(btnLabel.GetComponent<RectTransform>(), 0f, 0f);

            // ── Save ──────────────────────────────────────────────────────
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            if (saved)
                Debug.Log($"[UISystem] FoundationDemo.unity saved to {ScenePath}");
            else
                Debug.LogError("[UISystem] FoundationDemo.unity FAILED to save!");
        }

        // ── Factories ─────────────────────────────────────────────────────

        private static GameObject CreateGO(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject MakeSDF(Transform parent, string name, ColorRole role,
                                          Vector4 radii, int elev, bool shadow)
        {
            var go  = CreateGO(name, parent);
            go.AddComponent<RectTransform>();
            var sdf = go.AddComponent<SDFRectGraphic>();

            const System.Reflection.BindingFlags Bf =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = typeof(SDFRectGraphic);
            t.GetField("_useThemeColor",  Bf)?.SetValue(sdf, true);
            t.GetField("_baseColorRole",  Bf)?.SetValue(sdf, role);
            t.GetField("_cornerRadius",   Bf)?.SetValue(sdf, radii);
            t.GetField("_elevationLevel", Bf)?.SetValue(sdf, elev);
            t.GetField("_shadowEnabled",  Bf)?.SetValue(sdf, shadow);
            return go;
        }

        private static GameObject MakeTMP(Transform parent, string name, TextRole role, string text)
        {
            var go      = CreateGO(name, parent);
            go.AddComponent<RectTransform>();
            var tmp     = go.AddComponent<TextMeshProUGUI>();
            tmp.text    = text;
            tmp.color   = Color.black;
            tmp.enableWordWrapping = true;

            var resolver = go.AddComponent<TypographyResolver>();
            const System.Reflection.BindingFlags Bf =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(TypographyResolver).GetField("_role", Bf)?.SetValue(resolver, role);
            return go;
        }

        private static void SetCenter(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = pos;
        }

        private static void FillRT(RectTransform rt, float hPad, float vPad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(hPad,  vPad);
            rt.offsetMax = new Vector2(-hPad, -vPad);
        }
    }
}
