using System;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Menu Item — a single entry within an M3Menu.
    ///
    /// Composition:
    ///   VisualElement (this) — menu item row
    ///   Label (_leadingIcon) — optional leading icon
    ///   Label (_label) — item text
    ///   Label (_trailingIcon) — optional trailing icon / shortcut hint
    ///
    /// USS: menu.uss. Colors via var(--m3-*) tokens.
    /// </summary>
    [UxmlElement]
    public partial class M3MenuItem : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        internal const string BaseClass         = "m3-menu-item";
        internal const string DisabledClass     = "m3-menu-item--disabled";
        internal const string LeadingIconClass  = "m3-menu-item__leading";
        internal const string LabelClass        = "m3-menu-item__label";
        internal const string TrailingIconClass = "m3-menu-item__trailing";

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly Label _leadingIcon;
        private readonly Label _label;
        private readonly Label _trailingIcon;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private string _labelText = string.Empty;
        private bool   _disabled;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when the menu item is clicked (not fired when disabled).</summary>
        public event Action OnClick;

        /// <summary>Menu item display label.</summary>
        [UxmlAttribute("label")]
        public string LabelText
        {
            get => _labelText;
            set
            {
                _labelText  = value ?? string.Empty;
                _label.text = _labelText;
            }
        }

        /// <summary>Optional leading icon (MaterialSymbols codepoint).</summary>
        [UxmlAttribute("leading-icon")]
        public string LeadingIcon
        {
            get => _leadingIcon.text;
            set
            {
                _leadingIcon.text         = value ?? string.Empty;
                _leadingIcon.style.display = string.IsNullOrEmpty(value)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }

        /// <summary>Optional trailing icon or shortcut label.</summary>
        [UxmlAttribute("trailing-icon")]
        public string TrailingIcon
        {
            get => _trailingIcon.text;
            set
            {
                _trailingIcon.text         = value ?? string.Empty;
                _trailingIcon.style.display = string.IsNullOrEmpty(value)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }

        /// <summary>Whether this item is non-interactive.</summary>
        [UxmlAttribute("disabled")]
        public new bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                EnableInClassList(DisabledClass, _disabled);
                pickingMode = _disabled ? PickingMode.Ignore : PickingMode.Position;
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3MenuItem()
        {
            AddToClassList(BaseClass);
            pickingMode = PickingMode.Position;
            focusable   = true;

            _leadingIcon = new Label();
            _leadingIcon.AddToClassList("m3-icon");
            _leadingIcon.AddToClassList(LeadingIconClass);
            _leadingIcon.style.display = DisplayStyle.None;

            _label = new Label(_labelText);
            _label.AddToClassList(LabelClass);
            _label.AddToClassList("m3-body-large");

            _trailingIcon = new Label();
            _trailingIcon.AddToClassList("m3-icon");
            _trailingIcon.AddToClassList(TrailingIconClass);
            _trailingIcon.style.display = DisplayStyle.None;

            Add(_leadingIcon);
            Add(_label);
            Add(_trailingIcon);

            RegisterCallback<ClickEvent>(_ =>
            {
                if (!_disabled) OnClick?.Invoke();
            });
        }
    }
}
