using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.IO;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using System.Linq;

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

        // GUI

        static GUIStyle IntTextStyle()
        {
            if (IntTextStyleCache == null)
            {
                IntTextStyleCache = new GUIStyle(EditorStyles.label);
                IntTextStyleCache.normal.textColor = Color.red;
            }
            return IntTextStyleCache;
        }
        static GUIStyle IntTextStyleCache;


        static GUIStyle FloatTextStyle()
        {
            if (FloatTextStyleCache == null)
            {
                FloatTextStyleCache = new GUIStyle(EditorStyles.label);
                FloatTextStyleCache.normal.textColor = Color.green;
            }
            return FloatTextStyleCache;
        }
        static GUIStyle FloatTextStyleCache;

        static GUIStyle IntFieldStyle()
        {
            if (IntFieldStyleCache == null)
            {
                IntFieldStyleCache = new GUIStyle(EditorStyles.textField);
                IntFieldStyleCache.normal.textColor = Color.red;
            }
            return IntFieldStyleCache;
        }
        static GUIStyle IntFieldStyleCache;


        static GUIStyle FloatFieldStyle()
        {
            if (FloatFieldStyleCache == null)
            {
                FloatFieldStyleCache = new GUIStyle(EditorStyles.textField);
                FloatFieldStyleCache.normal.textColor = Color.green;
            }
            return FloatFieldStyleCache;
        }
        static GUIStyle FloatFieldStyleCache;

        // UI

        bool AutoCheckChangedParameters = true;
        bool PreferEnabledParameters = true;
        int SortMode;
        static string[] SortModes = new string[] { "設定順", "名前順" };
        HashSet<VRCExpressionParameters.ValueType> HideTypes = new HashSet<VRCExpressionParameters.ValueType>();
        bool IsShow(VRCExpressionParameters.ValueType type)
        {
            return !HideTypes.Contains(type);
        }
        void AdjustShow(VRCExpressionParameters.ValueType type, bool isShow)
        {
            if (isShow)
            {
                HideTypes.Remove(type);
            }
            else
            {
                HideTypes.Add(type);
            }
        }
        void DisplayShowToggle(VRCExpressionParameters.ValueType type, GUIStyle style)
        {
            AdjustShow(type, EditorGUILayout.ToggleLeft(type.ToString(), IsShow(type), style));
        }

        AvatarParametersPresets PresetsComponent;
        AvatarParametersPresets SelectingPresetsComponent;
        AvatarParametersSaverPresetGroup Presets;
        int CurrentPresetIndex
        {
            get => PresetsList == null || PresetsList.index == -1 ? 0 : PresetsList.index;
        }
        int PreviousPresetIndex;
        AvatarParametersSaverPreset DriveParameter
        {
            get
            {
                if (Presets.presets.Count < CurrentPresetIndex + 1)
                {
                    Presets.presets.AddRange(Enumerable.Range(0, CurrentPresetIndex + 1 - Presets.presets.Count).Select((_) => new AvatarParametersSaverPreset()));
                }
                return Presets.presets[CurrentPresetIndex];
            }
        }
        static string[] SyncModes = new string[] { "プリセットパラメータを同期", "結果パラメータを同期" };

        // UI

        VRCAvatarDescriptor SelectingAvatar;
        VRCAvatarDescriptor Avatar;
        Dictionary<string, object> PreviousValues = new Dictionary<string, object>();
        bool Advanced;

        Vector2 scrollPos;

        SerializedObject so;
        UnityEditorInternal.ReorderableList PresetsList;

        void Update()
        {
            Repaint();
        }

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Playしてください", MessageType.Info);
                return;
            }

            if (Avatar == null)
            {
                EditorGUILayout.HelpBox("Avatarを選択して下さい", MessageType.Info);
                var avatar = Selection.activeGameObject?.GetComponent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    SelectingAvatar = avatar;
                }
                SelectingAvatar = EditorGUILayout.ObjectField("Avatar", SelectingAvatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;

                if (GUILayout.Button("選択"))
                {
                    Avatar = SelectingAvatar;
                    SelectingAvatar = null;
                    PreviousValues.Clear();
                }
                return;
            }
            else
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField("Avatar", Avatar, typeof(VRCAvatarDescriptor), true);
                }
                if (GUILayout.Button("選択解除"))
                {
                    if (!EditorUtility.DisplayDialog("警告", "保存していない設定はクリアされますがよろしいですか？", "OK", "Cancel")) return;
                    Avatar = null;
                    PreviousValues.Clear();
                    return;
                }
            }

            var runtime = Avatar.GetComponent<LyumaAv3Runtime>();
            if (runtime == null)
            {
                EditorGUILayout.LabelField("AV3Emulatorが有効であることを確認して下さい");
            }

            if (Presets == null)
            {
                EditorGUILayout.HelpBox("プリセットPrefabを選択して下さい", MessageType.Info);
                SelectingPresetsComponent = EditorGUILayout.ObjectField("プリセットPrefab", SelectingPresetsComponent, typeof(AvatarParametersPresets), false) as AvatarParametersPresets;
                if (GUILayout.Button("新規作成"))
                {
                    SelectingPresetsComponent = CreatePrefab();
                }
                if (GUILayout.Button("選択"))
                {
                    PresetsComponent = SelectingPresetsComponent;
                    Presets = SelectingPresetsComponent.AvatarParametersSaverPresetGroup;
                    so = new SerializedObject(PresetsComponent);
                    PresetsList = null;
                    PreviousValues.Clear();
                }
                return;
            }
            else
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField("プリセットPrefab", PresetsComponent, typeof(AvatarParametersPresets), false);
                }
                if (GUILayout.Button("選択解除"))
                {
                    if (!EditorUtility.DisplayDialog("警告", "保存していない設定はクリアされますがよろしいですか？", "OK", "Cancel")) return;
                    PresetsComponent = null;
                    Presets = null;
                    so = null;
                    PresetsList = null;
                    PreviousValues.Clear();
                    return;
                }
            }

            EditorGUILayout.LabelField("全般設定", EditorStyles.boldLabel);

            Presets.networkSynced = GUILayout.Toolbar(Presets.networkSynced ? 0 : 1, SyncModes) == 0;

            if (Presets.networkSynced)
            {
                EditorGUILayout.HelpBox("プリセットパラメーターをSyncし、VRC_AvatarParameterDriver側で値を同期せずにlocalOnlyで値を変更するモードです。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("プリセットパラメーターをSyncせず、VRC_AvatarParameterDriver側で値を同期して変更するモードです。", MessageType.Info);
            }
            Presets.parameterName = EditorGUILayout.TextField("プリセットパラメーター名", Presets.parameterName);

            Advanced = EditorGUILayout.Foldout(Advanced, "高度な設定");
            if (Advanced)
            {
                Presets.IndexOffset = EditorGUILayout.IntField("パラメーター値のオフセット", Presets.IndexOffset);
            }

            EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);
            if (PresetsList == null)
            {
                var presets = so.FindProperty("AvatarParametersSaverPresetGroup").FindPropertyRelative("presets");
                Debug.Log(Presets);
                Debug.Log(Presets.presets);
                Debug.Log(presets);
                PresetsList = new UnityEditorInternal.ReorderableList(so, presets);
                PresetsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "プリセット");
                var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                PresetsList.elementHeightCallback = index => height;
                PresetsList.drawElementCallback = (rect, index, _isActive, isFocused) =>
                {
                    var p = Presets.presets[index];
                    var menuName = p.menuName;
                    var paramCount = p.parameters.Count;
                    rect.width -= 70;
                    EditorGUIUtility.labelWidth = 110;
                    p.menuName = EditorGUI.TextField(rect, $"{index + 1 + Presets.IndexOffset} プリセットメニュー名", menuName);
                    EditorGUIUtility.labelWidth = 0;
                    rect.x += rect.width;
                    rect.width = 70;
                    EditorGUI.LabelField(rect, $"({paramCount} パラメータ)");
                };
            }
            so.Update();
            PresetsList.DoLayoutList();
            so.ApplyModifiedProperties();

            var defaultColor = GUI.backgroundColor;

            if (PreviousPresetIndex != CurrentPresetIndex)
            {
                DriveParameter.ValuesToRuntime(runtime, Avatar.expressionParameters.parameters);
                PreviousPresetIndex = CurrentPresetIndex;
            }
            DriveParameter.ApplyValues(runtime, Avatar.expressionParameters.parameters);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("メニューを保存"))
            {
                if (string.IsNullOrEmpty(Presets.parameterName))
                {
                    EditorUtility.DisplayDialog("Error", "プリセットパラメーター名を指定して下さい", "OK");
                    return;
                }
                if (Presets.presets.Any(p => string.IsNullOrEmpty(p.menuName)))
                {
                    EditorUtility.DisplayDialog("Error", $"空になっているプリセットメニュー名を指定して下さい", "OK");
                    return;
                }
                Save(runtime, Avatar);
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.LabelField("パラメーター", EditorStyles.boldLabel);

            AutoCheckChangedParameters = EditorGUILayout.ToggleLeft("変化したパラメーターを自動でチェック", AutoCheckChangedParameters);
            if (GUILayout.Button("選択をクリア"))
            {
                DriveParameter.parameters.Clear();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var parameter in SortedParameters(Avatar.expressionParameters.parameters))
            {
                DipslayParameter(runtime, parameter);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("表示", EditorStyles.boldLabel);

            SortMode = GUILayout.Toolbar(SortMode, SortModes);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 30;
            DisplayShowToggle(VRCExpressionParameters.ValueType.Bool, EditorStyles.label);
            DisplayShowToggle(VRCExpressionParameters.ValueType.Int, IntTextStyle());
            DisplayShowToggle(VRCExpressionParameters.ValueType.Float, FloatTextStyle());
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
            PreferEnabledParameters = EditorGUILayout.ToggleLeft("チェックした項目を優先して並べる", PreferEnabledParameters);
        }

        IEnumerable<VRCExpressionParameters.Parameter> SortedParameters(VRCExpressionParameters.Parameter[] parameters)
        {
            var sorted = SortMode == 0 ? parameters.AsEnumerable() : parameters.OrderBy(p => p.name);
            if (PreferEnabledParameters)
            {
                sorted = sorted.OrderBy(p => !DriveParameter.IsTarget(p.name));
            }
            return sorted;
        }

        void DipslayParameter(LyumaAv3Runtime runtime, VRCExpressionParameters.Parameter parameter)
        {
            switch (parameter.valueType)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    {
                        var param = runtime.Bools.Find(v => v.name == parameter.name);
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
                        var param = runtime.Floats.Find(v => v.name == parameter.name);
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
                        var param = runtime.Ints.Find(v => v.name == parameter.name);
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
            if (AutoCheckChangedParameters && CheckChanged(parameter, value))
            {
                DriveParameter.AdjustTargetParameter(parameter, true);
            }
            if (IsShow(type))
            {
                EditorGUILayout.BeginHorizontal();
                var result = EditorGUILayout.ToggleLeft(parameter, DriveParameter.IsTarget(parameter));
                EditorGUI.BeginDisabledGroup(true);
                switch (type)
                {
                    case VRCExpressionParameters.ValueType.Bool:
                        EditorGUILayout.Toggle((bool)value);
                        break;
                    case VRCExpressionParameters.ValueType.Int:
                        EditorGUILayout.IntField((int)value, IntFieldStyle());
                        break;
                    case VRCExpressionParameters.ValueType.Float:
                        EditorGUILayout.FloatField((float)value, FloatFieldStyle());
                        break;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                DriveParameter.AdjustTargetParameter(parameter, result);
            }
        }

        bool CheckChanged(string parameter, object value)
        {
            if (PreviousValues.TryGetValue(parameter, out var previousValue))
            {
                if (previousValue is bool)
                {
                    var changed = ((bool)previousValue) != ((bool)value);
                    if (changed) PreviousValues[parameter] = value;
                    return changed;
                }
                else if (previousValue is int)
                {
                    var changed = ((int)previousValue) != ((int)value);
                    if (changed) PreviousValues[parameter] = value;
                    return changed;
                }
                else
                {
                    var changed = ((float)previousValue) != ((float)value);
                    if (changed) PreviousValues[parameter] = value;
                    return changed;
                }
            }
            else
            {
                PreviousValues[parameter] = value;
            }
            return false;
        }

        AvatarParametersPresets CreatePrefab()
        {
            var path = EditorUtility.SaveFilePanelInProject("save prefab", "AvatarParametersPresets", "prefab", "save prefab", "Assets");

            var go = new GameObject("AvatarParametersPresets");
            go.AddComponent<ModularAvatarMenuInstaller>();
            go.AddComponent<AvatarParametersPresets>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);

            var prefab = PrefabUtility.LoadPrefabContents(path);
            var presets = prefab.GetComponent<AvatarParametersPresets>();
            PrefabUtility.UnloadPrefabContents(prefab);
            return presets;
        }

        void Save(LyumaAv3Runtime runtime, VRCAvatarDescriptor avatar)
        {
            var path = AssetDatabase.GetAssetPath(PresetsComponent);
            var prefab = PrefabUtility.LoadPrefabContents(path);

            var presets = prefab.GetComponent<AvatarParametersPresets>();

            presets.AvatarParametersSaverPresetGroup = Presets;

            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}
