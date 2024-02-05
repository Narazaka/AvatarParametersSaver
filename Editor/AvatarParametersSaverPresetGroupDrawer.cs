using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [CustomPropertyDrawer(typeof(AvatarParametersSaverPresetGroup))]
    public class AvatarParametersSaverPresetGroupDrawer : PropertyDrawer
    {
        SerializedProperty Current;
        SerializedProperty ParameterName;
        SerializedProperty NetworkSynced;
        SerializedProperty Presets;
        SerializedProperty IndexOffset;
        ReorderableList PresetsList;
        bool ShowAdvanced;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = OnBaseGUI(position, property, label);
            if (ShowPresetContents)
            {
                OnPresetGUI(position, property, label);
            }
        }

        public Rect OnBaseGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdatePropertiesIfNeeded(property);
            position.yMin += EditorGUIUtility.standardVerticalSpacing;
            ShowAdvanced = EditorGUI.Foldout(SingleLineRect(position), ShowAdvanced, new GUIContent("Advanced"));
            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (ShowAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(SingleLineRect(position), NetworkSynced);
                position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (NetworkSynced.boolValue)
                {
                    EditorGUI.HelpBox(HeightRect(position, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing), "プリセットパラメーターをSyncし、VRC_AvatarParameterDriver側で値を同期せずにlocalOnlyで値を変更するモードです。", MessageType.Info);
                }
                else
                {
                    EditorGUI.HelpBox(HeightRect(position, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing), "プリセットパラメーターをSyncせず、VRC_AvatarParameterDriver側で値を同期して変更するモードです。", MessageType.Info);
                }
                position.yMin += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                EditorGUI.PropertyField(SingleLineRect(position), ParameterName);
                position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(SingleLineRect(position), IndexOffset);
                position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;
            }
            var height = PresetsList.GetHeight();
            PresetsList.DoList(HeightRect(position, height));
            position.yMin += height + EditorGUIUtility.standardVerticalSpacing;
            return position;
        }

        public void OnPresetGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdatePropertiesIfNeeded(property);
            if (ShowPresetContents)
            {
                EditorGUI.PropertyField(position, CurrentPreset);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetBasePropertyHeight(property, label) + GetPresetPropertyHeight(property, label);
        }

        public float GetBasePropertyHeight(SerializedProperty property, GUIContent label = null)
        {
            UpdatePropertiesIfNeeded(property);
            var lines = ShowAdvanced ? 6 : 1;
            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines + 2) + PresetsList.GetHeight();
        }

        public float GetPresetPropertyHeight(SerializedProperty property, GUIContent label = null)
        {
            UpdatePropertiesIfNeeded(property);
            return ShowPresetContents ? EditorGUI.GetPropertyHeight(CurrentPreset) + EditorGUIUtility.standardVerticalSpacing : 0;
        }

        Rect SingleLineRect(Rect position)
        {
            return HeightRect(position, EditorGUIUtility.singleLineHeight);
        }

        Rect HeightRect(Rect position, float height)
        {
            position.height = height;
            return position;
        }

        public int PresetIndex(SerializedProperty property)
        {
            UpdatePropertiesIfNeeded(property);
            return PresetsList.index;
        }

        public void SetPresetIndex(SerializedProperty property, int index)
        {
            UpdatePropertiesIfNeeded(property);
            PresetsList.index = index;
        }

        bool ShowPresetContents => PresetsList.index >= 0 && Presets.arraySize > PresetsList.index;

        SerializedProperty CurrentPreset => Presets.GetArrayElementAtIndex(PresetsList.index);

        void UpdatePropertiesIfNeeded(SerializedProperty property)
        {
            if (!SerializedProperty.EqualContents(Current, property))
            {
                Current = property;
                UpdateProperties();
            }
        }

        void UpdateProperties()
        {
            ParameterName = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.parameterName));
            NetworkSynced = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.networkSynced));
            Presets = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.presets));
            IndexOffset = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.IndexOffset));
            PresetsList = new ReorderableList(Current.serializedObject, Presets);
            PresetsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Presets");
            PresetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = Presets.GetArrayElementAtIndex(index);
                var menuName = element.FindPropertyRelative("menuName");
                var parameters = element.FindPropertyRelative("parameters");
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= 100;
                EditorGUI.PropertyField(rect, menuName, new GUIContent($"Preset {avatar_parameters_saver.AvatarParametersSaverPresetGroup.GetPresetParameterValue(index, IndexOffset.intValue)}"));
                rect.x += rect.width;
                rect.width = 100;
                EditorGUI.LabelField(rect, $"{parameters.arraySize} Parameters");
            };
            PresetsList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }
}
