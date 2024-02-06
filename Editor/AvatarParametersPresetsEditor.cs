using nadena.dev.modular_avatar.core;
using UnityEditor;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [CustomEditor(typeof(AvatarParametersPresets))]
    public class AvatarParametersPresetsEditor : Editor
    {
        SerializedProperty AvatarParametersSaverPresetGroup;

        void OnEnable()
        {
            AvatarParametersSaverPresetGroup = serializedObject.FindProperty(nameof(AvatarParametersPresets.AvatarParametersSaverPresetGroup));
        }

        public override void OnInspectorGUI()
        {
            if ((target as AvatarParametersPresets).GetComponent<ModularAvatarMenuInstaller>() == null)
            {
                EditorGUILayout.HelpBox("このコンポーネントはMA Menu Itemのように振る舞います。\nMA Menu Installerを同時に付けるとインストール先を指定できます。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("このコンポーネントはMA Menu Itemのように振る舞います。\nMA Menu Installerのインストール先にメニューが生成されます。", MessageType.Info);
            }
            serializedObject.Update();
            EditorGUILayout.PropertyField(AvatarParametersSaverPresetGroup);
            if (serializedObject.hasModifiedProperties)
            {
                AvatarParametersSaverPlayModePersist.Store((AvatarParametersPresets)target);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
