using System.IO;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Kamgam.PowerPivot
{
    public class PowerPivotSettings : ScriptableObject
    {
        public const string SettingsFilePath = "Assets/PowerPivotSettings.asset";

        [SerializeField, Tooltip(_LogLevelTooltip)]
        public Logger.LogLevel LogLevel;
        public const string _LogLevelTooltip = "Log levels to determine how many log messages will be shown (Log = all message, Error = only critical errors).";

        public bool ShowWindow = true;

        [SerializeField, Tooltip(_PlaneScalingKeyTooltip)]
        public KeyCode PlaneScalingKey;
        public const string _PlaneScalingKeyTooltip = "Pressing this key while dragging one of the axis will scale that object along the two perpendicular axis (plane). Set to KeyCode.None to disable.\nNOTICE: Currently only implemented for single objects.";


        [Header("Shortcuts")]
        [SerializeField, Tooltip(_ActivationKeyTooltip)]
        public KeyCode ActivationKey;
        public const string _ActivationKeyTooltip = "Pressing this key will switch to the cursor tool.";

        [SerializeField, Tooltip(_DeactivationKeyTooltip)]
        public KeyCode DeactivationKey;
        public const string _DeactivationKeyTooltip = "Pressing this key will exit the cursor tool and return to the normal tools.";

        [SerializeField, Tooltip(_SnapKeyTooltip)]
        public KeyCode SnapKey;
        public const string _SnapKeyTooltip = "Pressing and holding this key will snap the cursor to the colsest vertex.";

        [SerializeField, Tooltip(_ContextualActivationTooltip)]
        public bool ContextualActivation;
        public const string _ContextualActivationTooltip = "Pressing the snap key while rotating or scaling will activate the tool.";

        [SerializeField, Tooltip(_ContextualActivationOnMoveTooltip)]
        public bool ContextualActivationOnMove;
        public const string _ContextualActivationOnMoveTooltip = "Should contextual activation also be allowed while using the move tool?";

        [SerializeField, Tooltip(_UndoKeyTooltip)]
        public KeyCode UndoKey = KeyCode.Z;
        public const string _UndoKeyTooltip = "Undo key";

        [SerializeField, Tooltip(_RedoKeyTooltip)]
        public KeyCode RedoKey = KeyCode.Y;
        public const string _RedoKeyTooltip = "Redo key";

        [HideInInspector]
        public KeyCode MoveKey = KeyCode.W;

        [HideInInspector]
        public KeyCode RotateKey = KeyCode.E;

        [HideInInspector]
        public KeyCode ScaleKey = KeyCode.R;

        [Header("Snapping")]

        [Tooltip(_SnapToPivotTooltip)]
        public bool SnapToPivot;
        public const string _SnapToPivotTooltip = "Should the original pivot also be a valid snap target in addition to the vertices?\nUseful to get the cursor back to the origin.";

        [Tooltip(_IgnoreBackFacingVerticesTooltip)]
        public bool IgnoreBackFacingVertices = false;
        public const string _IgnoreBackFacingVerticesTooltip = "Should back facing vertices be ignored while snapping?";

        [Tooltip(_PreferSelectedDistanceTooltip)]
        public float PreferSelectedDistance;
        public const string _PreferSelectedDistanceTooltip = "If two vertices are close then the vertex on the currently selected object(s) is preferred as snap target. The distance is in screen space (pixels).\n\nHint: Hold down the SHIFT key to temporarily disable this behaviour.";

        [Tooltip(_PreferZDistanceTooltip)]
        public float PreferZDistance;
        public const string _PreferZDistanceTooltip = "All vertices that are within this distance of each other are considered to be equal. If there are mutiple vetices within that radius then the selection is based on the z value (the closest vertex will be taken). The distance is in screen space (pixels).\n\nHint: Hold down the SHIFT key to temporarily disable this behaviour.";

        [Tooltip(_IgnoreZDistanceTooltip)]
        public float IgnoreZDistance;
        public const string _IgnoreZDistanceTooltip = "PreferZDistance will only be applied if the vertices are more than IgnoreZDistance apart (in world units).";

        [Header("Other")]
        [Tooltip(_OverrideFramingTooltip)]
        public bool OverrideFraming = true;
        public const string _OverrideFramingTooltip = "Override framing of selected object. If enabled then the camera will focus on the cursor position if the \"frame selected\" key is pressed.";

        [SerializeField, Tooltip(_FrameSelectedKeyTooltip)]
        public KeyCode FrameSelectedKey = KeyCode.F;
        public const string _FrameSelectedKeyTooltip = "Frame selected Key";

        public enum UpdateCursorOptions { Always, OnlyIfLocal, Never }
        [Tooltip(_UpdateCursorWithObjectTooltip)]
        public UpdateCursorOptions UpdateCursorWithObject;
        public const string _UpdateCursorWithObjectTooltip = "If an object is moved outside of the cursor tool then should the cursor be moved along or remain where it is?";

        [Tooltip(_ShowParentingButtonsTooltip)]
        public bool ShowParentingButtons = false;
        public const string _ShowParentingButtonsTooltip = "Show the \"Create parent\" and \"Extract children\" buttons?";

        [SerializeField, Tooltip(_AutoRefreshModelsWithModifiedPivotsTooltip)]
        public bool AutoRefreshModelsWithModifiedPivots = false;
        public const string _AutoRefreshModelsWithModifiedPivotsTooltip = "Should all models be checked for modified pivots after import? If enabled then those meshes with modified pivots are updated automatically.";



        //[HideInInspector]
        public Vector2 WindowPosition;

        [Header("Colors")]

        [Range(0, 1f)]
        public float GizmoOpacity;
        public Color GizmoColorA;
        public Color GizmoColorB;
        public Color GizmoCursorColor;


        protected static PowerPivotSettings cachedSettings;

        public static PowerPivotSettings GetOrCreateSettings()
        {
            if (cachedSettings == null)
            {
                cachedSettings = AssetDatabase.LoadAssetAtPath<PowerPivotSettings>(SettingsFilePath);
                if (cachedSettings == null)
                {
                    cachedSettings = ScriptableObject.CreateInstance<PowerPivotSettings>();

                    cachedSettings.LogLevel = Logger.LogLevel.Warning;
                    cachedSettings.ShowWindow = true;
                    cachedSettings.PlaneScalingKey = KeyCode.LeftShift;

                    cachedSettings.ActivationKey = KeyCode.U;
                    cachedSettings.DeactivationKey = KeyCode.Escape;
                    cachedSettings.SnapKey = KeyCode.V; // replaced by UpdateKeysFromBindings()
                    cachedSettings.UndoKey = KeyCode.Z;
                    cachedSettings.RedoKey = KeyCode.Y;
                    cachedSettings.ContextualActivation = true;
                    cachedSettings.ContextualActivationOnMove = true;

                    cachedSettings.SnapToPivot = true;
                    cachedSettings.IgnoreBackFacingVertices = true;
                    cachedSettings.PreferSelectedDistance = 15f;
                    cachedSettings.PreferZDistance = 15f;
                    cachedSettings.IgnoreZDistance = 1.5f;
                    cachedSettings.AutoRefreshModelsWithModifiedPivots = false;

                    cachedSettings.GizmoOpacity = 1f;
                    cachedSettings.GizmoColorA = Color.red;
                    cachedSettings.GizmoColorB = Color.white;
                    cachedSettings.GizmoCursorColor = Color.blue;

                    cachedSettings.OverrideFraming = true;
                    cachedSettings.UpdateCursorWithObject = UpdateCursorOptions.OnlyIfLocal;
                    cachedSettings.ShowParentingButtons = false;

                    cachedSettings.WindowPosition = new Vector2(-1, -1);

                    // update from key bindings
                    cachedSettings.UpdateKeysFromBindings();
                    ShortcutManager.instance.shortcutBindingChanged -= cachedSettings.onKeyBindingsChanged;
                    ShortcutManager.instance.shortcutBindingChanged += cachedSettings.onKeyBindingsChanged;

                    AssetDatabase.CreateAsset(cachedSettings, SettingsFilePath);
                    AssetDatabase.SaveAssets();

                    Logger.OnGetLogLevel = () => cachedSettings.LogLevel;
                }
            }
            return cachedSettings;
        }

        private void onKeyBindingsChanged(ShortcutBindingChangedEventArgs change)
        {
            UpdateKeysFromBindings();
        }

        public void UpdateKeysFromBindings()
        {
            cachedSettings.MoveKey = TryGetKeyCodeForBinding("Tools/Move", KeyCode.W);
            cachedSettings.RotateKey = TryGetKeyCodeForBinding("Tools/Rotate", KeyCode.E);
            cachedSettings.ScaleKey = TryGetKeyCodeForBinding("Tools/Scale", KeyCode.R);
            cachedSettings.FrameSelectedKey = TryGetKeyCodeForBinding("Main Menu/Edit/Frame Selected", KeyCode.F);
            cachedSettings.SnapKey = TryGetKeyCodeForBinding("Scene View/Vertex Snapping", KeyCode.V);
        }

        static KeyCode TryGetKeyCodeForBinding(string binding, KeyCode defaultValue)
        {
            // https://docs.unity3d.com/2019.4/Documentation/ScriptReference/ShortcutManagement.IShortcutManager.GetAvailableShortcutIds.html
            // Throws ArgumentException if shortcutId is not available, i.e. when GetAvailableShortcutIds does not contain shortcutId.
            var bindings = ShortcutManager.instance.GetAvailableShortcutIds();
            foreach (var b in bindings)
            {
                if (b == binding)
                {
                    var combo = ShortcutManager.instance.GetShortcutBinding(binding).keyCombinationSequence;
                    foreach (var c in combo)
                    {
                        return c.keyCode;
                    }
                }
            }

            return defaultValue;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        // settings
        public static void SelectSettings()
        {
            var settings = PowerPivotSettings.GetOrCreateSettings();
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "PowerPivotTool settings could not be found or created.", "Ok");
            }
        }

        public void OnActivationKeyChanged()
        {
            var projectPath = UtilsEditor.GetProjectDirWithEndingSlash();
            var filePath = projectPath + "Assets/Kamgam/PowerPivot/Editor/PowerPivotTool.Shortcut.cs";

            try
            {
                if (!UtilsEditor.IsFileLocked(filePath))
                {
                    File.Copy(filePath, filePath + ".backup~", true);
                    File.Copy(filePath, filePath + ".tmp~", true);
                    string text = File.ReadAllText(filePath + ".tmp~", System.Text.Encoding.UTF8);
                    string delimiter = "/*KEY*/";
                    string[] parts = text.Split(new[] { delimiter }, System.StringSplitOptions.None);
                    if (parts.Length == 3)
                    {
                        // disable shortcut if no key code is set
                        if (ActivationKey == KeyCode.None)
                            parts[0] = parts[0].Replace(" [Shortcut", " //[Shortcut");
                        else
                            parts[0] = parts[0].Replace("//[Shortcut", "[Shortcut");

                        // update key code
                        parts[1] = "KeyCode." + ActivationKey.ToString();
                    }


                    File.WriteAllText(filePath + ".tmp~", string.Join(delimiter, parts), System.Text.Encoding.UTF8);
                    File.Copy(filePath + ".tmp~", filePath, true);
                    File.Delete(filePath + ".tmp~");
                    File.Delete(filePath + ".backup~");

                    EditorUtility.DisplayDialog("Success: recompilation is necessary", "Cursor Tool activation key changed. Refreshing asset database. Recompilataion will start now.", "Okay");
                    AssetDatabase.Refresh();
                }
                else
                {
                    string msg = "Could not update ActivationKey in 'Assets/PowerPivot/Editor/PivotCursorTool.Shortcut.cs'. Error: the file is not writeable or does not exist.";
                    EditorUtility.DisplayDialog("Error", msg, "Okay");
                    Debug.LogError(msg);
                }
            }
            catch (System.Exception e)
            {
                File.Copy(filePath + ".backup~", filePath, true);
                File.Delete(filePath + ".tmp~");

                string msg = "Could not update ActivationKey in 'Assets/PowerPivot/Editor/PivotCursorTool.Shortcut.cs'. Error: " + e.Message;
                EditorUtility.DisplayDialog("Error", msg, "Okay");
                Debug.LogError(msg);
            }
        }

        public const string ActivatePowerPivotMenuEntry = "Tools/" + Installer.AssetName + "/Activate";
        [MenuItem(ActivatePowerPivotMenuEntry, priority = 101)]
        public static void ActivateTool()
        {
            if (PowerPivotActiveState.IsActive)
            {
                PowerPivotTool.Instance.Deactivate();
            }
            else
            {
                if (Tools.current == Tool.Rotate)
                {
                    PowerPivotTool.Activate(PowerPivotTool.PTool.Rotate);
                }
                else if (Tools.current == Tool.Scale)
                {
                    PowerPivotTool.Activate(PowerPivotTool.PTool.Scale);
                }
                else if (Tools.current == Tool.Move)
                {
                    PowerPivotTool.Activate(PowerPivotTool.PTool.Move);
                }
            }
        }

        [MenuItem("Tools/" + Installer.AssetName + "/Settings", priority = 202)]
        public static void OpenSettings()
        {
            var settings = GetOrCreateSettings();
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Settings could not be found or created.", "Ok");
            }
        }
    }

    [CustomEditor(typeof(PowerPivotSettings))]
    public class PowerPivotSettingsEditor : Editor
    {
        PowerPivotSettings settings;

        public void Awake()
        {
            settings = target as PowerPivotSettings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var oldActivationKey = serializedObject.FindProperty("ActivationKey").enumValueIndex;

            SerializedProperty serializedProperty = serializedObject.GetIterator();
            serializedProperty.NextVisible(enterChildren: true); // skip script
            while (serializedProperty.NextVisible(enterChildren: false))
            {
                EditorGUILayout.PropertyField(serializedProperty);
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();

                // activation key changed
                var newActivationKey = serializedObject.FindProperty("ActivationKey").enumValueIndex;
                if (oldActivationKey != newActivationKey)
                {
                    settings.OnActivationKeyChanged(); 
                }

                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
        }
    }

    static class PowerPivotSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreatePowerPivotSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Power Pivot", SettingsScope.Project)
            {
                label = "Power Pivot",
                guiHandler = (searchContext) =>
                {
                    var settings = PowerPivotSettings.GetSerializedSettings();
                    settings.Update();

                    beginHorizontalIndent(10);

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Version: " + Installer.Version + "  (" + PowerPivotSettings.SettingsFilePath + ")");
                    if (drawButton(" Settings ", options: GUILayout.Width(80)))
                    {
                        PowerPivotSettings.OpenSettings();
                    }
                    if (drawButton(" Manual ", icon: "_Help", options: GUILayout.Width(80)))
                    {
                        Installer.OpenManual();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                    drawField(settings, "LogLevel", "Log level:", PowerPivotSettings._LogLevelTooltip);
                    drawField(settings, "ShowWindow", "Show Window:", null);
                    drawField(settings, "PlaneScalingKey", "Plane Scaling Key:", PowerPivotSettings._PlaneScalingKeyTooltip);
                    GUILayout.Space(5);

                    var oldActivationKey = settings.FindProperty("ActivationKey").enumValueIndex;
                    drawField(settings, "ActivationKey", "Activation key:", PowerPivotSettings._ActivationKeyTooltip);
                    drawField(settings, "DeactivationKey", "Deactivation key:", PowerPivotSettings._DeactivationKeyTooltip);
                    drawField(settings, "SnapKey", "Snap key:", PowerPivotSettings._SnapKeyTooltip);
                    drawField(settings, "ContextualActivation", "Contextual activation:", PowerPivotSettings._ContextualActivationTooltip);
                    drawField(settings, "ContextualActivationOnMove", "Contextual move:", PowerPivotSettings._ContextualActivationOnMoveTooltip);
                    GUILayout.Space(5);

                    drawField(settings, "SnapToPivot", "Snap to pivot:", PowerPivotSettings._SnapToPivotTooltip);
                    drawField(settings, "IgnoreBackFacingVertices", "Ignore hidden vertices:", PowerPivotSettings._IgnoreBackFacingVerticesTooltip);
                    drawField(settings, "PreferSelectedDistance", "Prefer selected distance:", PowerPivotSettings._PreferSelectedDistanceTooltip);
                    drawField(settings, "PreferZDistance", "Prefer Z distance:", PowerPivotSettings._PreferZDistanceTooltip);
                    drawField(settings, "IgnoreZDistance", "Ignore Z distance:", PowerPivotSettings._IgnoreZDistanceTooltip);
                    GUILayout.Space(5);

                    drawField(settings, "OverrideFraming", "Override framing:", PowerPivotSettings._OverrideFramingTooltip);
                    drawField(settings, "UpdateCursorWithObject", "Update Cursor:", PowerPivotSettings._UpdateCursorWithObjectTooltip);
                    drawField(settings, "ShowParentingButtons", "Parenting Buttons:", PowerPivotSettings._ShowParentingButtonsTooltip);
                    drawField(settings, "AutoRefreshModelsWithModifiedPivots", "Auto Refresh Models:", PowerPivotSettings._AutoRefreshModelsWithModifiedPivotsTooltip);
                    GUILayout.Space(5);

                    drawField(settings, "GizmoOpacity", "Gizmo Opacity:");
                    drawField(settings, "GizmoColorA", "Gizmo Color A:");
                    drawField(settings, "GizmoColorB", "Gizmo Color B:");
                    drawField(settings, "GizmoCursorColor", "Gizmo Cursor:");


                    // button to reset cache
                    GUI.enabled = PowerPivotTool.Instance != null;
                    if (GUILayout.Button( new GUIContent("Clear cursor positions", "Clears the cursror position cache in case you want to start over.")))
                    {
                        PowerPivotTool.Instance.ClearCursorCache();
                    }
                    GUI.enabled = true;

                    endHorizontalIndent();

                    settings.ApplyModifiedProperties();

                    // activation key changed
                    var newActivationKey = settings.FindProperty("ActivationKey").enumValueIndex;
                    if (oldActivationKey != newActivationKey)
                    {
                        var settingsObj = settings.targetObject as PowerPivotSettings;
                        if (settingsObj)
                        {
                            settingsObj.OnActivationKeyChanged();
                        }
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting.
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "power pivot", "pivot", "cursor", "pivot cursor", "tool", "rotate", "scale" })
            };

            return provider;
        }

        static bool drawButton(string text, string tooltip = null, string icon = null, params GUILayoutOption[] options)
        {
            GUIContent content;

            // icon
            if (!string.IsNullOrEmpty(icon))
                content = EditorGUIUtility.IconContent(icon);
            else
                content = new GUIContent();

            // text
            content.text = text;

            // tooltip
            if (!string.IsNullOrEmpty(tooltip))
                content.tooltip = tooltip;

            return GUILayout.Button(content, options);
        }

        static void drawField(SerializedObject settings, string fieldPropertyName, string label, string tooltip = null)
        {
            var prop = settings.FindProperty(fieldPropertyName);
            // Workaround for a layouting bug in Unity if EditorGUILayout.PropertyField is used (the toogle checkbox is off).
            if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                prop.boolValue = GUILayout.Toggle(prop.boolValue, label.Replace(":", ""));
            }
            else
            {
                EditorGUILayout.PropertyField(settings.FindProperty(fieldPropertyName), new GUIContent(label));
            }

            if (!string.IsNullOrEmpty(tooltip))
            {
                var style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                var col = style.normal.textColor;
                col.a = 0.5f;
                style.normal.textColor = col;

                beginHorizontalIndent(10);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(tooltip, style);
                GUILayout.EndVertical();
                endHorizontalIndent();
            }
            GUILayout.Space(5);
        }

        static void beginHorizontalIndent(int indentAmount = 10, bool beginVerticalInside = true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indentAmount);
            if (beginVerticalInside)
                GUILayout.BeginVertical();
        }

        static void endHorizontalIndent(float indentAmount = 10, bool begunVerticalInside = true, bool bothSides = false)
        {
            if (begunVerticalInside)
                GUILayout.EndVertical();
            if (bothSides)
                GUILayout.Space(indentAmount);
            GUILayout.EndHorizontal();
        }
    }
}