using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Core
{
    /// <summary>
    /// MonoBehaviour singleton that holds the active ThemeData and broadcasts
    /// OnThemeChanged when the active theme is swapped. Survives scene loads via
    /// DontDestroyOnLoad. Only one instance is allowed per application lifetime.
    ///
    /// USS sync: On SetTheme(), ThemeManager swaps the light/dark USS StyleSheet on
    /// each managed panel's rootVisualElement. The stylesheets define all --m3-*
    /// custom properties in a :root { } block, which cascade to all descendants.
    /// Elements using var(--m3-*) update automatically when the stylesheet is swapped.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Singleton                                                           //
        // ------------------------------------------------------------------ //
        private static ThemeManager _instance;

        /// <summary>
        /// Active singleton instance. Searches the scene if the static reference was cleared
        /// (e.g. after an Editor domain reload), so it is safe to call at any time.
        /// Returns null if no ThemeManager exists in any loaded scene.
        /// </summary>
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ThemeManager>();
                return _instance;
            }
        }

        // ------------------------------------------------------------------ //
        //  Serialized Fields                                                   //
        // ------------------------------------------------------------------ //
        [SerializeField] private ThemeData _lightTheme;
        [SerializeField] private ThemeData _darkTheme;
        [SerializeField] private ThemeData _activeTheme;
        [SerializeField] private TypographyConfig _typographyConfig;

        [Tooltip("USS stylesheet for the light theme — defines all --m3-* CSS custom properties.")]
        [SerializeField] private StyleSheet _lightSheet;

        [Tooltip("USS stylesheet for the dark theme — defines all --m3-* CSS custom properties.")]
        [SerializeField] private StyleSheet _darkSheet;

        [Tooltip("UIDocument panels that will receive theme USS stylesheet swaps on theme change.")]
        [SerializeField] private List<UIDocument> _managedPanels = new List<UIDocument>();

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
        /// Sets a new active theme, swaps USS stylesheets on all managed panels,
        /// and notifies all subscribers within the same frame.
        /// </summary>
        public void SetTheme(ThemeData theme)
        {
            if (theme == null)
            {
                Debug.LogError("[UISystem] ThemeManager.SetTheme: null theme provided — ignoring.", this);
                return;
            }
            _activeTheme = theme;
            SyncAllPanels();
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

        /// <summary>
        /// Register a UIDocument to receive theme USS sync. Call from Awake/OnEnable
        /// on any panel spawned at runtime after ThemeManager already exists.
        /// </summary>
        public void RegisterPanel(UIDocument doc)
        {
            if (doc == null) return;
            if (!_managedPanels.Contains(doc))
                _managedPanels.Add(doc);
            if (_activeTheme != null)
                SyncToPanel(doc);
        }

        /// <summary>
        /// Unregister a UIDocument (e.g., before it is destroyed).
        /// </summary>
        public void UnregisterPanel(UIDocument doc)
        {
            _managedPanels.Remove(doc);
        }

        /// <summary>
        /// Swap the active theme USS stylesheet on a single panel's rootVisualElement.
        /// Removes the inactive sheet (if present) and adds the active sheet.
        /// </summary>
        public void SyncToPanel(UIDocument doc)
        {
            if (doc == null || _activeTheme == null) return;
            var root = doc.rootVisualElement;
            if (root == null) return;

            bool isLight = _activeTheme == _lightTheme;
            var toRemove = isLight ? _darkSheet  : _lightSheet;
            var toAdd    = isLight ? _lightSheet : _darkSheet;

            if (toRemove != null)
                root.styleSheets.Remove(toRemove);

            if (toAdd != null && !root.styleSheets.Contains(toAdd))
                root.styleSheets.Add(toAdd);
        }

        // ------------------------------------------------------------------ //
        //  Private                                                             //
        // ------------------------------------------------------------------ //
        private void SyncAllPanels()
        {
            _managedPanels.RemoveAll(p => p == null);
            foreach (var doc in _managedPanels)
                SyncToPanel(doc);
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

            SyncAllPanels();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
