using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

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

        [Tooltip("Minimum Y position for the camera")]
        public float minY = -10f; // Minimum Y value

        [Tooltip("Maximum Y position for the camera")]
        public float maxY = 10f; // Maximum Y value

        private float initialY;
        private float accumulatedVerticalMovement = 0f;
        private bool isDragging = false;

        [Tooltip("The minimum scale for the orbits")]
        [Range(0.01f, 1f)]
        public float minScale = 0.5f;

        [Tooltip("The maximum scale for the orbits")]
        [Range(1f, 50f)]
        public float maxScale = 1.0f;

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
                initialCameraOffsetY = freelook.GetRig(1).GetCinemachineComponent<CinemachineComposer>().m_TrackedObjectOffset.y;
                accumulatedVerticalMovement = 0f; // Reset accumulated movement
                isDragging = true; // Set dragging flag to true
            }

            if (Input.GetMouseButtonUp(2)) // Middle mouse button released
            {
                isDragging = false; // Set dragging flag to false
            }

            if (isDragging) // If dragging
            {
                // Increment vertical movement by mouse Y axis
                accumulatedVerticalMovement += Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;
                float newOffsetY = Mathf.Clamp(initialCameraOffsetY + accumulatedVerticalMovement, minY, maxY);

                // Update the camera offset for each rig
                UpdateCameraRigsOffset(newOffsetY);
            }
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
        "neck", "spine3", "legs", "r_hand", "l_hand", "l_foot", "r_foot"
    };

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