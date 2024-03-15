using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace doppelganger
{
    public class AutoTargetCinemachineCamera : MonoBehaviour
    {
        public CinemachineVirtualCamera cinemachineCamera;
        public CinemachineFollowZoom cameraZoomTool;
        public CharacterBuilder characterBuilder;
        public CinemachineRecomposer recomposer;

        public string targetTag = "Player";
        public float padding = 1.1f;
        public bool debugBounds = true;
        private GameObject currentTarget;
        private Transform lookAtTarget;
        private Dictionary<string, GameObject> currentlyLoadedModels;
        private List<Bounds> debugBoundsList = new List<Bounds>();

        private Dictionary<string, float> areaToZoomMapping = new Dictionary<string, float>()
    {
        { "All", 2.0f },
        { "Face", 0.85f },
        { "UpperBody", 1.2f },
        { "Hands", 1.5f },
        { "LowerBody", 1.3f },
        { "Feet", 0.9f }
    };

        void Start()
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponent<CinemachineVirtualCamera>();
            }

            lookAtTarget = new GameObject("CinemachineLookAtTarget").transform;
            // Initialize currentlyLoadedModels from the characterBuilder reference
            if (characterBuilder != null)
            {
                currentlyLoadedModels = characterBuilder.currentlyLoadedModels;
            }
        }

        void OnDrawGizmos()
        {
            if (!debugBounds) return;

            Gizmos.color = Color.green; // Set the color of the Gizmos
            foreach (var bounds in debugBoundsList)
            {
                // Draw a wire cube for each bounds
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        public void FocusOnSingleObject(GameObject targetObject)
        {
            currentTarget = targetObject;
            if (debugBounds)
            {
                debugBoundsList.Clear();
            }
            recomposer.m_Tilt = 0.0f;
            AdjustCameraToFitObject(targetObject);
        }

        public void FocusOnGroup()
        {
            if (currentlyLoadedModels != null && currentlyLoadedModels.Count > 0)
            {
                if (debugBounds)
                {
                    debugBoundsList.Clear();
                }
                AdjustCameraToFitObjects(currentlyLoadedModels);
            }
        }

        public void FocusOnSkeleton(GameObject skeleton)
        {
            AdjustCameraZoom(2.0f);
            if (skeleton == null)
            {
                Debug.LogError("Skeleton GameObject is null.");
                return;
            }

            //Debug.Log($"FocusSkeleton called with {skeleton.name}");

            ParentSkeleton parentSkeleton = skeleton.GetComponent<ParentSkeleton>();
            if (parentSkeleton == null)
            {
                Debug.LogError("ParentSkeleton component not found on the target object.");
                return;
            }

            // Use the bounds directly from the ParentSkeleton component
            Bounds bounds = parentSkeleton.SkeletonBounds;

            if (debugBounds)
            {
                debugBoundsList.Clear();
                debugBoundsList.Add(bounds); // Add bounds to list for debugging
            }

            AdjustCameraToFitSkeleton(bounds);

#if UNITY_EDITOR // Conditional compilation to ensure it's only compiled in the editor
            Debug.Log($"Focusing on skeleton: {skeleton.name}, Bounds: Center = {bounds.center}, Size = {bounds.size}");
#endif
        }

        public void FocusOnArea(string areaName)
        {
            GameObject skeleton = GameObject.FindGameObjectWithTag("Skeleton");
            if (skeleton == null)
            {
                Debug.LogError("Skeleton GameObject is null.");
                return;
            }

            ParentSkeleton parentSkeleton = skeleton.GetComponent<ParentSkeleton>();
            if (parentSkeleton == null)
            {
                Debug.LogError("ParentSkeleton component not found on the target object.");
                return;
            }

            // Check if the area-specific bounds are available
            if (!parentSkeleton.AreaSpecificBounds.ContainsKey(areaName))
            {
                Debug.LogError($"Area-specific bounds not found for area: {areaName}");
                return;
            }

            Bounds bounds = parentSkeleton.AreaSpecificBounds[areaName];

            if (debugBounds)
            {
                debugBoundsList.Clear();
                debugBoundsList.Add(bounds); // Add bounds to list for debugging
            }

            if (areaToZoomMapping.TryGetValue(areaName, out float zoomLevel))
            {
                AdjustCameraZoom(zoomLevel);
            }
            else
            {
                Debug.LogWarning($"Zoom level not defined for area: {areaName}");
            }

            AdjustCameraToFitSkeleton(bounds);

#if UNITY_EDITOR
            Debug.Log($"Focusing on area: {areaName}, Bounds: Center = {bounds.center}, Size = {bounds.size}");
#endif
        }

        private void AdjustCameraZoom(float zoomLevel)
        {
            if (cameraZoomTool)
            {
                cameraZoomTool.m_Width = zoomLevel;
            }
        }

        private void AdjustCameraToFitSkeleton(Bounds bounds)
        {
            lookAtTarget.position = bounds.center;
            cinemachineCamera.LookAt = lookAtTarget;
            AdjustCameraBasedOnBounds(bounds);
        }

        private void AdjustCameraToFitObject(GameObject targetObject)
        {
            Debug.Log($"AdjustCameraToFitObject: targetObject {targetObject}");
            Bounds bounds = CalculateBounds(targetObject);
            lookAtTarget.position = bounds.center;
            cinemachineCamera.LookAt = lookAtTarget;
            AdjustCameraBasedOnBounds(bounds);
        }

        private void AdjustCameraToFitObjects(Dictionary<string, GameObject> models)
        {
            Bounds combinedBounds = CalculateCombinedBounds(models);
            lookAtTarget.position = combinedBounds.center;
            cinemachineCamera.LookAt = lookAtTarget;
            AdjustCameraBasedOnBounds(combinedBounds);
        }

        Bounds CalculateBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsSet = false;
            SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                bounds = skinnedMeshRenderer.bounds;
                boundsSet = true;
            }

            if (!boundsSet)
            {
                MeshRenderer meshRenderer = obj.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                {
                    bounds = meshRenderer.bounds;
                    boundsSet = true;
                }
            }

            if (!boundsSet)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (boundsSet)
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                    else
                    {
                        bounds = renderer.bounds;
                        boundsSet = true;
                    }
                }
            }

            if (debugBounds)
            {
                debugBoundsList.Add(bounds);
            }

            return bounds;
        }

        Bounds CalculateCombinedBounds(Dictionary<string, GameObject> models)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsSet = false;

            foreach (var model in models.Values)
            {
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (boundsSet)
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                    else
                    {
                        bounds = renderer.bounds;
                        boundsSet = true;
                    }
                }
            }

            if (debugBounds)
            {
                debugBoundsList.Add(bounds); // Add bounds to list for debugging
            }

            return bounds;
        }

        void AdjustCameraBasedOnBounds(Bounds bounds)
        {
            if (debugBounds)
            {
                debugBoundsList.Clear();
                debugBoundsList.Add(bounds);
            }

            // Calculate the required distance to ensure the object fits within the view
            float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = CalculateRequiredDistance(objectSize);

            // Set camera distance
            SetCameraDistance(distance);

            // Optionally, adjust the camera's position or the follow target's position
            // This example assumes you want to keep the camera at a certain height but focus on the bounds center
            Vector3 cameraPosition = lookAtTarget.position - cinemachineCamera.transform.forward * distance;
            cameraPosition.y = bounds.center.y; // Adjust based on your requirements
            lookAtTarget.position = cameraPosition;

            // Ensure the camera's LookAt is set to the center of the bounds
            cinemachineCamera.LookAt = lookAtTarget;
            cinemachineCamera.Follow = lookAtTarget;

            // If you're using a CinemachineTransposer for following, you might adjust its offset instead
            var transposer = cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = cinemachineCamera.transform.position - bounds.center;
            }
        }

        float CalculateRequiredDistance(float objectSize)
        {
            float cameraFOV = cinemachineCamera.m_Lens.FieldOfView;
            float distance = (objectSize / 2.0f) / Mathf.Tan(Mathf.Deg2Rad * cameraFOV / 2.0f);
            return distance * padding;
        }

        void SetCameraDistance(float distance)
        {
            var transposer = cinemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset.z = -distance;
            }
        }
    }
}