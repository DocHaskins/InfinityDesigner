using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The CinemachineCameraZoomTool script enhances the CinemachineFreeLook component with zoom and target cycling capabilities. 
/// It allows dynamic adjustment of the camera's orbits through user input, effectively scaling the camera's view. 
/// The script supports cycling through predefined targets, vertical movement restrictions, and smooth transitions between camera settings. 
/// It utilizes custom speed settings for vertical and horizontal movements and incorporates a feature for setting the camera back to its default field of view and orbit configurations.
/// </summary>

namespace Cinemachine
{
    [SaveDuringPlay]
    [RequireComponent(typeof(CinemachineFreeLook))]
public class CinemachineCameraZoomTool : MonoBehaviour
    {

        private bool isAdjusting = false;
        private bool manualOrbitAdjustmentsMade = false;
        private float initialYAxisValue = 0.5f;
        private CinemachineFreeLook freelook;
        public CinemachineFreeLook.Orbit[] originalOrbits = new CinemachineFreeLook.Orbit[0];
        public List<Transform> targets = new List<Transform>();
        private readonly List<string> pointNames = new List<string>
    {
        "pelvis", "spine3", "neck1", "legs", "r_hand", "l_hand", "l_foot", "r_foot"
    };
        private int currentTargetIndex = 0;
        private float initialCameraOffsetY;
        

        [Tooltip("Speed of vertical camera movement")]
        public float verticalSpeed = 10f; // Speed of vertical movement
        private float horizontalSpeed = 10f;

        [Tooltip("Minimum Y position for the camera")]
        public float minY = -10f; // Minimum Y value

        [Tooltip("Maximum Y position for the camera")]
        public float maxY = 10f; // Maximum Y value

        private bool isDragging = false;

        [Tooltip("The minimum scale for the orbits")]
        [Range(0.01f, 1f)]
        public float minScale = 0.5f;

        [Tooltip("The maximum scale for the orbits")]
        [Range(1f, 50f)]
        public float maxScale = 1.0f;

        [Tooltip("Default Field of View")]
        public float defaultFOV = 60f;

        [Tooltip("Default Radius for the Middle Rig")]
        public float defaultMiddleRigRadius = 3f;

        [Tooltip("Default Height for the Middle Rig")]
        public float defaultMiddleRigHeight = 0.75f;

        [Tooltip("The vertical Axis. Value is 0...1. How much to scale the orbits")]
        [AxisStateProperty]
        public AxisState zAxis = new AxisState(minValue: 0, maxValue: 1, wrap: false, rangeLocked: true, maxSpeed: 50f, accelTime: 0.1f, decelTime: 0.1f, name: "Mouse ScrollWheel", invert: true);
        void Start()
        {
            zAxis.Value = 0.2f;
            minScale = Mathf.Max(0.01f, minScale);
            maxScale = Mathf.Max(minScale, maxScale);
            UpdateCameraTarget();
        }

        void Awake()
        {
            freelook = GetComponentInChildren<CinemachineFreeLook>();
            if (freelook != null && originalOrbits.Length == 0)
            {
                zAxis.Update(Time.deltaTime);
                float scale = Mathf.Lerp(minScale, maxScale, zAxis.Value);
                for (int i = 0; i < Mathf.Min(originalOrbits.Length, freelook.m_Orbits.Length); i++)
                {
                    freelook.m_Orbits[i].m_Height = originalOrbits[i].m_Height * scale;
                    freelook.m_Orbits[i].m_Radius = originalOrbits[i].m_Radius * scale;
                }
            }
        }

        void Update()
        {
            if (isAdjusting)
                return;

            if (freelook != null)
            {
                if (!manualOrbitAdjustmentsMade && originalOrbits.Length != freelook.m_Orbits.Length)
                {
                    originalOrbits = new CinemachineFreeLook.Orbit[freelook.m_Orbits.Length];
                    Array.Copy(freelook.m_Orbits, originalOrbits, freelook.m_Orbits.Length);
                }
                zAxis.Update(Time.deltaTime);
                float scale = Mathf.Lerp(minScale, maxScale, zAxis.Value);
                for (int i = 0; i < Mathf.Min(originalOrbits.Length, freelook.m_Orbits.Length); i++)
                {
                    freelook.m_Orbits[i].m_Height = originalOrbits[i].m_Height * scale;
                    freelook.m_Orbits[i].m_Radius = originalOrbits[i].m_Radius * scale;
                }

                // Modify input handling for orbiting
                if (Input.GetMouseButton(1)) // Left mouse button
                {
                    freelook.m_XAxis.m_InputAxisValue = Input.GetAxis("Mouse X");
                    freelook.m_YAxis.m_InputAxisValue = Input.GetAxis("Mouse Y");
                }
                else
                {
                    freelook.m_XAxis.m_InputAxisValue = 0;
                    freelook.m_YAxis.m_InputAxisValue = 0;
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                CycleTarget(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                CycleTarget(-1);
            }
            if (Input.GetMouseButtonDown(2)) // Middle mouse button pressed
            {
                isDragging = true;
            }

            if (Input.GetMouseButtonUp(2)) // Middle mouse button released
            {
                isDragging = false;
            }

            if (isDragging)
            {
                // Calculate horizontal input for rotation
                float mouseXInput = Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime;
                // Rotate the camera or its target around the Y axis
                RotateCameraY(mouseXInput);
            }
        }

        private void RotateCameraY(float rotationAmount)
        {
            // Assuming freelook follows a target, rotate the target. Otherwise, adjust freelook's GameObject rotation.
            Vector3 currentRotation = freelook.transform.eulerAngles;
            currentRotation.y += rotationAmount;
            freelook.transform.eulerAngles = currentRotation;
        }

        public int CurrentTargetIndex
        {
            get { return currentTargetIndex; }
            set { currentTargetIndex = value; }
        }
        private void CycleTarget(int direction)
        {
            if (targets.Count == 0) return;

            currentTargetIndex += direction;
            if (currentTargetIndex >= targets.Count) currentTargetIndex = 0;
            if (currentTargetIndex < 0) currentTargetIndex = targets.Count - 1;

            // Update camera target after cycling
            UpdateCameraTarget();
        }

        public Transform GetCurrentFocusPoint()
        {
            if (targets.Count > 0 && currentTargetIndex >= 0 && currentTargetIndex < targets.Count)
            {
                return targets[currentTargetIndex];
            }
            return null;
        }

        public void FocusOn(FocusPoint focusPoint)
        {
            if (focusPoint == null) return;
            Debug.Log($"Adjusting camera to focus on: {focusPoint.targetTransform.name} with radius adjustment: {focusPoint.targetRadiusAdjustment}");

            // Directly setting the LookAt target without moving the GameObject.
            freelook.LookAt = focusPoint.targetTransform;
            //freelook.Follow = focusPoint.targetTransform;

            //If there's a radius adjustment, apply it to the orbits.
            if (Mathf.Abs(focusPoint.targetRadiusAdjustment) > 0f)
            {
                StartCoroutine(AdjustOrbitRadiusCoroutine(focusPoint.targetRadiusAdjustment));
            }
        }

        private IEnumerator AdjustOrbitRadiusCoroutine(float targetRadiusAdjustment)
        {
            float adjustmentDuration = 1.0f; // Duration over which to apply the adjustment, for smoothness.
            float elapsedTime = 0f;

            // Save the initial radius of each orbit to smoothly interpolate from.
            float[] initialRadii = new float[freelook.m_Orbits.Length];
            for (int i = 0; i < freelook.m_Orbits.Length; i++)
            {
                initialRadii[i] = freelook.m_Orbits[i].m_Radius;
            }

            while (elapsedTime < adjustmentDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / adjustmentDuration;
                for (int i = 0; i < freelook.m_Orbits.Length; i++)
                {
                    // Smoothly adjust the radius of each orbit.
                    freelook.m_Orbits[i].m_Radius = Mathf.Lerp(initialRadii[i], initialRadii[i] + targetRadiusAdjustment, t);
                    freelook.m_YAxis.Value = Mathf.Lerp(initialYAxisValue, 0.5f, t);
                }
                freelook.m_YAxis.Value = 0.5f;
                yield return null;
            }

            // After completing the adjustment, update the originalOrbits to reflect the new radius values.
            for (int i = 0; i < freelook.m_Orbits.Length; i++)
            {
                originalOrbits[i].m_Radius = freelook.m_Orbits[i].m_Radius;
                // If you also adjust height or other parameters, update them similarly here.
            }

            // Optionally, log the update for debugging purposes
            Debug.Log("Original orbits updated with new radius values.");
        }

        public void UpdateCameraTarget()
        {
            if (Application.isPlaying && freelook != null && targets.Count > currentTargetIndex)
            {
                Transform newTarget = targets[currentTargetIndex];
                freelook.LookAt = newTarget;
                freelook.Follow = newTarget; // Only set this if you want the camera to also follow the target positionally
            }
        }
    }
}