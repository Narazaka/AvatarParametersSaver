using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Lyuma.Av3Emulator.Runtime;
using VRC.SDK3.Avatars.Components;
using Narazaka.VRChat.AvatarParametersUtil.Editor;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [CustomPropertyDrawer(typeof(AvatarParametersSaverPreset))]
    public class AvatarParametersSaverPresetDrawer : PropertyDrawer
    {
        ReorderableList ParametersList;
        AvatarParametersUtilEditor AvatarParametersUtilEditor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CreateParameterList(property, label);
            ParametersList.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CreateParameterList(property, label);
            return ParametersList.GetHeight();
        }

        void CreateParameterList(SerializedProperty property, GUIContent label)
        {
            if (SerializedProperty.EqualContents(ParametersList?.serializedProperty, property.FindPropertyRelative(nameof(AvatarParametersSaverPreset.parameters)))) return;
            if (AvatarParametersUtilEditor == null) AvatarParametersUtilEditor = new AvatarParametersUtilEditor(property.serializedObject);  

            var runtime = GetRuntime(property);
            var parameters = property.FindPropertyRelative(nameof(AvatarParametersSaverPreset.parameters));
            ParametersList = new ReorderableList(parameters.serializedObject, parameters);
            ParametersList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, label);
            ParametersList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = parameters.GetArrayElementAtIndex(index);
                var name = element.FindPropertyRelative(nameof(AvatarParametersSaverParameter.name));
                var value = element.FindPropertyRelative(nameof(AvatarParametersSaverParameter.value));
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= 100;
                EditorGUIUtility.labelWidth = 55;
                AvatarParametersUtilEditor.ShowParameterNameField(rect, name);
                EditorGUIUtility.labelWidth = 0;
                rect.x += rect.width;
                rect.width = 100;
                AvatarParametersUtilEditor.ShowParameterValueField(rect, name.stringValue, value, GUIContent.none);
            };
            ParametersList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        VRCAvatarDescriptor GetAvatar(SerializedProperty property)
        {
            var component = property.serializedObject.targetObject as Component;
            if (component == null) return null;
            return component.GetComponentInParent<VRCAvatarDescriptor>();
        }

        LyumaAv3Runtime GetRuntime(SerializedProperty property)
        {
            var component = property.serializedObject.targetObject as Component;
            if (component == null) return null;
            return component.GetComponentInParent<LyumaAv3Runtime>();
        }
    }
}
