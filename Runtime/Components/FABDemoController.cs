using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class FABDemoController : MonoBehaviour
    {
        private const string M3FABUrl = "https://m3.material.io/components/floating-action-button/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            ThemeManager.RegisterPanel(doc);

            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target   = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3FABUrl));
            }

            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.ToggleLightDark();

            var clickLabel = root.Q<Label>("fab-click-label");

            WireFAB(root, "fab-small",     "Small FAB",    clickLabel);
            WireFAB(root, "fab-regular",   "Regular FAB",  clickLabel);
            WireFAB(root, "fab-large",     "Large FAB",    clickLabel);
            WireFAB(root, "fab-extended",  "New message",  clickLabel);
            WireFAB(root, "fab-extended-2","Compose",      clickLabel);
        }

        private void WireFAB(VisualElement root, string name, string label, Label clickLabel)
        {
            var fab = root.Q<M3FAB>(name);
            if (fab == null) return;
            fab.OnClick += () =>
            {
                if (clickLabel != null) clickLabel.text = $"Tapped: {label}";
                Debug.Log($"[FABDemo] {label} clicked");
            };
        }
    }
}
