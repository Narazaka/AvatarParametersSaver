using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using Lyuma.Av3Emulator.Runtime;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    public class AvatarParametersSaver : EditorWindow
    {
        [MenuItem("Tools/Avatar Parameters Saver")]
        public static void Open()
        {
            GetWindow<AvatarParametersSaver>("Avatar Parameters Saver");
        }

        AvatarParametersPresetsRuntimeEditor PresetEditor;
        AvatarParametersSaverPlayModePersist.StoreDataGroupAsset Presets;

        VRCAvatarDescriptor Avatar;
        AvatarParametersSaverPlayModePersist.StoreDataGroupAsset[] AvatarParametersPresetsList;

        void Update()
        {
            Repaint();
        }

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Playしてください", MessageType.Info);
                EditorGUILayout.HelpBox("プリセットはアバターオブジェクトを右クリックして「Modular Avatar」→「AvatarParametersPresets」で作成できます。", MessageType.Info);
                return;
            }

            var avatar = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();
            if (Avatar != avatar)
            {
                Debug.Log("Avatar changed");
                Avatar = avatar;
                AvatarParametersPresetsList = null;
            }
            if (AvatarParametersPresetsList == null)
            {
                AvatarParametersPresetsList = Avatar == null ? null : AvatarParametersSaverPlayModePersist.ForAvatar(Avatar);
            }

            if (Avatar == null)
            {
                Avatar = null;
                EditorGUILayout.HelpBox("アバターを選択して下さい", MessageType.Info);
                return;
            }

            var runtime = Avatar.GetComponent<LyumaAv3Runtime>();
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("AV3Emulatorが有効であることを確認して下さい", MessageType.Info);
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Avatar", Avatar, typeof(VRCAvatarDescriptor), true);
            EditorGUI.EndDisabledGroup();

            if (AvatarParametersPresetsList != null && AvatarParametersPresetsList.Length > 0)
            {
                EditorGUILayout.LabelField("プリセット");
                foreach (var presets in AvatarParametersPresetsList)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (EditorGUILayout.Toggle(presets == Presets, GUILayout.Width(30)) && presets != Presets)
                    {
                        Presets = presets;
                        PresetEditor = Editor.CreateEditor(Presets, typeof(AvatarParametersPresetsRuntimeEditor)) as AvatarParametersPresetsRuntimeEditor;
                    }
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(presets, typeof(AvatarParametersSaverPlayModePersist.StoreDataGroupAsset), true);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("プリセットがありません", MessageType.Info);
                EditorGUILayout.HelpBox("Playモードを抜けてアバターにプリセットを作成して下さい。\nアバターオブジェクトを右クリックして「Modular Avatar」→「AvatarParametersPresets」で作成できます。", MessageType.Info);
                return;
            }

            if (Presets == null)
            {
                Presets = null;
                EditorGUILayout.HelpBox("プリセットを選択して下さい", MessageType.Info);
            }
            EditorGUILayout.HelpBox("プリセットを追加・削除する場合はPlayモードを抜けて下さい", MessageType.Info);
            if (Presets == null)
            {
                return;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{NoOptName(Presets.name)}", UIStyles.HeaderLabel);
            EditorGUILayout.Space();

            PresetEditor.Runtime = runtime;
            PresetEditor.OnInspectorGUI();
        }

        System.Text.RegularExpressions.Regex OptNameRe = new System.Text.RegularExpressions.Regex(@"^.*?([^$]+)\$[^$]+$");

        string NoOptName(string name)
        {
            return OptNameRe.Replace(name, @"$1");
        }
    }
}
