using Cinemachine;
using RootMotion.FinalIK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace doppelganger
{
    public class CinemachineCameraInputController : MonoBehaviour
    {
        public CinemachineFollowZoom cameraZoomTool;
        private CinemachineVirtualCamera virtualCamera;
        private CinemachineRecomposer recomposer;


        private float initialY;
        public float orbitSpeed = 10f;
        public float verticalSpeed = 5f;
        public float tiltSpeed = 0.1f;
        public float minY = -5f;
        public float maxY = 5f;
        private Vector3 dragOrigin;

        public float scrollSpeed = 1.0f;
        public float minWidth = 0.5f;
        public float maxWidth = 2.5f;
        public float scrollAmount = 0.5f;

        void Start()
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
            recomposer = virtualCamera.GetComponent<CinemachineRecomposer>();
            if (virtualCamera == null)
            {
                Debug.LogError("CinemachineVirtualCamera component not found on the GameObject.");
            }
            initialY = virtualCamera.transform.position.y;

            if (recomposer == null)
            {
                Debug.LogError("CinemachineComposer component not found on the Virtual Camera.");
            }
        }

        void Update()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            // Check if scroll input is not zero (scrolling occurred)
            if (scrollInput != 0)
            {
                // Negate scroll input to reverse the direction
                scrollInput *= -1;

                // Calculate new width based on scroll direction and amount
                float newWidth = cameraZoomTool.m_Width + scrollInput * scrollSpeed * scrollAmount;

                // Clamp new width within the specified range
                newWidth = Mathf.Clamp(newWidth, minWidth, maxWidth);

                // Set the new width to the camera tool
                cameraZoomTool.m_Width = newWidth;
            }

            Transform lookAtTarget = virtualCamera.LookAt;
            if (lookAtTarget == null) return;

            if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(1)) // Right mouse button held down
            {
                Vector3 mouseDelta = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;

                // Calculate vertical movement based on mouse input, adjusting by verticalSpeed and Time.deltaTime
                float verticalMovement = -mouseDelta.y * verticalSpeed * Time.deltaTime; // Negative to invert direction

                // Calculate the new Y position, starting from the camera's initial Y position, and clamp it
                float newYPosition = Mathf.Clamp(virtualCamera.transform.position.y + verticalMovement, initialY + minY, initialY + maxY);

                // Update the camera's position with the new clamped Y position, while keeping X and Z the same
                virtualCamera.transform.position = new Vector3(virtualCamera.transform.position.x, newYPosition, virtualCamera.transform.position.z);
            }

            if (Input.GetMouseButtonDown(2)) // Middle mouse button pressed
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(2) && recomposer != null) // Middle mouse button held down
            {
                Vector3 mouseDelta = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;

                // Calculate the tilt adjustment based on the vertical mouse movement
                float tiltAdjustment = -mouseDelta.y * tiltSpeed;

                // Update the recomposer's tilt
                recomposer.m_Tilt += tiltAdjustment;

                // Optionally, clamp the tilt value to prevent over-tilting
                recomposer.m_Tilt = Mathf.Clamp(recomposer.m_Tilt, -20f, 20f); // Adjust these values as necessary
            }
        }
    }
}