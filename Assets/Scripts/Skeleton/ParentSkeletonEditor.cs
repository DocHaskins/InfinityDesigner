#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// The ParentSkeletonEditor script extends the Unity Editor to provide custom inspector functionality for the ParentSkeleton component. 
/// It allows users to manually trigger the serialization of bone data directly from the inspector with a "Refresh Bone Data" button. 
/// Additionally, it displays a list of all mapped bones within the ParentSkeleton, enhancing the visibility and ease of managing bone data for developers.
/// </summary>

namespace doppelganger
{
    [CustomEditor(typeof(ParentSkeleton))]
    public class ParentSkeletonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); 

            ParentSkeleton parentSkeleton = (ParentSkeleton)target;

            if (GUILayout.Button("Refresh Bone Data"))
            {
                parentSkeleton.SerializeBones();
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
}
#endif