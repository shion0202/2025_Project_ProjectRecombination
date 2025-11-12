using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kamgam.PowerPivot
{
    public static class UnityVersionUtils
    {
        public static T[] FindObjectsOfType<T>(bool includeInactive = false) where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return GameObject.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            return GameObject.FindObjectsOfType<T>(includeInactive);
#endif 
        }

        public static string GetCloseIcon()
        {
#if UNITY_2023_1_OR_NEWER
            return "d_clear@2x";
#else
            return "d_winbtn_win_close_a@2x";
#endif
        }
    }
}

