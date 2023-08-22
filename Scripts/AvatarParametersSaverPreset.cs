using Lyuma.Av3Emulator.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

public class AvatarParametersSaverPreset
{
    public string menuName;
    public string name;
    public VRCExpressionParameters.ValueType valueType;
    public bool networkSynced;
    public float value = 1;
    public List<AvatarParametersSaverParameter> parameters = new List<AvatarParametersSaverParameter>();

    public float defaultValue { get => value; set => this.value = value; }

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
}
