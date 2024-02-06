using net.narazaka.vrchat.avatar_parameters_saver.editor.util;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [InitializeOnLoad]
    public static class AvatarParametersSaverPlayModePersist
    {
        const string playModePersistKey = "AvatarParametersSaverPlayModePersist";

        [Serializable]
        public class StoreDataGroup
        {
            public int instanceID;
            public AvatarParametersSaverPresetGroup avatarParametersSaverPresetGroup;
        }

        [Serializable]
        public class StoreData
        {
            public StoreDataGroup[] StoreDataGroups;
        }

        static Dictionary<int, AvatarParametersSaverPresetGroup> data;

        static AvatarParametersSaverPlayModePersist()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Load();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Save();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Initialize();
            }
        }

        public static void Store(AvatarParametersPresets avatarParametersPresets)
        {
            if (!EditorApplication.isPlaying) return;
            if (data == null) data = new Dictionary<int, AvatarParametersSaverPresetGroup>();
            data[avatarParametersPresets.GetInstanceID()] = avatarParametersPresets.AvatarParametersSaverPresetGroup;
        }

        static void Initialize()
        {
            if (EditorPrefs.HasKey(playModePersistKey)) EditorPrefs.DeleteKey(playModePersistKey);
            data = null;
        }

        static void Save()
        {
            if (data == null) return;
            var storeData = new StoreData();
            storeData.StoreDataGroups = new StoreDataGroup[data.Count];
            int i = 0;
            foreach (var pair in data)
            {
                storeData.StoreDataGroups[i] = new StoreDataGroup();
                storeData.StoreDataGroups[i].instanceID = pair.Key;
                storeData.StoreDataGroups[i].avatarParametersSaverPresetGroup = pair.Value;
                i++;
            }
            var json = JsonUtility.ToJson(storeData);
            EditorPrefs.SetString(playModePersistKey, json);
        }

        public static void Load()
        {
            if (!EditorPrefs.HasKey(playModePersistKey)) return;
            var json = EditorPrefs.GetString(playModePersistKey);
            EditorPrefs.DeleteKey(playModePersistKey);
            var storeData = JsonUtility.FromJson<StoreData>(json);
            if (storeData != null)
            {
                if (data == null) data = new Dictionary<int, AvatarParametersSaverPresetGroup>();
                foreach (var storeDataGroup in storeData.StoreDataGroups)
                {
                    var obj = EditorUtility.InstanceIDToObject(storeDataGroup.instanceID) as AvatarParametersPresets;
                    if (obj == null) continue;
                    UndoUtility.RecordObject(obj, "play mode restore");
                    if (obj.AvatarParametersSaverPresetGroup == null) obj.AvatarParametersSaverPresetGroup = new AvatarParametersSaverPresetGroup();
                    obj.AvatarParametersSaverPresetGroup.parameterName = storeDataGroup.avatarParametersSaverPresetGroup.parameterName;
                    obj.AvatarParametersSaverPresetGroup.networkSynced = storeDataGroup.avatarParametersSaverPresetGroup.networkSynced;
                    obj.AvatarParametersSaverPresetGroup.presets = storeDataGroup.avatarParametersSaverPresetGroup.presets;
                    obj.AvatarParametersSaverPresetGroup.IndexOffset = storeDataGroup.avatarParametersSaverPresetGroup.IndexOffset;
                }
            }
        }
    }
}
