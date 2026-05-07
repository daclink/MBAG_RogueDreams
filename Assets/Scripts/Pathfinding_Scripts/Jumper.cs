using UnityEngine;

public class Jumper : NPC
{
    [Tooltip("BFS steps ahead the jumper moves per jump.")]
    [Min(2)]
    [SerializeField] private int _jumpDistance = 3;
    [Tooltip("Seconds to pause after landing before jumping again.")]
    [Min(0f)]
    [SerializeField] private float _postJumpPause = 1f;

    private float _lastLandingTime = float.NegativeInfinity;

    public int JumpDistance => _jumpDistance;
    public void SetJumpDistance(int steps) => _jumpDistance = Mathf.Max(2, steps);
    public void SetPostJumpPause(float seconds) => _postJumpPause = Mathf.Max(0f, seconds);

    /// <summary>
    /// Returns the number of BFS steps to advance this tick.
    /// Returns 0 while the post-jump pause is still active.
    /// </summary>
    public int ConsumeMovementSteps()
    {
        if (Time.time < _lastLandingTime + _postJumpPause)
            return 0;
        return _jumpDistance;
    }

    public override void RecordLanding() => _lastLandingTime = Time.time;
}
