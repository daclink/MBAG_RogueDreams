using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D demo camera: follows the Player (tag <c>Player</c> or optional <see cref="followTarget"/>).
/// Scroll = zoom. Attach to Main Camera. No keyboard pan.
/// </summary>
[RequireComponent(typeof(Camera))]
public class WFC_DemoCameraController : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("If set, this transform is followed instead of searching for tag Player.")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset;
    [Tooltip("0 = snap to target each frame; higher = smoother follow.")]
    [SerializeField] private float smoothTime = 0.12f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minOrthoSize = 2f;
    [SerializeField] private float maxOrthoSize = 50f;

    private Camera _cam;
    private Transform _resolvedFollow;
    private Vector3 _followVelocity;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam != null && !_cam.orthographic)
            _cam.orthographic = true;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;

        ResolveFollow();
        if (_resolvedFollow != null)
        {
            Vector3 target = _resolvedFollow.position + followOffset;
            target.z = transform.position.z;
            if (smoothTime <= 0f)
                transform.position = target;
            else
                transform.position = Vector3.SmoothDamp(transform.position, target, ref _followVelocity, smoothTime);
        }

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float delta = (scroll > 0 ? -1 : 1) * zoomSpeed * Time.deltaTime;
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + delta, minOrthoSize, maxOrthoSize);
            }
        }
    }

    private void ResolveFollow()
    {
        if (followTarget != null)
        {
            _resolvedFollow = followTarget;
            return;
        }

        if (_resolvedFollow != null && !_resolvedFollow.gameObject.activeInHierarchy)
            _resolvedFollow = null;

        if (_resolvedFollow == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                _resolvedFollow = go.transform;
        }
    }

    /// <summary>Clear cached follow so a respawned player is picked up (optional call from game code).</summary>
    public void ClearFollowCache() => _resolvedFollow = null;
}
