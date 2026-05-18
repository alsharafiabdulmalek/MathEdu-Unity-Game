// -----------------------------------------------------------------------------
// ArabicLocalizationMenu.cs
// -----------------------------------------------------------------------------
// Editor-only helper menu for setting up the Arabic font. Surfaces the same
// info as Docs/ARABIC_FONT_SETUP.md but as a clickable in-Editor flow.
//
//   MathEdu / Localization / Open Arabic Font Setup Guide
//      → step-by-step popup + button to open the Google Fonts download page
//
//   MathEdu / Localization / Check Arabic Font Installation
//      → scans Assets/Resources/Fonts/ for a recognized Arabic TTF and
//         tells the user whether the runtime will pick it up
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathEdu.EditorTools
{
    public static class ArabicLocalizationMenu
    {
        private const string FontsDir = "Assets/Resources/Fonts";

        [MenuItem("MathEdu/Localization/Open Arabic Font Setup Guide", priority = 300)]
        public static void OpenSetupGuide()
        {
            string message =
                "Arabic text renders as SQUARE BOXES because no Arabic TTF is in the project yet.\n\n" +
                "ONE-TIME SETUP (~1 minute):\n\n" +
                "  1. Click 'Open Google Fonts' below.\n" +
                "  2. Click 'Get font' then 'Download all' in your browser. Unzip the file.\n" +
                "  3. Find NotoSansArabic-Regular.ttf in the unzipped folder.\n" +
                "  4. Drag that .ttf into Assets/Resources/Fonts/ in this Unity project.\n" +
                "  5. Stop and restart Play mode. Tap the Arabic language button in Settings.\n\n" +
                "The code auto-detects the .ttf and creates a TMP font asset at runtime.\n" +
                "See Docs/ARABIC_FONT_SETUP.md for full details and alternative fonts.";

            int choice = EditorUtility.DisplayDialogComplex(
                "MathEdu — Arabic Font Setup",
                message,
                "Open Google Fonts",
                "Close",
                "Check installation");

            switch (choice)
            {
                case 0: Application.OpenURL("https://fonts.google.com/noto/specimen/Noto+Sans+Arabic"); break;
                case 2: CheckInstallation(); break;
            }
        }

        [MenuItem("MathEdu/Localization/Check Arabic Font Installation", priority = 301)]
        public static void CheckInstallation()
        {
            string[] expectedNames =
            {
                "NotoSansArabic-Regular", "NotoSansArabic",
                "Cairo-Regular", "Cairo",
                "Amiri-Regular", "Amiri",
                "NotoNaskhArabic-Regular", "NotoNaskhArabic",
                "Arabic"
            };

            if (!AssetDatabase.IsValidFolder(FontsDir))
            {
                EditorUtility.DisplayDialog(
                    "MathEdu — Arabic Font Status",
                    "❌  Assets/Resources/Fonts/ does not exist yet.\n\n" +
                    "Create the folder and drop an Arabic .ttf (e.g. NotoSansArabic-Regular.ttf) " +
                    "into it. See 'Open Arabic Font Setup Guide' for full instructions.",
                    "OK");
                return;
            }

            string foundPath = null;
            foreach (var name in expectedNames)
            {
                string path = FontsDir + "/" + name + ".ttf";
                if (File.Exists(path)) { foundPath = path; break; }
                path = FontsDir + "/" + name + ".otf";
                if (File.Exists(path)) { foundPath = path; break; }
            }

            // Also check for a preauthored TMP font asset.
            bool preauthored = File.Exists(FontsDir + "/Arabic SDF.asset");

            if (preauthored)
            {
                EditorUtility.DisplayDialog(
                    "MathEdu — Arabic Font Status",
                    "✅  Found preauthored TMP font asset at:\n   " +
                    FontsDir + "/Arabic SDF.asset\n\n" +
                    "This is the highest-quality option — the runtime will use it.\n" +
                    "Arabic glyphs should render correctly in Play mode and in builds.",
                    "OK");
                return;
            }

            if (foundPath != null)
            {
                EditorUtility.DisplayDialog(
                    "MathEdu — Arabic Font Status",
                    "✅  Found Arabic font:\n   " + foundPath + "\n\n" +
                    "The runtime will convert it to a TMP font asset on first Arabic use.\n" +
                    "Arabic glyphs should render correctly in Play mode and in builds.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "MathEdu — Arabic Font Status",
                "❌  No Arabic font found in " + FontsDir + "/.\n\n" +
                "Drop one of these .ttf files into that folder:\n" +
                "   • NotoSansArabic-Regular.ttf  (recommended)\n" +
                "   • Cairo-Regular.ttf\n" +
                "   • Amiri-Regular.ttf\n" +
                "   • NotoNaskhArabic-Regular.ttf\n\n" +
                "See 'Open Arabic Font Setup Guide' for download links.",
                "OK");
        }
    }
}
#endif
