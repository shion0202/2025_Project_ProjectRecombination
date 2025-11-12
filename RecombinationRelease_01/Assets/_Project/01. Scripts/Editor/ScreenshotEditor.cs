using UnityEditor;
using UnityEngine;

public class ScreenshotEditor : EditorWindow
{
    private string savePath = "Assets/_Shared/Screenshots";

    [MenuItem("Tools/Take Screenshot")]
    static void Init()
    {
        var window = (ScreenshotEditor)EditorWindow.GetWindow(typeof(ScreenshotEditor));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Screenshot Settings", EditorStyles.boldLabel);
        
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        if (GUILayout.Button("Save Screenshot"))
        {
            TakeScreenshot();
        }
    }

    private void TakeScreenshot()
    {
        var filePath = savePath + "Screenshot_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
        ScreenCapture.CaptureScreenshot(filePath);
        Debug.Log("Screenshot saved to: " + filePath);
    }
}
