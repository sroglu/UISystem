using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class ChipDemoController : MonoBehaviour
    {
        private const string M3ChipUrl = "https://m3.material.io/components/chips/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            ThemeManager.RegisterPanel(doc);

            // M3 reference link
            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target   = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3ChipUrl));
            }

            // Theme switch
            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.ToggleLightDark();

            // Input chip remove events
            var removeLabel = root.Q<Label>("chip-remove-label");

            WireInputChip(root, "chip-input-1", "Kotlin", removeLabel);
            WireInputChip(root, "chip-input-2", "Swift", removeLabel);
            WireInputChip(root, "chip-input-3", "Dart", removeLabel);

            // Assist chip click
            var assistChip1 = root.Q<M3Chip>("chip-assist-1");
            if (assistChip1 != null)
                assistChip1.OnClick += () => Debug.Log("[ChipDemo] Add to calendar clicked");
        }

        private void WireInputChip(VisualElement root, string name, string chipName, Label removeLabel)
        {
            var chip = root.Q<M3Chip>(name);
            if (chip == null) return;
            chip.OnRemove += () =>
            {
                chip.style.display = DisplayStyle.None;
                if (removeLabel != null)
                    removeLabel.text = $"Removed: {chipName}";
                Debug.Log($"[ChipDemo] Removed chip: {chipName}");
            };
        }
    }
}
