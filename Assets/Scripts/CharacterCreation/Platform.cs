using UnityEngine;

namespace doppelganger
{
    public class Platform : MonoBehaviour
    {
        public float rotationSpeedMultiplier = 0.2f; // Adjust this value to control rotation speed
        private float rotationSpeed = 0f; // Current rotation speed based on mouse drag
        public float deceleration = 0.95f; // Deceleration rate
        private Vector3 previousMousePosition;

        void Update()
        {
            HandleMouseInput();
            ApplyRotation();
        }

        void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
            {
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(1)) // Right mouse button held down
            {
                Vector3 delta = Input.mousePosition - previousMousePosition;
                previousMousePosition = Input.mousePosition;

                // Update rotation speed based on horizontal mouse movement
                rotationSpeed += delta.x * rotationSpeedMultiplier;
            }
        }

        void ApplyRotation()
        {
            if (!Mathf.Approximately(rotationSpeed, 0f))
            {
                // Rotate the platform around its Y axis at the current rotation speed
                transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

                // Gradually decrease the rotation speed over time
                rotationSpeed *= deceleration;

                // Optional: Clamp the deceleration to stop completely at a very low speed to avoid endless rotation
                if (Mathf.Abs(rotationSpeed) < 0.01f)
                {
                    rotationSpeed = 0f;
                }
            }
        }

        public void ResetChildRotations()
        {
            foreach (Transform child in transform)
            {
                Quaternion currentRotation = child.localRotation;
                Vector3 currentEulerAngles = currentRotation.eulerAngles;
                currentEulerAngles.y = 0f;
                Quaternion newRotation = Quaternion.Euler(currentEulerAngles);
                child.localRotation = newRotation;
            }
        }
    }
}