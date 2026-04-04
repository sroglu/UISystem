using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// MonoBehaviour controller for ToggleDemo scene.
    /// - Interactive toggle updates its label
    /// - Switch Theme button → ThemeManager.ToggleLightDark()
    /// - M3 reference link → opens spec in browser
    /// Attach to the UIDocument GameObject in the ToggleDemo scene.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ToggleDemoController : MonoBehaviour
    {
        private const string M3SwitchUrl = "https://m3.material.io/components/switch/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            // Register panel with ThemeManager for light/dark switching
            ThemeManager.RegisterPanel(doc);

            // M3 reference link
            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ =>
                {
                    Debug.Log("[ToggleDemo] M3 reference link clicked");
                    Application.OpenURL(M3SwitchUrl);
                });
            }

            // Theme switch button
            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.ToggleLightDark();

            // Interactive toggle — update label on value change
            var interactiveToggle = root.Q<M3Toggle>("toggle-interactive");
            var interactiveLabel  = root.Q<Label>("toggle-interactive-label");
            if (interactiveToggle != null && interactiveLabel != null)
            {
                interactiveToggle.OnValueChanged += val =>
                {
                    interactiveLabel.text = val ? "Toggle is ON" : "Toggle is OFF";
                    Debug.Log($"[ToggleDemo] Interactive toggle: {val}");
                };
            }
        }
    }
}
