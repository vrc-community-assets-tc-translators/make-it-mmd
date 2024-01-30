#if NDMMD_VRCSDK3_AVATARS

using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using L = enitimeago.NonDestructiveMMD.Localization;

namespace enitimeago.NonDestructiveMMD
{
    public class CommonAsserts
    {
        private bool _isEditor;

        public CommonAsserts(bool isEditor)
        {
            _isEditor = isEditor;
        }

        public bool RunAsserts(GameObject avatarRootObject)
        {
            var avatarDescriptor = avatarRootObject.GetComponent<VRCAvatarDescriptor>();
            var mappingsComponents = avatarRootObject.GetComponentsInChildren<BlendShapeMappings>();
            if (mappingsComponents.Length == 0)
            {
                Log("No Make It MMD component found in avatar. Nothing to do", Severity.Debug);
                return false;
            }
            if (mappingsComponents.Length > 1)
            {
                Log("More than one Make It MMD component found in avatar!", Severity.Error);
                return false;
            }
            return RunAsserts(avatarDescriptor);
        }

        public bool RunAsserts(VRCAvatarDescriptor avatarDescriptor)
        {
            if (avatarDescriptor == null)
            {
                Log(L.Tr("CommonAsserts:AvatarNotFound"), Severity.Warning);
                return false;
            }

            var visemeSkinnedMesh = avatarDescriptor.VisemeSkinnedMesh;
            if (visemeSkinnedMesh == null)
            {
                Log(L.Tr("CommonAsserts:AvatarNoFaceMeshSet"), Severity.Warning);
                return false;
            }

            if (visemeSkinnedMesh.name != "Body")
            {
                Log(L.Tr("CommonAsserts:AvatarFaceSMRNotCalledBody"), Severity.Warning);
                return false;
            }

            if (visemeSkinnedMesh.sharedMesh == null)
            {
                Log(L.Tr("CommonAsserts:AvatarFaceSMRNoMesh"), Severity.Warning);
                return false;
            }

            if (visemeSkinnedMesh.sharedMesh.blendShapeCount == 0)
            {
                Log(L.Tr("CommonAsserts:AvatarFaceSMRNoBlendShapes"), Severity.Warning);
                return false;
            }

            for (int i = 0; i < visemeSkinnedMesh.sharedMesh.blendShapeCount; i++)
            {
                string blendShapeName = visemeSkinnedMesh.sharedMesh.GetBlendShapeName(i);
                if (MMDBlendShapes.JapaneseNames().Any(blendShape => blendShape.name == blendShapeName))
                {
                    Log(L.Tr("CommonAsserts:AvatarFaceSMRExistingBlendShapesUnsupported"), Severity.Warning);
                    return false;
                }
            }

            return true;
        }

#if UNITY_2021_3_OR_NEWER
        [HideInCallstackAttribute]
#endif
        private void Log(string message, Severity severity)
        {
            if (_isEditor)
            {
                switch (severity)
                {
                    case Severity.Debug: break;
                    case Severity.Warning: EditorGUILayout.HelpBox(message, MessageType.Warning); break;
                    case Severity.Error: EditorGUILayout.HelpBox(message, MessageType.Error); break;
                    default: Debug.LogWarning($"Unknown severity type raised with message \"${message}\""); break;
                }
            }
            else
            {
                // TODO: switch to NDMF error logging.
                // Error has to be downgraded to Debug.LogWarning in order not to break tests
                switch (severity)
                {
                    case Severity.Debug: Debug.Log(message); break;
                    case Severity.Warning: Debug.LogWarning(message); break;
                    case Severity.Error: Debug.LogWarning(message); break;
                    default: Debug.LogWarning($"Unknown severity type raised with message \"${message}\""); break;
                }
            }
        }

        public enum Severity
        {
            Debug,
            Warning,
            Error
        }
    }
}

#endif
