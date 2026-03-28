using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sets up and controls a reusable angled top-down camera rig for the prototype.
/// The root object controls planar movement, yaw rotation, and bounded world-space
/// positioning, while the child camera holds the viewing angle and local distance
/// from the root.
/// </summary>
public class CameraRig : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera m_mainCamera;

    [Header("View Settings")]
    [SerializeField] private float m_pitchDegrees = 50.0f;

    [Header("Movement Settings")]
    [SerializeField] private float m_moveSpeed = 12.0f;
    [SerializeField] private bool m_enableMovement = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool m_enableRotation = true;
    [SerializeField] private float m_rotationSpeed = 90.0f;

    [Header("Zoom Settings")]
    [SerializeField] private bool m_enableZoom = true;
    [SerializeField] private float m_zoomDistance = 14.0f;
    [SerializeField] private float m_zoomSpeed = 10.0f;
    [SerializeField] private float m_minZoomDistance = 6.0f;
    [SerializeField] private float m_maxZoomDistance = 22.0f;
    [SerializeField] private float m_zoomSmoothness = 10.0f;

    [Header("Bounds Settings")]
    [SerializeField] private bool m_enableBounds = true;
    [SerializeField] private Vector2 m_minBounds = new Vector2(-20.0f, -20.0f);
    [SerializeField] private Vector2 m_maxBounds = new Vector2(20.0f, 20.0f);

    [Header("Startup")]
    [SerializeField] private bool m_applyOnStart = true;
    [SerializeField] private bool m_applyInEditor = true;

    private float m_targetZoomDistance;

    /// <summary>
    /// Applies a default rig setup when the component is first added or reset.
    /// </summary>
    private void Reset()
    {
        if (m_mainCamera == null)
        {
            m_mainCamera = GetComponentInChildren<Camera>();
        }

        ValidateSettings();
        m_targetZoomDistance = m_zoomDistance;
        ApplyRigSetup();
        ClampRootPositionToBounds();
    }

    /// <summary>
    /// Applies updated inspector values in the editor so the rig is easy to tune.
    /// </summary>
    private void OnValidate()
    {
        if (m_mainCamera == null)
        {
            m_mainCamera = GetComponentInChildren<Camera>();
        }

        ValidateSettings();
        m_targetZoomDistance = Mathf.Clamp(m_targetZoomDistance, m_minZoomDistance, m_maxZoomDistance);

        if (!m_applyInEditor)
        {
            return;
        }

        ApplyRigSetup();
        ClampRootPositionToBounds();
    }

    /// <summary>
    /// Ensures the camera starts in the intended prototype view at runtime.
    /// </summary>
    private void Start()
    {
        ValidateSettings();

        if (m_targetZoomDistance <= 0.0f)
        {
            m_targetZoomDistance = m_zoomDistance;
        }

        m_targetZoomDistance = Mathf.Clamp(m_targetZoomDistance, m_minZoomDistance, m_maxZoomDistance);
        m_zoomDistance = m_targetZoomDistance;

        if (!m_applyOnStart)
        {
            return;
        }

        ApplyRigSetup();
        ClampRootPositionToBounds();
    }

    /// <summary>
    /// Handles runtime camera controls for the prototype.
    /// </summary>
    private void Update()
    {
        if (m_enableRotation)
        {
            HandleRotation();
        }

        if (m_enableMovement)
        {
            HandleMovement();
        }

        if (m_enableZoom)
        {
            HandleZoomInput();
            UpdateZoom();
        }

        if (m_enableBounds)
        {
            ClampRootPositionToBounds();
        }
    }

    /// <summary>
    /// Applies the local transform and camera settings used by this rig.
    /// </summary>
    public void ApplyRigSetup()
    {
        if (m_mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = m_mainCamera.transform;

        if (cameraTransform.parent != transform)
        {
            cameraTransform.SetParent(transform);
        }

        m_mainCamera.orthographic = false;
        cameraTransform.localRotation = Quaternion.Euler(m_pitchDegrees, 0.0f, 0.0f);
        cameraTransform.localPosition = CalculateCameraLocalPosition(m_zoomDistance);
    }

    /// <summary>
    /// Rotates the camera root left or right around the yaw axis using Q and E.
    /// </summary>
    private void HandleRotation()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float rotationInput = 0.0f;

        if (keyboard.qKey.isPressed)
        {
            rotationInput -= 1.0f;
        }

        if (keyboard.eKey.isPressed)
        {
            rotationInput += 1.0f;
        }

        if (Mathf.Abs(rotationInput) <= Mathf.Epsilon)
        {
            return;
        }

        transform.Rotate(0.0f, rotationInput * m_rotationSpeed * Time.deltaTime, 0.0f, Space.World);
    }

    /// <summary>
    /// Moves the camera root relative to its current facing direction on the XZ plane.
    /// Supports WASD and arrow keys using Unity's Input System keyboard API.
    /// </summary>
    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float horizontalInput = 0.0f;
        float verticalInput = 0.0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1.0f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1.0f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            verticalInput -= 1.0f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            verticalInput += 1.0f;
        }

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0.0f;
        right.y = 0.0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * verticalInput) + (right * horizontalInput);

        if (moveDirection.sqrMagnitude > 1.0f)
        {
            moveDirection.Normalize();
        }

        transform.position += moveDirection * m_moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Reads mouse wheel input from Unity's Input System and updates the target zoom distance.
    /// Lower distance means closer zoom. Higher distance means farther zoom.
    /// </summary>
    private void HandleZoomInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        float scrollInput = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(scrollInput) <= Mathf.Epsilon)
        {
            return;
        }

        m_targetZoomDistance -= scrollInput * 0.01f * m_zoomSpeed;
        m_targetZoomDistance = Mathf.Clamp(m_targetZoomDistance, m_minZoomDistance, m_maxZoomDistance);
    }

    /// <summary>
    /// Smoothly moves the current zoom distance toward the target zoom distance.
    /// </summary>
    private void UpdateZoom()
    {
        m_zoomDistance = Mathf.Lerp(
            m_zoomDistance,
            m_targetZoomDistance,
            m_zoomSmoothness * Time.deltaTime);

        if (m_mainCamera == null)
        {
            return;
        }

        m_mainCamera.transform.localPosition = CalculateCameraLocalPosition(m_zoomDistance);
    }

    /// <summary>
    /// Clamps the camera root position to the configured world-space bounds.
    /// The X component maps to world X, and the Y/Z mapping uses Vector2 as (X, Z).
    /// </summary>
    private void ClampRootPositionToBounds()
    {
        Vector3 currentPosition = transform.position;

        currentPosition.x = Mathf.Clamp(currentPosition.x, m_minBounds.x, m_maxBounds.x);
        currentPosition.z = Mathf.Clamp(currentPosition.z, m_minBounds.y, m_maxBounds.y);

        transform.position = currentPosition;
    }

    /// <summary>
    /// Calculates the child camera local position from the current pitch and zoom distance.
    /// This keeps the camera moving along the angled view line instead of only sliding on Z.
    /// </summary>
    private Vector3 CalculateCameraLocalPosition(float zoomDistance)
    {
        float pitchRadians = m_pitchDegrees * Mathf.Deg2Rad;

        float localY = Mathf.Sin(pitchRadians) * zoomDistance;
        float localZ = -Mathf.Cos(pitchRadians) * zoomDistance;

        return new Vector3(0.0f, localY, localZ);
    }

    /// <summary>
    /// Clamps and normalizes camera settings so the rig remains valid and tunable.
    /// </summary>
    private void ValidateSettings()
    {
        m_pitchDegrees = Mathf.Clamp(m_pitchDegrees, 10.0f, 89.0f);

        m_moveSpeed = Mathf.Max(0.0f, m_moveSpeed);
        m_rotationSpeed = Mathf.Max(0.0f, m_rotationSpeed);

        m_minZoomDistance = Mathf.Max(0.1f, m_minZoomDistance);
        m_maxZoomDistance = Mathf.Max(m_minZoomDistance, m_maxZoomDistance);

        m_zoomDistance = Mathf.Clamp(m_zoomDistance, m_minZoomDistance, m_maxZoomDistance);
        m_zoomSpeed = Mathf.Max(0.0f, m_zoomSpeed);
        m_zoomSmoothness = Mathf.Max(0.0f, m_zoomSmoothness);

        if (m_minBounds.x > m_maxBounds.x)
        {
            float temp = m_minBounds.x;
            m_minBounds.x = m_maxBounds.x;
            m_maxBounds.x = temp;
        }

        if (m_minBounds.y > m_maxBounds.y)
        {
            float temp = m_minBounds.y;
            m_minBounds.y = m_maxBounds.y;
            m_maxBounds.y = temp;
        }
    }

    /// <summary>
    /// Draws the configured camera bounds in the editor for easier tuning.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!m_enableBounds)
        {
            return;
        }

        Vector3 center = new Vector3(
            (m_minBounds.x + m_maxBounds.x) * 0.5f,
            transform.position.y,
            (m_minBounds.y + m_maxBounds.y) * 0.5f);

        Vector3 size = new Vector3(
            m_maxBounds.x - m_minBounds.x,
            0.0f,
            m_maxBounds.y - m_minBounds.y);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }

    /// <summary>
    /// Returns the referenced child camera for future camera-control systems.
    /// </summary>
    public Camera MainCamera
    {
        get { return m_mainCamera; }
    }
}