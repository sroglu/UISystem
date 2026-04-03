using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class CheckboxDemoController : MonoBehaviour
    {
        private const string M3CheckboxUrl = "https://m3.material.io/components/checkbox/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            ThemeManager.Instance?.RegisterPanel(doc);

            // M3 reference link
            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3CheckboxUrl));
            }

            // Theme switch
            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.Instance?.ToggleLightDark();

            // Interactive checkbox
            var cb    = root.Q<M3Checkbox>("cb-interactive");
            var label = root.Q<Label>("cb-interactive-label");
            if (cb != null && label != null)
            {
                cb.OnStateChanged += state =>
                {
                    label.text = state.ToString();
                    Debug.Log($"[CheckboxDemo] Interactive: {state}");
                };
            }
        }
    }
}
