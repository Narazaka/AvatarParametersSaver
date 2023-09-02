using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

public class AvatarParametersSaverPresets : ScriptableObject
{
    public GameObject prefab;
    public string parameterName;
    public bool networkSynced;
    public List<AvatarParametersSaverPreset> presets = new List<AvatarParametersSaverPreset>();
    public int IndexOffset;
}
