#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;

public class SkeletonAttachmentEditor : EditorWindow
{
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
    }

    private void PreparePrefabSkeletons()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Resources/Prefabs" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log("Processing prefab at path: " + path);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("Failed to load prefab at path: " + path);
                continue;
            }

            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (prefabInstance == null)
            {
                Debug.LogError("Failed to instantiate prefab: " + path);
                continue;
            }

            Transform armature = prefabInstance.transform.Find("Armature");
            if (armature == null)
            {
                Debug.LogError("Armature not found in prefab: " + path);
                DestroyImmediate(prefabInstance);
                continue;
            }

            PrefabSkeletonMapper mapper = prefabInstance.GetComponent<PrefabSkeletonMapper>();
            if (mapper == null)
            {
                mapper = prefabInstance.AddComponent<PrefabSkeletonMapper>();
                if (mapper == null)
                {
                    Debug.LogError("Failed to add PrefabSkeletonMapper to prefab: " + path);
                    DestroyImmediate(prefabInstance);
                    continue;
                }
            }

            Debug.Log("Caching bones for prefab: " + path);
            mapper.CacheBones();

            PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
            DestroyImmediate(prefabInstance);
        }
    }
}
#endif