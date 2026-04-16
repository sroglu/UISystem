using System.Collections.Generic;
using mehmetsrl.Utilities.EditorHelpers;
using mehmetsrl.UISystem.Data;
using mehmetsrl.UISystem.Enums;
using UnityEngine;

namespace mehmetsrl.UISystem.Editor
{
    internal class UISystemAssetProvider : IGameSpecificAssetProvider
    {
        private const string ResourcesRoot = "Assets/GameSpecific/UISystem/Resources/UISystem";

        public IEnumerable<GameSpecificAssetRegistration> GetRegistrations()
        {
            yield return new GameSpecificAssetRegistration
            {
                AssetPath = ResourcesRoot + "/DefaultLight.asset",
                AssetType = typeof(ThemeData),
                ResourcesName = "UISystem/DefaultLight",
                Factory = so => DefaultAssetsSetup.ConfigureTheme(so, isLight: true)
            };

            yield return new GameSpecificAssetRegistration
            {
                AssetPath = ResourcesRoot + "/DefaultDark.asset",
                AssetType = typeof(ThemeData),
                ResourcesName = "UISystem/DefaultDark",
                Factory = so => DefaultAssetsSetup.ConfigureTheme(so, isLight: false)
            };

            yield return new GameSpecificAssetRegistration
            {
                AssetPath = ResourcesRoot + "/DefaultTypography.asset",
                AssetType = typeof(TypographyConfig),
                ResourcesName = "UISystem/DefaultTypography",
                Factory = so => DefaultAssetsSetup.ConfigureTypography(so)
            };
        }
    }
}
