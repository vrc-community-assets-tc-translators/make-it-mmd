﻿using System.Collections.Generic;
using System.Linq;
using enitimeago.ExportPackgeWithVpai;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using L = enitimeago.NonDestructiveMMD.Localization;

namespace enitimeago.NonDestructiveMMD
{
    [CustomEditor(typeof(BlendShapeMappings))]
    public class BlendShapeMappingsEditor : Editor
    {
        private CommonChecks _commonChecks;
        private bool _showStoredData = false;
        private float _hasMmdShapeKeysHelpBoxHeight = 0;

        public void OnEnable()
        {
            _commonChecks = new CommonChecks(isEditor: true);
        }

        public override void OnInspectorGUI()
        {
            var data = (BlendShapeMappings)target;
            var avatar = data.GetComponentInParent<VRCAvatarDescriptor>();

            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Make It MMD 1.2", titleStyle);

            // Run asserts, however continue rendering GUI if errors are encountered.
            bool avatarOkay = _commonChecks.RunChecks(avatar?.gameObject, isBuildTime: false);

            var visemeSkinnedMesh = avatar?.VisemeSkinnedMesh;
            var renameFaceForMmdComponents = avatar?.gameObject.GetComponentsInChildren<RenameFaceForMmdComponent>();
            if (!data.ignoreFaceMeshName && visemeSkinnedMesh?.name != "Body" && renameFaceForMmdComponents.Count() == 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(L.Tr("CommonChecks:AvatarFaceSMRNotCalledBody"), MessageType.Info);
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button(L.Tr("Common:FixThis")))
                {
                    data.gameObject.AddComponent<RenameFaceForMmdComponent>();
                }
                if (GUILayout.Button(L.Tr("Common:Ignore")))
                {
                    data.ignoreFaceMeshName = true;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            if (!data.ignoreFaceMeshNotAtRoot && visemeSkinnedMesh.transform.parent?.gameObject != avatar?.gameObject)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(L.Tr("CommonChecks:AvatarFaceSMRNotAtRoot"), MessageType.Info);
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button(L.Tr("Common:Ignore")))
                {
                    data.ignoreFaceMeshNotAtRoot = true;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            var writeDefaultsComponents = avatar?.gameObject.GetComponentsInChildren<WriteDefaultsComponent>();
            if (!data.ignoreWriteDefaultsOff && _commonChecks.AvatarHasWriteDefaultOff(avatar) && writeDefaultsComponents.All(x => !x.forceAvatarWriteDefaults))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(L.Tr("CommonChecks:AvatarWriteDefaultOffFound"), MessageType.Info);
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button(L.Tr("Common:FixThis")))
                {
                    if (writeDefaultsComponents.Count() == 0)
                    {
                        var newComponent = data.gameObject.AddComponent<WriteDefaultsComponent>();
                        newComponent.forceAvatarWriteDefaults = true;
                    }
                    else
                    {
                        writeDefaultsComponents.First().forceAvatarWriteDefaults = true;
                    }
                }
                if (GUILayout.Button(L.Tr("Common:Ignore")))
                {
                    data.ignoreWriteDefaultsOff = true;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            bool hasMmdShapeKeys = false;
            if (!EditorApplication.isPlaying)
            {
                // TODO: check all languages
                for (int i = 0; i < visemeSkinnedMesh?.sharedMesh.blendShapeCount; i++)
                {
                    string blendShapeName = visemeSkinnedMesh.sharedMesh.GetBlendShapeName(i);
                    hasMmdShapeKeys = hasMmdShapeKeys || MmdBlendShapeNames.All.Any(blendShape => blendShape.Name == blendShapeName);
                }
            }
            if (hasMmdShapeKeys)
            {
                EditorGUILayout.BeginHorizontal();
                var helpBoxRect = EditorGUILayout.BeginVertical();
                EditorGUILayout.HelpBox(L.Tr("MappingsEditor:ImportBlendShapesSuggestion"), MessageType.Info);
                EditorGUILayout.EndVertical();
                _hasMmdShapeKeysHelpBoxHeight = helpBoxRect.height > 2 ? helpBoxRect.height - 2 : _hasMmdShapeKeysHelpBoxHeight;
                if (GUILayout.Button(L.Tr("MappingsEditor:ImportBlendShapesButton"), GUILayout.MinHeight(_hasMmdShapeKeysHelpBoxHeight)))
                {
                    MmdScanAndImportWindow.ShowWindow(avatar);
                }
                EditorGUILayout.EndHorizontal();
            }

            var openEditorButtonStyle = new GUIStyle(GUI.skin.button);
            openEditorButtonStyle.fontSize = 20;
            openEditorButtonStyle.padding = new RectOffset(10, 10, 10, 10);
            if (GUILayout.Button(L.Tr("MappingsEditor:OpenEditor"), openEditorButtonStyle))
            {
                BlendShapeMappingsEditorWindow.ShowWindow(data);
            }

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = avatarOkay;
            if (GUILayout.Button(L.Tr("MappingsEditor:ShareMenuLabel")))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent(L.Tr("MappingsEditor:ShareAsUnitypackage")), false, () => ExportPackage(data));
                menu.ShowAsContext();
            }
            GUI.enabled = true;

            if (GUILayout.Button(L.Tr("MappingsEditor:MoreMenuLabel")))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent(L.Tr("MappingsEditor:ShowStoredData")), _showStoredData, () => _showStoredData = !_showStoredData);
                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            L.DrawLanguagePicker();
            EditorGUILayout.EndHorizontal();

            if (_showStoredData)
            {
                var mappings = serializedObject.FindProperty(nameof(BlendShapeMappings.blendShapeMappings));
                EditorGUILayout.PropertyField(mappings);
            }
        }

        private void ExportPackage(BlendShapeMappings mappingsComponent)
        {
            string packagePath = EditorUtility.SaveFilePanel(
                L.Tr("SaveFilePanel:SaveUnitypackage"),
                "",
                "",
                "unitypackage");

            if (string.IsNullOrEmpty(packagePath))
            {
                return;
            }

            // Create a new GameObject with a clone of current mappings.
            // The name doesn't matter because it will match the prefab's name.
            var newGameObject = new GameObject();
            var newMappingsComponent = newGameObject.AddComponent<BlendShapeMappings>();
            foreach (var mapping in mappingsComponent.blendShapeMappings)
            {
                newMappingsComponent.blendShapeMappings[mapping.Key] = mapping.Value.Clone();
            }

            // Ensure a unique path for the prefab.
            // TODO: choose a temp folder so that this doesn't turn into "MakeItMMD 1" etc. if it already exists?
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath("Assets/MakeItMMD.prefab");

            // Save the prefab.
            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAsset(newGameObject, prefabPath, out prefabSuccess);
            if (!prefabSuccess)
            {
                // TODO: see if this can be presented as a dialog.
                Debug.Log("Prefab failed to save");
                return;
            }

            // Export the prefab as a package.
            var packageExporter = VpaiPackageExporter.WithConfig(
                "Packages/enitimeago.non-destructive-mmd/Editor/vendor/ExportPackageWithVpai/com.anatawa12.vpm-package-auto-installer.dll",
                new List<string> { "https://enitimeago.github.io/vpm-repos/index.json" },
                new Dictionary<string, string> { { "enitimeago.non-destructive-mmd", "^1.0.0" } },
                silentIfInstalled: true);
            packageExporter.ExportPackage(prefabPath, packagePath);

            // Delete the prefab.
            // This is safe because AssetDatabase.GenerateUniqueAssetPath was used.
            AssetDatabase.DeleteAsset(prefabPath);
        }
    }
}
