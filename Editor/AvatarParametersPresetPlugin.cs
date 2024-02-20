using nadena.dev.ndmf;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;


[assembly: ExportsPlugin(typeof(net.narazaka.vrchat.avatar_parameters_saver.editor.AvatarParametersPresetsPlugin))]

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    public class AvatarParametersPresetsPlugin : Plugin<AvatarParametersPresetsPlugin>
    {
        public override string DisplayName => "AvatarParametersPresets";
        public override string QualifiedName => "net.narazaka.vrchat.avatar_parameters_saver.AvatarParametersPresets";
        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).BeforePlugin("nadena.dev.modular-avatar").Run("AvatarParametersPresets", (ctx) =>
            {
                foreach (var presets in ctx.AvatarRootObject.GetComponentsInChildren<AvatarParametersPresets>())
                {
                    var parameterName = ParameterName(presets.AvatarParametersSaverPresetGroup, presets.gameObject);
                    var animator = MakeAnimator(presets.AvatarParametersSaverPresetGroup, parameterName);
                    StoreAssets(presets.AvatarParametersSaverPresetGroup, presets.gameObject, parameterName, animator);
                    Object.DestroyImmediate(presets);
                }
            });
        }

        string ParameterName(AvatarParametersSaverPresetGroup presets, GameObject go)
        {
            return string.IsNullOrEmpty(presets.parameterName) ? go.name : presets.parameterName;
        }

        void StoreAssets(AvatarParametersSaverPresetGroup presets, GameObject go, string parameterName, AnimatorController animator)
        {
            var mergeAnimator = go.GetOrAddComponent<ModularAvatarMergeAnimator>();
            mergeAnimator.animator = animator;
            mergeAnimator.matchAvatarWriteDefaults = true;
            mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            var parameters = go.GetOrAddComponent<ModularAvatarParameters>();
            parameters.parameters = new List<ParameterConfig>
            {
                new ParameterConfig
                {
                    nameOrPrefix = parameterName,
                    syncType = ParameterSyncType.Int,
                    localOnly = !presets.networkSynced,
                    saved = false,
                    internalParameter = string.IsNullOrEmpty(presets.parameterName),
                },
            };
            var redirect = new GameObject("__AvatarParametersPresets__Redirect__");
            redirect.transform.parent = go.transform;
            var redirectCount = 0;
            for (var i = 0; i < presets.presets.Count; i++)
            {
                var preset = presets.presets[i];
                var menuTransform = go.transform.Find(preset.menuName);
                GameObject menu;
                if (menuTransform == null)
                {
                    menu = new GameObject(preset.menuName);
                    menu.transform.parent = go.transform;
                }
                else
                {
                    menu = menuTransform.gameObject;
                    if (menu.GetComponent<ModularAvatarMenuInstaller>() != null)
                    {
                        // 直下にあると子も出来てしまう
                        menuTransform.parent = redirect.transform;
                        redirectCount++;
                    }
                }
                var menuItem = menu.GetOrAddComponent<ModularAvatarMenuItem>();
                menuItem.Control = new VRCExpressionsMenu.Control
                {
                    name = preset.menuName,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = parameterName },
                    value = presets.GetPresetParameterValue(i),
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    icon = preset.icon,
                };
            }
            if (redirectCount == 0)
            {
                Object.DestroyImmediate(redirect);
            }
            else if (redirectCount == presets.presets.Count)
            {
                return;
            }
            if (presets.installParent)
            {
                var parentMenu = go.GetOrAddComponent<ModularAvatarMenuItem>();
                parentMenu.Control = new VRCExpressionsMenu.Control
                {
                    name = go.name,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    icon = presets.icon,
                };
                parentMenu.MenuSource = SubmenuSource.Children;
            }
            else
            {
                go.GetOrAddComponent<ModularAvatarMenuGroup>();
            }
        }

        AnimatorController MakeAnimator(AvatarParametersSaverPresetGroup presets, string parameterName)
        {
            var animator = new AnimatorController();
            if (animator.layers.Length == 0) animator.AddLayer("Base Layer");
            var layer = animator.layers[0];
            layer.stateMachine.anyStatePosition = new Vector3(-250, 250, 0);
            layer.stateMachine.entryPosition = new Vector3(-250, 0, 0);
            layer.stateMachine.exitPosition = new Vector3(-250, -250, 0);
            animator.AddParameter(parameterName, AnimatorControllerParameterType.Int);

            var idleState = layer.stateMachine.AddState("Idle", new Vector3(0, 0, 0));
            idleState.motion = EmptyClip;
            idleState.writeDefaultValues = false;
            layer.stateMachine.defaultState = idleState;

            for (var i = 0; i < presets.presets.Count; i++)
            {
                var value = presets.GetPresetParameterValue(i);
                var preset = presets.presets[i];
                var actionState = layer.stateMachine.AddState($"Action{value}", new Vector3(250, 125 * value, 0));
                actionState.motion = EmptyClip;
                actionState.writeDefaultValues = false;
                var driver = new VRCAvatarParameterDriver();
                driver.localOnly = presets.networkSynced;
                driver.parameters = preset.parameters.Select(p => new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                    name = p.name,
                    value = p.value,
                }).ToList();
                actionState.behaviours = new StateMachineBehaviour[] { driver };

                var activeTransition = idleState.AddTransition(actionState);
                activeTransition.hasExitTime = false;
                activeTransition.exitTime = 0;
                activeTransition.duration = 0;
                activeTransition.AddCondition(AnimatorConditionMode.Equals, value, parameterName);
                var idleTransition = actionState.AddTransition(idleState);
                idleTransition.hasExitTime = false;
                idleTransition.exitTime = 0;
                idleTransition.duration = 0;
                idleTransition.AddCondition(AnimatorConditionMode.NotEqual, value, parameterName);
            }

            return animator;
        }

        AnimationClip _EmptyClip;
        AnimationClip EmptyClip
        {
            get
            {
                if (_EmptyClip == null) _EmptyClip = MakeEmptyAnimationClip();
                return _EmptyClip;
            }
        }

        AnimationClip MakeEmptyAnimationClip()
        {
            var clip = new AnimationClip();
            clip.SetCurve("__AvatarParametersSaver_EMPTY__", typeof(GameObject), "localPosition.x", new AnimationCurve { keys = new Keyframe[] { new Keyframe { time = 0, value = 0 }, new Keyframe { time = 1f / 60f, value = 0 } } });
            return clip;
        }
    }
}
