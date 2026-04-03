using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class SliderDemoController : MonoBehaviour
    {
        private const string M3SliderUrl = "https://m3.material.io/components/sliders/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            ThemeManager.Instance?.RegisterPanel(doc);

            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target   = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3SliderUrl));
            }

            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.Instance?.ToggleLightDark();

            // Continuous slider
            var continuous      = root.Q<M3Slider>("slider-continuous");
            var continuousLabel = root.Q<Label>("slider-continuous-label");
            if (continuous != null && continuousLabel != null)
            {
                continuousLabel.text = $"Value: {continuous.Value:F2}";
                continuous.OnValueChanged += v =>
                {
                    continuousLabel.text = $"Value: {v:F2}";
                    Debug.Log($"[SliderDemo] Continuous: {v:F2}");
                };
            }

            // Stepped slider
            var stepped      = root.Q<M3Slider>("slider-stepped");
            var steppedLabel = root.Q<Label>("slider-stepped-label");
            if (stepped != null && steppedLabel != null)
            {
                steppedLabel.text = $"Value: {stepped.Value:F0}";
                stepped.OnValueChanged += v =>
                {
                    steppedLabel.text = $"Value: {v:F0}";
                    Debug.Log($"[SliderDemo] Stepped: {v:F0}");
                };
            }
        }
    }
}
