#if UNITY_EDITOR
using Lyuma.Av3Emulator.Runtime;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace net.narazaka.vrchat.avatar_parameters_saver
{
    [Serializable]
    public class AvatarParametersSaverPreset
    {
        public string menuName;
        public List<AvatarParametersSaverParameter> parameters = new List<AvatarParametersSaverParameter>();

        public bool IsTarget(string parameter)
        {
            return parameters.Any(p => p.name == parameter);
        }

        public void AdjustTargetParameter(string parameter, bool isTarget)
        {
            if (isTarget)
            {
                if (!parameters.Any(p => p.name == parameter))
                {
                    parameters.Add(new AvatarParametersSaverParameter { name = parameter });
                }
            }
            else
            {
                var index = parameters.FindIndex(p => p.name == parameter);
                if (index != -1)
                {
                    parameters.RemoveAt(index);
                }
            }
        }

#if UNITY_EDITOR
        public void ApplyValues(LyumaAv3Runtime runtime, IEnumerable<VRCExpressionParameters.Parameter> exParameters)
        {
            var newParameters = new List<AvatarParametersSaverParameter>();
            var parametersMap = parameters.ToDictionary(p => p.name, p => p);
            foreach (var exParameter in exParameters)
            {
                if (parametersMap.TryGetValue(exParameter.name, out var parameter))
                {
                    parameter.value = GetParameterValue(runtime, exParameter);
                    newParameters.Add(parameter);
                }
            }
            // 順序をあわせる
            parameters = newParameters;
        }

        public void ValuesToRuntime(LyumaAv3Runtime runtime, IEnumerable<VRCExpressionParameters.Parameter> exParameters)
        {
            var parametersMap = parameters.ToDictionary(p => p.name, p => p);
            foreach (var exParameter in exParameters)
            {
                if (parametersMap.TryGetValue(exParameter.name, out var parameter))
                {
                    switch (exParameter.valueType)
                    {
                        case VRCExpressionParameters.ValueType.Bool:
                            {
                                var runtimeParameter = runtime.Bools.Find(p => p.name == parameter.name);
                                runtimeParameter.value = parameter.value != 0;
                                break;
                            }
                        case VRCExpressionParameters.ValueType.Int:
                            {
                                var runtimeParameter = runtime.Ints.Find(p => p.name == parameter.name);
                                runtimeParameter.value = (int)parameter.value;
                                break;
                            }
                        case VRCExpressionParameters.ValueType.Float:
                            {
                                var runtimeParameter = runtime.Floats.Find(p => p.name == parameter.name);
                                runtimeParameter.expressionValue = parameter.value;
                                break;
                            }
                    }
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
#endif
    }
}
