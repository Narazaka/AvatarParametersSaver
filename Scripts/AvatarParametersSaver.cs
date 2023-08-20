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

    static AnimationClip EmptyClip()
    {
        if (EmptyClipCache == null)
        {
            EmptyClipCache = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/net.narazaka.vrchat.avatar-parameters-saver/Empty.anim");
        }
        return EmptyClipCache;
    }

    static AnimationClip EmptyClipCache;

    string MenuName;
    VRCExpressionParameters.Parameter DriveParameter = new VRCExpressionParameters.Parameter();

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

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var parameter in avatar.expressionParameters.parameters)
        {
            DipslayParameter(runtime, parameter);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField("反映条件");
        MenuName = EditorGUILayout.TextField("メニュー名", MenuName);
        DriveParameter.valueType = (VRCExpressionParameters.ValueType)EditorGUILayout.EnumPopup("type", DriveParameter.valueType);
        if (DriveParameter.valueType == VRCExpressionParameters.ValueType.Float)
        {
            EditorGUILayout.LabelField("Floatには未対応です");
            return;
        }
        DriveParameter.name = EditorGUILayout.TextField("name", DriveParameter.name);
        if (DriveParameter.valueType == VRCExpressionParameters.ValueType.Int)
        {
            DriveParameter.defaultValue = EditorGUILayout.FloatField("value", DriveParameter.defaultValue);
        }
        DriveParameter.networkSynced = !EditorGUILayout.ToggleLeft("localOnly", !DriveParameter.networkSynced);

        if (GUILayout.Button("保存"))
        {
            Save(runtime, avatar);
        }
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
                    DisplayIsTargetToggle(parameter.name, param.value);
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
                    DisplayIsTargetToggle(parameter.name, param.value);
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
                    DisplayIsTargetToggle(parameter.name, param.value);
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

    void DisplayIsTargetToggle(string parameter, object value)
    {
        AdjustTargetParameter(parameter, EditorGUILayout.ToggleLeft($"{parameter} ({value})", IsTarget(parameter)));
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
        driver.localOnly = !DriveParameter.networkSynced;
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
                localOnly = true,
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
