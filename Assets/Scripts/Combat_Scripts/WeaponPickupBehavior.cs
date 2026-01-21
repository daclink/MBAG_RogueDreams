using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickupBehavior : MonoBehaviour
{
    [SerializeField] private WeaponPickup weaponSO;
    [SerializeField] private TextMeshProUGUI collectTextPopUp;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private PlayerInput playerInput;
    private InputAction collectAction;
    
    void Start()
    {
        if (spriteRenderer != null && weaponSO != null)
        {
            spriteRenderer.sprite = weaponSO.weaponPickupSprite;
        }

        if (collectTextPopUp != null)
        {
            collectTextPopUp.enabled = false;
        }
    }
    
    /**
    * OnTriggerEnter
    * Set the popup text to enabled = true
    */
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Entered pickup trigger");
        if (other.CompareTag("Player"))
        {
            //set playerinput and get the collect action from it
            playerInput = other.gameObject.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                collectAction = playerInput.actions["Collect"];
            }
            
            collectTextPopUp.enabled = true;
        }
    }

    /**
     * OnTriggerStay -- could also be Update() but that may use more pc power to constantly check
     * check if player presses the collect button
     *  if so then pass the weapon prefab to the player object and destroy this
     */
    void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("Staying in pickup trigger");
        if (other.CompareTag("Player"))
        {
            //check for player input, the playerInput input action that is on the player contains a 'OnCollect' that needs to be pressed here
            if (collectAction != null && collectAction.triggered)
            {
                CollectWeapon(other.GameObject());
            }
        }
    }

    /**
     * OnTriggerExit
     * set the popup collect text to enabled = false, and member variables to null
     */
    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Exited pickup trigger");
        playerInput = null;
        collectAction = null;
        
        if (other.CompareTag("Player"))
        {
            collectTextPopUp.enabled = false;
        }
    }

    /**
     * Handles the weapon collection, equipping it to the player, and destroying this object
     */
    private void CollectWeapon(GameObject player)
    {
        Debug.Log("Attempting to collect weapon");
        collectTextPopUp.enabled = false;
        player.GetComponent<WeaponManager>().EquipWeapon(weaponSO.weaponPrefab);
        //pass the weaponSO.weaponPrefab to a player weapon manager that handles weapons on the player
        
        Destroy(gameObject);
    }
    
    




}