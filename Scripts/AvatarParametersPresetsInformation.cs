#if AvatarParametersSaver_HAS_AVATAR_OPTIMIZER && UNITY_EDITOR

using Anatawa12.AvatarOptimizer.API;
using UnityEditor;

namespace net.narazaka.vrchat.avatar_parameters_saver
{

    [ComponentInformation(typeof(AvatarParametersPresets))]
    internal class AvatarParametersPresetsInformation : ComponentInformation<AvatarParametersPresets>
    {
        protected override void CollectMutations(AvatarParametersPresets component, ComponentMutationsCollector collector)
        {
        }

        protected override void CollectDependency(AvatarParametersPresets component, ComponentDependencyCollector collector)
        {
            if (EditorApplication.isPlaying)
            {
                collector.MarkEntrypoint();
                collector.AddDependency(component.transform.parent);
            }
        }
    }
}

#endif
