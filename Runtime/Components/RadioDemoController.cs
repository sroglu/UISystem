using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class RadioDemoController : MonoBehaviour
    {
        private const string M3RadioUrl = "https://m3.material.io/components/radio-button/overview";

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
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3RadioUrl));
            }

            // Theme switch
            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.ToggleLightDark();

            // Interactive group
            var rb0   = root.Q<M3RadioButton>("rb-group-0");
            var rb1   = root.Q<M3RadioButton>("rb-group-1");
            var rb2   = root.Q<M3RadioButton>("rb-group-2");
            var label = root.Q<Label>("rb-group-label");

            if (rb0 != null && rb1 != null && rb2 != null)
            {
                var group = new M3RadioGroup();
                group.Add(rb0);
                group.Add(rb1);
                group.Add(rb2);
                group.SelectedIndex = 0;

                string[] options = { "Option A", "Option B", "Option C" };
                group.OnSelectionChanged += idx =>
                {
                    if (label != null)
                        label.text = $"Selected: {(idx >= 0 ? options[idx] : "None")}";
                    Debug.Log($"[RadioDemo] Selected index: {idx}");
                };
            }
        }
    }
}
