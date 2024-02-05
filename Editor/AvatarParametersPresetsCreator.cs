using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace net.narazaka.vrchat.avatar_parameters_saver.editor
{
    public class AvatarParametersPresetsCreator
    {

        [MenuItem("GameObject/ModularAvatar/AvatarParametersPresets", true)]
        public static bool ValidateCreate()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>() != null;
        }

        [MenuItem("GameObject/ModularAvatar/AvatarParametersPresets", false)]
        public static void Create()
        {
            var obj = new GameObject("AvatarParametersPresets", typeof(ModularAvatarMenuInstaller), typeof(AvatarParametersPresets));
            obj.transform.SetParent(Selection.activeGameObject.transform);
            Undo.RegisterCreatedObjectUndo(obj, "create AvatarParametersPresets");
            Selection.activeGameObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
