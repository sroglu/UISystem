using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class SnackbarDemoController : MonoBehaviour
    {
        private const string M3SnackbarUrl = "https://m3.material.io/components/snackbar/overview";

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            ThemeManager.Instance?.RegisterPanel(doc);

            // Set up snackbar manager root
            var manager = M3SnackbarManager.Instance;
            if (manager != null)
                manager.SetRoot(root);

            var refPanel = root.Q<VisualElement>("m3-reference");
            if (refPanel != null)
            {
                refPanel.pickingMode = PickingMode.Position;
                var refLabel = refPanel.Q<Label>();
                var target   = refLabel ?? (VisualElement)refPanel;
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3SnackbarUrl));
            }

            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.Instance?.ToggleLightDark();

            // Simple message
            var btnSimple = root.Q<M3Button>("btn-snack-simple");
            if (btnSimple != null)
                btnSimple.OnClick += () => ShowSnackbar(root, "Photo saved", null, null);

            // With action
            var btnAction = root.Q<M3Button>("btn-snack-action");
            if (btnAction != null)
                btnAction.OnClick += () => ShowSnackbar(root, "Email archived", "Undo",
                    () => Debug.Log("[SnackbarDemo] Undo clicked"));

            // With close button
            var btnClose = root.Q<M3Button>("btn-snack-close");
            if (btnClose != null)
                btnClose.OnClick += () =>
                {
                    var sb = new M3Snackbar
                    {
                        Text      = "Connection timed out",
                        ShowClose = true,
                    };
                    sb.Show(root);
                };

            // Queue 3
            var btnQueue = root.Q<M3Button>("btn-snack-queue");
            if (btnQueue != null)
            {
                btnQueue.OnClick += () =>
                {
                    ShowSnackbar(root, "First message", null, null, 2000);
                    ShowSnackbar(root, "Second message", "Retry", () => Debug.Log("[SnackbarDemo] Retry"), 2000);
                    ShowSnackbar(root, "Third message", null, null, 2000);
                };
            }
        }

        private void ShowSnackbar(VisualElement root, string text, string actionText, System.Action onAction, int duration = 4000)
        {
            if (M3SnackbarManager.Instance != null)
            {
                M3SnackbarManager.Instance.Show(text, actionText, onAction, duration);
            }
            else
            {
                // Fallback: show directly
                var sb = new M3Snackbar
                {
                    Text       = text,
                    ActionText = actionText ?? string.Empty,
                    DurationMs = duration,
                };
                if (onAction != null)
                    sb.OnAction += onAction;
                sb.Show(root);
            }
        }
    }
}
