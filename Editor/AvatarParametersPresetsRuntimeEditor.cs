using Lyuma.Av3Emulator.Runtime;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Presets;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    public class AvatarParametersPresetsRuntimeEditor : Editor
    {
        public LyumaAv3Runtime Runtime;
        SerializedProperty AvatarParametersSaverPresetGroup;
        AvatarParametersSaverPresetGroupDrawer AvatarParametersSaverPresetGroupDrawer;
        SerializedProperty Presets;
        SerializedProperty Preset;
        SerializedProperty PresetParameters;
        HashSet<string> ParameterNamesSet;
        [SerializeField]
        int PresetIndex = -1;
        [SerializeField]
        bool AutoCheckChangedParameters = true;
        [SerializeField]
        bool PreferEnabledParameters = true;
        [SerializeField]
        bool ShowBool = true;
        [SerializeField]
        bool ShowInt = true;
        [SerializeField]
        bool ShowFloat = true;
        [SerializeField]
        int SortMode;

        Vector2 ScrollPosition;
        Dictionary<string, object> RuntimeValues = new Dictionary<string, object>();
        static string[] SortModes = new string[] { "設定順", "名前順" };

        void OnEnable()
        {
            AvatarParametersSaverPresetGroup = serializedObject.FindProperty(nameof(AvatarParametersPresets.AvatarParametersSaverPresetGroup));
            AvatarParametersSaverPresetGroupDrawer = new AvatarParametersSaverPresetGroupDrawer();
            Presets = AvatarParametersSaverPresetGroup.FindPropertyRelative(nameof(avatar_parameters_saver.AvatarParametersSaverPresetGroup.presets));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var prevPresetIndex = AvatarParametersSaverPresetGroupDrawer.PresetIndex(AvatarParametersSaverPresetGroup);
            if (PresetIndex != prevPresetIndex)
            {
                AvatarParametersSaverPresetGroupDrawer.SetPresetIndex(AvatarParametersSaverPresetGroup, PresetIndex);
                if (PresetIndex >= 0)
                {
                    ApplyValuesToRuntime(Runtime, Presets.GetArrayElementAtIndex(PresetIndex));
                }
            }

            var groupBasePosition = EditorGUILayout.GetControlRect(GUILayout.Height(AvatarParametersSaverPresetGroupDrawer.GetBasePropertyHeight(AvatarParametersSaverPresetGroup)));
            AvatarParametersSaverPresetGroupDrawer.OnBaseGUI(groupBasePosition, AvatarParametersSaverPresetGroup, new GUIContent(serializedObject.targetObject.name));
            
            var presetIndex = AvatarParametersSaverPresetGroupDrawer.PresetIndex(AvatarParametersSaverPresetGroup);

            if (presetIndex >= 0)
            {
                Preset = Presets.GetArrayElementAtIndex(presetIndex);
                PresetParameters = Preset.FindPropertyRelative(nameof(AvatarParametersSaverPreset.parameters));
                ParameterNamesSet = MakeParameterNamesSet(PresetParameters);
                if (PresetIndex != presetIndex)
                {
                    RecordObject("preset selection change");
                    PresetIndex = presetIndex;
                    if (PresetIndex >= 0)
                    {
                        ApplyValuesToRuntime(Runtime, Preset);
                    }
                }
                ApplyValuesFromRuntime(Runtime, Preset);

                PresetGUIHeader();

                ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
                PresetGUI();
                EditorGUILayout.EndScrollView();
            }

            if (serializedObject.hasModifiedProperties)
            {
                AvatarParametersSaverPlayModePersist.Store((AvatarParametersPresets)target);
            }
            serializedObject.ApplyModifiedProperties();
        }

        void PresetGUIHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 30;
            ShowBool = ChangeCheckedToggleLeft("Bool", ShowBool, EditorStyles.label);
            ShowInt = ChangeCheckedToggleLeft("Int", ShowInt, UIStyles.IntTextStyle);
            ShowFloat = ChangeCheckedToggleLeft("Float", ShowFloat, UIStyles.FloatTextStyle);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
            PreferEnabledParameters = ChangeCheckedToggleLeft("チェックした項目を優先して並べる", PreferEnabledParameters);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var sortMode = GUILayout.Toolbar(SortMode, SortModes);
                if (check.changed)
                {
                    RecordObject("sort mode changed");
                    SortMode = sortMode;
                }
            }
            AutoCheckChangedParameters = ChangeCheckedToggleLeft("変化したパラメーターを自動でチェック", AutoCheckChangedParameters);
            if (GUILayout.Button("選択をクリア") && EditorUtility.DisplayDialog("選択をクリア", "本当にパラメーターをクリアしますか？", "OK", "Cancel"))
            {
                PresetParameters.ClearArray();
            }
        }

        void PresetGUI()
        {
            foreach (var parameter in SortedParameters())
            {
                DipslayParameter(parameter);
            }
        }

        IEnumerable<VRCExpressionParameters.Parameter> SortedParameters()
        {
            var parameters = Runtime.avadesc.expressionParameters.parameters;

            var sorted = SortMode == 0 ? parameters.AsEnumerable() : parameters.OrderBy(p => p.name);
            if (PreferEnabledParameters)
            {
                sorted = sorted.OrderBy(p => !ParameterNamesSet.Contains(p.name));
            }
            return sorted;
        }

        void DipslayParameter(VRCExpressionParameters.Parameter parameter)
        {
            switch (parameter.valueType)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    {
                        var param = Runtime.Bools.Find(v => v.name == parameter.name);
                        if (param == null)
                        {
                            EditorGUILayout.LabelField($"{parameter.name}がありません");
                            break;
                        }
                        DisplayIsTargetToggle(parameter.name, param.value, parameter.valueType);
                        break;
                    }
                case VRCExpressionParameters.ValueType.Float:
                    {
                        var param = Runtime.Floats.Find(v => v.name == parameter.name);
                        if (param == null)
                        {
                            EditorGUILayout.LabelField($"{parameter.name}がありません");
                            break;
                        }
                        DisplayIsTargetToggle(parameter.name, param.value, parameter.valueType);
                        break;
                    }
                case VRCExpressionParameters.ValueType.Int:
                    {
                        var param = Runtime.Ints.Find(v => v.name == parameter.name);
                        if (param == null)
                        {
                            EditorGUILayout.LabelField($"{parameter.name}がありません");
                            break;
                        }
                        DisplayIsTargetToggle(parameter.name, param.value, parameter.valueType);
                        break;
                    }
            }
        }

        void DisplayIsTargetToggle(string parameter, object value, VRCExpressionParameters.ValueType type)
        {
            if (AutoCheckChangedParameters && CheckChanged(parameter, value) && !ParameterNamesSet.Contains(parameter))
            {
                AddParameter(parameter);
            }
            if (IsShowType(type))
            {
                EditorGUILayout.BeginHorizontal();
                var hasParamter = ParameterNamesSet.Contains(parameter);
                var newHasParameter = EditorGUILayout.ToggleLeft(parameter, hasParamter);
                EditorGUI.BeginDisabledGroup(true);
                switch (type)
                {
                    case VRCExpressionParameters.ValueType.Bool:
                        EditorGUILayout.Toggle((bool)value);
                        break;
                    case VRCExpressionParameters.ValueType.Int:
                        EditorGUILayout.IntField((int)value, UIStyles.IntFieldStyle);
                        break;
                    case VRCExpressionParameters.ValueType.Float:
                        EditorGUILayout.FloatField((float)value, UIStyles.FloatFieldStyle);
                        break;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                if (hasParamter != newHasParameter)
                {
                    if (newHasParameter)
                    {
                        AddParameter(parameter);
                    }
                    else
                    {
                        RemoveParameter(parameter);
                    }
                }
            }
        }


        bool CheckChanged(string parameter, object value)
        {
            if (RuntimeValues.TryGetValue(parameter, out var previousValue))
            {
                if (previousValue is bool)
                {
                    var changed = ((bool)previousValue) != ((bool)value);
                    if (changed) RuntimeValues[parameter] = value;
                    return changed;
                }
                else if (previousValue is int)
                {
                    var changed = ((int)previousValue) != ((int)value);
                    if (changed) RuntimeValues[parameter] = value;
                    return changed;
                }
                else
                {
                    var changed = ((float)previousValue) != ((float)value);
                    if (changed) RuntimeValues[parameter] = value;
                    return changed;
                }
            }
            else
            {
                RuntimeValues[parameter] = value;
            }
            return false;
        }

        bool IsShowType(VRCExpressionParameters.ValueType type)
        {
            switch (type)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    return ShowBool;
                case VRCExpressionParameters.ValueType.Int:
                    return ShowInt;
                case VRCExpressionParameters.ValueType.Float:
                    return ShowFloat;
                default:
                    return false;
            }
        }

        void AddParameter(string name)
        {
            PresetParameters.arraySize++;
            var newParameter = PresetParameters.GetArrayElementAtIndex(PresetParameters.arraySize - 1);
            newParameter.FindPropertyRelative(nameof(AvatarParametersSaverParameter.name)).stringValue = name;
        }

        void RemoveParameter(string name)
        {
            var index = -1;
            for (int i = 0; i < PresetParameters.arraySize; i++)
            {
                if (PresetParameters.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(AvatarParametersSaverParameter.name)).stringValue == name)
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
            {
                PresetParameters.DeleteArrayElementAtIndex(index);
            }
        }

        bool ChangeCheckedToggleLeft(string label, bool value, GUIStyle labelStyle = null)
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var result = labelStyle == null ? EditorGUILayout.ToggleLeft(label, value) : EditorGUILayout.ToggleLeft(label, value, labelStyle);
                if (check.changed)
                {
                    RecordObject($"{label} changed");
                }
                return result;
            }
        }

        void RecordObject(string message = "editor state change")
        {
            Undo.RecordObject(this, message);
        }

        static void ApplyValuesToRuntime(LyumaAv3Runtime runtime, SerializedProperty preset)
        {
            var parametersMap = MakeParametersMap(preset);
            foreach (var exParameter in runtime.avadesc.expressionParameters.parameters)
            {
                if (parametersMap.TryGetValue(exParameter.name, out var parameter))
                {
                    switch (exParameter.valueType)
                    {
                        case VRCExpressionParameters.ValueType.Bool:
                            {
                                var runtimeParameter = runtime.Bools.Find(p => p.name == exParameter.name);
                                runtimeParameter.value = parameter.floatValue > 0.5f;
                                break;
                            }
                        case VRCExpressionParameters.ValueType.Int:
                            {
                                var runtimeParameter = runtime.Ints.Find(p => p.name == exParameter.name);
                                runtimeParameter.value = (int)parameter.floatValue;
                                break;
                            }
                        case VRCExpressionParameters.ValueType.Float:
                            {
                                var runtimeParameter = runtime.Floats.Find(p => p.name == exParameter.name);
                                runtimeParameter.expressionValue = parameter.floatValue;
                                break;
                            }
                    }
                }
            }
        }


        static void ApplyValuesFromRuntime(LyumaAv3Runtime runtime, SerializedProperty preset)
        {
            var parametersMap = MakeParametersMap(preset);
            foreach (var exParameter in runtime.avadesc.expressionParameters.parameters)
            {
                if (parametersMap.TryGetValue(exParameter.name, out var parameter))
                {
                    parameter.floatValue = GetParameterValue(runtime, exParameter);
                }
            }
        }

        static float GetParameterValue(LyumaAv3Runtime runtime, VRCExpressionParameters.Parameter parameter)
        {
            switch (parameter.valueType)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    {
                        var param = runtime.Bools.Find(v => v.name == parameter.name);
                        if (param == null)
                        {
                            return float.NaN;
                        }
                        return param.value ? 1 : 0;
                    }
                case VRCExpressionParameters.ValueType.Float:
                    {
                        var param = runtime.Floats.Find(v => v.name == parameter.name);
                        if (param == null)
                        {
                            return float.NaN;
                        }
                        return param.value;
                    }
                case VRCExpressionParameters.ValueType.Int:
                    {
                        var param = runtime.Ints.Find(v => v.name == parameter.name);
                        if (param == null)
                        {
                            return float.NaN;
                        }
                        return param.value;
                    }
                default:
                    return float.NaN;
            }
        }

        static Dictionary<string, SerializedProperty> MakeParametersMap(SerializedProperty preset)
        {
            var parameters = preset.FindPropertyRelative(nameof(AvatarParametersSaverPreset.parameters));
            var map = new Dictionary<string, SerializedProperty>();
            for (int i = 0; i < parameters.arraySize; i++)
            {
                var parameter = parameters.GetArrayElementAtIndex(i);
                map[parameter.FindPropertyRelative(nameof(AvatarParametersSaverParameter.name)).stringValue] = parameter.FindPropertyRelative(nameof(AvatarParametersSaverParameter.value));
            }
            return map;
        }

        static HashSet<string> MakeParameterNamesSet(SerializedProperty parameters)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < parameters.arraySize; i++)
            {
                set.Add(parameters.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(AvatarParametersSaverParameter.name)).stringValue);
            }
            return set;
        }
    }
}
