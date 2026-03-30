using mehmetsrl.UISystem;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Implemented by any component that reacts to ThemeManager.OnThemeChanged.
    /// Subscribe/unsubscribe in OnEnable/OnDisable and call ApplyTheme in the handler.
    /// </summary>
    public interface IThemeSubscriber
    {
        void OnThemeApplied(ThemeData theme);
    }
}
