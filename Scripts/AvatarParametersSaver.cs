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

    string MenuName;
    VRCExpressionParameters.Parameter DriveParameter = new VRCExpressionParameters.Parameter();
    static string[] ParameterTypes = new string[] { "Int", "Bool" };
    static string[] SyncModes = new string[] { "プリセットパラメータを同期", "結果パラメータを同期" };

    HashSet<string> TargetParameterNames = new HashSet<string>();

    bool IsTarget(string parameter)
    {
        return TargetParameterNames.Contains(parameter);
    }

    void AdjustTargetParameter(string parameter, bool isTarget)
    {
        if (isTarget)
        {
            TargetParameterNames.Add(parameter);
        }
        else
        {
            TargetParameterNames.Remove(parameter);
        }
    }

    // UI

    VRCAvatarDescriptor PreviousAvatar;
    Dictionary<string, object> PreviousValues = new Dictionary<string, object>();

    Vector2 scrollPos;

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

        EditorGUILayout.LabelField("プリセットメニュー設定", EditorStyles.boldLabel);

        MenuName = EditorGUILayout.TextField("プリセットメニュー名", MenuName);
        DriveParameter.name = EditorGUILayout.TextField("プリセットパラメーター名", DriveParameter.name);
        DriveParameter.valueType = (VRCExpressionParameters.ValueType)(EditorGUILayout.Popup("プリセットパラメーター型", (int)DriveParameter.valueType / 2, ParameterTypes) * 2);
        if (DriveParameter.valueType == VRCExpressionParameters.ValueType.Float)
        {
            EditorGUILayout.LabelField("Floatには未対応です");
            return;
        }
        if (DriveParameter.valueType == VRCExpressionParameters.ValueType.Int)
        {
            DriveParameter.defaultValue = EditorGUILayout.FloatField("プリセットパラメーター値", DriveParameter.defaultValue);
        }
        DriveParameter.networkSynced = GUILayout.Toolbar(DriveParameter.networkSynced ? 0 : 1, SyncModes) == 0;

        if (DriveParameter.networkSynced)
        {
            EditorGUILayout.HelpBox("プリセットパラメーターをSyncし、VRC_AvatarParameterDriver側で値を同期せずにlocalOnlyで値を変更するモードです。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("プリセットパラメーターをSyncせず、VRC_AvatarParameterDriver側で値を同期して変更するモードです。", MessageType.Info);
        }

        var defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("保存"))
        {
            Save(runtime, avatar);
        }
        GUI.backgroundColor = defaultColor;

        EditorGUILayout.LabelField("パラメーター", EditorStyles.boldLabel);

        AutoCheckChangedParameters = EditorGUILayout.ToggleLeft("変化したパラメーターを自動でチェック", AutoCheckChangedParameters);
        if (GUILayout.Button("選択をクリア"))
        {
            TargetParameterNames.Clear();
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
            sorted = sorted.OrderBy(p => !IsTarget(p.name));
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

    float GetParameterValue(LyumaAv3Runtime runtime, VRCExpressionParameters.Parameter parameter)
    {
        switch (parameter.valueType)
        {
            case VRCExpressionParameters.ValueType.Bool:
                {
                    var param = runtime.Bools.Find(v => v.name == parameter.name);
                    if (param == null)
                    {
                        return float.NaN;
                    }
                    return param.value ? 1 : 0;
                }
            case VRCExpressionParameters.ValueType.Float:
                {
                    var param = runtime.Floats.Find(v => v.name == parameter.name);
                    if (param == null)
                    {
                        return float.NaN;
                    }
                    return param.value;
                }
            case VRCExpressionParameters.ValueType.Int:
                {
                    var param = runtime.Ints.Find(v => v.name == parameter.name);
                    if (param == null)
                    {
                        return float.NaN;
                    }
                    return param.value;
                }
            default:
                return float.NaN;
        }
    }

    void DisplayIsTargetToggle(string parameter, object value, VRCExpressionParameters.ValueType type)
    {
        if (AutoCheckChangedParameters && CheckChanged(parameter, value))
        {
            AdjustTargetParameter(parameter, true);
        }
        if (IsShow(type))
        {
            EditorGUILayout.BeginHorizontal();
            var result = EditorGUILayout.ToggleLeft(parameter, IsTarget(parameter));
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
            AdjustTargetParameter(parameter, result);
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
        var path = EditorUtility.SaveFilePanelInProject("save prefab", MenuName, "prefab", "save prefab");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        var filename = Path.GetFileNameWithoutExtension(path);

        var targetParameters = avatar.expressionParameters.parameters.Where(p => TargetParameterNames.Contains(p.name));

        var controllerPath = Path.Combine(Path.GetDirectoryName(path), $"{filename}.controller");
        var animator = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var layer = animator.layers[0];
        layer.stateMachine.anyStatePosition = new Vector3(-250, 250, 0);
        layer.stateMachine.entryPosition = new Vector3(-250, 0, 0);
        layer.stateMachine.exitPosition = new Vector3(-250, -250, 0);
        animator.AddParameter(DriveParameter.name, DriveParameter.valueType == VRCExpressionParameters.ValueType.Int ? AnimatorControllerParameterType.Int : AnimatorControllerParameterType.Bool);
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
        var actionState = layer.stateMachine.AddState("Action", new Vector3(0, 250, 0));
        actionState.motion = EmptyClip();
        actionState.writeDefaultValues = false;
        var driver = actionState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
        driver.localOnly = DriveParameter.networkSynced;
        driver.parameters = targetParameters.Select(p => new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter
        {
            type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
            name = p.name,
            value = GetParameterValue(runtime, p),
        }).ToList();

        layer.stateMachine.defaultState = idleState;
        var activeTransition = idleState.AddTransition(actionState);
        activeTransition.hasExitTime = false;
        activeTransition.exitTime = 0;
        activeTransition.duration = 0;
        if (DriveParameter.valueType == VRCExpressionParameters.ValueType.Int)
        {
            activeTransition.AddCondition(AnimatorConditionMode.Equals, DriveParameter.defaultValue, DriveParameter.name);
        }
        else
        {
            activeTransition.AddCondition(AnimatorConditionMode.If, 1, DriveParameter.name);
        }
        var idleTransition = actionState.AddTransition(idleState);
        idleTransition.hasExitTime = false;
        idleTransition.exitTime = 0;
        idleTransition.duration = 0;
        if (DriveParameter.valueType == VRCExpressionParameters.ValueType.Int)
        {
            idleTransition.AddCondition(AnimatorConditionMode.NotEqual, DriveParameter.defaultValue, DriveParameter.name);
        }
        else
        {
            idleTransition.AddCondition(AnimatorConditionMode.IfNot, 1, DriveParameter.name);
        }

        var go = new GameObject(MenuName);
        var mergeAnimator = go.AddComponent<ModularAvatarMergeAnimator>();
        mergeAnimator.animator = animator;
        mergeAnimator.matchAvatarWriteDefaults = true;
        mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        var parameters = go.AddComponent<ModularAvatarParameters>();
        parameters.parameters = new List<ParameterConfig>
        {
            new ParameterConfig
            {
                nameOrPrefix = DriveParameter.name,
                syncType = DriveParameter.valueType == VRCExpressionParameters.ValueType.Int ? ParameterSyncType.Int : ParameterSyncType.Bool,
                localOnly = !DriveParameter.networkSynced,
                saved = false,
            },
        };
        var menuItem = go.AddComponent<ModularAvatarMenuItem>();
        menuItem.Control = new VRCExpressionsMenu.Control
        {
            name = MenuName,
            parameter = new VRCExpressionsMenu.Control.Parameter { name = DriveParameter.name },
            value = DriveParameter.defaultValue,
            type = VRCExpressionsMenu.Control.ControlType.Button,
        };
        go.AddComponent<ModularAvatarMenuInstaller>();
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
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
