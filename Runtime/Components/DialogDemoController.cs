using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    [RequireComponent(typeof(UIDocument))]
    public class DialogDemoController : MonoBehaviour
    {
        private const string M3DialogUrl = "https://m3.material.io/components/dialogs/overview";

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
                target.RegisterCallback<PointerUpEvent>(_ => Application.OpenURL(M3DialogUrl));
            }

            var switchBtn = root.Q<M3Button>("btn-switch-theme");
            if (switchBtn != null)
                switchBtn.OnClick += () => ThemeManager.ToggleLightDark();

            var resultLabel = root.Q<Label>("dialog-result-label");

            // Basic dialog
            var btnBasic = root.Q<M3Button>("btn-dialog-basic");
            if (btnBasic != null)
            {
                btnBasic.OnClick += () =>
                {
                    var dialog = new M3Dialog
                    {
                        Headline    = "Delete item?",
                        Body        = "This action will permanently delete the selected item and cannot be undone.",
                        ConfirmText = "Delete",
                        DismissText = "Cancel",
                        Dismissable = true,
                    };
                    dialog.OnConfirm += () =>
                    {
                        if (resultLabel != null) resultLabel.text = "Action: Confirmed (Delete)";
                        Debug.Log("[DialogDemo] Confirmed");
                    };
                    dialog.OnDismiss += () =>
                    {
                        if (resultLabel != null) resultLabel.text = "Action: Dismissed";
                        Debug.Log("[DialogDemo] Dismissed");
                    };
                    dialog.Show(root);
                };
            }

            // Non-dismissable dialog
            var btnNoDismiss = root.Q<M3Button>("btn-dialog-no-dismiss");
            if (btnNoDismiss != null)
            {
                btnNoDismiss.OnClick += () =>
                {
                    var dialog = new M3Dialog
                    {
                        Headline    = "Save changes?",
                        Body        = "You have unsaved changes. Would you like to save before leaving?",
                        ConfirmText = "Save",
                        DismissText = "Discard",
                        Dismissable = false,
                    };
                    dialog.OnConfirm += () =>
                    {
                        if (resultLabel != null) resultLabel.text = "Action: Saved";
                        Debug.Log("[DialogDemo] Saved");
                    };
                    dialog.OnDismiss += () =>
                    {
                        if (resultLabel != null) resultLabel.text = "Action: Discarded";
                        Debug.Log("[DialogDemo] Discarded");
                    };
                    dialog.Show(root);
                };
            }
        }
    }
}
