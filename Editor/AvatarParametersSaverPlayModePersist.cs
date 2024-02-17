using net.narazaka.vrchat.avatar_parameters_saver.editor.util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    [InitializeOnLoad]
    public static class AvatarParametersSaverPlayModePersist
    {
        const string playModePersistKey = "AvatarParametersSaverPlayModePersist";

        [Serializable]
        public class StoreDataGroup
        {
            public int instanceId;
            public int avatarObjectInstanceId;
            public string name;
            public AvatarParametersSaverPresetGroup avatarParametersSaverPresetGroup;
        }

        public class StoreDataGroupAsset : ScriptableObject
        {
            public int instanceId;
            public int avatarObjectInstanceId;
            public AvatarParametersSaverPresetGroup AvatarParametersSaverPresetGroup;

            public StoreDataGroup ToStoreDataGroup() => new StoreDataGroup
            {
                instanceId = instanceId,
                avatarObjectInstanceId = avatarObjectInstanceId,
                name = name,
                avatarParametersSaverPresetGroup = AvatarParametersSaverPresetGroup,
            };
        }

        [Serializable]
        public class StoreData
        {
            public StoreDataGroup[] StoreDataGroups;

            public static void PersistedToScene()
            {
                if (!EditorPrefs.HasKey(playModePersistKey)) return;
                var json = EditorPrefs.GetString(playModePersistKey);
                EditorPrefs.DeleteKey(playModePersistKey);
                var storeData = JsonUtility.FromJson<StoreData>(json);
                if (storeData != null)
                {
                    foreach (var storeDataGroup in storeData.StoreDataGroups)
                    {
                        var obj = EditorUtility.InstanceIDToObject(storeDataGroup.instanceId) as AvatarParametersPresets;
                        if (obj == null) continue;
                        UndoUtility.RecordObject(obj, "play mode restore");
                        if (obj.AvatarParametersSaverPresetGroup == null) obj.AvatarParametersSaverPresetGroup = new AvatarParametersSaverPresetGroup();
                        obj.AvatarParametersSaverPresetGroup.parameterName = storeDataGroup.avatarParametersSaverPresetGroup.parameterName;
                        obj.AvatarParametersSaverPresetGroup.networkSynced = storeDataGroup.avatarParametersSaverPresetGroup.networkSynced;
                        obj.AvatarParametersSaverPresetGroup.presets = storeDataGroup.avatarParametersSaverPresetGroup.presets;
                        obj.AvatarParametersSaverPresetGroup.IndexOffset = storeDataGroup.avatarParametersSaverPresetGroup.IndexOffset;
                        obj.AvatarParametersSaverPresetGroup.icon = storeDataGroup.avatarParametersSaverPresetGroup.icon;
                    }
                }
            }
        }

        class SceneData
        {
            public static void SceneToPersist()
            {
                if (EditorPrefs.HasKey(playModePersistKey)) EditorPrefs.DeleteKey(playModePersistKey);
                var sceneData = new SceneData();
                foreach (var preset in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<AvatarParametersPresets>(true)))
                {
                    sceneData.Store(preset);
                }
                sceneData.DataToPersist();
            }

            public Dictionary<int, StoreDataGroup> data;

            public void Store(AvatarParametersPresets avatarParametersPresets)
            {
                var instanceId = avatarParametersPresets.GetInstanceID();
                var avatarObject = avatarParametersPresets.GetComponentInParent<VRCAvatarDescriptor>()?.gameObject;
                if (data == null) data = new Dictionary<int, StoreDataGroup>();
                var storeDataGroup = data[instanceId] = new StoreDataGroup();
                storeDataGroup.instanceId = instanceId;
                storeDataGroup.avatarObjectInstanceId = avatarObject == null ? -1 : avatarObject.GetInstanceID();
                storeDataGroup.name = avatarParametersPresets.name;
                storeDataGroup.avatarParametersSaverPresetGroup = avatarParametersPresets.AvatarParametersSaverPresetGroup;
            }

            public void DataToPersist()
            {
                if (data == null) return;
                var storeData = new StoreData();
                storeData.StoreDataGroups = new StoreDataGroup[data.Count];
                int i = 0;
                foreach (var pair in data)
                {
                    storeData.StoreDataGroups[i] = pair.Value;
                    i++;
                }
                var json = JsonUtility.ToJson(storeData);
                EditorPrefs.SetString(playModePersistKey, json);
            }
        }

        class PlayData
        {
            public static PlayData FromPersisted()
            {
                if (!EditorPrefs.HasKey(playModePersistKey)) return null;
                var json = EditorPrefs.GetString(playModePersistKey);
                EditorPrefs.DeleteKey(playModePersistKey);
                var storeData = JsonUtility.FromJson<StoreData>(json);
                if (storeData == null) return null;
                var playData = new PlayData();
                foreach (var storeDataGroup in storeData.StoreDataGroups)
                {
                    playData.Restore(storeDataGroup.instanceId, storeDataGroup.avatarObjectInstanceId, storeDataGroup.name, storeDataGroup.avatarParametersSaverPresetGroup);
                }
                return playData;
            }

            public Dictionary<int, StoreDataGroupAsset> data;

            public StoreDataGroupAsset[] ForAvatar(VRCAvatarDescriptor avatar)
            {
                if (data == null) return null;
                var avatarObjectInstanceId = avatar.gameObject.GetInstanceID();
                return data.Values.Where(d => d.avatarObjectInstanceId == avatarObjectInstanceId).ToArray();
            }

            public void Restore(int instanceId, int avatarObjectInstanceId, string name, AvatarParametersSaverPresetGroup avatarParametersSaverPresetGroup)
            {
                if (data == null) data = new Dictionary<int, StoreDataGroupAsset>();
                var storeDataGroup = data[instanceId] = ScriptableObject.CreateInstance<StoreDataGroupAsset>();
                storeDataGroup.instanceId = instanceId;
                storeDataGroup.avatarObjectInstanceId = avatarObjectInstanceId;
                storeDataGroup.name = name;
                storeDataGroup.AvatarParametersSaverPresetGroup = avatarParametersSaverPresetGroup;
            }

            public void Store(StoreDataGroupAsset storeDataGroup)
            {
                data[storeDataGroup.instanceId].AvatarParametersSaverPresetGroup = storeDataGroup.AvatarParametersSaverPresetGroup;
            }

            public void DataToPersist()
            {
                if (data == null) return;
                var storeData = new StoreData();
                storeData.StoreDataGroups = new StoreDataGroup[data.Count];
                int i = 0;
                foreach (var pair in data)
                {
                    storeData.StoreDataGroups[i] = pair.Value.ToStoreDataGroup();
                    i++;
                }
                var json = JsonUtility.ToJson(storeData);
                EditorPrefs.SetString(playModePersistKey, json);
            }
        }

        static PlayData playData;

        static AvatarParametersSaverPlayModePersist()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                StoreData.PersistedToScene();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                playData.DataToPersist();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playData = PlayData.FromPersisted();
            }
            else if (state == PlayModeStateChange.ExitingEditMode)
            {
                SceneData.SceneToPersist();
            }
        }

        public static StoreDataGroupAsset[] ForAvatar(VRCAvatarDescriptor avatar) => playData?.ForAvatar(avatar);

        public static void Store(StoreDataGroupAsset storeDataGroupAsset) => playData?.Store(storeDataGroupAsset);
    }
}
