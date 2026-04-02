using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Wires the "btn-switch-theme" VisualElement in FoundationDemo.uxml to
    /// ThemeManager.ToggleLightDark(). Place on the UIDocument GameObject.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ThemeSwitchButton : MonoBehaviour
    {
        private UIDocument _doc;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            // Register this panel with ThemeManager so light/dark USS is applied to rootVisualElement.
            if (ThemeManager.Instance != null)
                ThemeManager.Instance.RegisterPanel(_doc);

            // rootVisualElement is guaranteed to be ready by Start() — not by OnEnable().
            var root = _doc?.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[UISystem] ThemeSwitchButton: rootVisualElement is null in Start().", this);
                return;
            }

            var btn = root.Q("btn-switch-theme");
            if (btn == null)
            {
                Debug.LogWarning("[UISystem] ThemeSwitchButton: 'btn-switch-theme' element not found.", this);
                return;
            }

            btn.RegisterCallback<ClickEvent>(OnClick);
        }

        private void OnDisable()
        {
            var root = _doc?.rootVisualElement;
            if (root == null) return;

            var btn = root.Q("btn-switch-theme");
            btn?.UnregisterCallback<ClickEvent>(OnClick);
        }

        private void OnDestroy()
        {
            ThemeManager.Instance?.UnregisterPanel(_doc);
        }

        private void OnClick(ClickEvent _)
        {
            if (ThemeManager.Instance == null)
            {
                Debug.LogWarning("[UISystem] ThemeSwitchButton: ThemeManager.Instance is null.", this);
                return;
            }
            ThemeManager.Instance.ToggleLightDark();
        }
    }
}
