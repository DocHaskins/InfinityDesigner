using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine 
{
    [SaveDuringPlay]
    [RequireComponent(typeof(CinemachineFreeLook))]
public class CinemachineCameraZoomTool : MonoBehaviour
    {

        private CinemachineFreeLook freelook;
        public CinemachineFreeLook.Orbit[] originalOrbits = new CinemachineFreeLook.Orbit[0];

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
                if (Input.GetMouseButton(0)) // Left mouse button
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
        }
    }
}