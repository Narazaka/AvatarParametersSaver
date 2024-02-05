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
