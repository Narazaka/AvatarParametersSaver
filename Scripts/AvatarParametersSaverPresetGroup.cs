using System;
using System.Collections.Generic;
using UnityEngine;

namespace net.narazaka.vrchat.avatar_parameters_saver
{
    [Serializable]
    public class AvatarParametersSaverPresetGroup
    {
        public Texture2D icon;
        public string parameterName;
        public bool networkSynced;
        public List<AvatarParametersSaverPreset> presets = new List<AvatarParametersSaverPreset>();
        public int IndexOffset;
        public bool installParent = true;

        public int GetPresetParameterValue(int index)
        {
            return GetPresetParameterValue(index, IndexOffset);
        }

        public static int GetPresetParameterValue(int index, int indexOffset)
        {
            return index + indexOffset + 1;
        }
    }
}
