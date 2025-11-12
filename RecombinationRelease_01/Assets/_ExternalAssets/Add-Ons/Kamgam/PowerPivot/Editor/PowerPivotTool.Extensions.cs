using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;
using System.Text;

namespace Kamgam.PowerPivot
{
    public static class Extensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var go = enumerator.Current as GameObject;
                    if (go != null)
                        return false;
                }
            }

            return true;
        }

        public static bool HasExactlyOneGameObject<T>(this IEnumerable<T> source)
        {
            int count = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var go = enumerator.Current as GameObject;
                    if (go != null)
                    {
                        count++;
                        if (count > 1)
                            break;
                    }
                }
            }

            return count == 1;
        }

        public static bool HasMoreThanOneGameObject<T>(this IEnumerable<T> source)
        {
            int count = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var go = enumerator.Current as GameObject;
                    if (go != null)
                    {
                        count++;
                        if (count > 1)
                            return true;
                    }
                }
            }

            return false;
        }

        public static GameObject FirstGameObject<T>(this IEnumerable<T> source)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var go = enumerator.Current as GameObject;
                    if (go != null)
                        return go;
                }
            }

            return null;
        }

        /// <summary>
        /// Convert rotation from world space to local space.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public static Quaternion InverseTransformRotation(this Transform t, Quaternion rot)
        {
            return rot * Quaternion.Inverse(t.rotation);
        }
    }
}
