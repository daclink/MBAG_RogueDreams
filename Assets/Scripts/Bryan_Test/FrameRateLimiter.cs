using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    public int FPS = 60;

    void Start()

    {

        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = FPS;

    }
}
