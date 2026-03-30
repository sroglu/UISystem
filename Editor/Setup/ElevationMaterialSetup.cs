using UnityEditor;
using UnityEngine;

namespace mehmetsrl.UISystem.Editor
{
    /// <summary>
    /// One-shot editor utility that creates the 6 SDFRect elevation material assets.
    /// Run via Assets > UISystem > Create Elevation Materials, or it runs automatically
    /// on package import if the materials folder is empty.
    /// </summary>
    public static class ElevationMaterialSetup
    {
        private const string ShaderName     = "UISystem/SDFRect";
        private const string MaterialFolder = "Assets/UISystem/Assets/Materials";

        // Shadow parameters per elevation level (blur in ref-px, alpha 0-1)
        private static readonly (float blur, float alpha, float offsetY)[] s_ElevParams =
        {
            (blur: 0f,  alpha: 0f,    offsetY: 0f),   // Elev 0 — no shadow
            (blur: 4f,  alpha: 0.12f, offsetY: -2f),  // Elev 1
            (blur: 8f,  alpha: 0.16f, offsetY: -4f),  // Elev 2
            (blur: 12f, alpha: 0.20f, offsetY: -6f),  // Elev 3
            (blur: 16f, alpha: 0.24f, offsetY: -8f),  // Elev 4
            (blur: 24f, alpha: 0.30f, offsetY: -12f), // Elev 5
        };

        [MenuItem("Assets/UISystem/Create Elevation Materials")]
        public static void CreateElevationMaterials()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[UISystem] Shader '{ShaderName}' not found. " +
                               "Make sure the shader is compiled before creating materials.");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < 6; i++)
                    CreateOrUpdateMaterial(shader, i);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[UISystem] Elevation materials created/updated in " + MaterialFolder);
        }

        private static void CreateOrUpdateMaterial(Shader shader, int level)
        {
            string path = $"{MaterialFolder}/SDFRectMat_Elev{level}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            Material mat;
            if (existing != null)
            {
                mat = existing;
            }
            else
            {
                mat = new Material(shader) { name = $"SDFRectMat_Elev{level}" };
                AssetDatabase.CreateAsset(mat, path);
            }

            var p = s_ElevParams[level];

            mat.SetFloat("_ShadowEnabled", level > 0 ? 1f : 0f);
            mat.SetVector("_ShadowOffset", new Vector4(0f, p.offsetY, 0f, 0f));
            mat.SetFloat("_ShadowBlur",    p.blur);
            mat.SetColor("_ShadowColor",   new Color(0f, 0f, 0f, p.alpha));
            mat.SetFloat("_OutlineEnabled",    0f);
            mat.SetFloat("_OutlineThickness",  0f);
            mat.SetColor("_OutlineColor",      Color.black);

            if (existing != null)
                EditorUtility.SetDirty(mat);
        }
    }
}
