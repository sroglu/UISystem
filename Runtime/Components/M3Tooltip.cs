using System;
using mehmetsrl.UISystem.Core;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Tooltip — informational overlay triggered by hover.
    ///
    /// Variants:
    ///   Plain: text only (body medium, single line)
    ///   Rich: title + body text
    ///
    /// Composition:
    ///   VisualElement (this) — tooltip surface
    ///   Label (_title) — title (Rich variant only)
    ///   Label (_body) — tooltip text
    ///
    /// M3 spec:
    ///   Background: --m3-inverse-surface
    ///   Text: --m3-inverse-on-surface
    ///   Corner radius: 4dp
    ///   Appear after: 500ms hover delay
    ///   Auto-positioned above/below anchor
    ///
    /// Usage:
    ///   var tip = new M3Tooltip { Body = "This action saves your work." };
    ///   tip.Attach(saveButton);
    ///
    /// USS: tooltip.uss. Colors via var(--m3-*) tokens.
    /// </summary>
    public class M3Tooltip : VisualElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass     = "m3-tooltip";
        private const string PlainClass    = "m3-tooltip--plain";
        private const string RichClass     = "m3-tooltip--rich";
        private const string TitleClass    = "m3-tooltip__title";
        private const string BodyClass     = "m3-tooltip__body";

        // ------------------------------------------------------------------ //
        //  Children                                                            //
        // ------------------------------------------------------------------ //
        private readonly Label _title;
        private readonly Label _body;

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private TooltipVariant             _variant = TooltipVariant.Plain;
        private string                     _titleText = string.Empty;
        private string                     _bodyText  = string.Empty;
        private VisualElement              _anchor;
        private IVisualElementScheduledItem _showSchedule;

        private const int HoverDelayMs = 500;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        public enum TooltipVariant { Plain, Rich }

        /// <summary>Plain (text only) or Rich (title + body).</summary>
        public TooltipVariant Variant
        {
            get => _variant;
            set
            {
                _variant = value;
                ApplyVariant();
            }
        }

        /// <summary>Tooltip title text (Rich variant only).</summary>
        public string Title
        {
            get => _titleText;
            set
            {
                _titleText  = value ?? string.Empty;
                _title.text = _titleText;
                _title.style.display = string.IsNullOrEmpty(_titleText)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }

        /// <summary>Tooltip body / plain text.</summary>
        public string Body
        {
            get => _bodyText;
            set
            {
                _bodyText  = value ?? string.Empty;
                _body.text = _bodyText;
            }
        }

        // ------------------------------------------------------------------ //
        //  Constructor                                                         //
        // ------------------------------------------------------------------ //

        public M3Tooltip()
        {
            AddToClassList(BaseClass);
            style.position = Position.Absolute;
            style.display  = DisplayStyle.None;
            pickingMode    = PickingMode.Ignore;

            _title = new Label();
            _title.AddToClassList(TitleClass);
            _title.AddToClassList("m3-title-small");
            _title.style.display = DisplayStyle.None;

            _body = new Label();
            _body.AddToClassList(BodyClass);
            _body.AddToClassList("m3-body-medium");

            Add(_title);
            Add(_body);

            ApplyVariant();
        }

        // ------------------------------------------------------------------ //
        //  Public helpers                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Attaches this tooltip to the given anchor element.
        /// The tooltip shows after 500ms hover and hides on mouse leave.
        /// </summary>
        public void Attach(VisualElement anchor)
        {
            Detach();
            _anchor = anchor;
            _anchor.RegisterCallback<MouseEnterEvent>(OnAnchorEnter);
            _anchor.RegisterCallback<MouseLeaveEvent>(OnAnchorLeave);
        }

        /// <summary>Detaches the tooltip from its current anchor.</summary>
        public void Detach()
        {
            if (_anchor == null) return;
            _anchor.UnregisterCallback<MouseEnterEvent>(OnAnchorEnter);
            _anchor.UnregisterCallback<MouseLeaveEvent>(OnAnchorLeave);
            _anchor = null;
            Hide();
        }

        // ------------------------------------------------------------------ //
        //  Internal                                                            //
        // ------------------------------------------------------------------ //

        private void OnAnchorEnter(MouseEnterEvent _)
        {
            _showSchedule?.Pause();
            _showSchedule = _anchor?.schedule.Execute(Show).StartingIn(HoverDelayMs);
        }

        private void OnAnchorLeave(MouseLeaveEvent _)
        {
            _showSchedule?.Pause();
            _showSchedule = null;
            Hide();
        }

        private void Show()
        {
            if (_anchor == null) return;

            var root = _anchor.panel?.visualTree;
            if (root == null) return;

            // Add to root overlay so tooltip is on top
            if (parent != root) root.Add(this);

            // Position above anchor
            var bounds = _anchor.worldBound;
            style.left = bounds.x;
            style.top  = bounds.y - 32f; // approximate tooltip height
            style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            style.display = DisplayStyle.None;
            RemoveFromHierarchy();
        }

        private void ApplyVariant()
        {
            if (_variant == TooltipVariant.Plain)
            {
                AddToClassList(PlainClass);
                RemoveFromClassList(RichClass);
            }
            else
            {
                AddToClassList(RichClass);
                RemoveFromClassList(PlainClass);
            }
        }
    }
}
