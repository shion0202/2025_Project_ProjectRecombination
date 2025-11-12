#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    public class Installer : IActiveBuildTargetChanged
    {
		public const string AssetName = "Power Pivot";
        public const string Version = "2.0.6";
        public const string Define = "KAMGAM_POWER_PIVOT";
        public const string ManualUrl = "https://kamgam.com/unity/PowerPivotManual.pdf";
		public const string AssetLink = "https://assetstore.unity.com/packages/slug/267036";
		
		public static string _assetRootPathDefault = "Assets/Kamgam/PowerPivot/";
        public static string AssetRootPath
        {
            get
            {
#if UNITY_EDITOR
                if (System.IO.File.Exists(_assetRootPathDefault))
                {
                    return _assetRootPathDefault;
                }

                // The the tool was moved then search for the installer script and derive the root
                // path from there. Used Assets/ as ultimate fallback.
                string finalPath = "Assets/";
                string assetRootPathRelative = _assetRootPathDefault.Replace("Assets/", "");
                var installerGUIDS = AssetDatabase.FindAssets("t:Script Installer");
                foreach (var guid in installerGUIDS)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains(assetRootPathRelative))
                    {
                        int index = path.IndexOf(assetRootPathRelative);
                        return path.Substring(0, index) + assetRootPathRelative;
                    }
                }

                return finalPath;
#else
                return _assetRootPathDefault;
#endif
            }
        }

        public static Version GetVersion() => new Version(Version);

        [UnityEditor.Callbacks.DidReloadScripts(998001)]
        public static void InstallIfNeeded()
        {
            bool versionChanged = VersionHelper.UpgradeVersion(GetVersion, out Version oldVersion, out Version newVersion);
            if (versionChanged)
            {
                if (versionChanged)
                {
                    Debug.Log( AssetName + " version changed from " + oldVersion + " to " + newVersion);

                    showWelcomeMessage();
                }
            }
        }

        public int callbackOrder => 0;

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            Logger.LogMessage($"Build target changed from {previousTarget} to {newTarget}. Refreshing define symbols.");
            AddDefineSymbol();
        }

        [MenuItem("Tools/" + AssetName + "/Debug/Add Defines", priority = 501)]
        private static void AddDefineSymbolMenu()
        {
            AddDefineSymbol();
        }

        private static bool AddDefineSymbol()
        {
            bool didChange = false;

            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (targetGroup == BuildTargetGroup.Unknown || targetGroup == BuildTargetGroup.GameCoreScarlett)
                    continue;
#pragma warning restore CS0618 // Type or member is obsolete

                try
                {
#if UNITY_2023_1_OR_NEWER
                    string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
#else
                    string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

                    if (currentDefineSymbols.Contains(Define))
                        continue;

#if UNITY_2023_1_OR_NEWER
                    PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), currentDefineSymbols + ";" + Define);
#else
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, currentDefineSymbols + ";" + Define);
#endif
                    Logger.LogMessage($"{Define} symbol has been added for {targetGroup}.");

                    didChange = true;
                }
                catch (Exception)
                {
                    // There are many obsolete defines in the enum, skip them silently.
                }
            }

            return didChange;
        }

        [MenuItem("Tools/" + AssetName + "/Debug/Remove Defines", priority = 502)]
        private static void RemoveDefineSymbol()
        {
            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (targetGroup == BuildTargetGroup.Unknown || targetGroup == BuildTargetGroup.GameCoreScarlett)
                    continue;
#pragma warning restore CS0618 // Type or member is obsolete

                try
                {
#if UNITY_2023_1_OR_NEWER
                    string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
#else
                    string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

                    if (currentDefineSymbols.Contains(Define))
                    {
                        currentDefineSymbols = currentDefineSymbols.Replace(";" + Define, "");
#if UNITY_2023_1_OR_NEWER
                        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), currentDefineSymbols);
#else
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, currentDefineSymbols);
#endif
                        Logger.LogMessage($"{Define} symbol has been removed for {targetGroup}.");
                    }
                }
                catch (Exception)
                {
                    // There are many obsolete defines in the enum, skip them silently.
                }

            }
        }

        static void showWelcomeMessage()
        {
            bool openManual = EditorUtility.DisplayDialog(
                    AssetName,
                    "Thank you for choosing " + AssetName + ".\n\n" +
                    "Please start by reading the manual.\n\n" +
                    "If you can find the time I would appreciate your feedback in the form of a review.",
                    "Open manual (web)", "Close"
                    );

            if (openManual)
                OpenManual();
        }


        [MenuItem("Tools/" + AssetName + "/Manual", priority = 201)]
        public static void OpenManual()
        {
            Application.OpenURL(ManualUrl);
        }

        /*
        [MenuItem("Tools/" + AssetName + "/Open Example Scene", priority = 103)]
        public static void OpenExample()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ExamplePath);
                EditorGUIUtility.PingObject(scene);
                EditorSceneManager.OpenScene(ExamplePath);
            };
        }
        */

        [MenuItem("Tools/" + AssetName + "/Please leave a review :-)", priority = 510)]
        public static void LeaveReview()
        {
            Application.OpenURL(AssetLink + "?aid=1100lqC54&pubref=asset");
        }

        [MenuItem("Tools/" + AssetName + "/More Asset by KAMGAM", priority = 511)]
        public static void MoreAssets()
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/37829?aid=1100lqC54&pubref=asset");
        }

        [MenuItem("Tools/" + AssetName + "/Version " + Version, priority = 512)]
        public static void LogVersion()
        {
            Debug.Log(AssetName + " v" + Version);
        }
    }
}
#endif