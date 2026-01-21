using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    public static class UtilsEditor
    {
		public static bool IsNotInScene(GameObject go)
		{
			return go == null || go.scene == null || !go.scene.isLoaded;
		}

		public static bool IsInScene(GameObject go)
		{
			return !IsNotInScene(go);
		}

        public static bool IsInPrefabStage()
        {
            try
            {
                // This uses UnityEditor.Experimental.SceneManagement.PrefabStageUtility.
#if UNITY_2021_2_OR_NEWER
                var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                return stage != null;
#elif UNITY_2018_3_OR_NEWER
                var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                return stage != null;
#else
                return false;
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError("Prefab Stage object not found. Maybe Unity has changed the namespace of 'PrefabStageUtility' (thrown error: " + e.Message + "). Please let us know of this error (Tools > Smart Ui Selection > Feedback and Support). Assuming we are not in PrefabStage to continue.");
                return false;
            }
        }

        public static GameObject GetPrefabStageRoot()
        {
            try
            {
#if UNITY_2021_2_OR_NEWER
                var root = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                return root;
#elif UNITY_2018_3_OR_NEWER
                var root = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                return root;
#else
                return null;
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError("Stage root object not found. Maybe Unity has changed the namespace of 'PrefabStageUtility' (thrown error: " + e.Message + "). Please let us know of this error (Tools > Smart Ui Selection > Feedback and Support).");
                return null;
            }
        }

        public static void SmartDestroy(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject.DestroyImmediate(obj);
            }
            else
#endif
            {
                GameObject.Destroy(obj);
            }
        }

        public static bool IsLightTheme()
        {
            return !EditorGUIUtility.isProSkin;
        }

        public static string GetProjectDirWithEndingSlash()
        {
            string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../")).Replace("\\", "/");
            if (projectDir.EndsWith("/"))
                return projectDir;
            else
                return projectDir + "/";
        }

        /// <summary>
        /// Is the files locked or read-only or does not exists at all?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsFileLocked(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                using (FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        public static float SqrDistanceToGUIPointInScreenSpace(Camera cam, Vector2 mousePosition, Vector3 worldPosition)
        {
            var worldPosInScreenSpace = cam.WorldToScreenPoint(worldPosition);
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var mousePosInScreenSpace = cam.WorldToScreenPoint(ray.origin);
            var delta = worldPosInScreenSpace - mousePosInScreenSpace;
            delta.z = 0;
            return delta.sqrMagnitude;
        }
    }
}