using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    
    private float startingWidth = 100f;
    
    void Start()
    {
        PlayerHealthManager.OnSetHealthAmt += SetHealthBar;
    }

    /**
     * Calculates and sets the health bar to display the correct level of health based on the int passed in
     */
    private void SetHealthBar(int health)
    {
        RectTransform healthBar = transform as RectTransform;
        float newWidth = (health / maxHealth) * startingWidth;
        healthBar.sizeDelta = new Vector2(newWidth, healthBar.sizeDelta.y);
    }

    void OnDestroy()
    {
        PlayerHealthManager.OnSetHealthAmt -= SetHealthBar;
    }
    
    
}
