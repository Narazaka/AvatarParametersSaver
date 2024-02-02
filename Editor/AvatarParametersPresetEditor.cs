using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [CustomEditor(typeof(AvatarParametersPresets))]
    public class AvatarParametersPresetsEditor : Editor
    {
        SerializedProperty AvatarParametersSaverPresetGroup;
        SerializedProperty ParameterName;
        SerializedProperty NetworkSynced;
        SerializedProperty Presets;
        SerializedProperty IndexOffset;
        ReorderableList PresetsList;
        ReorderableList ParametersList;

        void OnEnable()
        {
            AvatarParametersSaverPresetGroup = serializedObject.FindProperty("AvatarParametersSaverPresetGroup");
            ParameterName = AvatarParametersSaverPresetGroup.FindPropertyRelative("parameterName");
            NetworkSynced = AvatarParametersSaverPresetGroup.FindPropertyRelative("networkSynced");
            Presets = AvatarParametersSaverPresetGroup.FindPropertyRelative("presets");
            IndexOffset = AvatarParametersSaverPresetGroup.FindPropertyRelative("IndexOffset");
            PresetsList = new ReorderableList(serializedObject, Presets);
            PresetsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Presets");
            PresetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = Presets.GetArrayElementAtIndex(index);
                var menuName = element.FindPropertyRelative("menuName");
                var parameters = element.FindPropertyRelative("parameters");
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= 100;
                EditorGUI.PropertyField(rect, menuName);
                rect.x += rect.width;
                rect.width = 100;
                EditorGUI.LabelField(rect, $"{parameters.arraySize} Parameters");
            };
            PresetsList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            PresetsList.onReorderCallback = list =>
            {
                CreateParameterList();
            };
            PresetsList.onSelectCallback = list =>
            {
                CreateParameterList();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(ParameterName);
            EditorGUILayout.PropertyField(NetworkSynced);
            EditorGUILayout.PropertyField(IndexOffset);
            PresetsList.DoLayoutList();
            if (ParametersList != null)
            {
                ParametersList.DoLayoutList();
            }
            serializedObject.ApplyModifiedProperties();
        }

        void CreateParameterList()
        {
            if (!(Presets.arraySize > PresetsList.index && PresetsList.index >= 0)) return;

            var parameters = Presets.GetArrayElementAtIndex(PresetsList.index).FindPropertyRelative("parameters");
            ParametersList = new ReorderableList(serializedObject, parameters);
            ParametersList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Parameters");
            ParametersList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = parameters.GetArrayElementAtIndex(index);
                var name = element.FindPropertyRelative("name");
                var value = element.FindPropertyRelative("value");
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= 100;
                EditorGUI.PropertyField(rect, name);
                rect.x += rect.width;
                rect.width = 100;
                EditorGUI.PropertyField(rect, value, GUIContent.none);
            };
            ParametersList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }
}
