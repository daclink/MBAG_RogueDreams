using TMPro;
using UnityEngine;

public class HealthText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    public void UpdateHealth(int health)
    {
        text.text = "Health: " + health;
    }
}
