using UnityEngine;
using UnityEditor;
using nadena.dev.modular_avatar.core;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [CustomEditor(typeof(AvatarParametersSaverPresets))]
    public class AvatarParametersSaverPresetsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Convert to new prefab"))
            {
                var path = EditorUtility.SaveFilePanelInProject("save prefab", target.name, "prefab", "save prefab", System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(target)));

                var go = new GameObject(target.name);
                go.AddComponent<ModularAvatarMenuInstaller>();
                var presets = go.AddComponent<AvatarParametersPresets>();
                presets.AvatarParametersSaverPresetGroup = new AvatarParametersSaverPresetGroup();
                var asset = target as AvatarParametersSaverPresets;
                presets.AvatarParametersSaverPresetGroup.parameterName = asset.parameterName;
                presets.AvatarParametersSaverPresetGroup.networkSynced = asset.networkSynced;
                presets.AvatarParametersSaverPresetGroup.presets = asset.presets;
                presets.AvatarParametersSaverPresetGroup.IndexOffset = asset.IndexOffset;

                PrefabUtility.SaveAsPrefabAsset(go, path);
                DestroyImmediate(go);
            }
            EditorGUILayout.HelpBox("このアセットは古いプリセットデータです。新しいGameObjectベースのプリセットデータに変換します。", MessageType.Warning);
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
