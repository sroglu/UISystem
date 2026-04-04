using System;
using System.Collections.Generic;
using mehmetsrl.UISystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// M3-style Menu — a popup list of actions shown at an anchor position.
    ///
    /// Composition:
    ///   SDFRectElement (this) — elevated menu surface (elevation 2)
    ///   [M3MenuItem children] — menu items added via AddItem
    ///
    /// Usage:
    ///   var menu = new M3Menu();
    ///   menu.AddItem("Copy", MaterialSymbols.ContentCopy, () => DoCopy());
    ///   menu.AddItem("Delete", MaterialSymbols.Delete, () => DoDelete());
    ///   menu.Show(anchorElement);
    ///   // dismiss on outside click handled automatically
    ///
    /// M3 spec:
    ///   Min width: 112dp, Max width: 280dp
    ///   Background: --m3-surface-container
    ///   Elevation: 2 (shadow)
    ///   Corner radius: 4dp
    ///
    /// USS: menu.uss. Colors via var(--m3-*) tokens.
    /// </summary>
    public class M3Menu : SDFRectElement
    {
        // ------------------------------------------------------------------ //
        //  USS class constants                                                 //
        // ------------------------------------------------------------------ //
        private const string BaseClass = "m3-menu";

        // ------------------------------------------------------------------ //
        //  Backing fields                                                      //
        // ------------------------------------------------------------------ //
        private readonly List<M3MenuItem> _items = new();
        private VisualElement             _scrimOverlay;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        public M3Menu()
        {
            AddToClassList(BaseClass);
            CornerRadius  = 4f;
            style.display = DisplayStyle.None;
            style.position = Position.Absolute;
        }

        /// <summary>
        /// Adds a menu item. Returns the created item so callers can further customize it.
        /// </summary>
        public M3MenuItem AddItem(string label, string leadingIconCodepoint = null, Action onClick = null)
        {
            var item = new M3MenuItem
            {
                LabelText   = label,
                LeadingIcon = leadingIconCodepoint,
            };
            if (onClick != null)
                item.OnClick += onClick;

            item.OnClick += Hide; // auto-dismiss on item click
            _items.Add(item);
            Add(item);
            return item;
        }

        /// <summary>Adds a disabled separator label item.</summary>
        public void AddDivider()
        {
            var div = new VisualElement();
            div.AddToClassList("m3-menu__divider");
            Add(div);
        }

        /// <summary>
        /// Shows the menu positioned below (or above) the anchor element.
        /// Adds a transparent scrim to the root panel to capture outside clicks.
        /// </summary>
        public void Show(VisualElement anchor)
        {
            var root = anchor.panel?.visualTree;
            if (root == null) return;

            // Scrim to capture outside clicks
            _scrimOverlay?.RemoveFromHierarchy();
            _scrimOverlay = new VisualElement();
            _scrimOverlay.style.position = Position.Absolute;
            _scrimOverlay.style.left   = 0;
            _scrimOverlay.style.top    = 0;
            _scrimOverlay.style.right  = 0;
            _scrimOverlay.style.bottom = 0;
            _scrimOverlay.pickingMode  = PickingMode.Position;
            _scrimOverlay.RegisterCallback<ClickEvent>(_ => Hide());

            root.Add(_scrimOverlay);
            root.Add(this);

            // Position relative to anchor
            var anchorBounds = anchor.worldBound;
            style.left = anchorBounds.x;
            style.top  = anchorBounds.yMax + 4f; // 4dp gap below anchor

            style.display = DisplayStyle.Flex;
        }

        /// <summary>Hides and removes the menu and its scrim.</summary>
        public void Hide()
        {
            style.display = DisplayStyle.None;
            _scrimOverlay?.RemoveFromHierarchy();
            _scrimOverlay = null;
            RemoveFromHierarchy();
        }

        /// <summary>Removes all menu items.</summary>
        public void ClearItems()
        {
            _items.Clear();
            Clear();
        }
    }
}
