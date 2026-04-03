using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class NavigationBarDemoController : MonoBehaviour
    {
        private const string M3NavUrl = "https://m3.material.io/components/navigation-bar/overview";

        private readonly string[] _tabNames = { "Home", "Search", "Settings" };

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
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3NavUrl));
            }

            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.Instance?.ToggleLightDark();

            var navBar       = root.Q<M3NavigationBar>("nav-bar");
            var selectedLabel = root.Q<Label>("nav-selected-label");

            if (navBar != null)
            {
                navBar.OnItemSelected += idx =>
                {
                    if (selectedLabel != null && idx >= 0 && idx < _tabNames.Length)
                        selectedLabel.text = _tabNames[idx];
                    Debug.Log($"[NavigationBarDemo] Selected: {(idx >= 0 ? _tabNames[idx] : "none")}");
                };
            }
        }
    }
}
