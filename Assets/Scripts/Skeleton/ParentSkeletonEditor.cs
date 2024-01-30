using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ParentSkeleton))]
public class ParentSkeletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default inspector

        ParentSkeleton parentSkeleton = (ParentSkeleton)target;

        if (GUILayout.Button("Refresh Bone Data"))
        {
            parentSkeleton.SerializeBones(); // Refresh the bone data
        }

        EditorGUILayout.LabelField("Mapped Bones:", EditorStyles.boldLabel);
        List<ParentSkeleton.BoneData> bones = parentSkeleton.GetBoneData();
        foreach (var bone in bones)
        {
            if (bone.boneTransform != null)
            {
                EditorGUILayout.LabelField(bone.boneTransform.name);
            }
        }
    }
}