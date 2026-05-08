using UnityEngine;

/// <summary>
/// Rotates this transform (typically the player <c>Body</c>) toward <see cref="Player.MoveDirection"/> in the XY plane.
/// </summary>
public class PlayerBodyFacing2D : MonoBehaviour
{
    [SerializeField] private Player player;
    [Tooltip("Added to Atan2(move.y, move.x) so your sprite's default forward matches movement.")]
    [SerializeField] private float angleOffsetDegrees = -90f;
    [SerializeField] private float rotateSpeedDegreesPerSecond = 720f;
    [Tooltip("Below this input magnitude, body keeps its last facing.")]
    [SerializeField] private float moveDeadZone = 0.08f;

    private float _currentZ;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
        _currentZ = transform.eulerAngles.z;
    }

    private void Update()
    {
        if (player == null) return;
        Vector2 md = player.MoveDirection;
        if (md.sqrMagnitude < moveDeadZone * moveDeadZone)
            return;

        float targetZ = Mathf.Atan2(md.y, md.x) * Mathf.Rad2Deg + angleOffsetDegrees;
        _currentZ = Mathf.MoveTowardsAngle(_currentZ, targetZ, rotateSpeedDegreesPerSecond * Time.deltaTime);
        var e = transform.eulerAngles;
        transform.eulerAngles = new Vector3(e.x, e.y, _currentZ);
    }
}
