using System;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Dialog component.
    ///
    /// Structure:
    ///   VisualElement (scrim)    — full-screen overlay, rgba(0,0,0,0.32)
    ///   SDFRectElement (dialog)  — 280-560dp centered card
    ///     Label (headline)
    ///     Label (body)
    ///     VisualElement (actions) — Dismiss + Confirm buttons
    ///
    /// M3 Spec:
    ///   Width: 280-560dp, corners: 28dp
    ///   Elevation: Level 3
    ///   Colors: surface-container-high bg, on-surface headline, on-surface-variant body
    ///
    /// Usage:
    ///   var dialog = new M3Dialog { Headline = "Title", Body = "Message" };
    ///   dialog.OnConfirm += () => ...;
    ///   dialog.Show(root);
    /// </summary>
    public class M3Dialog : M3ComponentBase
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string ScrimClass    = "m3-dialog__scrim";
        private const string DialogClass   = "m3-dialog";
        private const string HeadlineClass = "m3-dialog__headline";
        private const string BodyClass     = "m3-dialog__body";
        private const string ActionsClass  = "m3-dialog__actions";

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly VisualElement  _scrim;
        private readonly SDFRectElement _card;
        private readonly Label          _headlineLabel;
        private readonly Label          _bodyLabel;
        private readonly VisualElement  _actions;
        private readonly M3Button       _dismissBtn;
        private readonly M3Button       _confirmBtn;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private string _headline     = string.Empty;
        private string _body         = string.Empty;
        private string _confirmText  = "OK";
        private string _dismissText  = "Cancel";
        private bool   _dismissable  = true;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        public event Action OnConfirm;
        public event Action OnDismiss;

        public string Headline
        {
            get => _headline;
            set { _headline = value; _headlineLabel.text = value ?? string.Empty; }
        }

        public string Body
        {
            get => _body;
            set { _body = value; _bodyLabel.text = value ?? string.Empty; }
        }

        public string ConfirmText
        {
            get => _confirmText;
            set { _confirmText = value; _confirmBtn.Text = value ?? string.Empty; }
        }

        public string DismissText
        {
            get => _dismissText;
            set { _dismissText = value; _dismissBtn.Text = value ?? string.Empty; }
        }

        public bool Dismissable
        {
            get => _dismissable;
            set => _dismissable = value;
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3Dialog()
        {
            // --- Scrim (full-screen overlay) ---
            _scrim = new VisualElement();
            _scrim.AddToClassList(ScrimClass);
            _scrim.style.position        = Position.Absolute;
            _scrim.style.top             = 0;
            _scrim.style.left            = 0;
            _scrim.style.right           = 0;
            _scrim.style.bottom          = 0;
            _scrim.style.justifyContent  = Justify.Center;
            _scrim.style.alignItems      = Align.Center;
            _scrim.pickingMode           = PickingMode.Position;
            _scrim.RegisterCallback<ClickEvent>(OnScrimClicked);

            // --- Dialog card ---
            _card = new SDFRectElement { CornerRadius = 28f, pickingMode = PickingMode.Position };
            _card.AddToClassList(DialogClass);
            _card.style.borderTopLeftRadius     = 28f;
            _card.style.borderTopRightRadius    = 28f;
            _card.style.borderBottomLeftRadius  = 28f;
            _card.style.borderBottomRightRadius = 28f;
            _card.style.minWidth   = 280f;
            _card.style.maxWidth   = 560f;
            _card.style.paddingTop    = 24f;
            _card.style.paddingBottom = 24f;
            _card.style.paddingLeft   = 24f;
            _card.style.paddingRight  = 24f;
            _card.style.flexDirection = FlexDirection.Column;
            // M3 elevation level 3
            _card.ShadowBlur    = 6f;
            _card.ShadowOffsetY = 2f;
            _card.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            // --- Headline ---
            _headlineLabel = new Label(string.Empty);
            _headlineLabel.AddToClassList("m3-headline");
            _headlineLabel.AddToClassList(HeadlineClass);
            _headlineLabel.style.marginBottom = 16f;
            _card.Add(_headlineLabel);

            // --- Body ---
            _bodyLabel = new Label(string.Empty);
            _bodyLabel.AddToClassList("m3-body");
            _bodyLabel.AddToClassList(BodyClass);
            _bodyLabel.style.marginBottom  = 24f;
            _bodyLabel.style.whiteSpace    = WhiteSpace.Normal;
            _card.Add(_bodyLabel);

            // --- Actions (right-aligned) ---
            _actions = new VisualElement();
            _actions.AddToClassList(ActionsClass);
            _actions.style.flexDirection = FlexDirection.Row;
            _actions.style.justifyContent = Justify.FlexEnd;
            // M3 spec: 8dp gap — applied as marginLeft on confirm button

            _dismissBtn = new M3Button { Text = _dismissText };
            _dismissBtn.Variant = ButtonVariant.Text;
            _dismissBtn.OnClick += () =>
            {
                OnDismiss?.Invoke();
                Close();
            };

            _confirmBtn = new M3Button { Text = _confirmText };
            _confirmBtn.Variant = ButtonVariant.Text;
            _confirmBtn.style.marginLeft = 8f; // M3 spec: 8dp gap between action buttons
            _confirmBtn.OnClick += () =>
            {
                OnConfirm?.Invoke();
                Close();
            };

            _actions.Add(_dismissBtn);
            _actions.Add(_confirmBtn);
            _card.Add(_actions);

            _scrim.Add(_card);
        }

        // ------------------------------------------------------------------ //
        //  Show / Close                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>Add the dialog scrim to a parent element.</summary>
        public void Show(VisualElement parent)
        {
            if (_scrim.parent != null)
                _scrim.RemoveFromHierarchy();
            parent.Add(_scrim);
        }

        /// <summary>Remove the dialog from the hierarchy.</summary>
        public void Close()
        {
            _scrim.RemoveFromHierarchy();
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers                                                      //
        // ------------------------------------------------------------------ //

        private void OnScrimClicked(ClickEvent evt)
        {
            if (!_dismissable) return;
            OnDismiss?.Invoke();
            Close();
        }


    }
}
