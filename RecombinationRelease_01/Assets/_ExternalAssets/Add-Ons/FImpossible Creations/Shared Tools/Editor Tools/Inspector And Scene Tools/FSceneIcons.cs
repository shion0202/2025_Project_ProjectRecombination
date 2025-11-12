#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

public static class FSceneIcons
{
    public static void SetGizmoIconEnabled(MonoBehaviour beh, bool on)
    {
        if (beh == null) return;
        SetGizmoIconEnabled(beh.GetType(), on);
    }

    public static void SetGizmoIconEnabled(System.Type type, bool on)
    {
#if UNITY_EDITOR

        if (Application.isPlaying) return;

        //#if UNITY_2022_1_OR_NEWER
        // On newer unity versions it stopped working
        // giving warning: "Warning: Annotation not found!"
        // and can't find any info in docs, how to make it work again
        //#else
        // giving warning: "Warning: Annotation not found!"
        // sometimes it works, sometimes not ¯\_(ツ)_/¯ lets see how bad it goes now 

        //try
        //{
        //    var etype = typeof(Editor);
        //    var annotation = etype.Assembly.GetType("UnityEditor.Annotation");
        //    var scriptClass = annotation.GetField("scriptClass");
        //    var classID = annotation.GetField("classID");
        //    var annotation_util = etype.Assembly.GetType("UnityEditor.AnnotationUtility");
        //    var setIconEnabled = annotation_util.GetMethod("SetIconEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        //    const int MONO_BEHAVIOR_CLASS_ID = 114; // https://docs.unity3d.com/Manual/ClassIDReference.html
        //    setIconEnabled.Invoke(null, new object[] { MONO_BEHAVIOR_CLASS_ID, type.Name, on ? 1 : 0 });
        //}
        //catch (System.Exception)
        //{
        //}

        var etype = typeof( Editor );
        var annotation = etype.Assembly.GetType( "UnityEditor.Annotation" );
        var scriptClass = annotation.GetField( "scriptClass" );
        var classID = annotation.GetField( "classID" );
        var annotation_util = etype.Assembly.GetType( "UnityEditor.AnnotationUtility" );
        var getAnnotations = annotation_util.GetMethod( "GetAnnotations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );
        var setIconEnabled = annotation_util.GetMethod( "SetIconEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );

        var annotations = getAnnotations.Invoke( null, null ) as System.Array;

        foreach( var a in annotations )
        {
            int cid = (int)classID.GetValue( a );
            string cls = (string)scriptClass.GetValue( a );

            if( cls == type.Name )
            {
                setIconEnabled.Invoke( null, new object[] { cid, cls, on ? 1 : 0 } );
                break;
            }
        }

#endif
    }
}

