using UnityEngine;
using VRC.SDKBase;

namespace net.narazaka.vrchat.avatar_parameters_saver
{
    public class AvatarParametersPresets : MonoBehaviour, IEditorOnly
    {
        public AvatarParametersSaverPresetGroup AvatarParametersSaverPresetGroup = new AvatarParametersSaverPresetGroup();
    }
}
