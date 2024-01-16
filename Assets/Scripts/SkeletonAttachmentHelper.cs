using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.XR;

public class SkeletonAttachmentHelper
{
    private Dictionary<string, Transform> cachedBoneTransforms;
    private GameObject loadedSkeleton;


    public SkeletonAttachmentHelper(GameObject skeleton)
    {
        loadedSkeleton = skeleton;
        CacheBoneTransforms();
    }


    public void AttachToRiggedSkeleton(GameObject modelInstance, Dictionary<string, string> boneNameMap)
    {
        SkinnedMeshRenderer modelRenderer = modelInstance.GetComponent<SkinnedMeshRenderer>();
        if (modelRenderer != null)
        {
            Transform[] boneTransforms = new Transform[modelRenderer.bones.Length];
            for (int i = 0; i < modelRenderer.bones.Length; i++)
            {
                string boneName = modelRenderer.bones[i].name;
                if (boneNameMap.TryGetValue(boneName, out string skeletonBoneName))
                {
                    Transform boneInSkeleton = FindBoneInSkeleton(skeletonBoneName);
                    if (boneInSkeleton != null)
                    {
                        boneTransforms[i] = boneInSkeleton;
                    }
                    else
                    {
                        Debug.LogWarning($"Bone '{boneName}' not found in rigged skeleton.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Mapping for '{boneName}' not found.");
                }
            }
            modelRenderer.bones = boneTransforms;
            if (boneNameMap.TryGetValue("root", out string rootBoneName))
            {
                modelRenderer.rootBone = FindBoneInSkeleton(rootBoneName);
            }
        }
    }

    private Transform FindBoneInSkeleton(string boneName)
    {
        if (cachedBoneTransforms.TryGetValue(boneName, out Transform boneTransform))
        {
            return boneTransform;
        }
        else
        {
            return FindDeepChild(loadedSkeleton.transform, boneName);
        }
    }

    public Dictionary<string, Transform> CacheBoneTransforms()
    {
        cachedBoneTransforms = new Dictionary<string, Transform>();
        PopulateTransformsDictionary(loadedSkeleton.transform);
        return cachedBoneTransforms;
    }

    private void PopulateTransformsDictionary(Transform parent)
    {
        foreach (Transform child in parent)
        {
            cachedBoneTransforms[child.name] = child;
            PopulateTransformsDictionary(child); // Recursively add all child transforms
        }
    }

    public RigBuilder EnsureRigBuilder(GameObject skeleton)
    {
        RigBuilder rigBuilder = skeleton.GetComponent<RigBuilder>();
        if (rigBuilder == null)
        {
            rigBuilder = skeleton.AddComponent<RigBuilder>();
        }
        return rigBuilder;
    }

    public MultiPositionConstraint SetupMultiPositionConstraint(GameObject skeleton, Dictionary<string, string[]> boneMappings, GameObject modelInstance)
    {
        GameObject multiPosConstraintObj = new GameObject("MultiPositionConstraint");
        multiPosConstraintObj.transform.SetParent(skeleton.transform);

        MultiPositionConstraint multiPosConstraint = multiPosConstraintObj.AddComponent<MultiPositionConstraint>();

        foreach (var mapping in boneMappings)
        {
            Transform modelBone = TransformExtensions.FindDeepChild(modelInstance.transform, mapping.Key);
            WeightedTransformArray sourceObjects = new WeightedTransformArray();

            foreach (string targetBoneName in mapping.Value)
            {
                Transform targetBone = TransformExtensions.FindDeepChild(skeleton.transform, targetBoneName);
                sourceObjects.Add(new WeightedTransform(targetBone, 1f));
            }

            multiPosConstraint.data.sourceObjects = sourceObjects;
            multiPosConstraint.data.constrainedObject = modelBone;
            multiPosConstraint.data.maintainOffset = true;
        }
        return multiPosConstraint;
    }

    public TwoBoneIKConstraint SetupTwoBoneIKConstraint(GameObject skeleton, string rootBoneName, string midBoneName, string tipBoneName, Transform target, Transform hint = null)
    {
        GameObject twoBoneIKObj = new GameObject("TwoBoneIKConstraint");
        twoBoneIKObj.transform.SetParent(skeleton.transform);

        TwoBoneIKConstraint twoBoneIK = twoBoneIKObj.AddComponent<TwoBoneIKConstraint>();

        twoBoneIK.data.root = TransformExtensions.FindDeepChild(skeleton.transform, rootBoneName
);
        twoBoneIK.data.mid = TransformExtensions.FindDeepChild(skeleton.transform, midBoneName);
        twoBoneIK.data.tip = TransformExtensions.FindDeepChild(skeleton.transform, tipBoneName);
        twoBoneIK.data.target = target;
        twoBoneIK.data.hint = hint;    
        twoBoneIK.data.targetPositionWeight = 1f;
        twoBoneIK.data.targetRotationWeight = 1f;
        twoBoneIK.data.hintWeight = 0.1f;

        return twoBoneIK;
    }

    //public void SetupModelRigConstraints(GameObject modelInstance, Dictionary<string, Transform> boneDictionary)
    //{
    //    Transform modelPelvis;
    //    if (boneDictionary.TryGetValue("pelvis", out modelPelvis))
    //    {
    //        Transform skeletonPelvis = loadedSkeleton.transform.FindDeepChild("pelvis");
    //        if (skeletonPelvis != null)
    //        {
    //            pelvisConstraint = SetupConstraintOnModel(modelPelvis, skeletonPelvis);
    //        }
    //        else
    //        {
    //            Debug.LogError("Pelvis bone not found in the skeleton.");
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError("Pelvis bone not found in the model.");
    //    }

    //    // Setup for TwoBoneIKConstraint for legs
    //    bool leftLegIKSetup = SetupLegIKConstraints(modelInstance, "l_", "l_sole_helper");
    //    bool rightLegIKSetup = SetupLegIKConstraints(modelInstance, "r_", "r_sole_helper");

    //    // Update source objects for pelvis MultiPositionConstraint if thigh bones are present and pelvis constraint is created
    //    if (pelvisConstraint != null && (leftLegIKSetup || rightLegIKSetup))
    //    {
    //        UpdatePelvisConstraintSourceObjects(pelvisConstraint, modelInstance);
    //    }
    //}

    private MultiPositionConstraint SetupConstraintOnModel(Transform modelBone, Transform skeletonBone)
    {
        MultiPositionConstraint constraint = modelBone.gameObject.AddComponent<MultiPositionConstraint>();
        constraint.data.constrainedObject = modelBone;
        WeightedTransformArray sourceObjects = new WeightedTransformArray();
        sourceObjects.Add(new WeightedTransform(skeletonBone, 1f));
        constraint.data.sourceObjects = sourceObjects;
        constraint.data.maintainOffset = true;

        return constraint;
    }

    private bool SetupLegIKConstraints(GameObject modelInstance, string prefix, string helperBoneName)
    {
        // Start by finding the topmost bone in the hierarchy
        Transform rootBone = modelInstance.transform.Find($"Armature/{prefix}thigh");

        if (rootBone == null)
        {
            Debug.LogError($"[SetupLegIKConstraints] {prefix}thigh bone not found in the model.");
            return false;
        }

        // Find the child bones relative to the rootBone
        Transform midBone = rootBone.Find($"{prefix}calf");
        if (midBone == null)
        {
            Debug.LogError($"[SetupLegIKConstraints] {prefix}calf bone not found in the model.");
            return false;
        }

        Transform tipBone = midBone.Find($"{prefix}foot");
        if (tipBone == null)
        {
            Debug.LogError($"[SetupLegIKConstraints] {prefix}foot bone not found in the model.");
            return false;
        }

        // Find the helper bone in the skeleton
        Transform helperBone = loadedSkeleton.transform.FindDeepChild(helperBoneName);
        if (helperBone == null)
        {
            Debug.LogError($"[SetupLegIKConstraints] {helperBoneName} bone not found in the skeleton.");
            return false;
        }

        // Create and configure the TwoBoneIKConstraint
        TwoBoneIKConstraint ikConstraint = rootBone.gameObject.AddComponent<TwoBoneIKConstraint>();
        ikConstraint.data.root = rootBone;
        ikConstraint.data.mid = midBone;
        ikConstraint.data.tip = tipBone;
        ikConstraint.data.target = helperBone;
        ikConstraint.data.targetPositionWeight = 1f;
        ikConstraint.data.targetRotationWeight = 0.1f;
        ikConstraint.data.hintWeight = 0.1f;

        Debug.Log($"[SetupLegIKConstraints] TwoBoneIKConstraint successfully added for {prefix} leg");
        return true;
    }

    private Transform FindBoneInModel(GameObject modelInstance, string boneIdentifier)
    {
        Transform bone = FindDeepChild(modelInstance.transform.Find("Armature"), boneIdentifier);
        if (bone == null)
        {
            Debug.LogError($"[FindBoneInModel] Bone with identifier '{boneIdentifier}' not found.");
        }
        else
        {
            Debug.Log($"[FindBoneInModel] Bone with identifier '{boneIdentifier}' found: { bone.name}");
        }
        return bone;
    }


    private void UpdatePelvisConstraintSourceObjects(MultiPositionConstraint pelvisConstraint, GameObject modelInstance)
    {
        Transform lThigh = modelInstance.transform.Find("Armature/l_thigh");
        Transform rThigh = modelInstance.transform.Find("Armature/r_thigh");

        if (lThigh != null)
        {
            pelvisConstraint.data.sourceObjects.Add(new WeightedTransform(lThigh, 1f));
        }

        if (rThigh != null)
        {
            pelvisConstraint.data.sourceObjects.Add(new WeightedTransform(rThigh, 1f));
        }
    }

    public Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}