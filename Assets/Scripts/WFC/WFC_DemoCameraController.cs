using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple 2D pan/zoom camera for inspecting WFC demo. Uses new Input System (Keyboard/Mouse).
/// WASD or Arrows = pan. Scroll = zoom. Attach to Main Camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class WFC_DemoCameraController : MonoBehaviour
{
    [Header("Pan")]
    [SerializeField] private float panSpeed = 20f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minOrthoSize = 2f;
    [SerializeField] private float maxOrthoSize = 50f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam != null && !_cam.orthographic)
            _cam.orthographic = true;
    }

    private void Update()
    {
        if (_cam == null) return;

        // Pan
        Vector3 move = Vector3.zero;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) move.y += 1;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) move.y -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) move.x += 1;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) move.x -= 1;
        }
        if (move.sqrMagnitude > 0)
        {
            move = move.normalized * (panSpeed * Time.deltaTime);
            transform.Translate(move, Space.World);
        }

        // Zoom
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
}
