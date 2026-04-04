using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class TextFieldDemoController : MonoBehaviour
    {
        private const string M3TextFieldUrl = "https://m3.material.io/components/text-fields/overview";

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
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3TextFieldUrl));
            }

            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.ToggleLightDark();

            var tf1 = root.Q<M3TextField>("tf-filled-1");
            if (tf1 != null)
                tf1.OnValueChanged += v => Debug.Log($"[TextFieldDemo] Filled 1: {v}");

            var tf4 = root.Q<M3TextField>("tf-outlined-1");
            if (tf4 != null)
                tf4.OnSubmit += v => Debug.Log($"[TextFieldDemo] Submit: {v}");
        }
    }
}
