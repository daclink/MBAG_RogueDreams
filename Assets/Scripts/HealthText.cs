using System;
using TMPro;
using UnityEngine;

public class HealthText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    public void Start()
    {
        //PlayerHealthManager.OnSetHealthText += UpdateHealth;
    }

    public void UpdateHealth(int health)
    {
        text.text = "Health: " + health;
    }

    public void OnDestroy()
    {
        //PlayerHealthManager.OnSetHealthText -= UpdateHealth;
    }
}
