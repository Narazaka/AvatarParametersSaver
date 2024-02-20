using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using nadena.dev.modular_avatar.core;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [CustomPropertyDrawer(typeof(AvatarParametersSaverPresetGroup))]
    public class AvatarParametersSaverPresetGroupDrawer : PropertyDrawer
    {
        SerializedProperty Current;
        SerializedProperty Icon;
        SerializedProperty ParameterName;
        SerializedProperty NetworkSynced;
        SerializedProperty Presets;
        SerializedProperty IndexOffset;
        SerializedProperty InstallParent;
        ReorderableList PresetsList;
        bool ShowAdvanced;
        bool ShowCustomize;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = OnBaseGUI(position, property, label);
            if (ShowCreatePresetGUI(property))
            {
                position = OnCreatePresetGUI(position, property, label);
            }
            if (ShowPresetContents)
            {
                OnPresetGUI(position, property, label);
            }
        }

        public Rect OnBaseGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdatePropertiesIfNeeded(property);
            position.yMin += EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(SingleLineRect(position), Icon);
            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(SingleLineRect(position), InstallParent, new GUIContent("親メニューを作る"));
            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
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
                EditorGUI.HelpBox(HeightRect(position, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing), "パラメーター名を指定しない場合、オブジェクト名が内部パラメーター名として使われます。", MessageType.Info);
                position.yMin += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                EditorGUI.PropertyField(SingleLineRect(position), IndexOffset);
                position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;
            }
            var height = PresetsList.GetHeight();
            PresetsList.DoList(HeightRect(position, height));
            position.yMin += height + EditorGUIUtility.standardVerticalSpacing;
            return position;
        }

        public Rect OnCreatePresetGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdatePropertiesIfNeeded(property);
            if (ShowCreatePresetGUI(property))
            {
                EditorGUI.indentLevel++;
                ShowCustomize = EditorGUI.Foldout(SingleLineRect(position), ShowCustomize, new GUIContent("Customize"));
                position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (ShowCustomize)
                {
                    var rect = HeightRect(position, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);
                    rect.width -= 60;
                    EditorGUI.HelpBox(rect, "プリセットと同名の子オブジェクトに MA Menu Installer を付けるとインストール先を個別に選択できます。", MessageType.Info);
                    rect.x += rect.width;
                    rect.width = 60;
                    var menuName = CurrentPreset.FindPropertyRelative(nameof(AvatarParametersSaverPreset.menuName)).stringValue;
                    var parentTransform = (property.serializedObject.targetObject as Component).transform;
                    var menuTransform = parentTransform.Find(menuName);
                    if (menuTransform == null)
                    {
                        if (GUI.Button(rect, "Create"))
                        {
                            var go = new GameObject(menuName, typeof(ModularAvatarMenuInstaller));
                            go.transform.SetParent(parentTransform);
                            EditorGUIUtility.PingObject(go);
                        }
                    }
                    else
                    {
                        if (GUI.Button(rect, "Ping"))
                        {
                            EditorGUIUtility.PingObject(menuTransform.gameObject);
                        }
                    }
                    position.yMin += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                }
                EditorGUI.indentLevel--;
            }
            return position;
        }

        public void OnPresetGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdatePropertiesIfNeeded(property);
            if (ShowPresetContents)
            {
                EditorGUI.PropertyField(position, CurrentPreset, new GUIContent(CurrentPreset.FindPropertyRelative(nameof(AvatarParametersSaverPreset.menuName)).stringValue));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetBasePropertyHeight(property, label) + GetCreatePresetPropertyHeight(property, label) + GetPresetPropertyHeight(property, label);
        }

        public float GetBasePropertyHeight(SerializedProperty property, GUIContent label = null)
        {
            UpdatePropertiesIfNeeded(property);
            var lines = ShowAdvanced ? 10 : 3;
            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines + 2) + PresetsList.GetHeight();
        }

        public float GetCreatePresetPropertyHeight(SerializedProperty property, GUIContent label = null)
        {
            UpdatePropertiesIfNeeded(property);
            if (!ShowCreatePresetGUI(property)) return 0;
            var lines = ShowCustomize ? 3 : 2;
            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines + 1);
        }

        public float GetPresetPropertyHeight(SerializedProperty property, GUIContent label = null)
        {
            UpdatePropertiesIfNeeded(property);
            return ShowPresetContents ? EditorGUI.GetPropertyHeight(CurrentPreset) + EditorGUIUtility.standardVerticalSpacing : 0;
        }

        bool ShowCreatePresetGUI(SerializedProperty property)
        {
            UpdatePropertiesIfNeeded(property);
            return ShowPresetContents && !EditorApplication.isPlaying && (property.serializedObject.targetObject as Component) != null;
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
            Icon = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.icon));
            ParameterName = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.parameterName));
            NetworkSynced = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.networkSynced));
            Presets = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.presets));
            IndexOffset = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.IndexOffset));
            InstallParent = Current.FindPropertyRelative(nameof(AvatarParametersSaverPresetGroup.installParent));
            PresetsList = new ReorderableList(Current.serializedObject, Presets);
            PresetsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Presets");
            PresetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = Presets.GetArrayElementAtIndex(index);
                var icon = element.FindPropertyRelative(nameof(AvatarParametersSaverPreset.icon));
                var menuName = element.FindPropertyRelative(nameof(AvatarParametersSaverPreset.menuName));
                var parameters = element.FindPropertyRelative(nameof(AvatarParametersSaverPreset.parameters));
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= 102;
                var width = rect.width;
                rect.width = width * 3 / 4;
                EditorGUIUtility.labelWidth = 55;
                EditorGUI.PropertyField(rect, menuName, new GUIContent($"Preset {avatar_parameters_saver.AvatarParametersSaverPresetGroup.GetPresetParameterValue(index, IndexOffset.intValue)}"));
                EditorGUIUtility.labelWidth = 0;
                rect.x += rect.width + 2;
                rect.width = width / 4;
                EditorGUI.PropertyField(rect, icon, GUIContent.none);
                rect.x += rect.width;
                rect.width = 100;
                EditorGUI.LabelField(rect, $"{parameters.arraySize} Parameters");
            };
            PresetsList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }
}
