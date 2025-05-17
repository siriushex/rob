#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    internal class EditorMenuItems
    {
        [MenuItem("Window/Dragon Water/Configuration", priority = 50)]
        public static void ShowWindow()
        {
            DragonWaterWindow.ShowWindow();
        }

        [MenuItem("Window/Dragon Water/Online Documentation", priority = 101)]
        public static void OnlineDocmentation()
        {
            Application.OpenURL("https://docs.bartekdragon.com/water/");
        }
        [MenuItem("Window/Dragon Water/Official Website", priority = 102)]
        public static void OfficialWebsite()
        {
            Application.OpenURL("https://bartekdragon.com/dragon-water/");
        }
        [MenuItem("Window/Dragon Water/Discord", priority = 103)]
        public static void Discord()
        {
            Application.OpenURL("https://bartekdragon.com/discord/");
        }


        [MenuItem("Window/Dragon Water/Changelog", priority = 201)]
        public static void Changelog()
        {
            Application.OpenURL("https://docs.bartekdragon.com/water/changelog");
        }
        [MenuItem("Window/Dragon Water/About", priority = 202)]
        public static void About()
        {
            EditorUtility.DisplayDialog("Dragon Water", $"Dragon Water v{Constants.Version}.\nCreated by Bartek Dragon.\nwww.bartekdragon.com\n\nAll Rights Reserved.", "Ok");
        }
    }
}
#endif