using UnityEngine;
using UnityEngine.InputSystem;

namespace ChugHaul.View
{
    public class CameraRig : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] float panSpeed = 10f;
        [SerializeField] float dragSpeed = 0.01f;
        [Header("Zoom")]
        [SerializeField] float minDistance = 10f;
        [SerializeField] float maxDistance = 80f;
        [SerializeField] float startDistance = 40f;
        [SerializeField] float zoomStep = 0.1f; // 10% per scroll notch
        [Header("Camera")]
        [SerializeField] Transform cameraTransform; // Main Camera here
        [SerializeField, Range(30, 80)] float pitch = 50f; // tilt angle of the camera
        [Header("Smoothing")]
        [SerializeField] float moveSmoothTime = 0.15f;
        [SerializeField] float zoomSmoothTime = 0.12f;
        [Header("Bounds")]
        [SerializeField] float minX = -100;
        [SerializeField] float maxX = 100;
        [SerializeField] float minZ = -60;
        [SerializeField] float maxZ = 60;

        Vector3 targetPosition, velocity;
        float targetDistance, currentDistance, zoomVelocity;

        void Awake()
        {
            targetPosition = transform.position;
            targetDistance = currentDistance = startDistance;
        }

        void LateUpdate()
        {
            ReadInput();
            ApplySmoothing();
        }

        void ReadInput()
        {
            float zoomFactor = currentDistance / minDistance; // faster when zoomed out
            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector3 move = Vector3.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                targetPosition += move.normalized * (panSpeed * zoomFactor * Time.deltaTime);
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    targetPosition -= new Vector3(d.x, 0, d.y) * (dragSpeed * zoomFactor);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    targetDistance = Mathf.Clamp(targetDistance * (1f - Mathf.Sign(scroll) * zoomStep),
                                                 minDistance, maxDistance);
            }

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
        }

        void ApplySmoothing()
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, moveSmoothTime);
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
            var rot = Quaternion.Euler(pitch, 0, 0);
            cameraTransform.localRotation = rot;
            cameraTransform.localPosition = rot * new Vector3(0, 0, -currentDistance);
        }

        void OnValidate() { if (maxDistance < minDistance) maxDistance = minDistance; }
    }
}