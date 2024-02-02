using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The ChildSkeleton script dynamically aligns its bones to match those of a ParentSkeleton in Unity. 
/// It initially maps each child bone to the corresponding parent bone, storing their relative rotations. 
/// During runtime, it optionally updates child bone transforms to match the parent's, allowing for synchronized animations or transformations between parent and child skeletons with adjustable interpolation speed and weighting.
/// </summary>

namespace doppelganger
{
    public class ChildSkeleton : MonoBehaviour
    {
        public ParentSkeleton parentSkeleton { get; private set; }
        private Dictionary<string, Quaternion> initialRelativeRotations = new Dictionary<string, Quaternion>();
        public float lerpSpeed = 0.01f; // Adjust this speed to control the interpolation speed
        public float weight = 0.7f; // Weight for interpolation

        void Start()
        {
            parentSkeleton = FindObjectOfType<ParentSkeleton>();
            if (parentSkeleton != null)
            {
                MapToParentSkeleton();
            }
        }

        //void Update()
        //{
        //    if (parentSkeleton != null)
        //    {
        //        UpdateBoneTransforms();
        //    }
        //}

        void MapToParentSkeleton()
        {
            List<ParentSkeleton.BoneData> parentBones = parentSkeleton.GetBoneData();
            Transform armatureTransform = transform.Find("Armature");

            if (armatureTransform != null)
            {
                foreach (var boneData in parentBones)
                {
                    Transform childBone = armatureTransform.FindDeepChild(boneData.boneTransform.name);
                    if (childBone != null)
                    {
                        // Set child bone's position to match parent bone's
                        childBone.position = boneData.boneTransform.position;

                        // Calculate and store the initial relative rotation
                        Quaternion relativeRotation = Quaternion.Inverse(boneData.boneTransform.rotation) * childBone.rotation;
                        initialRelativeRotations[boneData.boneTransform.name] = relativeRotation;
                    }
                }
            }
        }

        void UpdateBoneTransforms()
        {
            List<ParentSkeleton.BoneData> parentBones = parentSkeleton.GetBoneData();
            Transform armatureTransform = transform.Find("Armature");

            if (armatureTransform != null)
            {
                foreach (var boneData in parentBones)
                {
                    if (ParentSkeleton.IncludedBones.Contains(boneData.boneTransform.name))
                    {
                        Transform childBone = armatureTransform.FindDeepChild(boneData.boneTransform.name);
                        if (childBone != null)
                        {
                            // Set child bone's position to match parent bone's
                            childBone.position = boneData.boneTransform.position;

                            // Apply the initial relative rotation to the parent's current rotation
                            childBone.rotation = boneData.boneTransform.rotation * initialRelativeRotations[boneData.boneTransform.name];
                        }
                    }
                }
            }
        }
    }


    public static class TransformDeepChildExtension
    {
        // Recursive method to find a child by name in the transform hierarchy
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                Transform result = child.FindDeepChild(name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}