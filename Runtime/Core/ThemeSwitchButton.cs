using UnityEngine;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// Lightweight MonoBehaviour that exposes a SwitchTheme() method for use
    /// as a Button.onClick target. Attach alongside a uGUI Button component.
    /// </summary>
    public class ThemeSwitchButton : MonoBehaviour
    {
        /// <summary>Toggles between LightTheme and DarkTheme on the active ThemeManager.</summary>
        public void SwitchTheme()
        {
            if (ThemeManager.Instance != null)
                ThemeManager.Instance.ToggleLightDark();
            else
                Debug.LogWarning("[UISystem] ThemeSwitchButton: ThemeManager.Instance is null.", this);
        }
    }
}
