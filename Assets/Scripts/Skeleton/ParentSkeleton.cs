using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The ParentSkeleton script in Unity is designed to serialize bone data for a character's skeleton. 
/// It specifically targets a predefined set of bones, identified by name, to include in the serialization process. 
/// Starting from the "pelvis" bone, it recursively adds relevant bone data, excluding children of the "head" bone to maintain a focused set of bones. 
/// This structured approach facilitates complex skeletal manipulations and interactions within the Unity engine, such as coordinating animations across different models.
/// </summary>

namespace doppelganger
{
    public class ParentSkeleton : MonoBehaviour
    {
        public static readonly HashSet<string> IncludedBones = new HashSet<string>
{
    "pelvis", "spine", "spine1", "spine2", "spine3",
    "neck", "neck1", "neck2", "head", "r_eye", "l_eye",
    "l_clavicle", "l_upperarm", "l_forearm", "l_hand",
    "r_clavicle", "r_upperarm", "r_forearm", "r_hand",
    "l_thigh", "r_thigh", "l_calf", "r_calf",
    "l_foot", "r_foot", "l_sole_helper", "r_sole_helper"
};

        [System.Serializable]
        public class BoneData
        {
            public Transform boneTransform;
        }

        private List<BoneData> serializedBones = new List<BoneData>();
        public Dictionary<string, Bounds> AreaSpecificBounds { get; private set; } = new Dictionary<string, Bounds>();

        public Bounds SkeletonBounds { get; private set; }

        void Awake() // Changed from Start to Awake to ensure bounds are calculated as soon as possible
        {
            SerializeBones();
            CalculateAndSetBounds();
            CalculateAreaSpecificBounds();
        }

        public void SerializeBones()
        {
            Transform pelvis = transform.Find("pelvis");
            if (pelvis != null)
            {
                AddBoneData(pelvis, includeChildren: true);
            }
        }

        public void AddBoneData(Transform bone, bool includeChildren)
        {
            if (bone.name == "head")
            {
                includeChildren = false;
            }

            if (IncludedBones.Contains(bone.name))
            {
                serializedBones.Add(new BoneData
                {
                    boneTransform = bone
                });
            }

            if (includeChildren)
            {
                foreach (Transform child in bone)
                {
                    AddBoneData(child, includeChildren);
                }
            }
        }

        public List<BoneData> GetBoneData()
        {
            return serializedBones;
        }

        private void CalculateAndSetBounds()
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.negativeInfinity);
            foreach (BoneData boneData in serializedBones)
            {
                bounds.Encapsulate(boneData.boneTransform.position);
            }

            if (bounds.size == Vector3.negativeInfinity)
            {
                bounds.size = Vector3.zero;
            }

            SkeletonBounds = bounds;
        }

        private void CalculateAreaSpecificBounds()
        {
            // Define mappings for areas to bone names
            var areaToBoneNames = new Dictionary<string, HashSet<string>>()
        {
            { "All", new HashSet<string> { "pelvis", "spine", "spine1", "spine2", "spine3", "neck", "neck1", "neck2", "head", "r_eye", "l_eye","l_clavicle", "l_upperarm", "l_forearm", "l_hand","r_clavicle", "r_upperarm", "r_forearm", "r_hand","l_thigh", "r_thigh", "l_calf", "r_calf", "l_foot", "r_foot", "l_sole_helper", "r_sole_helper" } },
            { "Face", new HashSet<string> { "spine2", "spine3", "neck", "neck1", "neck2", "head", "r_eye", "l_eye" } },
            { "UpperBody", new HashSet<string> { "pelvis", "l_thigh", "r_thigh", "spine1", "spine2", "spine3", "neck", "l_clavicle", "l_upperarm", "l_forearm", "l_hand", "r_clavicle", "r_upperarm", "r_forearm", "r_hand" } },
            { "Hands", new HashSet<string> { "l_forearm", "l_hand", "r_forearm", "r_hand" } },
            { "LowerBody", new HashSet<string> { "l_thigh", "r_thigh", "l_calf", "r_calf", "l_foot", "r_foot", "l_sole_helper", "r_sole_helper" } },
            { "Feet", new HashSet<string> { "l_foot", "r_foot", "l_sole_helper", "r_sole_helper" } },
        };

            foreach (var area in areaToBoneNames)
            {
                Bounds areaBounds = new Bounds(Vector3.zero, Vector3.negativeInfinity);
                foreach (var boneData in serializedBones)
                {
                    if (area.Value.Contains(boneData.boneTransform.name))
                    {
                        areaBounds.Encapsulate(boneData.boneTransform.position);
                    }
                }

                if (areaBounds.size == Vector3.negativeInfinity)
                {
                    areaBounds.size = Vector3.zero;
                }

                AreaSpecificBounds[area.Key] = areaBounds;
            }
        }
    }
}