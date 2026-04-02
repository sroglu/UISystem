using System.IO;
using UnityEditor;
using UnityEngine;

namespace mehmetsrl.UISystem.Editor
{
    /// <summary>
    /// Adds UISystem component creation shortcuts to the Unity Project context menu.
    /// Assets > Create > UISystem > Button (Filled / Outlined)
    /// </summary>
    internal static class UISystemMenuItems
    {
        private const string StylePathButton     = "../../Styles/Components/button.uss";
        private const string StylePathStateLayer = "../../Styles/state-layer.uss";

        // ------------------------------------------------------------------ //
        //  Filled Button                                                        //
        // ------------------------------------------------------------------ //

        [MenuItem("Assets/Create/UISystem/Button (Filled)", priority = 200)]
        private static void CreateFilledButton()
            => CreateButtonUxml("NewFilledButton.uxml", "Filled");

        [MenuItem("Assets/Create/UISystem/Button (Filled)", validate = true)]
        private static bool ValidateCreateFilledButton()
            => GetSelectedFolder() != null;

        // ------------------------------------------------------------------ //
        //  Outlined Button                                                      //
        // ------------------------------------------------------------------ //

        [MenuItem("Assets/Create/UISystem/Button (Outlined)", priority = 201)]
        private static void CreateOutlinedButton()
            => CreateButtonUxml("NewOutlinedButton.uxml", "Outlined");

        [MenuItem("Assets/Create/UISystem/Button (Outlined)", validate = true)]
        private static bool ValidateCreateOutlinedButton()
            => GetSelectedFolder() != null;

        // ------------------------------------------------------------------ //
        //  Helpers                                                              //
        // ------------------------------------------------------------------ //

        private static void CreateButtonUxml(string fileName, string variant)
        {
            string folder = GetSelectedFolder() ?? "Assets";
            string path   = Path.Combine(folder, fileName);

            // Avoid overwriting an existing file
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            string content =
$@"<ui:UXML xmlns:ui=""UnityEngine.UIElements""
         xmlns:components=""mehmetsrl.UISystem.Components"">
    <ui:Style src=""{StylePathButton}"" />
    <ui:Style src=""{StylePathStateLayer}"" />
    <components:M3Button variant=""{variant}"" text=""Button"" />
</ui:UXML>
";
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();

            // Ping the newly created asset in the Project window
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"[UISystem] Created {variant} button at {path}");
        }

        /// <summary>
        /// Returns the selected folder path in the Project window,
        /// or null if nothing is selected.
        /// </summary>
        private static string GetSelectedFolder()
        {
            if (Selection.activeObject == null) return "Assets";

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path)) return "Assets";

            return Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        }
    }
}
