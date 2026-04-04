using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Abstract base class for all M3 UI components.
    ///
    /// Provides:
    ///   - Centralized ThemeManager.OnThemeChanged subscription on AttachToPanel
    ///     and automatic unsubscription on DetachFromPanel — no per-component boilerplate
    ///   - <see cref="InitStateLayer"/> helper to attach a StateLayerController
    ///   - <see cref="StateLayer"/> property to access the attached controller
    ///   - <see cref="Disabled"/> property that delegates to the state layer (or applies
    ///     the .m3-disabled class directly if no state layer is attached)
    ///   - <see cref="RefreshThemeColors"/> virtual hook called once when the component
    ///     first attaches to a panel (initial colors) and again on every theme change
    ///
    /// Usage:
    ///   public partial class M3MyComponent : M3ComponentBase
    ///   {
    ///       public M3MyComponent() { AddToClassList("m3-mycomponent"); BuildVisualTree(); }
    ///       protected override void BuildVisualTree() { /* ... */ }
    ///       protected override void RefreshThemeColors() { /* ... */ }
    ///   }
    ///
    /// See COMPONENT-GUIDE.md for full usage guidelines and the mandatory USS-only rule.
    /// </summary>
    public abstract class M3ComponentBase : VisualElement
    {
        private const string DisabledClass = "m3-disabled";

        private StateLayerController _stateLayer;
        private bool                 _disabled;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// The state layer controller attached via <see cref="InitStateLayer"/>.
        /// Null if the component does not have a state layer.
        /// </summary>
        protected StateLayerController StateLayer => _stateLayer;

        /// <summary>
        /// Whether this component is in the disabled state.
        /// Delegates to the StateLayerController when available, otherwise
        /// toggles the .m3-disabled CSS class directly.
        /// </summary>
        public bool Disabled
        {
            get => _disabled;
            set
            {
                if (_disabled == value) return;
                _disabled = value;
                if (_stateLayer != null)
                    _stateLayer.Disabled = _disabled;
                else
                    EnableInClassList(DisabledClass, _disabled);
            }
        }

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                           //
        // ------------------------------------------------------------------ //

        protected M3ComponentBase()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnFirstGeometryChanged);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            ThemeManager.OnThemeChanged += OnThemeChanged;
            RefreshThemeColors();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ThemeManager.OnThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(mehmetsrl.UISystem.ThemeData _) => RefreshThemeColors();

        /// <summary>
        /// Fires once after the first layout pass completes, ensuring resolvedStyle
        /// dimensions are valid and ThemeManager is initialized. Re-applies theme
        /// colors so components that depend on geometry (e.g. Slider track width)
        /// or whose initial RefreshThemeColors ran before the theme was ready get
        /// a second chance to render correctly.
        /// </summary>
        private void OnFirstGeometryChanged(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(OnFirstGeometryChanged);
            RefreshThemeColors();
        }

        // ------------------------------------------------------------------ //
        //  Overridable Hooks                                                   //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called to build the component's visual element hierarchy.
        /// Override in subclass and call from the constructor.
        /// </summary>
        protected virtual void BuildVisualTree() { }

        /// <summary>
        /// Called on initial panel attach and on every theme change.
        /// Override to cache and apply theme-derived properties that cannot
        /// be expressed in USS (see COMPONENT-GUIDE.md § Exception Registry).
        ///
        /// ✅ CORRECT use: set SDFRectElement.FillColorOverride, OverlayColor, etc.
        /// ❌ WRONG use: set style.color, style.backgroundColor, etc.
        /// </summary>
        protected virtual void RefreshThemeColors() { }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Attaches a <see cref="StateLayerController"/> to the given SDF container
        /// and optional ripple. Must be called from the subclass constructor or
        /// <see cref="BuildVisualTree"/> after the elements are created.
        /// </summary>
        protected void InitStateLayer(VisualElement container, RippleElement ripple = null)
        {
            _stateLayer = new StateLayerController(container, ripple);
            _stateLayer.Attach();

            // Re-apply disabled state if it was set before InitStateLayer was called
            if (_disabled)
                _stateLayer.Disabled = true;
        }
    }
}
