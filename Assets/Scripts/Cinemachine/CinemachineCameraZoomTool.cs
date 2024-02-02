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

        private CinemachineFreeLook freelook;
        public CinemachineFreeLook.Orbit[] originalOrbits = new CinemachineFreeLook.Orbit[0];
        public List<Transform> targets = new List<Transform>();
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

        [SerializeField] private float transitionDuration = 1.0f;
        private bool isTransitioning = false;
        private float transitionStartTime;
        private Vector3 initialTargetPosition;

        [Tooltip("The vertical Axis. Value is 0...1. How much to scale the orbits")]
        [AxisStateProperty]
        public AxisState zAxis = new AxisState(minValue: 0, maxValue: 1, wrap: false, rangeLocked: true, maxSpeed: 50f, accelTime: 0.1f, decelTime: 0.1f, name: "Mouse ScrollWheel", invert: true);
        void Start()
        {
            zAxis.Value = 0.2f;
            minScale = Mathf.Max(0.01f, minScale);
            maxScale = Mathf.Max(minScale, maxScale);
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
            UpdateCameraTarget();
        }

        void Update()
        {
            if (freelook != null)
            {
                if (originalOrbits.Length != freelook.m_Orbits.Length)
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
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                SetDefaultRigSettings();
            }
        }

        private void RotateCameraY(float rotationAmount)
        {
            // Assuming freelook follows a target, rotate the target. Otherwise, adjust freelook's GameObject rotation.
            Vector3 currentRotation = freelook.transform.eulerAngles;
            currentRotation.y += rotationAmount;
            freelook.transform.eulerAngles = currentRotation;

            // Optionally, if clamping X and Z positions is required at this step, ensure they are reset to initial values
        }

        private void UpdateCameraRigsOffset(float newOffsetY)
        {
            for (int i = 0; i < freelook.m_Orbits.Length; i++)
            {
                var rig = freelook.GetRig(i);
                var composer = rig.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset.y = newOffsetY;
                }
            }
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

            UpdateCameraTarget();
        }

        private readonly List<string> pointNames = new List<string>
    {
        "pelvis", "spine2", "legs", "r_hand", "l_hand", "l_foot", "r_foot"
    };

        public void SetDefaultRigSettings()
        {
            if (freelook != null)
            {
                isTransitioning = true;
                transitionStartTime = Time.time;

                // Store the initial target position
                initialTargetPosition = freelook.Follow.position;

                // Start coroutine to smoothly adjust settings
                StartCoroutine(SmoothlyAdjustCameraSettings());
            }
        }

        private IEnumerator SmoothlyAdjustCameraSettings()
        {
            float initialFOV = freelook.m_Lens.FieldOfView;
            CinemachineFreeLook.Orbit initialOrbit = freelook.m_Orbits[1];
            float elapsedTime = 0;

            while (elapsedTime < transitionDuration)
            {
                elapsedTime = Time.time - transitionStartTime;
                float t = elapsedTime / transitionDuration;

                // Smoothly interpolate the field of view and orbit settings
                freelook.m_Lens.FieldOfView = Mathf.Lerp(initialFOV, defaultFOV, t);
                freelook.m_Orbits[1].m_Radius = Mathf.Lerp(initialOrbit.m_Radius, defaultMiddleRigRadius, t);
                freelook.m_Orbits[1].m_Height = Mathf.Lerp(initialOrbit.m_Height, defaultMiddleRigHeight, t);

                yield return null; // Wait for the next frame
            }

            // Ensure final values are set
            freelook.m_Lens.FieldOfView = defaultFOV;
            freelook.m_Orbits[1].m_Radius = defaultMiddleRigRadius;
            freelook.m_Orbits[1].m_Height = defaultMiddleRigHeight;
            isTransitioning = false;

            // Set the camera's position to the new target position
            freelook.Follow.position = initialTargetPosition;

            // Optionally update the camera target here if necessary
        }

        public void UpdateTargetPoints()
        {
            // Check if the application is in play mode
            if (Application.isPlaying)
            {
                targets.Clear();
                foreach (var pointName in pointNames)
                {
                    var targetObject = GameObject.Find(pointName);
                    if (targetObject != null)
                    {
                        targets.Add(targetObject.transform);
                    }
                    else
                    {
                        Debug.LogWarning("Target not found in the scene: " + pointName);
                    }
                }

                // Reset the current target index and update the camera target
                currentTargetIndex = 0;
                UpdateCameraTarget();
            }
        }

        public void UpdateCameraTarget()
        {
            // Check if the application is in play mode and if freelook is not null
            if (Application.isPlaying && freelook != null)
            {
                if (targets.Count > 0 && currentTargetIndex < targets.Count)
                {
                    freelook.Follow = targets[currentTargetIndex];
                    freelook.LookAt = targets[currentTargetIndex];
                }
            }
        }
    }
}