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
        public Texture2D icon;
        public string menuName;
        public List<AvatarParametersSaverParameter> parameters = new List<AvatarParametersSaverParameter>();
    }
}
