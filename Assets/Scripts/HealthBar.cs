using UnityEngine;

public class HealthBar : MonoBehaviour
{

    [SerializeField] private float maxHealth;
    private float startingWidth = 100f;
    
    
    void Start()
    {
        PlayerHealthManager.OnSetHealthAmt += SetHealthBar;
    }

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
