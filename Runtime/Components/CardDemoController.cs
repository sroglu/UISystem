using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// MonoBehaviour controller for CardDemo scene.
    /// - Switch Theme button click → ThemeManager.ToggleLightDark()
    /// - Clickable card clicks → Debug.Log feedback
    /// Attach to the UIDocument GameObject in the CardDemo scene.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CardDemoController : MonoBehaviour
    {
        private const string M3CardsUrl = "https://m3.material.io/components/cards/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            // Register panel with ThemeManager for light/dark switching
            ThemeManager.Instance?.RegisterPanel(doc);

            // M3 reference link — pointer-up on label opens spec in browser.
            // PointerUpEvent is used instead of ClickEvent because ClickEvent synthesis
            // can be blocked by the sibling ScrollView's touch drag handling.
            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ =>
                {
                    Debug.Log("[CardDemo] M3 reference link clicked");
                    Application.OpenURL(M3CardsUrl);
                });
            }

            // Theme switch button
            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.Instance?.ToggleLightDark();

            // Clickable cards — log feedback
            WireClickableCard(root, "card-clickable-elevated", "Elevated Card clicked");
            WireClickableCard(root, "card-clickable-filled",   "Filled Card clicked");
        }

        private static void WireClickableCard(VisualElement root, string name, string message)
        {
            var card = root.Q<M3Card>(name);
            if (card != null)
                card.OnClick += () => Debug.Log($"[CardDemo] {message}");
        }
    }
}
