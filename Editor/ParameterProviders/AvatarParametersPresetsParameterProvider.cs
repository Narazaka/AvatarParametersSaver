using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;
using System.Collections.Immutable;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor.parameter_providers
{
    [ParameterProviderFor(typeof(AvatarParametersPresets))]
    internal class AvatarParametersPresetsParameterProvider : IParameterProvider
    {
        readonly AvatarParametersPresets Presets;

        public AvatarParametersPresetsParameterProvider(AvatarParametersPresets c)
        {
            Presets = c;
        }

        public IEnumerable<ProvidedParameter> GetSuppliedParameters(BuildContext context = null)
        {
            return new ProvidedParameter[]
            {
                new ProvidedParameter(Presets.ParameterName, ParameterNamespace.Animator, Presets, AvatarParametersPresetsPlugin.Instance, AnimatorControllerParameterType.Int)
                {
                    WantSynced = Presets.AvatarParametersSaverPresetGroup.networkSynced,
                    IsHidden = string.IsNullOrEmpty(Presets.AvatarParametersSaverPresetGroup.parameterName),
                }
            };
        }

        public void RemapParameters(ref ImmutableDictionary<(ParameterNamespace, string), ParameterMapping> nameMap,
            BuildContext context)
        {
            // no-op
        }
    }
}
