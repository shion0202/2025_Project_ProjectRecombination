#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    public static class UtilsSceneVis
    {
        // Thanks SceneVis, *sigh*
        // reflection cache for mesh raycast (speeds up code execution)
        private static Type[] _refEditorTypes;
        private static Type _refSceneVisibilityStateType;
        private static System.Reflection.MethodInfo _refSceneVisibilityState_GetInstanceMethod;
        private static System.Reflection.MethodInfo _refSceneVisibilityState_SetGameObjectHiddenMethod;
        private static System.Reflection.PropertyInfo _refSceneVisibilityState_VisibilityActiveProperty;

        private static void buildReflectionCache()
        {
            try
            {
                if (_refEditorTypes != null)
                    return;

                _refEditorTypes = typeof(Editor).Assembly.GetTypes();
                if (_refEditorTypes != null && _refEditorTypes.Length > 0)
                {
                    _refSceneVisibilityStateType = _refEditorTypes.FirstOrDefault(t => t.Name == "SceneVisibilityState");
                    if (_refSceneVisibilityStateType != null)
                    {
                        _refSceneVisibilityState_GetInstanceMethod = _refSceneVisibilityStateType.GetMethod(
                            "GetInstance",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                        _refSceneVisibilityState_SetGameObjectHiddenMethod = _refSceneVisibilityStateType.GetMethod(
                            "SetGameObjectHidden",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                        _refSceneVisibilityState_VisibilityActiveProperty = _refSceneVisibilityStateType.GetProperty("visibilityActive");
                    }
                }
            }
            catch (Exception)
            {
                // fail silently
            }
        }

        /// <summary>
        /// Return true is visiblity is active, otherwise false<br />
        /// Notice: It will return false if reflection failed.
        /// </summary>
        /// <returns></returns>
        public static bool IsVisibilityActive()
        {
            try
            {
                buildReflectionCache();
                var state = GetSceneVisibilityStateViaReflection();
                if (state != null)
                {
                    return (bool)_refSceneVisibilityState_VisibilityActiveProperty.GetValue(state, null);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Based on the info found here:
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/SceneView/SceneVisibilityState.bindings.cs#L20
        /// and here:
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/SceneVisibilityManager.cs
        /// </summary>
        /// <returns></returns>
        public static UnityEngine.Object GetSceneVisibilityStateViaReflection()
		{
			try
			{
				buildReflectionCache();
				return (UnityEngine.Object)_refSceneVisibilityState_GetInstanceMethod.Invoke(null, new object[] { });
			}
			catch (Exception)
			{
				// fail silently
				return null;
			}
		}

		public static bool IsVisible(GameObject obj)
		{
			// Skip if object is hidden by the SceneVis feature introduced in 2019.1
#if UNITY_2019_2_OR_NEWER
			return !SceneVisibilityManager.instance.IsHidden(obj);
#elif UNITY_2019_1
            // Sadly the SceneVisibilityManager API is not available in 2019.1 (SceneVis was introduced in 2019.1)
            // We have to use reflections to get this info in 2019.1
            var editorTypes = typeof(Editor).Assembly.GetTypes();
            if (editorTypes != null && editorTypes.Length > 0 )
            {
                var sceneVMType = editorTypes.FirstOrDefault(t => t.Name == "SceneVisibilityManager");
                if (sceneVMType != null)
                {
                    var sceneVMMethod = sceneVMType.GetMethod("IsGameObjectHidden", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (sceneVMMethod != null)
                    {
                        var isHidden = sceneVMMethod.Invoke(null, new object[] { rectTransforms[i].gameObject });
                        if (isHidden != null)
                        {
                            return !(bool)isHidden;
                        }
                    }
                }
            }
#else
            return true;
#endif
		}

        /// <summary>
        /// Based on the info found here:
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/SceneView/SceneVisibilityState.bindings.cs#L20
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="isHidden"></param>
        /// <param name="includeChildren"></param>
        /// <returns>True if the reflection code has executed without exceptions.</returns>
        public static bool SetGameObjectHiddenNoUndoViaReflection(GameObject gameObject, bool isHidden, bool includeChildren)
        {
            try
            {
                buildReflectionCache();
                var state = GetSceneVisibilityStateViaReflection();
                if (state != null)
                {
                    _refSceneVisibilityState_SetGameObjectHiddenMethod.Invoke(state, new object[] { gameObject, isHidden, includeChildren });
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
#endif