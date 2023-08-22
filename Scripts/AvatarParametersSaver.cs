using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.IO;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using System.Linq;

#if UNITY_EDITOR

using UnityEditor;
using Lyuma.Av3Emulator.Runtime;

public class AvatarParametersSaver : EditorWindow
{
    [MenuItem("Tools/Avatar Parameters Saver")]
    public static void Open()
    {
        GetWindow<AvatarParametersSaver>("Avatar Parameters Saver");
    }

    // assets
    
    static AnimationClip EmptyClip()
    {
        if (EmptyClipCache == null)
        {
            EmptyClipCache = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/net.narazaka.vrchat.avatar-parameters-saver/Empty.anim");
        }
        return EmptyClipCache;
    }

    static AnimationClip EmptyClipCache;

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

    // Settings

    bool AllowLoadPresets;

    AvatarParametersSaverPresets Presets;
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

    VRCAvatarDescriptor PreviousAvatar;
    Dictionary<string, object> PreviousValues = new Dictionary<string, object>();

    Vector2 scrollPos;

    SerializedObject so;
    UnityEditorInternal.ReorderableList PresetsList;

    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("Playしてください");
            return;
        }

        if (Selection.activeGameObject == null)
        {
            EditorGUILayout.LabelField("Avatarを選択して下さい");
            return;
        }

        var avatar = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();
        if (avatar == null)
        {
            EditorGUILayout.LabelField("Avatarを選択して下さい");
            return;
        }

        var runtime = avatar.GetComponent<LyumaAv3Runtime>();
        if (runtime == null)
        {
            EditorGUILayout.LabelField("AV3Emulatorが有効であることを確認して下さい");
        }

        if (PreviousAvatar != avatar)
        {
            PreviousAvatar = avatar;
            PreviousValues.Clear();
        }

        if (Presets == null)
        {
            CreatePresets();
        }

        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Presets)))
        {
            if (AllowLoadPresets)
            {
                var newPresets = EditorGUILayout.ObjectField("設定をロード", null, typeof(AvatarParametersSaverPresets), false);
                if (newPresets != null)
                {
                    Presets = newPresets as AvatarParametersSaverPresets;
                    so = null;
                    AllowLoadPresets = false;
                }
                if (GUILayout.Button("Cancel"))
                {
                    AllowLoadPresets = false;
                }
            }
            else
            {
                if (GUILayout.Button("設定をロード"))
                {
                    AllowLoadPresets = true;
                }
            }
        }
        else
        {
            EditorGUILayout.ObjectField("設定", Presets, typeof(AvatarParametersSaverPresets), false);
        }

        if (GUILayout.Button("リセット"))
        {
            if (EditorUtility.DisplayDialog("警告", "現在の設定が全てクリアされますがよろしいですか？", "OK", "Cancel"))
            {
                CreatePresets();
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

        EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);
        if (so == null)
        {
            so = new SerializedObject(Presets);
            var presets = so.FindProperty("presets");
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
                EditorGUIUtility.labelWidth = 100;
                p.menuName = EditorGUI.TextField(rect, "プリセットメニュー名", menuName);
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
            DriveParameter.ValuesToRuntime(runtime, avatar.expressionParameters.parameters);
            PreviousPresetIndex = CurrentPresetIndex;
        }
        DriveParameter.ApplyValues(runtime, avatar.expressionParameters.parameters);

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
            Save(runtime, avatar);
        }
        GUI.backgroundColor = defaultColor;

        EditorGUILayout.LabelField("パラメーター", EditorStyles.boldLabel);

        AutoCheckChangedParameters = EditorGUILayout.ToggleLeft("変化したパラメーターを自動でチェック", AutoCheckChangedParameters);
        if (GUILayout.Button("選択をクリア"))
        {
            DriveParameter.parameters.Clear();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var parameter in SortedParameters(avatar.expressionParameters.parameters))
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

    void CreatePresets()
    {
        so = null;
        // CurrentPresetIndex = 0;
        Presets = ScriptableObject.CreateInstance<AvatarParametersSaverPresets>();
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

    void Save(LyumaAv3Runtime runtime, VRCAvatarDescriptor avatar)
    {
        var path = EditorUtility.SaveFilePanelInProject("save prefab", Presets.parameterName, "prefab", "save prefab");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var animator = MakeAnimator(path);

        var prefabExists = File.Exists(path);
        var go = prefabExists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(Presets.parameterName);

        var prefab = SavePrefab(path, go, animator);
        SaveAsset(path, prefab);

        if (prefabExists)
        {
            PrefabUtility.UnloadPrefabContents(go);
        }
        else
        {
            DestroyImmediate(go);
        }
    }

    GameObject SavePrefab(string path, GameObject go, AnimatorController animator)
    {
        var prefabExists = File.Exists(path);
        var mergeAnimator = go.GetOrAddComponent<ModularAvatarMergeAnimator>();
        mergeAnimator.animator = animator;
        mergeAnimator.matchAvatarWriteDefaults = true;
        mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        var parameters = go.GetOrAddComponent<ModularAvatarParameters>();
        parameters.parameters = new List<ParameterConfig>
        {
            new ParameterConfig
            {
                nameOrPrefix = Presets.parameterName,
                syncType = ParameterSyncType.Int,
                localOnly = !Presets.networkSynced,
                saved = false,
            },
        };
        for (var i = 0; i < Presets.presets.Count; i++)
        {
            var preset = Presets.presets[i];
            var menu = new GameObject(preset.menuName);
            menu.transform.parent = go.transform;
            var menuItem = menu.GetOrAddComponent<ModularAvatarMenuItem>();
            menuItem.Control = new VRCExpressionsMenu.Control
            {
                name = preset.menuName,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = Presets.parameterName },
                value = i + 1,
                type = VRCExpressionsMenu.Control.ControlType.Button,
            };
            menu.GetOrAddComponent<ModularAvatarMenuInstaller>();
        }
        return PrefabUtility.SaveAsPrefabAsset(go, path);
    }

    void SaveAsset(string path, GameObject go)
    {
        Presets.prefab = go;
        var filename = Path.GetFileNameWithoutExtension(path);
        var assetPath = Path.Combine(Path.GetDirectoryName(path), $"{filename}.asset");
        var previousAssetPath = AssetDatabase.GetAssetPath(Presets);
        if (Path.GetFullPath(previousAssetPath) == Path.GetFullPath(assetPath))
        {
            EditorUtility.SetDirty(Presets);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(Presets, assetPath);
        }
    }

    AnimatorController MakeAnimator(string path)
    {
        var filename = Path.GetFileNameWithoutExtension(path);

        var controllerPath = Path.Combine(Path.GetDirectoryName(path), $"{filename}.controller");
        var animator = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var layer = animator.layers[0];
        layer.stateMachine.anyStatePosition = new Vector3(-250, 250, 0);
        layer.stateMachine.entryPosition = new Vector3(-250, 0, 0);
        layer.stateMachine.exitPosition = new Vector3(-250, -250, 0);
        animator.AddParameter(Presets.parameterName, AnimatorControllerParameterType.Int);
        /*
        foreach (var parameter in targetParameters)
        {
            animator.AddParameter(parameter.name, ToAnimParamType(parameter.valueType));
        }
        foreach (var parameter in targetParameters)
        {
            var param = animator.parameters.First(p => p.name == parameter.name);
            switch (parameter.valueType)
            {
                case VRCExpressionParameters.ValueType.Int:
                    param.defaultInt = (int)parameter.defaultValue; break;
                case VRCExpressionParameters.ValueType.Bool:
                    param.defaultBool = parameter.defaultValue != 0; break;
                case VRCExpressionParameters.ValueType.Float:
                    param.defaultFloat = parameter.defaultValue; break;
            }
        }
        */
        var idleState = layer.stateMachine.AddState("Idle", new Vector3(0, 0, 0));
        idleState.motion = EmptyClip();
        idleState.writeDefaultValues = false;
        layer.stateMachine.defaultState = idleState;

        for (var i = 0; i < Presets.presets.Count; i++)
        {
            var cnt = i + 1;
            var preset = Presets.presets[i];
            var actionState = layer.stateMachine.AddState($"Action{cnt}", new Vector3(0, 125 * cnt, 0));
            actionState.motion = EmptyClip();
            actionState.writeDefaultValues = false;
            var driver = actionState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.localOnly = Presets.networkSynced;
            driver.parameters = preset.parameters.Select(p => new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter
            {
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                name = p.name,
                value = p.value,
            }).ToList();

            var activeTransition = idleState.AddTransition(actionState);
            activeTransition.hasExitTime = false;
            activeTransition.exitTime = 0;
            activeTransition.duration = 0;
            activeTransition.AddCondition(AnimatorConditionMode.Equals, cnt, Presets.parameterName);
            var idleTransition = actionState.AddTransition(idleState);
            idleTransition.hasExitTime = false;
            idleTransition.exitTime = 0;
            idleTransition.duration = 0;
            idleTransition.AddCondition(AnimatorConditionMode.NotEqual, cnt, Presets.parameterName);
        }
        
        return animator;
    }

    AnimatorControllerParameterType ToAnimParamType(VRCExpressionParameters.ValueType valueType)
    {
        switch (valueType)
        {
            case VRCExpressionParameters.ValueType.Int: return AnimatorControllerParameterType.Int;
            case VRCExpressionParameters.ValueType.Bool: return AnimatorControllerParameterType.Bool;
            case VRCExpressionParameters.ValueType.Float: return AnimatorControllerParameterType.Float;
            default: return AnimatorControllerParameterType.Trigger;
        }
    }
}

#endif
