using System;
using UnityEngine;

using mehmetsrl.UISystem;  // TypographyConfig

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// MonoBehaviour singleton that holds the active ThemeData and broadcasts
    /// OnThemeChanged when the active theme is swapped. Survives scene loads via
    /// DontDestroyOnLoad. Only one instance is allowed per application lifetime.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Singleton                                                           //
        // ------------------------------------------------------------------ //
        private static ThemeManager _instance;

        /// <summary>Active singleton instance. Null if no ThemeManager is in the scene.</summary>
        public static ThemeManager Instance => _instance;

        // ------------------------------------------------------------------ //
        //  Serialized Fields                                                   //
        // ------------------------------------------------------------------ //
        [SerializeField] private ThemeData _lightTheme;
        [SerializeField] private ThemeData _darkTheme;
        [SerializeField] private ThemeData _activeTheme;
        [SerializeField] private TypographyConfig _typographyConfig;

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //
        /// <summary>Currently active theme. Never null after Awake if themes are assigned.</summary>
        public ThemeData ActiveTheme => _activeTheme;

        /// <summary>Typography configuration used by TypographyResolver when no local override is set.</summary>
        public TypographyConfig TypographyConfig => _typographyConfig;

        /// <summary>Fired on the same frame as SetTheme(). Arg is the new active ThemeData.</summary>
        public event Action<ThemeData> OnThemeChanged;

        /// <summary>
        /// Sets a new active theme and notifies all subscribers within the same frame.
        /// </summary>
        public void SetTheme(ThemeData theme)
        {
            if (theme == null)
            {
                Debug.LogWarning("[UISystem] ThemeManager.SetTheme: null theme provided — ignoring.", this);
                return;
            }
            _activeTheme = theme;
            OnThemeChanged?.Invoke(_activeTheme);
        }

        /// <summary>
        /// Toggles between the assigned light and dark theme variants.
        /// Logs a warning if either theme asset is unassigned.
        /// </summary>
        public void ToggleLightDark()
        {
            if (_lightTheme == null || _darkTheme == null)
            {
                Debug.LogWarning("[UISystem] ThemeManager.ToggleLightDark: LightTheme or DarkTheme is not assigned.", this);
                return;
            }
            SetTheme(_activeTheme == _lightTheme ? _darkTheme : _lightTheme);
        }

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                           //
        // ------------------------------------------------------------------ //
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[UISystem] ThemeManager: duplicate instance detected — destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_activeTheme == null && _lightTheme != null)
            {
                _activeTheme = _lightTheme;
                Debug.Log("[UISystem] ThemeManager: ActiveTheme defaulted to LightTheme.", this);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
