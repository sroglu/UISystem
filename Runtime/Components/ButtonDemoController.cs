using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// MonoBehaviour controller for ButtonDemo scene.
    /// - M3 reference panel click → opens m3.material.io/components/buttons/overview
    /// - Switch Theme button click → ThemeManager.ToggleLightDark()
    /// Attach to the UIDocument GameObject in the ButtonDemo scene.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ButtonDemoController : MonoBehaviour
    {
        private const string M3ButtonsUrl = "https://m3.material.io/components/buttons/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

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
                    Debug.Log("[ButtonDemo] M3 reference link clicked");
                    Application.OpenURL(M3ButtonsUrl);
                });
            }
        }
    }
}
