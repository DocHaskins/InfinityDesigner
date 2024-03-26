using Cinemachine;
using RootMotion.FinalIK;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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

            if (EventSystem.current.IsPointerOverGameObject())
            {
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerEventData, results);

                bool overPanel = results.Any(r => r.gameObject.CompareTag("NoZoomPanel"));

                if (overPanel)
                {
                    return;
                }
            }

            if (scrollInput != 0)
            {
                scrollInput *= -1;

                float newWidth = cameraZoomTool.m_Width + scrollInput * scrollSpeed * scrollAmount;

                newWidth = Mathf.Clamp(newWidth, minWidth, maxWidth);

                cameraZoomTool.m_Width = newWidth;
            }

            Transform lookAtTarget = virtualCamera.LookAt;
            if (lookAtTarget == null) return;

            if (Input.GetMouseButtonDown(1))
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(1))
            {
                Vector3 mouseDelta = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;

                float verticalMovement = -mouseDelta.y * verticalSpeed * Time.deltaTime;

                float newYPosition = Mathf.Clamp(virtualCamera.transform.position.y + verticalMovement, initialY + minY, initialY + maxY);
                virtualCamera.transform.position = new Vector3(virtualCamera.transform.position.x, newYPosition, virtualCamera.transform.position.z);
            }

            if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(2) && recomposer != null)
            {
                Vector3 mouseDelta = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;

                float tiltAdjustment = -mouseDelta.y * tiltSpeed;

                recomposer.m_Tilt += tiltAdjustment;

                recomposer.m_Tilt = Mathf.Clamp(recomposer.m_Tilt, -20f, 20f);
            }
        }
    }
}