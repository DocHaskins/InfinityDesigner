using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;

public class SkeletonAttachmentEditor : EditorWindow
{
    private GameObject selectedSkeleton;
    private GameObject modelInstance;
    private Dictionary<string, string> boneNameMap = new Dictionary<string, string>();
    private Dictionary<string, Transform> cachedBones = new Dictionary<string, Transform>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Skeleton Attachment Helper")]
    public static void ShowWindow()
    {
        GetWindow<SkeletonAttachmentEditor>("Skeleton Attachment Helper");
    }

    void OnGUI()
    {
        GUILayout.Label("Skeleton Attachment Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Prepare Prefab Skeletons"))
        {
            PreparePrefabSkeletons();
        }

        selectedSkeleton = (GameObject)EditorGUILayout.ObjectField("Skeleton", selectedSkeleton, typeof(GameObject), true);
        modelInstance = (GameObject)EditorGUILayout.ObjectField("Model Instance", modelInstance, typeof(GameObject), true);

        if (selectedSkeleton != null && modelInstance != null) // Update this line
        {
            

            if (GUILayout.Button("Cache Skeleton Bones"))
            {
                CacheSkeletonBones();
            }

            // Display and edit bone mappings
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var boneName in cachedBones.Keys)
            {
                GUILayout.Label("Model Bone: " + boneName);
                if (!boneNameMap.ContainsKey(boneName))
                {
                    boneNameMap[boneName] = "";
                }
                boneNameMap[boneName] = EditorGUILayout.TextField("Skeleton Bone", boneNameMap[boneName]);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add/Update Bone Mappings"))
            {
                // Here you can process the updated mappings
            }

            if (GUILayout.Button("Prepare Skeleton"))
            {
                PrepareSkeleton(modelInstance); // Pass modelInstance
            }
        }
    }

    private void CacheSkeletonBones()
    {
        if (selectedSkeleton == null) return;

        SkeletonAttachmentHelper helper = new SkeletonAttachmentHelper(selectedSkeleton);
        cachedBones = helper.CacheBoneTransforms();
    }

    private void PreparePrefabSkeletons()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Resources/Prefabs" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                // Load the prefab into the scene for editing
                GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                // Check for the presence of an 'Armature' child
                Transform armature = prefabInstance.transform.Find("Armature");
                if (armature != null)
                {
                    PrefabSkeletonMapper mapper = prefabInstance.GetComponent<PrefabSkeletonMapper>();
                    if (mapper == null)
                    {
                        mapper = prefabInstance.AddComponent<PrefabSkeletonMapper>();
                    }
                    mapper.CacheBones();

                    // Apply changes and update the prefab
                    PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
                }

                // Clean up the instantiated prefab in the scene
                DestroyImmediate(prefabInstance);
            }
        }
    }

    private void PrepareSkeleton(GameObject modelInstance)
    {
        if (selectedSkeleton == null || modelInstance == null) return;

        SkeletonAttachmentHelper helper = new SkeletonAttachmentHelper(selectedSkeleton);

        // Ensure RigBuilder exists
        RigBuilder rigBuilder = helper.EnsureRigBuilder(selectedSkeleton);

        // Define your bone mappings for MultiPositionConstraint
        Dictionary<string, string[]> boneMappings = new Dictionary<string, string[]>
    {
        {"PelvisBoneNameInModel", new [] {"PelvisBoneNameInSkeleton"}},
        {"Spine2BoneNameInModel", new [] {"Spine2BoneNameInSkeleton"}},
        {"HeadBoneNameInModel", new [] {"HeadBoneNameInSkeleton"}}
    };

        // Setup MultiPositionConstraint
        MultiPositionConstraint multiPosConstraint = helper.SetupMultiPositionConstraint(selectedSkeleton, boneMappings, modelInstance); // Pass modelInstance here

        // Setup TwoBoneIKConstraints for each limb
        // Repeat this for each limb with appropriate bone names and target transforms
        TwoBoneIKConstraint armIK = helper.SetupTwoBoneIKConstraint(selectedSkeleton, "LArmPivot", "LForearmPivot", "LHandPivot", FindTargetTransform("HandTarget"));
    }


    private Transform FindTargetTransform(string targetName)
    {
        // Assuming the target is a child of the modelInstance
        // You can adjust this logic based on where your targets are located
        return modelInstance.transform.Find(targetName);
    }
}