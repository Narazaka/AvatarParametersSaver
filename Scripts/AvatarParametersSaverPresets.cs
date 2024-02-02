using System.Collections.Generic;
using UnityEngine;

namespace net.narazaka.vrchat.avatar_parameters_saver
{
    public class AvatarParametersSaverPresets : ScriptableObject
    {
        public GameObject prefab;
        public string parameterName;
        public bool networkSynced;
        public List<AvatarParametersSaverPreset> presets = new List<AvatarParametersSaverPreset>();
        public int IndexOffset;
    }
}
