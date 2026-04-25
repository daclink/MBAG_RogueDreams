using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Aims the head (and optionally the melee pivot) toward the cursor each frame.
/// Body facing is handled by PlayerBodyFacing2D on the torso.
/// </summary>
public class PlayerRotator : Rotator
{
    [Header("Player Rotator Objects")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform meleePivotTransform;

    /// <summary>Legacy prefab field name; YAML still assigns this when <see cref="meleePivotTransform"/> is empty.</summary>
    [SerializeField, HideInInspector] private GameObject objectToRotate;

    [Header("Debug (aim)")]
    [Tooltip("Logs cursor screen position, mouse world on player plane, aim angle, and head rotation each LateUpdate. Disable when done troubleshooting.")]
    [SerializeField] private bool logAimDiagnostics;

    private bool _hasLookScreen;
    private Vector2 _lookScreen;

    private void Awake()
    {
        if (meleePivotTransform == null && objectToRotate != null)
            meleePivotTransform = objectToRotate.transform;
    }

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void LateUpdate()
    {
        ApplyAimTowardMouse();
    }

    /// <summary>Input callback; mouse aim is also refreshed in <see cref="LateUpdate"/>.</summary>
    public void OnPlayerLook(InputAction.CallbackContext context)
    {
        _lookScreen = context.ReadValue<Vector2>();
        _hasLookScreen = true;
        ApplyAimTowardMouse();
    }

    private void ApplyAimTowardMouse()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null) return;

        Vector2 screen;
        if (_hasLookScreen)
            screen = _lookScreen;
        else if (Mouse.current != null)
            screen = Mouse.current.position.ReadValue();
        else
            screen = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector3 pivot = headTransform != null ? headTransform.position : transform.position;
        Vector3 mouseWorld = ScreenToWorldAtDepth(cam, screen, pivot);

        float lookZ = ComputeLookAngleZ(pivot, mouseWorld);
        if (headTransform != null)
            ApplyWorldLookZAsLocalRelativeToParent(headTransform, lookZ);
        if (meleePivotTransform != null)
            ApplyWorldLookZAsLocalRelativeToParent(meleePivotTransform, lookZ);

        if (logAimDiagnostics)
        {
            Debug.DrawLine(pivot, mouseWorld, Color.yellow);
            if (headTransform != null)
                Debug.DrawRay(headTransform.position, headTransform.right * 1.0f, Color.cyan);

            Vector2 headForward2D = headTransform != null
                ? new Vector2(headTransform.right.x, headTransform.right.y)
                : Vector2.zero;
            float headWorldZ = headTransform != null ? headTransform.eulerAngles.z : float.NaN;
            float headLocalZ = headTransform != null ? headTransform.localEulerAngles.z : float.NaN;
            Debug.Log(
                $"[PlayerRotator Aim] screen={screen} mouseWorld=({mouseWorld.x:F3},{mouseWorld.y:F3},{mouseWorld.z:F3}) " +
                $"pivot=({pivot.x:F3},{pivot.y:F3},{pivot.z:F3}) lookZ={lookZ:F1}° " +
                $"headWorldZ={headWorldZ:F1}° headLocalZ={headLocalZ:F1}° headRight2D=({headForward2D.x:F3},{headForward2D.y:F3}) " +
                $"cam={(cam != null ? cam.name : "null")} headAssigned={(headTransform != null)}");
        }
    }

    /// <summary>
    /// Converts screen pixels to a world point at the depth of <paramref name="pivotWorld"/> along the camera forward.
    /// This matches the original "works perfectly" melee approach (ScreenToWorldPoint) but with a correct depth value.
    /// </summary>
    private static Vector3 ScreenToWorldAtDepth(Camera camera, Vector2 screenPixels, Vector3 pivotWorld)
    {
        float dist = Vector3.Dot(pivotWorld - camera.transform.position, camera.transform.forward);
        if (Mathf.Abs(dist) < 0.0001f) dist = 10f;
        Vector3 p = camera.ScreenToWorldPoint(new Vector3(screenPixels.x, screenPixels.y, dist));
        p.z = pivotWorld.z;
        return p;
    }

    /// <summary>
    /// Aim in world Z; if under a rotating parent (torso), write <b>localRotation</b> so world aim matches the cursor.
    /// </summary>
    private static void ApplyWorldLookZAsLocalRelativeToParent(Transform t, float worldLookZDegrees)
    {
        Quaternion desiredWorld = Quaternion.AngleAxis(worldLookZDegrees, Vector3.forward);
        if (t.parent == null) { t.rotation = desiredWorld; return; }
        t.localRotation = Quaternion.Inverse(t.parent.rotation) * desiredWorld;
    }
}
