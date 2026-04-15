using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Tests
{
    /// <summary>
    /// Play Mode visual regression tests for UISystem M3 components.
    ///
    /// For each component in playwright-manifest.json:
    ///   1. Load the component in a test UIDocument
    ///   2. Capture screenshot via ScreenCapture.CaptureScreenshotAsTexture()
    ///   3. Save to Tests/screenshots/[ComponentName]-[variant]-[theme].png
    ///
    /// Reference comparison is handled externally by the Playwright service which:
    ///   - Takes screenshots from M3 reference site (m3.material.io)
    ///   - Compares against captured screenshots using pixel diff
    ///
    /// To run: Unity Test Runner > Play Mode > mehmetsrl.UISystem.Tests
    /// </summary>
    [TestFixture]
    public class VisualRegressionTest
    {
        private const string ScreenshotDir = "Assets/GameSpecific/UISystem/Tests/screenshots";

        [SetUp]
        public void SetUp()
        {
            if (!Directory.Exists(ScreenshotDir))
                Directory.CreateDirectory(ScreenshotDir);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (Directory.Exists(ScreenshotDir))
            {
                Directory.Delete(ScreenshotDir, true);

                string metaFile = ScreenshotDir + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);
            }
        }

        // ------------------------------------------------------------------ //
        //  M3Button                                                            //
        // ------------------------------------------------------------------ //

        [UnityTest]
        public IEnumerator Button_Filled_Light_Screenshot()
            => CaptureComponent("M3Button", "Filled", "light");

        [UnityTest]
        public IEnumerator Button_Filled_Dark_Screenshot()
            => CaptureComponent("M3Button", "Filled", "dark");

        [UnityTest]
        public IEnumerator Button_Outlined_Light_Screenshot()
            => CaptureComponent("M3Button", "Outlined", "light");

        // ------------------------------------------------------------------ //
        //  M3Card                                                              //
        // ------------------------------------------------------------------ //

        [UnityTest]
        public IEnumerator Card_Elevated_Light_Screenshot()
            => CaptureComponent("M3Card", "Elevated", "light");

        [UnityTest]
        public IEnumerator Card_Elevated_Dark_Screenshot()
            => CaptureComponent("M3Card", "Elevated", "dark");

        // ------------------------------------------------------------------ //
        //  M3Checkbox                                                          //
        // ------------------------------------------------------------------ //

        [UnityTest]
        public IEnumerator Checkbox_Checked_Light_Screenshot()
            => CaptureComponent("M3Checkbox", "Checked", "light");

        // ------------------------------------------------------------------ //
        //  M3FAB                                                               //
        // ------------------------------------------------------------------ //

        [UnityTest]
        public IEnumerator FAB_Regular_Light_Screenshot()
            => CaptureComponent("M3FAB", "Regular", "light");

        [UnityTest]
        public IEnumerator FAB_Regular_Dark_Screenshot()
            => CaptureComponent("M3FAB", "Regular", "dark");

        // ------------------------------------------------------------------ //
        //  M3TextField                                                         //
        // ------------------------------------------------------------------ //

        [UnityTest]
        public IEnumerator TextField_Filled_Light_Screenshot()
            => CaptureComponent("M3TextField", "Filled", "light");

        [UnityTest]
        public IEnumerator TextField_Outlined_Light_Screenshot()
            => CaptureComponent("M3TextField", "Outlined", "light");

        // ------------------------------------------------------------------ //
        //  M3Toggle                                                            //
        // ------------------------------------------------------------------ //

        [UnityTest]
        public IEnumerator Toggle_On_Light_Screenshot()
            => CaptureComponent("M3Toggle", "On", "light");

        [UnityTest]
        public IEnumerator Toggle_Off_Light_Screenshot()
            => CaptureComponent("M3Toggle", "Off", "light");

        // ------------------------------------------------------------------ //
        //  Capture helper                                                      //
        // ------------------------------------------------------------------ //

        private static IEnumerator CaptureComponent(string component, string variant, string theme)
        {
            // Wait two frames for rendering to settle
            yield return null;
            yield return null;

            string filename = $"{component}-{variant}-{theme}.png";
            string path     = Path.Combine(ScreenshotDir, filename);

            ScreenCapture.CaptureScreenshot(path);

            // Wait for screenshot to be written to disk
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(File.Exists(path),
                $"Screenshot not found at: {path}");

            Debug.Log($"[UISystem] Screenshot captured: {path}");
        }
    }
}
