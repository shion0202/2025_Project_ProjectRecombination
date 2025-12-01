using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        [System.Serializable]
        public class Variable
        {
            public string VariableName = "Variable";
            [SerializeField] private string Tooltip = "";

            #region Tooltip refresh clean code helper
            
            bool _tooltipWasSet = false;
            public bool TooltipAssigned { get { return _tooltipWasSet; } }
            public void AssignTooltip(string tooltip)
            {
                if (_tooltipWasSet) return;
                Tooltip = tooltip;
                _tooltipWasSet = true;
            }

            #endregion

            /// <summary> For Number type, .w == 1 -> Int   0 -> Float </summary>
            [SerializeField] private Vector4 _value = Vector4.zero;
            [SerializeField] private string _string = "";
            [SerializeField] private AnimationCurve _curve = null;
            [SerializeField] private UnityEngine.Object _uObject = null;
            [SerializeField] private object _object = null;

            public Variable(string name, object value)
            {
                VariableName = name;
                SetValue(value);
            }

            [NonSerialized] private int nameHash = 0;
            public int GetNameHash
            {
                get
                {
                    if (nameHash == 0) nameHash = VariableName.GetHashCode();
                    return nameHash;
                }
            }

            public enum EVariableType
            {
                Number, Bool, Vector2, Vector3, String, Curve, UnityObject, CustomObject
            }

            public EVariableType VariableType = EVariableType.Number;

            public void SetValue(object o)
            {
                if (o is int) { _value = new Vector4((int)o, 0, 0, 1); VariableType = EVariableType.Number; }
                else if (o is float) { _value = new Vector4((float)o, 0, 0, 0); VariableType = EVariableType.Number; }
                else if (o is bool) { bool v = (bool)o; if (v) _value.x = 1; else _value.x = 0; VariableType = EVariableType.Bool; }
                else if (o is Vector2) { Vector2 v = (Vector2)o; _value = v; VariableType = EVariableType.Vector2; }
                else if (o is Vector3) { Vector3 v = (Vector3)o; _value = v; VariableType = EVariableType.Vector3; }
                else if (o is string) { _string = o as string; VariableType = EVariableType.String; }
                else if (o is AnimationCurve) { _curve = o as AnimationCurve; VariableType = EVariableType.Curve; }
                else if (o is UnityEngine.Object) { _uObject = o as UnityEngine.Object; VariableType = EVariableType.UnityObject; }
                else { _object = o; VariableType = EVariableType.CustomObject; }
            }

            public int GetInt() { return (int)_value.x; }
            public float GetFloat() { return _value.x; }
            public bool GetBool() { return _value.x == 1; }
            public Vector2 GetVector2() { return new Vector2(_value.x, _value.y); }
            public Vector3 GetVector3() { return new Vector3(_value.x, _value.y, _value.z); }
            public string GetString() { return _string; }
            public AnimationCurve GetCurve() { return _curve; }
            public UnityEngine.Object GetUnityObject() { return _uObject; }
            public object GetObject() { return _object; }


            public void SetMinMaxSlider(float min, float max)
            { _rangeHelper = new Vector4(min, max, 0, 0); }

            public void SetCurveFixedRange(float startTime, float startValue, float endTime, float endValue)
            { _rangeHelper = new Vector4(startTime, startValue, endTime, endValue); }


            [SerializeField] private Vector4 _rangeHelper = Vector4.zero;



            /// <summary> Returns true if gui changed </summary>
            public bool Editor_DisplayVariableGUI()
            {
#if UNITY_EDITOR
                EditorGUI.BeginChangeCheck();

                GUIContent nameG = new GUIContent(VariableName, Tooltip);

                if (VariableType == EVariableType.Number)
                {
                    if (_value.w == 1) // Int
                    {
                        if (_rangeHelper.x != _rangeHelper.y && _rangeHelper.y != 0)
                            _value.x = EditorGUILayout.IntSlider(nameG, (int)_value.x, (int)_rangeHelper.x, (int)_rangeHelper.y);
                        else
                            _value.x = EditorGUILayout.IntField(nameG, (int)_value.x);
                    }
                    else // Float
                    {
                        if (_rangeHelper.x != _rangeHelper.y && _rangeHelper.y != 0)
                            _value.x = EditorGUILayout.Slider(nameG, _value.x, _rangeHelper.x, _rangeHelper.y);
                        else
                            _value.x = EditorGUILayout.FloatField(nameG, _value.x);
                    }
                }
                else if (VariableType == EVariableType.Bool)
                {
                    bool v = _value.x == 1;
                    v = EditorGUILayout.Toggle(nameG, v);
                    SetValue(v);
                }
                else if (VariableType == EVariableType.Vector2)
                {
                    _value = EditorGUILayout.Vector2Field(nameG, _value);
                }
                else if (VariableType == EVariableType.Vector3)
                {
                    _value = EditorGUILayout.Vector3Field(nameG, _value);
                }
                else if (VariableType == EVariableType.String)
                {
                    _string = EditorGUILayout.TextField(nameG, _string);
                }
                else if (VariableType == EVariableType.Curve)
                {
                    _curve = EditorGUILayout.CurveField(nameG, _curve);
                }
                else if (VariableType == EVariableType.UnityObject)
                {
                    _uObject = EditorGUILayout.ObjectField(nameG, _uObject, typeof(UnityEngine.Object), true);
                }
                else if (VariableType == EVariableType.CustomObject)
                {
                    if (_object == null)
                        EditorGUILayout.LabelField("Containing Null");
                    else
                        EditorGUILayout.LabelField("Containing custom, not serialized object");
                }

                return EditorGUI.EndChangeCheck();
#else
            return false;
#endif
            }

        }
    }
}